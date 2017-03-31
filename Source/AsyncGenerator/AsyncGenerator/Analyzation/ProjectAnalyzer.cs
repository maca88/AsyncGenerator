using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Extensions;
using AsyncGenerator.Internal;
using log4net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Document = Microsoft.CodeAnalysis.Document;
using IMethodSymbol = Microsoft.CodeAnalysis.IMethodSymbol;
using Project = Microsoft.CodeAnalysis.Project;
using Solution = Microsoft.CodeAnalysis.Solution;

namespace AsyncGenerator.Analyzation
{
	public class ProjectAnalyzer
	{
		private static readonly ILog Logger = LogManager.GetLogger(typeof(ProjectAnalyzer));

		private IImmutableSet<Document> _analyzeDocuments;
		private IImmutableSet<Project> _analyzeProjects;
		private ProjectAnalyzeConfiguration _configuration;
		private Solution _solution;
		private const int NoArg = -1;
		private readonly ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<int, IMethodSymbol>> _methodAsyncConterparts = 
			new ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<int, IMethodSymbol>>();

		public ProjectAnalyzer(ProjectData projectData)
		{
			ProjectData = projectData;
		}

		public ProjectData ProjectData { get; }

		public async Task<IProjectAnalyzationResult> Analyze()
		{
			Setup();
			Logger.Info("Parsing documents started");
			// 1. Step - Parse all documents inside the project and create a DocumentData for each
			var documentData = await Task.WhenAll(_analyzeDocuments.Select(o => ProjectData.CreateDocumentData(o))).ConfigureAwait(false);
			Logger.Info("Parsing documents completed");

			Logger.Info("Pre-analyzing documents started");
			// 2. Step - Each method in a document will be pre-analyzed and saved in a structural tree
			await Task.WhenAll(documentData.Select(PreAnalyzeDocumentData)).ConfigureAwait(false);
			Logger.Info("Pre-analyzing documents completed");

			Logger.Info("Scanning references started");
			// 3. Step - Find all references for each method and optionally scan its body for async counterparts
			await Task.WhenAll(documentData.Select(ScanDocumentData)).ConfigureAwait(false);
			Logger.Info("Scanning references completed");

			await Task.WhenAll(documentData.Select(AnalyzeDocumentData)).ConfigureAwait(false);

			return ProjectData;
		}

		#region PreAnalyze methods

		private async Task PreAnalyzeDocumentData(DocumentData documentData)
		{
			foreach (var typeNode in documentData.Node
				.DescendantNodes()
				.OfType<TypeDeclarationSyntax>())
			{
				var typeData = documentData.GetOrCreateTypeData(typeNode);
				typeData.Conversion = _configuration.TypeConversionFunction(typeData.Symbol);
				if (typeData.Conversion == TypeConversion.Ignore)
				{
					continue;
				}

				foreach (var methodNode in typeNode
					.DescendantNodes()
					.OfType<MethodDeclarationSyntax>())
				{
					var methodData = documentData.GetOrCreateMethodData(methodNode, typeData);
					await PreAnalyzeMethodData(methodData).ConfigureAwait(false);

					foreach (var funNode in methodNode
						.DescendantNodes()
						.OfType<AnonymousFunctionExpressionSyntax>())
					{
						var funData = documentData.GetOrCreateAnonymousFunctionData(funNode, methodData);
						PreAnalyzeAnonymousFunction(funData, documentData.SemanticModel);
					}
				}
			}
		}

		private void PreAnalyzeAnonymousFunction(AnonymousFunctionData functionData, SemanticModel semanticModel)
		{
			var funcionSymbol = functionData.Symbol;
			var forceAsync = functionData.MethodData.Conversion == MethodConversion.ToAsync;
			if (funcionSymbol.IsAsync)
			{
				if (forceAsync)
				{
					Logger.Warn($"Anonymous function inside method {functionData.MethodData.Symbol} is already async");
				}
				functionData.Conversion = MethodConversion.Ignore;
				functionData.IsAsync = true;
				return;
			}
			if (funcionSymbol.Parameters.Any(o => o.RefKind == RefKind.Out))
			{
				if (forceAsync)
				{
					Logger.Warn($"Anonymous function inside method {functionData.MethodData.Symbol} has out parameters and cannot be made async");
				}
				functionData.Conversion = MethodConversion.Ignore;
				return;
			}

			if (!functionData.Node.Parent.IsKind(SyntaxKind.Argument))
			{
				if (forceAsync)
				{
					Logger.Warn(
						$"Anonymous function inside method {functionData.MethodData.Symbol} is not passed as an argument but as a {Enum.GetName(typeof(SyntaxKind), functionData.Node.Parent.Kind())} which is not supported");
				}
				functionData.Conversion = MethodConversion.Ignore;
				return;
			}
			else
			{
				var invocationNode = functionData.Node.Ancestors()
				.TakeWhile(o => !o.IsKind(SyntaxKind.MethodDeclaration))
				.OfType<InvocationExpressionSyntax>()
				.First();
				var argumentNode = (ArgumentSyntax)functionData.Node.Parent;
				var argumentListNode = (ArgumentListSyntax)argumentNode.Parent;
				var index = argumentListNode.Arguments.IndexOf(argumentNode);
				var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(invocationNode.Expression).Symbol;
				functionData.ArgumentOfMethod = new Tuple<IMethodSymbol, int>(symbol, index);
			}
			
		}

