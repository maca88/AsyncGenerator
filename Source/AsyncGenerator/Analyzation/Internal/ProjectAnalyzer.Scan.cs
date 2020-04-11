using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Extensions;
using AsyncGenerator.Core.Extensions.Internal;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Analyzation.Internal
{
	internal partial class ProjectAnalyzer
	{
		private async Task ScanDocumentData(DocumentData documentData, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			foreach (var typeData in documentData.GetAllTypeDatas(o => o.Conversion != TypeConversion.Ignore))
			{
				// If the type have to be defined as a new type then we need to find all references to that type. 
				// We must not scan for nested types as they will not be renamed
				if (typeData.Conversion == TypeConversion.NewType)
				{
					await ScanForReferences(typeData, typeData.Symbol,
							(data, location, nameNode) => new TypeDataReference(data, location, nameNode, typeData.Symbol, typeData),
							cancellationToken)
						.ConfigureAwait(false);
				}
				FillBaseTypes(typeData, documentData.ProjectData);

				if (_configuration.CanScanForMissingAsyncMembers != null && _configuration.CanScanForMissingAsyncMembers(typeData.Symbol))
				{
					ScanForTypeMissingAsyncMethods(typeData);
				}
				// Scan also explicitly ignored methods in order to fix the conversion  if the user applies an invalid
				// conversion. (e.g. ignored a method that is used in a method that will be genereated)
				foreach (var methodOrAccessorData in typeData.MethodsAndAccessors
					.Where(o => o.Conversion.HasAnyFlag(MethodConversion.ToAsync, MethodConversion.Smart) ||
					            o.IgnoredReason == IgnoreReason.AsyncCounterpartExists || // TODO: Should be configurable?
								// TODO: Make configurable: Scan private methods in order to ignore them if they are not used
								((o.ExplicitlyIgnored /*|| o.IsPrivate*/) && o.TypeData.IsNewType)
					)
				)
				{
					await ScanMethodData(methodOrAccessorData, 0, cancellationToken).ConfigureAwait(false);
				}
				foreach (var fieldVariableData in typeData.Fields.Values.SelectMany(o => o.Variables)
					.Where(o => o.Conversion == FieldVariableConversion.Smart))
				{
					await ScanForReferences(fieldVariableData, fieldVariableData.Symbol,
							(data, location, nameNode) =>
								new FieldVariableDataReference(data, location, nameNode, fieldVariableData.Symbol, fieldVariableData),
							cancellationToken)
						.ConfigureAwait(false);
				}
			}
		}

		private readonly ConcurrentSet<IMethodSymbol> _searchedOverrides = new ConcurrentSet<IMethodSymbol>();

		private Task FindOverrides(IMethodSymbol methodSymbol, Action<IMethodSymbol, MethodOrAccessorData> action, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			methodSymbol = methodSymbol.OriginalDefinition;
			if (!_searchedOverrides.TryAdd(methodSymbol))
			{
				return Task.CompletedTask;
			}

			async Task Find()
			{
				var overrides = await SymbolFinder.FindOverridesAsync(methodSymbol, _solution, _analyzeProjects, cancellationToken)
					.ConfigureAwait(false);
				foreach (var overrideMethod in overrides.OfType<IMethodSymbol>())
				{
					var syntax = overrideMethod.DeclaringSyntaxReferences.Single();
					var overrideDocument = _solution.GetDocument(syntax.SyntaxTree);
					if (!CanProcessDocument(overrideDocument))
					{
						continue;
					}
					var overrideDocumentData = ProjectData.GetDocumentData(overrideDocument);
					var overrideMethodNode = await syntax.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
					var overrideMethodData = overrideDocumentData.GetMethodOrAccessorData(overrideMethodNode);

					action(overrideMethod, overrideMethodData);
				}
			}
			return Find();
		}

		private readonly ConcurrentSet<IMethodSymbol> _searchedImplementations = new ConcurrentSet<IMethodSymbol>();

		private Task FindImplementations(IMethodSymbol methodSymbol, Func<IMethodSymbol, MethodOrAccessorData, Task> action,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			methodSymbol = methodSymbol.OriginalDefinition;
			if (!_searchedImplementations.TryAdd(methodSymbol))
			{
				return Task.CompletedTask;
			}

			async Task Find()
			{
				IEnumerable<IMethodSymbol> implementations;
				// For properties FindImplementationsAsync will retrive implementations only for property symbol.
				// This may be a bug as FindOverridesAsync works also for accessors
				if (methodSymbol.MethodKind == MethodKind.PropertyGet || methodSymbol.MethodKind == MethodKind.PropertySet)
				{
					implementations = (await SymbolFinder
							.FindImplementationsAsync(methodSymbol.AssociatedSymbol /* Property symbol */, _solution, _analyzeProjects, cancellationToken)
							.ConfigureAwait(false))
						.OfType<IPropertySymbol>()
						.Select(o => methodSymbol.MethodKind == MethodKind.PropertyGet ? o.GetMethod : o.SetMethod)
						.Where(o => o != null);
				}
				else
				{
					implementations = (await SymbolFinder.FindImplementationsAsync(methodSymbol, _solution, _analyzeProjects, cancellationToken)
							.ConfigureAwait(false))
						.OfType<IMethodSymbol>();
				}
				foreach (var implMethod in implementations)
				{
					var syntax = implMethod.DeclaringSyntaxReferences.Single();
					var document = _solution.GetDocument(syntax.SyntaxTree);
					if (!CanProcessDocument(document))
					{
						continue;
					}
					var documentData = ProjectData.GetDocumentData(syntax);
					var methodNode = await syntax.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
					var implMethodData = documentData.GetMethodOrAccessorData(methodNode);

					await action(implMethod, implMethodData).ConfigureAwait(false);
				}
			}
			return Find();
		}

		private readonly ConcurrentSet<MethodOrAccessorData> _scannedMethodOrAccessors = new ConcurrentSet<MethodOrAccessorData>();

		private async Task ScanMethodData(MethodOrAccessorData methodOrAccessorData, int depth = 0, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			if (!_scannedMethodOrAccessors.TryAdd(methodOrAccessorData))
			{
				return;
			}

			SyntaxReference syntax;
			var bodyScanMethodDatas = new HashSet<MethodOrAccessorData>();
			var referenceScanMethods = new HashSet<IMethodSymbol>();

			if (_configuration.ScanMethodBody || methodOrAccessorData.Conversion.HasAnyFlag(MethodConversion.Smart, MethodConversion.ToAsync))
			{
				bodyScanMethodDatas.Add(methodOrAccessorData);
			}

			var interfaceMethods = methodOrAccessorData.ImplementedInterfaces.ToImmutableHashSet();
			if (methodOrAccessorData.InterfaceMethod)
			{
				interfaceMethods = interfaceMethods.Add(methodOrAccessorData.Symbol);
			}
			// Get and save all interface implementations
			foreach (var interfaceMethod in interfaceMethods)
			{
				referenceScanMethods.Add(interfaceMethod);

				syntax = interfaceMethod.DeclaringSyntaxReferences.Single();
				var document = _solution.GetDocument(syntax.SyntaxTree);
				if (!CanProcessDocument(document))
				{
					continue;
				}
				var documentData = ProjectData.GetDocumentData(document);
				var methodNode = await syntax.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
				var interfaceMethodData = documentData.GetMethodOrAccessorData(methodNode);

				// NOTE: FindImplementationsAsync will not find all implementations when we have an abstract/virtual implementation of the interface.
				// In this case we will get only the abstract/virtual method so we have to find all overrides for it manually
				await FindImplementations(interfaceMethod, async (implMethod, implMethodData) =>
				{
					interfaceMethodData.AddRelatedMethod(implMethodData);
					if (_configuration.ScanMethodBody || implMethodData.Conversion.HasAnyFlag(MethodConversion.Smart, MethodConversion.ToAsync))
					{
						bodyScanMethodDatas.Add(implMethodData);
					}
					if (!implMethod.IsAbstract && !implMethod.IsVirtual)
					{
						return;
					}
					// Find all overrides
					await FindOverrides(implMethod, (overrideSymbol, overrideMethodData) =>
					{
						interfaceMethodData.AddRelatedMethod(overrideMethodData);
						if (_configuration.ScanMethodBody || overrideMethodData.Conversion.HasAnyFlag(MethodConversion.Smart, MethodConversion.ToAsync))
						{
							bodyScanMethodDatas.Add(overrideMethodData);
						}
					}, cancellationToken).ConfigureAwait(false);
				}, cancellationToken).ConfigureAwait(false);
			}

			MethodOrAccessorData baseMethodData = null;
			IMethodSymbol baseMethodSymbol = null;
			if (methodOrAccessorData.BaseOverriddenMethod?.DeclaringSyntaxReferences.Any() == true)
			{
				baseMethodSymbol = methodOrAccessorData.BaseOverriddenMethod;
			}
			else if (!methodOrAccessorData.InterfaceMethod && (methodOrAccessorData.Symbol.IsVirtual || methodOrAccessorData.Symbol.IsAbstract)) // interface method has IsAbstract true
			{
				baseMethodSymbol = methodOrAccessorData.Symbol;
				baseMethodData = methodOrAccessorData;
			}

			// Get and save all derived methods
			if (baseMethodSymbol != null)
			{
				referenceScanMethods.Add(baseMethodSymbol);

				if (baseMethodData == null)
				{
					syntax = baseMethodSymbol.DeclaringSyntaxReferences.Single();
					var document = _solution.GetDocument(syntax.SyntaxTree);
					if (CanProcessDocument(document))
					{
						var methodNode = await syntax.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
						baseMethodData = ProjectData.GetDocumentData(document).GetMethodOrAccessorData(methodNode);
					}
				}

				if (baseMethodData != null && (_configuration.ScanMethodBody || baseMethodData.Conversion.HasAnyFlag(MethodConversion.Smart, MethodConversion.ToAsync)))
				{
					bodyScanMethodDatas.Add(baseMethodData);
				}

				// Check if the overrides will be found in the FindImplementations callback, if so we must skip this call otherwise a race condition may happen
				if (baseMethodData?.ImplementedInterfaces.All(o => !interfaceMethods.Contains(o)) == true)
				{
					await FindOverrides(baseMethodSymbol, (overrideMethod, overrideMethodData) =>
					{
						if (baseMethodData != null)
						{
							baseMethodData.AddRelatedMethod(overrideMethodData);
						}
						else
						{
							overrideMethodData.ExternalRelatedMethods.TryAdd(baseMethodSymbol);
						}
						if (!overrideMethod.IsAbstract && (_configuration.ScanMethodBody || overrideMethodData.Conversion.HasAnyFlag(MethodConversion.Smart, MethodConversion.ToAsync)))
						{
							bodyScanMethodDatas.Add(overrideMethodData);
						}
					}, cancellationToken).ConfigureAwait(false);
				}
			}

			if (baseMethodSymbol == null && !interfaceMethods.Any()) //TODO: what about hiding methods
			{
				referenceScanMethods.Add(methodOrAccessorData.Symbol);
			}

			foreach (var mData in bodyScanMethodDatas)
			{
				foreach (var method in FindNewlyInvokedMethodsWithAsyncCounterpart(mData, referenceScanMethods))
				{
					await ScanAllMethodReferenceLocations(method, depth, cancellationToken).ConfigureAwait(false);
				}
			}
			foreach (var methodToScan in referenceScanMethods)
			{
				await ScanAllMethodReferenceLocations(methodToScan, depth, cancellationToken).ConfigureAwait(false);
			}
		}

		private void FillBaseTypes(TypeData typeData, ProjectData projectData)
		{
			var currType = typeData.Symbol.BaseType;
			while (currType != null)
			{
				foreach (var baseTypeData in projectData.GetAllTypeData(currType))
				{
					typeData.BaseTypes.TryAdd(baseTypeData);
				}
				currType = currType.BaseType;
			}
		}

		private async Task ScanForReferences<TReferenceSymbol, TReferenceData>(TReferenceData data, TReferenceSymbol symbol, 
			Func<AbstractData, ReferenceLocation, SimpleNameSyntax, IDataReference> createRefFunc, CancellationToken cancellationToken = default(CancellationToken))
			where TReferenceData : AbstractData
			where TReferenceSymbol : ISymbol
		{
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			// References for ctor of the type and the type itself wont have any locations
			var references = await SymbolFinder.FindReferencesAsync(symbol, _solution, _analyzeDocuments, cancellationToken).ConfigureAwait(false);
			foreach (var refLocation in references.SelectMany(o => o.Locations))
			{
				var documentData = ProjectData.GetDocumentData(refLocation.Document);
				// We need to find the type where the reference location is
				var node = documentData.Node.GetSimpleName(refLocation.Location.SourceSpan, true);
				var referenceData = documentData.GetNearestNodeData(node.Parent, node.IsInsideCref());
				if (referenceData == null)
				{
					continue; // TODO: add unsupported nodes
				}
				var reference = createRefFunc(referenceData, refLocation, node);

				referenceData.References.TryAdd(reference);
				if (!data.SelfReferences.TryAdd(reference))
				{
					_logger.Debug($"Performance hit: Self reference for type {symbol} already exists");
				}
			}
		}

		private void ScanForTypeMissingAsyncMethods(TypeData typeData)
		{
			var documentData = typeData.NamespaceData.DocumentData;
			var syncMethods = typeData.Node.Members
				.OfType<MethodDeclarationSyntax>()
				.Where(o => !o.Identifier.ValueText.EndsWith("Async"))
				.Select(o => new
				{
					Node = (SyntaxNode) o,
					Symbol = documentData.SemanticModel.GetDeclaredSymbol(o)
				})
				// Expression properties
				.Concat(
					typeData.Node.Members
						.OfType<PropertyDeclarationSyntax>()
						.Where(o => o.ExpressionBody != null)
						.Select(o => new
						{
							Node = (SyntaxNode) o.ExpressionBody,
							Symbol = documentData.SemanticModel.GetDeclaredSymbol(o).GetMethod
						})
				)
				// Non expression properties
				.Concat(
					typeData.Node.Members
						.OfType<PropertyDeclarationSyntax>()
						.Where(o => o.ExpressionBody == null)
						.SelectMany(o => o.AccessorList.Accessors)
						.Select(o => new
						{
							Node = (SyntaxNode) o,
							Symbol = documentData.SemanticModel.GetDeclaredSymbol(o)
						})
				)
				.ToLookup(o => o.Symbol.GetAsyncName());

			var asyncMethods = typeData.Node.Members
				.OfType<MethodDeclarationSyntax>()
				.Where(o => o.Identifier.ValueText.EndsWith("Async"))
				.Select(o => new
				{
					Node = (SyntaxNode) o,
					Symbol = documentData.SemanticModel.GetDeclaredSymbol(o)
				})
				.ToLookup(o => o.Symbol.Name);

			foreach (var asyncMember in typeData.Symbol.AllInterfaces
												  .SelectMany(o => o.GetMembers().OfType<IMethodSymbol>()
												  .Where(m => m.Name.EndsWith("Async"))))
			{
				// Skip if there is already an implementation defined
				var impl = typeData.Symbol.FindImplementationForInterfaceMember(asyncMember);
				if (impl != null)
				{
					continue;
				}
				if (!syncMethods.Contains(asyncMember.Name))
				{
					// Try to find if there is a property with that name
					documentData.AddDiagnostic($"Sync counterpart of async member {asyncMember} not found", DiagnosticSeverity.Hidden);
					continue;
				}
				var nonAsyncMember = syncMethods[asyncMember.Name].First(o => o.Symbol.IsAsyncCounterpart(null, asyncMember, true, true, false)); // TODO: what to do if there are more than one?
				var methodData = documentData.GetMethodOrAccessorData(nonAsyncMember.Node);
				if (methodData.Conversion == MethodConversion.Ignore)
				{
					methodData.AddDiagnostic("Overriding conversion from Ignore to ToAsync in order to avoid having an invalid code generated.", DiagnosticSeverity.Hidden);
				}
				methodData.ToAsync();
				methodData.Missing = true;
				// We have to generate the cancellation token parameter if the async member has more parameters that the sync counterpart
				if (asyncMember.Parameters.Length > nonAsyncMember.Symbol.Parameters.Length)
				{
					methodData.CancellationTokenRequired = true;
					// We suppose that the cancellation token is the last parameter
					methodData.MethodCancellationToken = asyncMember.Parameters.Last().HasExplicitDefaultValue
						? MethodCancellationToken.Optional
						: MethodCancellationToken.Required;
				}
			}

			// Find all abstract and virtual non implemented async methods in all descend base types.
			var baseType = typeData.Symbol.BaseType;
			while (baseType != null)
			{
				foreach (var asyncMember in baseType.GetMembers()
					.OfType<IMethodSymbol>()
					.Where(o => (o.IsAbstract || o.IsVirtual) && o.Name.EndsWith("Async")))
				{
					if (!syncMethods.Contains(asyncMember.Name))
					{
						documentData.AddDiagnostic($"Abstract sync counterpart of async member {asyncMember} not found", DiagnosticSeverity.Hidden);
						continue;
					}
					var nonAsyncMember = syncMethods[asyncMember.Name].FirstOrDefault(o => o.Symbol.IsAsyncCounterpart(null, asyncMember, true, true, false));
					if (nonAsyncMember == null)
					{
						documentData.AddDiagnostic($"Abstract sync counterpart of async member {asyncMember} not found", DiagnosticSeverity.Hidden);
						continue;
					}
					// Skip if the type already implements the method
					if (asyncMethods[asyncMember.Name].Any(o => o.Symbol.MatchesDefinition(asyncMember)))
					{
						continue;
					}
					var methodData = documentData.GetMethodOrAccessorData(nonAsyncMember.Node);
					if (methodData.Conversion == MethodConversion.Ignore)
					{
						// An ignored virtual method can be ignored as it will not produce a compilation error
						if (!asyncMember.IsAbstract)
						{
							continue;
						}
						methodData.AddDiagnostic("Overriding conversion from Ignore to ToAsync in order to avoid having an invalid code generated.", DiagnosticSeverity.Hidden);
					}
					methodData.ToAsync();
					methodData.Missing = true;
					// We have to generate the cancellation token parameter if the async member has more parameters that the sync counterpart
					if (asyncMember.Parameters.Length > nonAsyncMember.Symbol.Parameters.Length)
					{
						methodData.CancellationTokenRequired = true;
						// We suppose that the cancellation token is the last parameter
						methodData.MethodCancellationToken = asyncMember.Parameters.Last().HasExplicitDefaultValue
							? MethodCancellationToken.Optional
							: MethodCancellationToken.Required;
					}
				}
				baseType = baseType.BaseType;
			}
		}

		#region ScanAllMethodReferenceLocations

		private readonly ConcurrentSet<IMethodSymbol> _searchedMethodReferences = new ConcurrentSet<IMethodSymbol>();

		private readonly ConcurrentSet<ReferenceLocation> _scannedLocationsSymbols = new ConcurrentSet<ReferenceLocation>();

		private int _maxScanningDepth;

		private IEnumerable<IMethodSymbol> GetAllRelatedMethods(IMethodSymbol methodSymbol)
		{
			var methodData = ProjectData.GetMethodOrAccessorData(methodSymbol);
			if (methodData != null)
			{
				return methodData.AllRelatedMethods;
			}

			var relatedSymbols = new List<IMethodSymbol>();
			relatedSymbols.AddRange(methodSymbol.ExplicitInterfaceImplementations);

			var overrideMethod = methodSymbol.OverriddenMethod;
			while (overrideMethod != null)
			{
				relatedSymbols.Add(overrideMethod);
				overrideMethod = overrideMethod.OverriddenMethod;
			}
			var type = methodSymbol.ContainingType;
			foreach (var interfaceMethod in type.AllInterfaces
				.SelectMany(o => o.GetMembers(methodSymbol.Name)
					.Where(m =>
					{
						// Find out if the method implements the interface member or an override 
						// method that implements it
						var impl = type.FindImplementationForInterfaceMember(m);
						return methodSymbol.EqualTo(impl) || relatedSymbols.Any(ov => ov.EqualTo(impl));
					}))
				.OfType<IMethodSymbol>())
			{
				relatedSymbols.Add(interfaceMethod);
			}
			return relatedSymbols;
		}

		private async Task ScanAllMethodReferenceLocations(IMethodSymbol methodSymbol, int depth, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			methodSymbol = methodSymbol.OriginalDefinition;
			if ((!_configuration.CanSearchForMethodReferences(methodSymbol) && !_mustScanForMethodReferences.Contains(methodSymbol)) || 
				!_searchedMethodReferences.TryAdd(methodSymbol))
			{
				return;
			}
			// FindReferencesAsync will not search just for the passed method symbol but also for all its overrides, interfaces and external interfaces
			// so we need to add all related symbols to the searched list in order to avoid scanning those related members in the future calls
			// If any of the related symbols was already searched then we know that all references of the current method were already found
			var alreadyScanned = false;
			foreach (var relatedMethod in GetAllRelatedMethods(methodSymbol))
			{
				if (!_searchedMethodReferences.TryAdd(relatedMethod))
				{
					alreadyScanned = true;
				}
			}
			if (alreadyScanned)
			{
				return;
			}

			var references = await SymbolFinder.FindReferencesAsync(methodSymbol, _solution, _analyzeDocuments, cancellationToken).ConfigureAwait(false);

			depth++;
			if (depth > _maxScanningDepth)
			{
				_maxScanningDepth = depth;
			}
			foreach (var refLocation in references.SelectMany(o => o.Locations))
			{
				if (_scannedLocationsSymbols.Contains(refLocation))
				{
					continue;
				}
				_scannedLocationsSymbols.TryAdd(refLocation);

				if (refLocation.Document.Project != ProjectData.Project)
				{
					throw new InvalidOperationException($"Reference {refLocation} is located in a document from another project");
				}

				var documentData = ProjectData.GetDocumentData(refLocation.Document);
				if (documentData == null)
				{
					continue;
				}
				var symbol = documentData.GetEnclosingSymbol(refLocation);
				if (symbol == null)
				{
					documentData.AddDiagnostic($"Symbol not found for reference ${refLocation}", DiagnosticSeverity.Hidden);
					continue;
				}

				if (symbol.Kind != SymbolKind.Method)
				{
					TryLinkToRealReference(symbol, documentData, refLocation);
					continue;
				}

				var baseMethodData = documentData.GetFunctionData(symbol);
				if (baseMethodData == null) // TODO: Current is null for lambda in fields
				{
					var refMethodSymbol = (IMethodSymbol) symbol;
					if (refMethodSymbol.MethodKind == MethodKind.AnonymousFunction || refMethodSymbol.MethodKind == MethodKind.LambdaMethod)
					{
						documentData.AddDiagnostic(
							$"Function inside member {refMethodSymbol.ContainingSymbol} cannot be async because of its kind {refMethodSymbol.MethodKind}",
							DiagnosticSeverity.Hidden);
					}
					else
					{
						documentData.AddDiagnostic(
							$"Method {refMethodSymbol} cannot be async because of its kind {refMethodSymbol.MethodKind}",
							DiagnosticSeverity.Hidden);
					}
					continue;
				}

				// Find the real method on that reference as FindReferencesAsync will also find references to base and interface methods
				// Save the reference as it can be made async
				var nameNode = baseMethodData.GetNode().GetSimpleName(refLocation.Location.SourceSpan);
				if (nameNode == null)
				{
					continue; // Can happen for a foreach token
				}
				var referenceSymbolInfo = documentData.SemanticModel.GetSymbolInfo(nameNode);
				var referenceSymbol = referenceSymbolInfo.Symbol;
				var methodReferenceSymbol = referenceSymbol as IMethodSymbol;
				if (methodReferenceSymbol == null && referenceSymbol is IPropertySymbol propertyReferenceSymbol)
				{
					// We need to find the usage of the property, if getter or setter is used
					methodReferenceSymbol = nameNode.IsAssigned()
						? propertyReferenceSymbol.SetMethod 
						: propertyReferenceSymbol.GetMethod;
				}
				if (methodReferenceSymbol == null)
				{
					// Check if the node is inside a nameof keyword as GetSymbolInfo will never return a symbol for it only candidates
					if (nameNode.IsInsideNameOf())
					{
						var referencedFuncs = new Dictionary<IMethodSymbol, FunctionData>();
						foreach (var candidateSymbol in referenceSymbolInfo.CandidateSymbols.OfType<IMethodSymbol>())
						{
							var nameofReferenceData = ProjectData.GetFunctionData(candidateSymbol);
							referencedFuncs.Add(candidateSymbol, nameofReferenceData);
						}
						var nameofReference = new NameofFunctionDataReference(baseMethodData, refLocation, nameNode, referencedFuncs, true);
						if (!baseMethodData.References.TryAdd(nameofReference))
						{
							_logger.Debug($"Performance hit: MembersReferences {nameNode} already added");
						}
						foreach (var referencedFun in referencedFuncs.Values.Where(o => o != null))
						{
							referencedFun.SelfReferences.TryAdd(nameofReference);
						}
						continue;
					}

					methodReferenceSymbol = TryFindCandidate(nameNode, referenceSymbolInfo, documentData.SemanticModel);
					if (methodReferenceSymbol == null)
					{
						baseMethodData.AddDiagnostic($"Unable to find symbol for node {nameNode} ({nameNode.GetLinePosition().Format()})",
							DiagnosticSeverity.Info);
						continue;
					}
					baseMethodData.AddDiagnostic(
						$"GetSymbolInfo did not successfully resolved symbol for node {nameNode} ({nameNode.GetLinePosition().Format()})" +
						$"but we got a candidate instead. CandidateReason: {referenceSymbolInfo.CandidateReason}",
						DiagnosticSeverity.Info);
				}
				var referenceFunctionData = ProjectData.GetFunctionData(methodReferenceSymbol);
				// Check if the reference is a cref reference or a nameof
				if (nameNode.IsInsideCref())
				{
					var crefReference = new CrefFunctionDataReference(baseMethodData, refLocation, nameNode, methodReferenceSymbol, referenceFunctionData, true);
					if (!baseMethodData.References.TryAdd(crefReference))
					{
						_logger.Debug($"Performance hit: MembersReferences {nameNode} already added");
					}
					referenceFunctionData?.SelfReferences.TryAdd(crefReference);
					continue; // No need to further scan a cref reference
				}
				var methodReferenceData = new BodyFunctionDataReference(baseMethodData, refLocation, nameNode, methodReferenceSymbol, referenceFunctionData);
				if (!baseMethodData.References.TryAdd(methodReferenceData))
				{
					_logger.Debug($"Performance hit: method reference {methodReferenceSymbol} already processed");
					continue; // Reference already processed
				}
				referenceFunctionData?.SelfReferences.TryAdd(methodReferenceData);

				if (baseMethodData.Conversion == MethodConversion.Ignore)
				{
					continue;
				}
				// Do not scan for method that will be only copied (e.g. the containing type is a new type). 
				if (baseMethodData.Conversion == MethodConversion.Copy)
				{
					continue;
				}

				if (baseMethodData is MethodOrAccessorData methodData && !_scannedMethodOrAccessors.Contains(methodData))
				{
					await ScanMethodData(methodData, depth, cancellationToken).ConfigureAwait(false);
				}
				// Scan a local/anonymous function only if there is a chance that can be called elsewere. (e.g. saved to a variable or local function)
				// TODO: support local variables
				if (baseMethodData is LocalFunctionData)
				{
					await ScanAllMethodReferenceLocations(baseMethodData.Symbol, depth, cancellationToken).ConfigureAwait(false);
				}
			}
		}

		private void TryLinkToRealReference(ISymbol typeSymbol, DocumentData documentData, ReferenceLocation refLocation)
		{
			if (typeSymbol.Kind != SymbolKind.NamedType)
			{
				return;
			}
			// A cref/nameof can be on a method or type trivia but we get always the type symbol
			var typeData = documentData.GetAllTypeDatas(o => o.Symbol.EqualTo(typeSymbol)).FirstOrDefault();
			if (typeData == null)
			{
				return;
			}
			// Try to find the real node where the cref/nameof is located
			var referenceNameNode = typeData.Node.GetSimpleName(refLocation.Location.SourceSpan, true);
			var referenceSymbolInfo = documentData.SemanticModel.GetSymbolInfo(referenceNameNode);
			var data = documentData.GetNearestNodeData(referenceNameNode.Parent, referenceNameNode.IsInsideCref());

			if (referenceSymbolInfo.Symbol is IMethodSymbol methodSymbol)
			{
				if (!referenceNameNode.IsInsideCref())
				{
					return;
				}
				var referenceData = ProjectData.GetFunctionData(methodSymbol);
				var reference = new CrefFunctionDataReference(data, refLocation, referenceNameNode, methodSymbol, referenceData, false);
				data.References.TryAdd(reference);
				referenceData?.SelfReferences.TryAdd(reference);
			}
			else if(referenceNameNode.IsInsideNameOf()) // GetSymbolInfo will never return a concrete symbol for nameof only candidates
			{
				var referencedFuncs = new Dictionary<IMethodSymbol, FunctionData>();
				foreach (var candidateSymbol in referenceSymbolInfo.CandidateSymbols.OfType<IMethodSymbol>())
				{
					var nameofReferenceData = ProjectData.GetFunctionData(candidateSymbol);
					referencedFuncs.Add(candidateSymbol, nameofReferenceData);
				}
				var nameofReference = new NameofFunctionDataReference(data, refLocation, referenceNameNode, referencedFuncs, false);
				data.References.TryAdd(nameofReference);
				foreach (var referencedFun in referencedFuncs.Values.Where(o => o != null))
				{
					referencedFun.SelfReferences.TryAdd(nameofReference);
				}
			}
		}

		private IMethodSymbol TryFindCandidate(SyntaxNode nameNode, SymbolInfo symbolInfo, SemanticModel semanticModel, bool ascend = true)
		{
			if (!symbolInfo.CandidateSymbols.Any())
			{
				return null;
			}
			if (symbolInfo.CandidateSymbols.Length == 1)
			{
				return (IMethodSymbol)symbolInfo.CandidateSymbols.First();
			}

			// Try to figure out which is the correct one by finding the symbol of the parent node
			// eg. new Ctor(GetList, GetListAsync) -> if we get multiple candidates for GetList, try to find the symbol for the Ctor.ctor.
			// If found (only one Ctor.ctor symbol) we can figure out which is the correct GetList candidate
			//var ascend = true;
			var currNode = ascend ? nameNode.Parent : nameNode;
			int? argumentIndex = null;
			while (ascend)
			{
				ascend = false;
				switch (currNode.Kind())
				{
					case SyntaxKind.SimpleMemberAccessExpression: // We get the same symbol as for the name node
						ascend = true;
						break;
					case SyntaxKind.Argument:
						ascend = true;
						argumentIndex = ((ArgumentListSyntax)currNode.Parent).Arguments.IndexOf((ArgumentSyntax)currNode);
						currNode = currNode.Parent; // Skip the ArgumentList node
						break;
				}
				if (ascend)
				{
					currNode = currNode.Parent;
				}
			}

			// Dynamic calls e.g. Database.Save("test", (dynamic)"test")
			if (symbolInfo.CandidateReason == CandidateReason.LateBound &&
			    currNode is InvocationExpressionSyntax invocationExpression)
			{
				var methodCandidates = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().ToList();
				var arguments = invocationExpression.ArgumentList.Arguments;
				for (var i = 0; i < arguments.Count; i++)
				{
					if (methodCandidates.Count == 0)
					{
						break;
					}
					var argument = arguments[i];
					ITypeSymbol type = null;
					var argumentType = semanticModel.GetTypeInfo(argument.Expression);
					if (argumentType.ConvertedType.TypeKind == TypeKind.Dynamic)
					{
						// It can be a dynamic value or an expression that contains it.
						var argSymbolInfo = semanticModel.GetSymbolInfo(argument.Expression);
						var symbol = argSymbolInfo.Symbol ?? TryFindCandidate(argument.Expression, argSymbolInfo, semanticModel, false);
						if (symbol == null)
						{
							continue;
						}
						switch (symbol.Kind)
						{
							case SymbolKind.Method:
								var argMethod = (IMethodSymbol) symbol;
								if (argument.Expression.IsKind(SyntaxKind.InvocationExpression))
								{
									type = argMethod.ReturnType;
								}
								break;
							default:
								continue;
						}
					}
					else
					{
						type = argumentType.ConvertedType;
					}
					if (type == null)
					{
						continue;
					}

					var toRemoveMethods = new List<IMethodSymbol>();
					foreach (var methodCandidate in methodCandidates)
					{
						var candidateType = methodCandidate.Parameters[i].Type;
						bool areEqual;
						switch (candidateType.TypeKind)
						{
							case TypeKind.TypeParameter:
								var typeParameter = (ITypeParameterSymbol) candidateType;
								areEqual = typeParameter.CanApply(type);
								break;
							default:
								areEqual = candidateType.EqualTo(type);
								break;
						}
						if (!areEqual)
						{
							toRemoveMethods.Add(methodCandidate);
						}
					}
					foreach (var toRemoveMethod in toRemoveMethods)
					{
						methodCandidates.Remove(toRemoveMethod);
					}

					if (methodCandidates.Count == 1)
					{
						return methodCandidates[0];
					}
				}
			}


			if (!argumentIndex.HasValue)
			{
				return null;
			}
			var parentSymbolInfo = semanticModel.GetSymbolInfo(currNode);
			IParameterSymbol parameterSymbol = null;
			if (parentSymbolInfo.CandidateSymbols.Length == 0)
			{
				// In certain cases we can get no candidate symbols for the parent node (e.g. generic types), when this happen try to get the parameter manually with all the available information
				if (currNode is ObjectCreationExpressionSyntax objCreationNode)
				{
					var typeSymbol = (INamedTypeSymbol) semanticModel.GetSymbolInfo(objCreationNode.Type).Symbol;
					var ctors = typeSymbol?.Constructors
						.Where(o => o.Parameters.Length == objCreationNode.ArgumentList.Arguments.Count)
						.ToList();
					if (ctors?.Count == 1)
					{
						parameterSymbol = ctors[0].Parameters[argumentIndex.Value];
					}
				}
			}
			else if (parentSymbolInfo.CandidateSymbols.Length == 1)
			{
				parameterSymbol = parentSymbolInfo.CandidateSymbols.OfType<IMethodSymbol>()
					.First()
					.Parameters[argumentIndex.Value];
			}
			if (parameterSymbol == null)
			{
				return null;
			}
			var parameterTypeSymbol = parameterSymbol.Type as INamedTypeSymbol;
			var parameterDelegate = parameterTypeSymbol?.DelegateInvokeMethod;

			return parameterDelegate != null
				? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>()
					.FirstOrDefault(o => o.MatchesDefinition(parameterDelegate))
				: null;
			

			// TODO: analyze if we need an option for the consumer to select the correct candidate
		}

		#endregion
	}
}
