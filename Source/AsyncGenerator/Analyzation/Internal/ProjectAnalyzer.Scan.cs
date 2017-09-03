using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Extensions;
using AsyncGenerator.Extensions;
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
		private async Task ScanDocumentData(DocumentData documentData)
		{
			foreach (var typeData in documentData.GetAllTypeDatas(o => o.Conversion != TypeConversion.Ignore))
			{
				// If the type have to be defined as a new type then we need to find all references to that type. 
				// We must not scan for nested types as they will not be renamed
				if (typeData.Conversion == TypeConversion.NewType)
				{
					await ScanForTypeReferences(typeData).ConfigureAwait(false);
				}

				if (_configuration.ScanForMissingAsyncMembers != null && _configuration.ScanForMissingAsyncMembers(typeData.Symbol))
				{
					ScanForTypeMissingAsyncMethods(typeData);
				}
				// Scan also explicitly ignored methods in order to fix the conversion  if the user applies an invalid
				// conversion. (e.g. ignored a method that is used in a method that will be genereated)
				foreach (var methodOrAccessorData in typeData.MethodsAndAccessors
					.Where(o => o.Conversion.HasAnyFlag(MethodConversion.ToAsync, MethodConversion.Smart) ||
					            (o.ExplicitlyIgnored && (o.TypeData.Conversion == TypeConversion.NewType || o.TypeData.Conversion == TypeConversion.Copy))
					)
				)
				{
					await ScanMethodData(methodOrAccessorData).ConfigureAwait(false);
				}
				foreach (var fieldVariableData in typeData.Fields.Values.SelectMany(o => o.Variables)
					.Where(o => o.Conversion == FieldVariableConversion.Smart))
				{
					await ScanFieldVariable(fieldVariableData).ConfigureAwait(false);
				}
			}
		}

		private readonly ConcurrentSet<IMethodSymbol> _searchedOverrides = new ConcurrentSet<IMethodSymbol>();

		private Task FindOverrides(IMethodSymbol methodSymbol, Action<IMethodSymbol, MethodOrAccessorData> action)
		{
			methodSymbol = methodSymbol.OriginalDefinition;
			if (!_searchedOverrides.TryAdd(methodSymbol))
			{
				return Task.CompletedTask;
			}

			async Task Find()
			{
				var overrides = await SymbolFinder.FindOverridesAsync(methodSymbol, _solution, _analyzeProjects)
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
					var overrideMethodNode = await syntax.GetSyntaxAsync().ConfigureAwait(false);
					var overrideMethodData = overrideDocumentData.GetMethodOrAccessorData(overrideMethodNode);

					action(overrideMethod, overrideMethodData);
				}
			}
			return Find();
		}

		private readonly ConcurrentSet<IMethodSymbol> _searchedImplementations = new ConcurrentSet<IMethodSymbol>();

		private Task FindImplementations(IMethodSymbol methodSymbol, Func<IMethodSymbol, MethodOrAccessorData, Task> action)
		{
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
							.FindImplementationsAsync(methodSymbol.AssociatedSymbol /* Property symbol */, _solution, _analyzeProjects)
							.ConfigureAwait(false))
						.OfType<IPropertySymbol>()
						.Select(o => methodSymbol.MethodKind == MethodKind.PropertyGet ? o.GetMethod : o.SetMethod)
						.Where(o => o != null);
				}
				else
				{
					implementations = (await SymbolFinder.FindImplementationsAsync(methodSymbol, _solution, _analyzeProjects)
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
					var methodNode = await syntax.GetSyntaxAsync().ConfigureAwait(false);
					var implMethodData = documentData.GetMethodOrAccessorData(methodNode);

					await action(implMethod, implMethodData).ConfigureAwait(false);
				}
			}
			return Find();
		}

		private async Task ScanMethodData(MethodOrAccessorData methodOrAccessorData, int depth = 0)
		{
			if (methodOrAccessorData.Scanned)
			{
				return;
			}
			methodOrAccessorData.Scanned = true;

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
				var methodNode = await syntax.GetSyntaxAsync().ConfigureAwait(false);
				var interfaceMethodData = documentData.GetMethodOrAccessorData(methodNode);

				// NOTE: FindImplementationsAsync will not find all implementations when we have an abstract/virtual implementation of the interface.
				// In this case we will get only the abstract/virtual method so we have to find all overrides for it manually
				await FindImplementations(interfaceMethod, async (implMethod, implMethodData) =>
				{
					interfaceMethodData.RelatedMethods.TryAdd(implMethodData);
					implMethodData.RelatedMethods.TryAdd(interfaceMethodData);

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
						overrideMethodData.RelatedMethods.TryAdd(interfaceMethodData);
						interfaceMethodData.RelatedMethods.TryAdd(overrideMethodData);
						implMethodData.RelatedMethods.TryAdd(overrideMethodData);
						overrideMethodData.RelatedMethods.TryAdd(implMethodData);
						if (_configuration.ScanMethodBody || overrideMethodData.Conversion.HasAnyFlag(MethodConversion.Smart, MethodConversion.ToAsync))
						{
							bodyScanMethodDatas.Add(overrideMethodData);
						}
					}).ConfigureAwait(false);
				}).ConfigureAwait(false);
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
						var methodNode = await syntax.GetSyntaxAsync().ConfigureAwait(false);
						baseMethodData = ProjectData.GetDocumentData(document).GetMethodOrAccessorData(methodNode);
					}
				}

				if (baseMethodData != null && (_configuration.ScanMethodBody || baseMethodData.Conversion.HasAnyFlag(MethodConversion.Smart, MethodConversion.ToAsync)))
				{
					bodyScanMethodDatas.Add(baseMethodData);
				}

				await FindOverrides(baseMethodSymbol, (overrideMethod, overrideMethodData) =>
				{
					if (baseMethodData != null)
					{
						overrideMethodData.RelatedMethods.TryAdd(baseMethodData);
						baseMethodData.RelatedMethods.TryAdd(overrideMethodData);
					}
					else
					{
						overrideMethodData.ExternalRelatedMethods.TryAdd(baseMethodSymbol);
					}
					if (!overrideMethod.IsAbstract && (_configuration.ScanMethodBody || overrideMethodData.Conversion.HasAnyFlag(MethodConversion.Smart, MethodConversion.ToAsync)))
					{
						bodyScanMethodDatas.Add(overrideMethodData);
					}
				}).ConfigureAwait(false);
			}

			if (baseMethodSymbol == null && !interfaceMethods.Any()) //TODO: what about hiding methods
			{
				referenceScanMethods.Add(methodOrAccessorData.Symbol);
			}

			foreach (var mData in bodyScanMethodDatas)
			{
				foreach (var method in FindNewlyInvokedMethodsWithAsyncCounterpart(mData))
				{
					await ScanAllMethodReferenceLocations(method, depth).ConfigureAwait(false);
				}
			}
			foreach (var methodToScan in referenceScanMethods)
			{
				await ScanAllMethodReferenceLocations(methodToScan, depth).ConfigureAwait(false);
			}
		}

		private async Task ScanFieldVariable(FieldVariableDeclaratorData fieldVariableData)
		{
			var references = await SymbolFinder.FindReferencesAsync(fieldVariableData.Symbol, _solution).ConfigureAwait(false);
			foreach (var refLocation in references.SelectMany(o => o.Locations))
			{
				var documentData = ProjectData.GetDocumentData(refLocation.Document);
				// We need to find the type where the reference location is
				var node = documentData.Node.GetSimpleName(refLocation.Location.SourceSpan, true);
				var dataNode = documentData.GetNearestNodeData(node.Parent);
				if (dataNode != null)
				{
					fieldVariableData.UsedBy.Add(dataNode);
				}
			}
		}

		/// <summary>
		/// When a type needs to be defined as a new type we need to find all references to them.
		/// Reference can point to a variable, field, base type, argument definition
		/// </summary>
		private async Task ScanForTypeReferences(TypeData typeData)
		{
			// References for ctor of the type and the type itself wont have any locations
			var references = await SymbolFinder.FindReferencesAsync(typeData.Symbol, _solution, _analyzeDocuments).ConfigureAwait(false);
			foreach (var refLocation in references.SelectMany(o => o.Locations))
			{
				var documentData = ProjectData.GetDocumentData(refLocation.Document);
				// We need to find the type where the reference location is
				var node = documentData.Node.GetSimpleName(refLocation.Location.SourceSpan, true);
				var typeReference = new TypeReferenceData(refLocation, node, typeData.Symbol)
				{
					IsCref = node.Parent.IsKind(SyntaxKind.NameMemberCref)
				};
				if (!typeData.SelfReferences.TryAdd(typeReference))
				{
					Logger.Debug($"Performance hit: Self reference for type {typeData.Symbol} already exists");
					continue; // Reference already processed
				}
				var isCref = node.Parent.IsKind(SyntaxKind.NameMemberCref);
				var dataNode = documentData.GetNearestNodeData(node.Parent, isCref);
				dataNode?.TypeReferences.TryAdd(typeReference);
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
				.Union(
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
				.Union(
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
					Logger.Debug($"Sync counterpart of async member {asyncMember} not found in file {documentData.FilePath}");
					continue;
				}
				var nonAsyncMember = syncMethods[asyncMember.Name].First(o => o.Symbol.IsAsyncCounterpart(null, asyncMember, true, true, false)); // TODO: what to do if there are more than one?
				var methodData = documentData.GetMethodOrAccessorData(nonAsyncMember.Node);
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

			// Find all abstract non implemented async methods. Descend base types until we find a non abstract one.
			var baseType = typeData.Symbol.BaseType;
			while (baseType != null)
			{
				if (!baseType.IsAbstract)
				{
					break;
				}
				foreach (var asyncMember in baseType.GetMembers()
					.OfType<IMethodSymbol>()
					.Where(o => o.IsAbstract && o.Name.EndsWith("Async")))
				{
					if (!syncMethods.Contains(asyncMember.Name))
					{
						Logger.Debug($"Abstract sync counterpart of async member {asyncMember} not found in file {documentData.FilePath}");
						continue;
					}
					var nonAsyncMember = syncMethods[asyncMember.Name].FirstOrDefault(o => o.Symbol.IsAsyncCounterpart(null, asyncMember, true, true, false));
					if (nonAsyncMember == null)
					{
						Logger.Debug($"Abstract sync counterpart of async member {asyncMember} not found in file {documentData.FilePath}");
						continue;
					}
					var methodData = documentData.GetMethodOrAccessorData(nonAsyncMember.Node);
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

		private readonly ConcurrentSet<IMethodSymbol> _scannedMethodReferenceSymbols = new ConcurrentSet<IMethodSymbol>();

		private readonly ConcurrentSet<ReferenceLocation> _scannedLocationsSymbols = new ConcurrentSet<ReferenceLocation>();

		private async Task ScanAllMethodReferenceLocations(IMethodSymbol methodSymbol, int depth)
		{
			if (_scannedMethodReferenceSymbols.Contains(methodSymbol.OriginalDefinition))
			{
				return;
			}
			_scannedMethodReferenceSymbols.TryAdd(methodSymbol.OriginalDefinition);

			var references = await SymbolFinder.FindReferencesAsync(methodSymbol.OriginalDefinition,
				_solution, _analyzeDocuments).ConfigureAwait(false);

			depth++;
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
					Logger.Debug($"Symbol not found for reference ${refLocation}");
					continue;
				}

				if (symbol.Kind != SymbolKind.Method)
				{
					if (symbol.Kind != SymbolKind.NamedType)
					{
						continue;
					}
					// A cref can be on a method or type trivia but we get always the type symbol
					var crefTypeData = documentData.GetAllTypeDatas(o => o.Symbol.Equals(symbol)).FirstOrDefault();
					if (crefTypeData == null)
					{
						continue;
					}
					// Try to find the real node where the cref is located
					var crefReferenceNameNode = crefTypeData.Node.GetSimpleName(refLocation.Location.SourceSpan, true);
					var crefReferenceSymbol = (IMethodSymbol)documentData.SemanticModel.GetSymbolInfo(crefReferenceNameNode).Symbol;
					if (crefReferenceSymbol == null)
					{
						continue;
					}
					var crefReferenceMethodData = ProjectData.GetMethodOrAccessorData(crefReferenceSymbol);
					var crefReferenceData = new CrefFunctionReferenceData(refLocation, crefReferenceNameNode, crefReferenceSymbol, crefReferenceMethodData);

					var memberNode = crefReferenceNameNode.Ancestors().OfType<MemberDeclarationSyntax>().First();
					var methodNode = memberNode as MethodDeclarationSyntax;
					if (methodNode != null)
					{
						var crefMethodData = (MethodData)documentData.GetNodeData(methodNode, typeData: crefTypeData);
						if (!crefMethodData.CrefMethodReferences.TryAdd(crefReferenceData))
						{
							Logger.Debug($"Performance hit: CrefFunctionReferenceData {crefReferenceNameNode} already added");
						}
					}
					else
					{
						if (!crefTypeData.CrefReferences.TryAdd(crefReferenceData))
						{
							Logger.Debug($"Performance hit: CrefFunctionReferenceData {crefReferenceNameNode} already added");
						}
					}
					continue; // No need to further scan a cref reference
				}

				var baseMethodData = documentData.GetFunctionData(symbol);
				if (baseMethodData == null) // TODO: Current is null for lambda in fields
				{
					var refMethodSymbol = (IMethodSymbol) symbol;
					if (refMethodSymbol.MethodKind == MethodKind.AnonymousFunction || refMethodSymbol.MethodKind == MethodKind.LambdaMethod)
					{
						Logger.Warn($"Function inside member {refMethodSymbol.ContainingSymbol} cannot be async because of its kind {refMethodSymbol.MethodKind}");
					}
					else
					{
						Logger.Warn($"Method {refMethodSymbol} cannot be async because of its kind {refMethodSymbol.MethodKind}");
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
					methodReferenceSymbol = TryFindCandidate(nameNode, referenceSymbolInfo, documentData.SemanticModel);
					if (methodReferenceSymbol == null)
					{
						throw new InvalidOperationException($"Unable to find symbol for node {nameNode} inside function {baseMethodData.Symbol}");
					}
					Logger.Warn($"GetSymbolInfo did not successfully resloved symbol for node {nameNode} inside function {baseMethodData.Symbol}, but we got a candidate instead. CandidateReason: {referenceSymbolInfo.CandidateReason}");
				}
				var referenceMethodData = ProjectData.GetMethodOrAccessorData(methodReferenceSymbol);
				// Check if the reference is a cref reference
				if (nameNode.Parent.IsKind(SyntaxKind.NameMemberCref))
				{
					var crefReferenceData = new CrefFunctionReferenceData(refLocation, nameNode, methodReferenceSymbol, referenceMethodData);
					if (!baseMethodData.CrefMethodReferences.TryAdd(crefReferenceData))
					{
						Logger.Debug($"Performance hit: CrefFunctionReferenceData {nameNode} already added");
					}
					continue; // No need to further scan a cref reference
				}
				referenceMethodData?.InvokedBy.Add(baseMethodData);
				var methodReferenceData = new BodyFunctionReferenceData(baseMethodData, refLocation, nameNode, methodReferenceSymbol, referenceMethodData);
				if (!baseMethodData.BodyMethodReferences.TryAdd(methodReferenceData))
				{
					Logger.Debug($"Performance hit: method reference {methodReferenceSymbol} already processed");
					continue; // Reference already processed
				}

				if (baseMethodData.Conversion == MethodConversion.Ignore)
				{
					LogIgnoredReason(baseMethodData, !baseMethodData.ExplicitlyIgnored);
					continue;
				}

				var methodData = baseMethodData as MethodOrAccessorData;
				if (methodData != null && !methodData.Scanned)
				{
					await ScanMethodData(methodData, depth).ConfigureAwait(false);
				}
			}
		}

		private IMethodSymbol TryFindCandidate(SyntaxNode nameNode, SymbolInfo symbolInfo, SemanticModel semanticModel)
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
			var ascend = true;
			var currNode = nameNode.Parent;
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
			var parentSymbolInfo = semanticModel.GetSymbolInfo(currNode);

			if (!argumentIndex.HasValue)
			{
				return null;
			}
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