		private async Task PreAnalyzeMethodData(MethodData methodData)
		{
			var methodSymbol = methodData.Symbol;
			methodData.Conversion = _configuration.MethodConversionFunction(methodSymbol);
			if (methodData.Conversion == MethodConversion.Ignore)
			{
				Logger.Debug($"Method {methodSymbol} will be ignored because of MethodConversionFunction");
				return;
			}
			//if (methodData.Conversion == MethodConversion.Smart && !await SmartAnalyzeMethod(methodData).ConfigureAwait(false))
			//{
			//	methodData.Conversion = MethodConversion.Unknown; //TODO: is needed?
			//}

			var forceAsync = methodData.Conversion == MethodConversion.ToAsync;
			if (methodSymbol.IsAsync || methodSymbol.Name.EndsWith("Async"))
			{
				if (forceAsync)
				{
					Logger.Warn($"Symbol {methodSymbol} is already async");
				}
				methodData.Conversion = MethodConversion.Ignore;
				methodData.IsAsync = true;
				return;
			}
			if (methodSymbol.MethodKind != MethodKind.Ordinary && methodSymbol.MethodKind != MethodKind.ExplicitInterfaceImplementation)
			{
				if (forceAsync)
				{
					Logger.Warn($"Method {methodSymbol} is a {methodSymbol.MethodKind} and cannot be made async");
				}
				methodData.Conversion = MethodConversion.Ignore;
				return;
			}

			if (methodSymbol.Parameters.Any(o => o.RefKind == RefKind.Out))
			{
				if (forceAsync)
				{
					Logger.Warn($"Method {methodSymbol} has out parameters and cannot be made async");
				}
				methodData.Conversion = MethodConversion.Ignore;
				return;
			}

			if (methodSymbol.DeclaringSyntaxReferences.FirstOrDefault() == null)
			{
				if (forceAsync)
				{
					Logger.Warn($"Method {methodSymbol} is external and cannot be made async");
				}
				methodData.Conversion = MethodConversion.Ignore;
				return;
			}

			// Check if explicitly implements external interfaces
			if (methodSymbol.MethodKind == MethodKind.ExplicitInterfaceImplementation)
			{
				foreach (var interfaceMember in methodSymbol.ExplicitInterfaceImplementations)
				{
					if (methodSymbol.ContainingAssembly.Name != interfaceMember.ContainingAssembly.Name)
					{
						methodData.ExternalRelatedMethods.TryAdd(interfaceMember);

						// Check if the interface member has an async counterpart
						var asyncConterPart = interfaceMember.ContainingType.GetMembers()
							.OfType<IMethodSymbol>()
							.Where(o => o.Name == methodSymbol.Name + "Async")
							.SingleOrDefault(o => o.HaveSameParameters(methodSymbol));

						if (asyncConterPart == null)
						{
							Logger.Warn($"Method {methodSymbol} implements an external interface {interfaceMember} and cannot be made async");
							methodData.Conversion = MethodConversion.Ignore;
							return;
						}
						methodData.ExternalAsyncMethods.TryAdd(asyncConterPart);
					}
					else
					{
						methodData.ImplementedInterfaces.TryAdd(interfaceMember);
					}
					//var syntax = interfaceMember.DeclaringSyntaxReferences.FirstOrDefault();
					//if (!CanProcessSyntaxReference(syntax))
					//{
					//	continue;
					//}

				}
			}

			// Check if the method is overriding an external method
			var overridenMethod = methodSymbol.OverriddenMethod;
			while (overridenMethod != null)
			{
				if (methodSymbol.ContainingAssembly.Name != overridenMethod.ContainingAssembly.Name)
				{
					methodData.ExternalRelatedMethods.TryAdd(overridenMethod);
					// Check if the external member has an async counterpart that is not implemented in the current type (missing member)
					var asyncConterPart = overridenMethod.ContainingType.GetMembers()
						.OfType<IMethodSymbol>()
						.Where(o => o.Name == methodSymbol.Name + "Async" && !o.IsSealed && (o.IsVirtual || o.IsAbstract || o.IsOverride))
						.SingleOrDefault(o => o.HaveSameParameters(methodSymbol));
					if (asyncConterPart == null)
					{
						Logger.Warn(
							$"Method {methodSymbol} overrides an external method {overridenMethod} that has not an async counterpart... method will not be converted");
						methodData.Conversion = MethodConversion.Ignore;
						return;
						//if (!asyncMethods.Any() || (asyncMethods.Any() && !overridenMethod.IsOverride && !overridenMethod.IsVirtual))
						//{
						//	Logger.Warn($"Method {methodSymbol} overrides an external method {overridenMethod} and cannot be made async");
						//	return MethodSymbolAnalyzeResult.Invalid;
						//}
					}
					methodData.ExternalAsyncMethods.TryAdd(asyncConterPart);
				}
				else
				{
					methodData.OverridenMethods.TryAdd(overridenMethod);
				}
				//var syntax = overridenMethod.DeclaringSyntaxReferences.SingleOrDefault();
				//else if (CanProcessSyntaxReference(syntax))
				//{
				//	methodData.OverridenMethods.TryAdd(overridenMethod);
				//}
				if (overridenMethod.OverriddenMethod != null)
				{
					overridenMethod = overridenMethod.OverriddenMethod;
				}
				else
				{
					break;
				}
			}
			methodData.BaseOverriddenMethod = overridenMethod;

			// Check if the method is implementing an external interface, if true skip as we cannot modify externals
			// FindImplementationForInterfaceMember will find the first implementation method starting from the deepest base class
			var type = methodSymbol.ContainingType;
			foreach (var interfaceMember in type.AllInterfaces
												.SelectMany(
													o => o.GetMembers(methodSymbol.Name)
														  .Where(
															  m =>
															  {
																  // Find out if the method implements the interface member or an override 
																  // method that implements it
																  var impl = type.FindImplementationForInterfaceMember(m);
																  return methodSymbol.Equals(impl) || methodData.OverridenMethods.Any(ov => ov.Equals(impl));
															  }
															))
														  .OfType<IMethodSymbol>())
			{
				if (methodSymbol.ContainingAssembly.Name != interfaceMember.ContainingAssembly.Name)
				{
					methodData.ExternalRelatedMethods.TryAdd(interfaceMember);

					// Check if the member has an async counterpart that is not implemented in the current type (missing member)
					var asyncConterPart = interfaceMember.ContainingType.GetMembers()
						.OfType<IMethodSymbol>()
						.Where(o => o.Name == methodSymbol.Name + "Async")
						.SingleOrDefault(o => o.HaveSameParameters(methodSymbol));
					if (asyncConterPart == null)
					{
						Logger.Warn($"Method {methodSymbol} implements an external interface {interfaceMember} and cannot be made async");
						methodData.Conversion = MethodConversion.Ignore;
						return;
					}
					methodData.ExternalAsyncMethods.TryAdd(asyncConterPart);
				}
				else
				{
					methodData.ImplementedInterfaces.TryAdd(interfaceMember);
				}
				//var syntax = interfaceMember.DeclaringSyntaxReferences.SingleOrDefault();
				//if (!CanProcessSyntaxReference(syntax))
				//{
				//	continue;
				//}

			}

			// Verify if there is already an async counterpart for this method
			var asyncCounterpart = await GetAsyncCounterpart(methodSymbol, null, false).ConfigureAwait(false);
			if (asyncCounterpart != null)
			{
				Logger.Debug($"Method {methodSymbol} has already an async counterpart {asyncCounterpart}");
				methodData.AsyncCounterpartSymbol = asyncCounterpart;
				methodData.Conversion = MethodConversion.Ignore;
				return;
			}
		}

