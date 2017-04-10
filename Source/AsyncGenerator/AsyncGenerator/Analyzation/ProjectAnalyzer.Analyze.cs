using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using static AsyncGenerator.Analyzation.AsyncCounterpartsSearchOptions;

namespace AsyncGenerator.Analyzation
{
	public partial class ProjectAnalyzer
	{
		private readonly string[] _taskResultMethods = {"Wait", "GetResult"};

		private async Task<IList<MethodData>> AnalyzeDocumentData(DocumentData documentData)
		{
			var validMethodData = new List<MethodData>();
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
					if (methodData.Conversion != MethodConversion.Ignore)
					{
						validMethodData.Add(methodData);
					}
				}
			}
			return validMethodData;
		}

		private async Task AnalyzeMethodData(DocumentData documentData, MethodData methodData)
		{
			foreach (var reference in methodData.MethodReferences)
			{
				methodData.MethodReferenceData.TryAdd(await AnalyzeMethodReference(documentData, methodData, reference).ConfigureAwait(false));
			}
			
			if (methodData.Conversion == MethodConversion.ToAsync)
			{
				return;
			}

			// If a method is never invoked and there is no invocations inside the method body that can be async and there is no related methods we can ignore it 
			if (!methodData.InvokedBy.Any() && !methodData.MethodReferenceData.Any(o => o.CanBeAsync) && !methodData.RelatedMethods.Any())
			{
				// If we have to create a new type we need to consider also the external related methods
				if (methodData.TypeData.Conversion != TypeConversion.NewType || !methodData.ExternalRelatedMethods.Any())
				{
					methodData.Conversion = MethodConversion.Ignore;
					return;
				}

			}

			// Done in the post analyzation step
			// At this point a method can be converted to async if we have atleast one external method invocation that can be asnyc
			// or one internal method that is marked to be async.
			//if (methodData.MethodReferenceData.Any(o => o.CanBeAsync &&
			//	!ProjectData.Contains(o.ReferenceSymbol) || o.ReferenceFunctionData?.Conversion == MethodConversion.ToAsync))
			//{
			//	methodData.Conversion = MethodConversion.ToAsync;
			//	return;
			//}
		}

		private async Task AnalyzeAnonymousFunctionData(DocumentData documentData, AnonymousFunctionData methodData)
		{
			foreach (var reference in methodData.MethodReferences)
			{
				methodData.MethodReferenceData.TryAdd(await AnalyzeMethodReference(documentData, methodData, reference).ConfigureAwait(false));
			}
		}

		//private async Task AnalyzeFunctionData(DocumentData documentData, FunctionData functionData)
		//{
		//	foreach (var reference in functionData.MethodReferences)
		//	{
		//		functionData.MethodReferenceData.TryAdd(await AnalyzeMethodReference(documentData, functionData, reference).ConfigureAwait(false));
		//	}
		//}

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
			var refFunctionData = await ProjectData.GetAnonymousFunctionOrMethodData(refMethodSymbol).ConfigureAwait(false);
			var refData = new FunctionReferenceData(functionData, reference, nameNode, refMethodSymbol, refFunctionData)
			{
				CanBeAsync = true
			};

			// Find the actual usage of the method
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
						AnalyzeInvocationExpression(documentData, (InvocationExpressionSyntax)currNode, refData);
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
			refData.ReferenceKind = currNode.Kind();
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

		private void AnalyzeInvocationExpression(DocumentData documentData, InvocationExpressionSyntax node, FunctionReferenceData functionReferenceData)
		{
			var functionData = functionReferenceData.FunctionData;
			var methodSymbol = functionReferenceData.ReferenceSymbol;
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
			var searchOptions = Default;
			if (_configuration.UseCancellationTokenOverload)
			{
				searchOptions |= HasCancellationToken;
			}
			functionReferenceData.ReferenceAsyncSymbols = new HashSet<IMethodSymbol>(GetAsyncCounterparts(methodSymbol.OriginalDefinition, searchOptions));
			if (functionReferenceData.ReferenceAsyncSymbols.Any())
			{
				if (functionReferenceData.ReferenceAsyncSymbols.All(o => o.ReturnsVoid || o.ReturnType.Name != "Task"))
				{
					functionReferenceData.CanBeAwaited = false;
					Logger.Info($"Cannot await method that is either void or do not return a Task:\r\n{methodSymbol}\r\n");
				}
			}
			// If the invocation returns a Task then we need to analyze it further to see how the Task is handled
			if (methodSymbol.ReturnType.Name == nameof(Task) && methodSymbol.ReturnType.ContainingNamespace.ToString() == "System.Threading.Tasks")
			{
				var retrunType = (INamedTypeSymbol) methodSymbol.ReturnType;
				var canBeAwaited = false;
				var currNode = node.Parent;
				while (true)
				{
					var memberExpression = currNode as MemberAccessExpressionSyntax;
					if (memberExpression == null)
					{
						break;
					}
					var memberName = memberExpression.Name.ToString();
					if (retrunType.IsGenericType && memberName == "Result")
					{
						canBeAwaited = true;
						break;
					}
					if (memberName == "ConfigureAwait")
					{
						var invocationNode = currNode.Parent as InvocationExpressionSyntax;
						if (invocationNode != null)
						{
							functionReferenceData.ConfigureAwait =
								invocationNode.ArgumentList.Arguments.First().Expression.IsKind(SyntaxKind.TrueLiteralExpression);
							currNode = invocationNode.Parent;
							continue;
						}
						break;
					}
					if (memberName == "GetAwaiter")
					{
						var invocationNode = currNode.Parent as InvocationExpressionSyntax;
						if (invocationNode != null)
						{
							currNode = invocationNode.Parent;
							continue;
						}
						break;
					}
					if (_taskResultMethods.Contains(memberName))
					{
						var invocationNode = currNode.Parent as InvocationExpressionSyntax;
						if (invocationNode != null)
						{
							canBeAwaited = true;
						}
					}
					break;
				}
				if (!canBeAwaited)
				{
					functionReferenceData.CanBeAwaited = false;
					Logger.Info($"Cannot await invocation of a method that returns a Task without be synchronously awaited:\r\n{methodSymbol}\r\n");
				}
			}


			// Set CancellationTokenRequired if we detect that one of the async counterparts has a cancellation token as a parameter
			if (_configuration.UseCancellationTokenOverload &&
			    functionReferenceData.ReferenceAsyncSymbols.Any(o => o.Parameters.Length > methodSymbol.Parameters.Length))
			{
				functionReferenceData.CancellationTokenRequired = true;
			}

			// If we are dealing with an external method and there are no async counterparts for it, we cannot convert it to async
			if (!functionReferenceData.ReferenceAsyncSymbols.Any() && !ProjectData.Contains(functionReferenceData.ReferenceSymbol))
			{
				functionReferenceData.CanBeAsync = false;
				Logger.Warn($"Method {methodSymbol} can not be async as there is no async counterparts for it");
				return;
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

			foreach (var analyzer in _configuration.InvocationExpressionAnalyzers)
			{
				analyzer.Analyze(node, functionReferenceData, documentData.SemanticModel);
			}

			// Propagate CancellationTokenRequired to the method data only if the invocation can be async 
			if (functionReferenceData.CancellationTokenRequired && functionReferenceData.GetConversion() == FunctionReferenceDataConversion.ToAsync)
			{
				// We need to set CancellationTokenRequired to true for the method that contains this invocation
				var methodData = functionReferenceData.FunctionData.GetMethodData();
				methodData.CancellationTokenRequired = true;
			}
		}

		private void AnalyzeArgumentExpression(SyntaxNode node, SimpleNameSyntax nameNode, FunctionReferenceData result)
		{
			//result.PassedAsArgument = true;
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
	}
}
