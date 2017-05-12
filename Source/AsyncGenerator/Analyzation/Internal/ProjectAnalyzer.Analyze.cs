using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AsyncGenerator.Extensions;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Analyzation.Internal
{
	internal partial class ProjectAnalyzer
	{
		private readonly string[] _taskResultMethods = {"Wait", "GetResult", "RunSynchronously"};

		private void AnalyzeDocumentData(DocumentData documentData)
		{
			foreach (var typeData in documentData.GetAllTypeDatas(o => o.Conversion != TypeConversion.Ignore))
			{
				foreach (var methodData in typeData.Methods.Values
					.Where(o => o.Conversion != MethodConversion.Ignore))
				{
					AnalyzeMethodData(documentData, methodData);
					foreach (var functionData in methodData.GetDescendantsChildFunctions(o => o.Conversion != MethodConversion.Ignore))
					{
						AnalyzeAnonymousFunctionData(documentData, functionData);
					}
				}
			}
		}

		private void AnalyzeMethodData(DocumentData documentData, MethodData methodData)
		{
			var methodBody = methodData.GetBodyNode();
			methodData.RewriteYields = methodBody?.DescendantNodes().OfType<YieldStatementSyntax>().Any() == true;
			methodData.MustRunSynchronized = methodData.Symbol.GetAttributes()
				.Where(o => o.AttributeClass.Name == "MethodImplAttribute")
				.Any(o => ((MethodImplOptions)(int)o.ConstructorArguments.First().Value).HasFlag(MethodImplOptions.Synchronized));
			if (methodBody == null)
			{
				methodData.OmitAsync = true;
			}

			foreach (var reference in methodData.BodyMethodReferences)
			{
				AnalyzeMethodReference(documentData, reference);
			}

			foreach (var reference in methodData.CrefMethodReferences)
			{
				AnalyzeCrefMethodReference(documentData, methodData, reference);
			}

			if (methodData.Conversion == MethodConversion.ToAsync)
			{
				return;
			}

			// If a method is never invoked and there is no invocations inside the method body that can be async and there is no related methods we can ignore it 
			if (!methodData.InvokedBy.Any() && methodData.BodyMethodReferences.All(o => o.Ignore) && !methodData.RelatedMethods.Any())
			{
				// If we have to create a new type we need to consider also the external related methods
				if (methodData.TypeData.Conversion != TypeConversion.NewType || !methodData.ExternalRelatedMethods.Any())
				{
					methodData.Conversion = MethodConversion.Ignore;
					methodData.Ignore("Method is never used and has no async invocations");
					LogIgnoredReason(methodData);
					return;
				}

			}
		}

		private void AnalyzeAnonymousFunctionData(DocumentData documentData, FunctionData methodData)
		{
			foreach (var reference in methodData.BodyMethodReferences)
			{
				AnalyzeMethodReference(documentData, reference);
			}
			methodData.RewriteYields = methodData.GetBodyNode()?.DescendantNodes().OfType<YieldStatementSyntax>().Any() == true;
		}

		private void AnalyzeCrefMethodReference(DocumentData documentData, MethodData methoData, CrefFunctionReferenceData crefData)
		{
			crefData.RelatedBodyFunctionReferences.AddRange(
				methoData.BodyMethodReferences.Where(o => o.ReferenceSymbol.Equals(crefData.ReferenceSymbol)));
		}

		private void AnalyzeMethodReference(DocumentData documentData, BodyFunctionReferenceData refData)
		{
			var nameNode = refData.ReferenceNameNode;

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
						refData.Ignore = true;
						Logger.Warn($"Cannot attach an async method to an event (void async is not an option as cannot be awaited):\r\n{nameNode.Parent}\r\n");
						break;
					case SyntaxKind.VariableDeclaration:
						refData.Ignore = true;
						Logger.Warn($"Assigning async method to a variable is not supported:\r\n{nameNode.Parent}\r\n");
						break;
					case SyntaxKind.CastExpression:
						refData.AwaitInvocation = true;
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
			refData.ReferenceNode = currNode;
			if (!refData.AwaitInvocation.HasValue)
			{
				refData.AwaitInvocation = !refData.Ignore;
			}
		}

		private void AnalyzeInvocationExpression(DocumentData documentData, InvocationExpressionSyntax node, BodyFunctionReferenceData functionReferenceData)
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
				functionReferenceData.Ignore = true;
				Logger.Warn($"Cannot await async method in a query expression:\r\n{queryExpression}\r\n");
				return;
			}

			var searchOptions = AsyncCounterpartsSearchOptions.Default;
			var useTokens = _configuration.UseCancellationTokens | _configuration.ScanForMissingAsyncMembers != null;
			if (useTokens)
			{
				searchOptions |= AsyncCounterpartsSearchOptions.HasCancellationToken;
			}
			functionReferenceData.ReferenceAsyncSymbols = new HashSet<IMethodSymbol>(GetAsyncCounterparts(methodSymbol.OriginalDefinition, searchOptions));
			if (functionReferenceData.ReferenceAsyncSymbols.Any())
			{
				if (functionReferenceData.ReferenceAsyncSymbols.All(o => o.ReturnsVoid || !o.ReturnType.IsTaskType()))
				{
					functionReferenceData.AwaitInvocation = false;
					Logger.Info($"Cannot await method that is either void or do not return a Task:\r\n{methodSymbol}\r\n");
				}
				// Set CancellationTokenRequired if we detect that one of the async counterparts has a cancellation token as a parameter
				if (useTokens)
				{
					var tokenOverload = functionReferenceData.ReferenceAsyncSymbols
						.SingleOrDefault(o => o.Parameters.Length > methodSymbol.Parameters.Length); // TODO: select the right one if we have more than one
					if (tokenOverload != null)
					{
						functionReferenceData.CancellationTokenRequired = true;
						functionReferenceData.AsyncCounterpartSymbol = tokenOverload;
						functionReferenceData.AsyncCounterpartName = tokenOverload.Name;
					}
				}
				if (functionReferenceData.AsyncCounterpartSymbol == null)
				{
					var nameGroups = functionReferenceData.ReferenceAsyncSymbols.GroupBy(o => o.Name).ToList();
					if (nameGroups.Count == 1)
					{
						functionReferenceData.AsyncCounterpartName = nameGroups[0].Key;
						functionReferenceData.AsyncCounterpartSymbol = nameGroups[0].Single(); // TODO: select the right one
					}
					else
					{
						// TODO: select the right one
					}
				}
			}
			else if (!ProjectData.Contains(methodSymbol))
			{
				// If we are dealing with an external method and there are no async counterparts for it, we cannot convert it to async
				functionReferenceData.Ignore = true;
				Logger.Info($"Method {methodSymbol} can not be async as there is no async counterparts for it");
				return;
			}
			else if(functionReferenceData.ReferenceFunctionData != null)
			{
				functionReferenceData.AsyncCounterpartName = methodSymbol.Name + "Async";
				functionReferenceData.AsyncCounterpartSymbol = methodSymbol;
			}

			// If the invocation returns a Task then we need to analyze it further to see how the Task is handled
			if (methodSymbol.ReturnType.IsTaskType())
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
							functionReferenceData.ConfigureAwaitParameter = invocationNode.ArgumentList.Arguments.First().Expression;
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
					functionReferenceData.AwaitInvocation = false;
					Logger.Info(
						$"Cannot await invocation of a method that returns a Task without be synchronously awaited:\r\n{methodSymbol}\r\n");
				}
				else
				{
					functionReferenceData.SynchronouslyAwaited = true;
				}
			}

			CalculateLastInvocation(node, functionReferenceData);

			foreach (var analyzer in _configuration.InvocationExpressionAnalyzers)
			{
				analyzer.Analyze(node, functionReferenceData, documentData.SemanticModel);
			}

			// Propagate CancellationTokenRequired to the method data only if the invocation can be async 
			if (functionReferenceData.CancellationTokenRequired && functionReferenceData.GetConversion() == ReferenceConversion.ToAsync)
			{
				// We need to set CancellationTokenRequired to true for the method that contains this invocation
				var methodData = functionData.GetMethodData();
				methodData.CancellationTokenRequired = true;
			}
		}

		private void CalculateLastInvocation(InvocationExpressionSyntax node, BodyFunctionReferenceData functionReferenceData)
		{
			var functionData = functionReferenceData.FunctionData;
			var methodSymbol = functionReferenceData.ReferenceSymbol;
			var functionBodyNode = functionData.GetBodyNode();
			if (functionBodyNode == null)
			{
				return;
			}
			// Check if the invocation node is returned in an expression body
			if (node.Parent.Equals(functionBodyNode) || //eg. bool ExpressionReturn() => SimpleFile.Write();
			    node.Equals(functionBodyNode) // eg. Func<bool> fn = () => SimpleFile.Write();
			)
			{
				functionReferenceData.LastInvocation = true;
				functionReferenceData.UseAsReturnValue = !methodSymbol.ReturnsVoid;
				return;
			}
			if (!functionBodyNode.IsKind(SyntaxKind.Block))
			{
				return; // The invocation node is inside an expression body but is not the last statement
			}
			// Check if the invocation is the last statement to be executed inside the method
			SyntaxNode currNode = node;
			StatementSyntax statement = null;
			while (!currNode.Equals(functionBodyNode))
			{
				currNode = currNode.Parent;
				switch (currNode.Kind())
				{
					case SyntaxKind.ReturnStatement:
						functionReferenceData.LastInvocation = true;
						functionReferenceData.UseAsReturnValue = true;
						return;
					case SyntaxKind.ConditionalExpression: // return num > 5 ? SimpleFile.Write() : false 
						var conditionExpression = (ConditionalExpressionSyntax)currNode;
						if (conditionExpression.Condition.Contains(node))
						{
							return;
						}
						continue;
					case SyntaxKind.IfStatement:
						var ifStatement = (IfStatementSyntax) currNode;
						if (ifStatement.Condition.Contains(node))
						{
							return;
						}
						statement = (StatementSyntax)currNode;
						continue;
					case SyntaxKind.ElseClause:
						continue;
					case SyntaxKind.ExpressionStatement:
						statement = (StatementSyntax) currNode;
						continue;
					case SyntaxKind.Block:
						if (statement == null)
						{
							return;
						}
						// We need to check that the current statement is the last block statement
						var block = (BlockSyntax) currNode;
						if (!statement.Equals(block.Statements.Last()))
						{
							return;
						}
						statement = block;
						continue;
					default:
						return;
				}
			}
			functionReferenceData.LastInvocation = true;
			functionReferenceData.UseAsReturnValue = !methodSymbol.ReturnsVoid; // here we don't now if the method will be converted to async or not
		}

		private void AnalyzeArgumentExpression(SyntaxNode node, SimpleNameSyntax nameNode, BodyFunctionReferenceData result)
		{
			var documentData = result.FunctionData.TypeData.NamespaceData.DocumentData;
			var methodArgTypeInfo = documentData.SemanticModel.GetTypeInfo(nameNode);
			if (methodArgTypeInfo.ConvertedType?.TypeKind != TypeKind.Delegate)
			{
				// TODO: debug and document
				return;
			}

			var delegateMethod = (IMethodSymbol)methodArgTypeInfo.ConvertedType.GetMembers("Invoke").First();

			if (!delegateMethod.IsAsync)
			{
				result.Ignore = true;
				Logger.Warn($"Cannot pass an async method as parameter to a non async Delegate method:\r\n{delegateMethod}\r\n");
			}
			else
			{
				var argumentMethodSymbol = (IMethodSymbol)documentData.SemanticModel.GetSymbolInfo(nameNode).Symbol;
				if (!argumentMethodSymbol.ReturnType.Equals(delegateMethod.ReturnType)) // i.e IList<T> -> IEnumerable<T>
				{
					result.AwaitInvocation = true;
				}
			}
		}
	}
}