		#endregion

		#region Scan methods

		private async Task ScanDocumentData(DocumentData documentData)
		{
			foreach (var typeData in documentData.GetAllTypeDatas(o => o.Conversion != TypeConversion.Ignore))
			{
				// If the type have to be defined as a new type then we need to find all references to that type 
				if (typeData.Conversion == TypeConversion.NewType)
				{
					await ScanForTypeReferences(typeData).ConfigureAwait(false);
				}

				if (_configuration.ScanForMissingAsyncMembers)
				{
					ScanForTypeMissingAsyncMethods(typeData);
				}

				foreach (var methodData in typeData.MethodData.Values
					.Where(o => o.Conversion != MethodConversion.Ignore))
				{
					await ScanMethodData(methodData);
					foreach (var functionData in methodData.GetAllAnonymousFunctionData(o => o.Conversion != MethodConversion.Ignore))
					{
						// TODO: do we need something here?
					}
				}
			}
		}

		private async Task ScanMethodData(MethodData methodData, int depth = 0)
		{
			if (methodData.Scanned)
			{
				return;
			}
			methodData.Scanned = true;

			SyntaxReference syntax;
			var bodyScanMethodDatas = new HashSet<MethodData> { methodData };
			var referenceScanMethods = new HashSet<IMethodSymbol>();

			var interfaceMethods = methodData.ImplementedInterfaces.ToImmutableHashSet();
			if (methodData.InterfaceMethod)
			{
				interfaceMethods = interfaceMethods.Add(methodData.Symbol);
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
				var methodNode = (MethodDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);
				var interfaceMethodData = documentData.GetMethodData(methodNode);

				var implementations = await SymbolFinder.FindImplementationsAsync(
					interfaceMethod.OriginalDefinition, _solution, _analyzeProjects)
					.ConfigureAwait(false);
				foreach (var implementation in implementations.OfType<IMethodSymbol>())
				{
					syntax = implementation.DeclaringSyntaxReferences.Single();
					documentData = ProjectData.GetDocumentData(document);
					methodNode = (MethodDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);
					var implMethodData = documentData.GetMethodData(methodNode);

					interfaceMethodData.RelatedMethods.TryAdd(implMethodData);
					implMethodData.RelatedMethods.TryAdd(interfaceMethodData);

					if (_configuration.ScanMethodBody)
					{
						bodyScanMethodDatas.Add(implMethodData);
					}
				}
			}

			MethodData baseMethodData = null;
			IMethodSymbol baseMethodSymbol = null;
			if (methodData.BaseOverriddenMethod?.DeclaringSyntaxReferences.Any() == true)
			{
				baseMethodSymbol = methodData.BaseOverriddenMethod;
			}
			else if (methodData.Symbol.IsVirtual || methodData.Symbol.IsAbstract)
			{
				baseMethodSymbol = methodData.Symbol;
				baseMethodData = methodData;
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
						var methodNode = (MethodDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);
						baseMethodData = ProjectData.GetDocumentData(document).GetMethodData(methodNode);
					}
				}

				if (baseMethodData != null && _configuration.ScanMethodBody)
				{
					bodyScanMethodDatas.Add(baseMethodData);
				}

				var overrides = await SymbolFinder.FindOverridesAsync(baseMethodSymbol.OriginalDefinition,
					_solution, _analyzeProjects).ConfigureAwait(false);
				foreach (var overrideMethod in overrides.OfType<IMethodSymbol>())
				{
					syntax = overrideMethod.DeclaringSyntaxReferences.Single();
					var document = _solution.GetDocument(syntax.SyntaxTree);
					if (!CanProcessDocument(document))
					{
						continue;
					}
					var documentData = ProjectData.GetDocumentData(document);
					var methodNode = (MethodDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);
					var overrideMethodData = documentData.GetMethodData(methodNode);

					if (baseMethodData != null)
					{
						overrideMethodData.RelatedMethods.TryAdd(baseMethodData);
						baseMethodData.RelatedMethods.TryAdd(overrideMethodData);
					}
					else
					{
						overrideMethodData.ExternalRelatedMethods.TryAdd(baseMethodSymbol);
					}

					if (!overrideMethod.IsAbstract && _configuration.ScanMethodBody)
					{
						bodyScanMethodDatas.Add(overrideMethodData);
					}
				}
			}

			if (baseMethodSymbol == null && !interfaceMethods.Any()) //TODO: what about hiding methods
			{
				referenceScanMethods.Add(methodData.Symbol);
			}

			if (_configuration.ScanMethodBody || methodData.Conversion == MethodConversion.Smart)
			{
				var asyncCounterparts = new List<AsyncCounterpartMethod>();
				foreach (var mData in bodyScanMethodDatas)
				{
					asyncCounterparts.AddRange(await FindNewAsyncCounterpartMethods(mData).ConfigureAwait(false));
				}
				foreach (var group in asyncCounterparts.GroupBy(o => o.MethodSymbol))
				{
					await ScanAllMethodReferenceLocations(group.Key, depth).ConfigureAwait(false);
				}
			}
			foreach (var methodToScan in referenceScanMethods)
			{
				await ScanAllMethodReferenceLocations(methodToScan, depth).ConfigureAwait(false);
			}
		}

		#endregion

		#region Analyze methods

		private async Task AnalyzeDocumentData(DocumentData documentData)
		{
			foreach (var typeData in documentData.GetAllTypeDatas(o => o.Conversion != TypeConversion.Ignore))
			{
				foreach (var methodData in typeData.MethodData.Values
					.Where(o => o.Conversion != MethodConversion.Ignore))
				{
					await AnalyzeMethodData(documentData, methodData).ConfigureAwait(false);
					foreach (var functionData in methodData.GetAllAnonymousFunctionData(o => o.Conversion != MethodConversion.Ignore))
					{
						await AnalyzeAnonymousFunctionData(documentData, functionData).ConfigureAwait(false);
					}
				}
			}
		}

		private async Task AnalyzeMethodData(DocumentData documentData, MethodData methodData)
		{
			foreach (var reference in methodData.MethodReferences)
			{
				methodData.MethodReferenceData.TryAdd(await AnalyzeMethodReference(documentData, methodData, reference).ConfigureAwait(false));
			}
		}

		private async Task AnalyzeAnonymousFunctionData(DocumentData documentData, AnonymousFunctionData methodData)
		{
			foreach (var reference in methodData.MethodReferences)
			{
				methodData.MethodReferenceData.TryAdd(await AnalyzeMethodReference(documentData, methodData, reference).ConfigureAwait(false));
			}
		}

		private async Task AnalyzeFunctionData(DocumentData documentData, FunctionData functionData)
		{
			foreach (var reference in functionData.MethodReferences)
			{
				functionData.MethodReferenceData.TryAdd(await AnalyzeMethodReference(documentData, functionData, reference).ConfigureAwait(false));
			}
		}

		private async Task<FunctionReferenceData> AnalyzeMethodReference(DocumentData documentData, FunctionData functionData, ReferenceLocation reference)
		{
			var nameNode = functionData.GetNode().DescendantNodes()
							   .OfType<SimpleNameSyntax>()
							   .First(
								   o =>
								   {
									   if (o.IsKind(SyntaxKind.GenericName))
									   {
										   return o.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).Span ==
												  reference.Location.SourceSpan;
									   }
									   return o.Span == reference.Location.SourceSpan;
								   });
			var refMethodSymbol = (IMethodSymbol)documentData.SemanticModel.GetSymbolInfo(nameNode).Symbol;

			FunctionData refFunctionData = null;
			var refMethodDocData = refMethodSymbol.DeclaringSyntaxReferences.Select(o => ProjectData.GetDocumentData(o))
				.SingleOrDefault();
			if (refMethodDocData != null)
			{
				refFunctionData = await refMethodDocData.GetAnonymousFunctionOrMethodData(refMethodSymbol).ConfigureAwait(false);
			}

			var refData = new FunctionReferenceData(functionData, reference, nameNode, refMethodSymbol, refFunctionData)
			{
				CanBeAsync = true
			};

			var currNode = nameNode.Parent;
			var ascend = true;
			while (ascend)
			{
				ascend = false;
				switch (currNode.Kind())
				{
					case SyntaxKind.ConditionalExpression:
						break;
					case SyntaxKind.InvocationExpression:
						AnalyzeInvocationExpression(documentData, currNode, nameNode, refData);
						break;
					case SyntaxKind.Argument:
						AnalyzeArgumentExpression(currNode, nameNode, refData);
						break;
					case SyntaxKind.AddAssignmentExpression:
						refData.CanBeAsync = false;
						Logger.Warn($"Cannot attach an async method to an event (void async is not an option as cannot be awaited):\r\n{nameNode.Parent}\r\n");
						break;
					case SyntaxKind.VariableDeclaration:
						refData.CanBeAsync = false;
						Logger.Warn($"Assigning async method to a variable is not supported:\r\n{nameNode.Parent}\r\n");
						break;
					case SyntaxKind.CastExpression:
						//refData.MustBeAwaited = true;
						ascend = true;
						break;
					case SyntaxKind.ReturnStatement:
						break;
					// skip
					case SyntaxKind.VariableDeclarator:
					case SyntaxKind.EqualsValueClause:
					case SyntaxKind.SimpleMemberAccessExpression:
					case SyntaxKind.ArgumentList:
					case SyntaxKind.ObjectCreationExpression:
						ascend = true;
						break;
					default:
						throw new NotSupportedException($"Unknown node kind: {currNode.Kind()}");
				}

				if (ascend)
				{
					currNode = currNode.Parent;
				}
			}

			var statementNode = currNode.AncestorsAndSelf().OfType<StatementSyntax>().First();
			if (statementNode.IsKind(SyntaxKind.ReturnStatement))
			{
				refData.UsedAsReturnValue = true;
			}
			//else if (Symbol.ReturnsVoid && statementNode.IsKind(SyntaxKind.ExpressionStatement))
			//{
			//	// Check if the reference is the last statement to execute
			//	var nextNode =
			//		statementNode.Ancestors()
			//						   .OfType<BlockSyntax>()
			//						   .First()
			//						   .DescendantNodesAndTokens()
			//						   .First(o => o.SpanStart >= statementNode.Span.End);
			//	// Check if the reference is the last statement in the method before returning
			//	if (nextNode.IsKind(SyntaxKind.ReturnStatement) || nextNode.Span.End == Node.Span.End)
			//	{
			//		refData.LastStatement = true;
			//	}
			//}
			return refData;
		}

		private void AnalyzeInvocationExpression(DocumentData documentData, SyntaxNode node, SimpleNameSyntax nameNode, FunctionReferenceData functionReferenceData)
		{
			var functionData = functionReferenceData.FunctionData;
			var functionNode = functionData.GetNode();
			var queryExpression = node.Ancestors()
				.TakeWhile(o => o != functionNode)
				.OfType<QueryExpressionSyntax>()
				.FirstOrDefault();
			if (queryExpression != null) // Await is not supported in a linq query
			{
				functionReferenceData.CanBeAsync = false;
				Logger.Warn($"Cannot await async method in a query expression:\r\n{queryExpression}\r\n");
				return;
			}
			var methodSymbol = (IMethodSymbol)documentData.SemanticModel.GetSymbolInfo(nameNode).Symbol;

			ConcurrentDictionary<int, IMethodSymbol> asynMethodSymbols;
			if (_methodAsyncConterparts.TryGetValue(methodSymbol.OriginalDefinition, out asynMethodSymbols))
			{
				var indexOfArgument = NoArg;
				// Find out if the invoked method uses any delegate arguments
				var invocationNode = (InvocationExpressionSyntax)node;
				var delegateParamNodes = invocationNode.ArgumentList.Arguments
					.Select((o, i) => new
					{
						o.Expression,
						Index = i,
						MethodSymbol = documentData.SemanticModel.GetSymbolInfo(o.Expression).Symbol as IMethodSymbol
					})
					.Where(o => o.MethodSymbol != null)
					.ToList();
				if (delegateParamNodes.Count > 1)
				{
					functionReferenceData.CanBeAsync = false;
					Logger.Warn($"Method {methodSymbol} has more than one delegate argument which is not supported");
				}
				else if (delegateParamNodes.Count == 1)
				{
					var delegateParam = delegateParamNodes.First();
					if (delegateParam.MethodSymbol.DeclaringSyntaxReferences.Any(o => documentData.ProjectData.Contains(o)))
					{

					}
					else
					{
						
					}


					var anonymousFunctionDatas = functionData.GetAnonymousFunctionData().ToList();


				}
				else
				{
					
				}

				IMethodSymbol asyncMethodSymbol;
				if (asynMethodSymbols.TryGetValue(indexOfArgument, out asyncMethodSymbol))
				{
					if (asyncMethodSymbol.ReturnsVoid || asyncMethodSymbol.ReturnType.Name != "Task")
					{
						functionReferenceData.CanBeAwaited = false;
						Logger.Info($"Cannot await method that is either void or do not return a Task:\r\n{methodSymbol}\r\n");
					}
					//TODO: do we need this?
					// Check if the invocation expression takes any func as a parameter, we will allow to rename the method only if there is an awaitable invocation
					//var invocationNode = (InvocationExpressionSyntax)node;
					//var delegateParamNodes = invocationNode.ArgumentList.Arguments
					//	.Select((o, i) => new { o.Expression, Index = i })
					//	.Where(o => indexOfArgument == NoArg || indexOfArgument == o.Index)
					//	.Where(o => functionData.MethodReferences.Any(r => o.Expression.Span.Contains(r.Location.SourceSpan)))
					//	.ToList();
					//if (!delegateParamNodes.Any())
					//{
					//	functionReferenceData.CanBeAsync = false;
					//	Logger.Warn($"Cannot convert method to async as it is either void or do not return a Task and has not any parameters that can be async:\r\n{methodSymbol}\r\n");
					//}
				}
				
			}

			// Custom code TODO: move
			if (nameNode.Identifier.ToString() == "ToList")
			{
				var beforeToListExpression = ((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)node).Expression).Expression;
				var operation = documentData.SemanticModel.GetOperation(beforeToListExpression);
				if (operation == null)
				{
					functionReferenceData.CanBeAsync = false;
					Logger.Warn($"Cannot find operation for previous node of ToList:\r\n{beforeToListExpression}\r\n");
				}
				else if (operation.Type.Name != "IQueryable")
				{
					functionReferenceData.CanBeAsync = false;
					Logger.Warn($"Operation for previous node of ToList is not IQueryable:\r\n{operation.Type.Name}\r\n");
				}
			}
			// End custom code

		}

		private void AnalyzeArgumentExpression(SyntaxNode node, SimpleNameSyntax nameNode, FunctionReferenceData result)
		{
			result.PassedAsArgument = true;
			var documentData = result.FunctionData.TypeData.NamespaceData.DocumentData;
			var methodArgTypeInfo = documentData.SemanticModel.GetTypeInfo(nameNode);
			if (methodArgTypeInfo.ConvertedType?.TypeKind != TypeKind.Delegate)
			{
				// TODO: debug and document
				return;
			}

			// Custom code TODO: move
			//var convertedType = methodArgTypeInfo.ConvertedType;
			//if (convertedType.ContainingAssembly.Name == "nunit.framework" && convertedType.Name == "TestDelegate")
			//{
			//	result.WrapInsideAsyncFunction = true;
			//	return;
			//}
			// End custom code

			var delegateMethod = (IMethodSymbol)methodArgTypeInfo.ConvertedType.GetMembers("Invoke").First();

			if (!delegateMethod.IsAsync)
			{
				result.CanBeAsync = false;
				Logger.Warn($"Cannot pass an async method as parameter to a non async Delegate method:\r\n{delegateMethod}\r\n");
			}
			else
			{
				var argumentMethodSymbol = (IMethodSymbol)documentData.SemanticModel.GetSymbolInfo(nameNode).Symbol;
				if (!argumentMethodSymbol.ReturnType.Equals(delegateMethod.ReturnType)) // i.e IList<T> -> IEnumerable<T>
				{
					//result.MustBeAwaited = true;
				}
			}
		}

		#endregion

		private bool CanProcessDocument(Document doc)
		{
			if (doc.Project != ProjectData.Project)
			{
				return false;
			}
			return _analyzeDocuments.Contains(doc);
		}

		#region ScanAllMethodReferenceLocations

		private readonly ConcurrentSet<IMethodSymbol> _scannedMethodReferenceSymbols = new ConcurrentSet<IMethodSymbol>();

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
				if (refLocation.Document.Project != ProjectData.Project)
				{
					throw new InvalidOperationException($"Reference {refLocation} is referencing a symbol from another project");
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

				var refMethodSymbol = symbol as IMethodSymbol;
				if (refMethodSymbol == null)
				{
					continue;
				}
				var baseMethodData = await documentData.GetAnonymousFunctionOrMethodData(refMethodSymbol).ConfigureAwait(false);
				if (baseMethodData == null)
				{
					continue;
				}
				// Save the reference as it can be made async
				if (!baseMethodData.MethodReferences.TryAdd(refLocation))
				{
					continue; // Reference already processed
				}

				// Find the real method on that reference as FindReferencesAsync will also find references to base and interface methods
				var nameNode = baseMethodData.GetNode().DescendantNodes()
							   .OfType<SimpleNameSyntax>()
							   .First(
								   o =>
								   {
									   if (o.IsKind(SyntaxKind.GenericName))
									   {
										   return o.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).Span ==
												  refLocation.Location.SourceSpan;
									   }
									   return o.Span == refLocation.Location.SourceSpan;
								   });
				var invokedSymbol = (IMethodSymbol)documentData.SemanticModel.GetSymbolInfo(nameNode).Symbol;
				var invokedMethodDocData = invokedSymbol.DeclaringSyntaxReferences
																  .Select(o => ProjectData.GetDocumentData(o))
																  .SingleOrDefault();
				if (invokedMethodDocData != null)
				{
					var invokedMethodData = await invokedMethodDocData.GetMethodData(invokedSymbol).ConfigureAwait(false);
					invokedMethodData?.InvokedBy.Add(baseMethodData);
				}
				var methodData = baseMethodData as MethodData;
				if (methodData != null)
				{
					await ScanMethodData(methodData, depth).ConfigureAwait(false);
				}
			}
		}

		#endregion


		private async Task<IMethodSymbol> GetAsyncCounterpart(IMethodSymbol methodSymbol, int? indexOfArgument, bool inherit)
		{
			IMethodSymbol asyncMethodSymbol = null;
			if (_configuration.FindAsyncCounterpartDelegates.Any())
			{
				foreach (var findAsyncConterpart in _configuration.FindAsyncCounterpartDelegates)
				{
					asyncMethodSymbol = await findAsyncConterpart(ProjectData.Project,
						methodSymbol, indexOfArgument, inherit).ConfigureAwait(false);
					if (asyncMethodSymbol != null)
					{
						break;
					}
				}
			}
			else
			{
				asyncMethodSymbol = methodSymbol.GetAsyncCounterpart(indexOfArgument, inherit);
			}
			return asyncMethodSymbol;
		}


		private void GetAsyncCounterpart(FunctionData functionData, InvocationExpressionSyntax invocationNode)
		{
			
		}

		/// <summary>
		/// Scan all invocation expression syntaxes and tries to get a async counterpart.
		/// The method will return only newly found async counterparts.
		/// </summary>
		/// <param name="methodData"></param>
		/// <returns></returns>
		private async Task<List<AsyncCounterpartMethod>> FindNewAsyncCounterpartMethods(MethodData methodData)
		{
			var result = new List<AsyncCounterpartMethod>();
			if (methodData.Node.Body == null)
			{
				return result;
			}
			var documentData = methodData.TypeData.NamespaceData.DocumentData;
			var semanticModel = documentData.SemanticModel;
			/*
			var indexLookup = methodData.GetAllAnonymousFunctionData(o => o.Conversion != MethodConversion.Ignore)
				.Select(o => o.ArgumentOfMethod)
				.ToLookup(o => o.Item1, o => o.Item2);*/

			foreach (var invocation in methodData.Node.Body.DescendantNodes()
										   .OfType<InvocationExpressionSyntax>())
			{
				var methodSymbol = semanticModel.GetSymbolInfo(invocation.Expression).Symbol as IMethodSymbol;
				if (methodSymbol == null)
				{
					continue;
				}
				var indexesOfArgument = new [] { NoArg };
				if (indexLookup.Contains(methodSymbol))
				{
					indexesOfArgument = indexLookup[methodSymbol].ToArray();
				}
				methodSymbol = methodSymbol.OriginalDefinition;
				var asyncMethodSymbols = _methodAsyncConterparts.GetOrAdd(methodSymbol,
					symbol => new ConcurrentDictionary<int, IMethodSymbol>());
				foreach (var indexOfArgument in indexesOfArgument)
				{
					IMethodSymbol asyncMethodSymbol;
					if (!asyncMethodSymbols.TryGetValue(indexOfArgument, out asyncMethodSymbol))
					{
						asyncMethodSymbol = await GetAsyncCounterpart(methodSymbol, indexOfArgument == NoArg ? null : (int?)indexOfArgument, true).ConfigureAwait(false);
						var value = asyncMethodSymbol?.OriginalDefinition;
						asyncMethodSymbols.AddOrUpdate(indexOfArgument, value, (k, v) => value);
					}
					else
					{
						continue; // do not return an already scanned method
					}
					if (asyncMethodSymbol == null)
					{
						continue;
					}

					result.Add(new AsyncCounterpartMethod
					{
						MethodSymbol = methodSymbol.OriginalDefinition,
						AsyncMethodSymbol = asyncMethodSymbol.OriginalDefinition,
						MethodNode = invocation.Expression
					});
				}
			}
			return result;
		}

		private void ScanForTypeMissingAsyncMethods(TypeData typeData)
		{
			var documentData = typeData.NamespaceData.DocumentData;
			var members = typeData.Node
				.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.Select(o => new { Node = o, Symbol = documentData.SemanticModel.GetDeclaredSymbol(o) })
				.ToLookup(o =>
					o.Symbol.MethodKind == MethodKind.ExplicitInterfaceImplementation
						? o.Symbol.Name.Split('.').Last()
						: o.Symbol.Name);
			//var methodDatas = new List<MethodData>();

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
				var nonAsyncName = asyncMember.Name.Remove(asyncMember.Name.LastIndexOf("Async", StringComparison.InvariantCulture));
				if (!members.Contains(nonAsyncName))
				{
					continue;
				}
				var nonAsyncMember = members[nonAsyncName].First(o => o.Symbol.HaveSameParameters(asyncMember));
				var methodData = documentData.GetMethodData(nonAsyncMember.Node);
				methodData.Conversion = MethodConversion.ToAsync;
				//methodDatas.Add(methodData);
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
					var nonAsyncName = asyncMember.Name.Remove(asyncMember.Name.LastIndexOf("Async", StringComparison.InvariantCulture));
					if (!members.Contains(nonAsyncName))
					{
						Logger.Info($"Abstract sync counterpart of async member {asyncMember} not found in file {documentData.FilePath}");
						continue;
					}
					var nonAsyncMember = members[nonAsyncName].FirstOrDefault(o => o.Symbol.HaveSameParameters(asyncMember));
					if (nonAsyncMember == null)
					{
						Logger.Info($"Abstract sync counterpart of async member {asyncMember} not found in file {documentData.FilePath}");
						continue;
					}
					var methodData = documentData.GetMethodData(nonAsyncMember.Node);
					methodData.Conversion = MethodConversion.ToAsync;
					//methodDatas.Add(methodData);
				}
				baseType = baseType.BaseType;
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
				if (!typeData.SelfReferences.TryAdd(refLocation))
				{
					continue; // Reference already processed
				}

				// We need to find the type where the reference location is
				var node = documentData.Node.DescendantNodes(descendIntoTrivia: true)
					.First(
						o =>
						{
							if (o.IsKind(SyntaxKind.GenericName))
							{
								return o.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).Span ==
									   refLocation.Location.SourceSpan;
							}
							return o.Span == refLocation.Location.SourceSpan;
						});

				var funcNode = node.Ancestors().OfType<AnonymousFunctionExpressionSyntax>().FirstOrDefault();
				if (funcNode != null)
				{
					var functionData = documentData.GetAnonymousFunctionData(funcNode);
					functionData.TypeReferences.TryAdd(refLocation);
					continue;
				}

				var methodNode = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
				if (methodNode != null)
				{
					var methodData = documentData.GetMethodData(methodNode);
					methodData.TypeReferences.TryAdd(refLocation);
					continue;
				}
				var type = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
				if (type != null)
				{
					var refTypeData = documentData.GetTypeData(type);
					refTypeData.TypeReferences.TryAdd(refLocation);
					continue;
				}
				// Can happen when declaring a Name in a using statement
				var namespaceNode = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
				var namespaceData = documentData.GetNamespaceData(namespaceNode);
				namespaceData.TypeReferences.TryAdd(refLocation);
			}
		}

		private void Setup()
		{
			_configuration = ProjectData.Configuration.AnalyzeConfiguration;
			_solution = ProjectData.Project.Solution;
			_analyzeDocuments = ProjectData.Project.Documents.Where(o => _configuration.DocumentSelectionPredicate(o))
				.ToImmutableHashSet();
			_analyzeProjects = new[] { ProjectData.Project }.ToImmutableHashSet();
		}
	}
}
