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
				foreach (var methodData in typeData.Methods.Values.Where(o => o.Conversion != MethodConversion.Ignore))
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
			if (methodData.Conversion == MethodConversion.Copy)
			{
				foreach (var bodyReference in methodData.BodyMethodReferences.Where(o => o.ReferenceFunctionData != null))
				{
					var invokedMethodData = bodyReference.ReferenceFunctionData;
					if (invokedMethodData.Conversion != MethodConversion.Ignore)
					{
						invokedMethodData.Copy(); // TODO: do we need to do this recursively?
					}
				}
				return; // We do not want to analyze method that will be only copied
			}
			
			// If all abstract/virtual related methods are ignored then ignore also this one (IsAbstract includes also interface members)
			var baseMethods = methodData.RelatedMethods.Where(o => o.Symbol.IsAbstract || o.Symbol.IsVirtual).ToList();
			if (!methodData.Conversion.HasFlag(MethodConversion.ToAsync) && baseMethods.Any() && baseMethods.All(o => o.Conversion == MethodConversion.Ignore))
			{
				if (methodData.TypeData.GetSelfAndAncestorsTypeData()
					.Any(o => o.Conversion == TypeConversion.NewType || o.Conversion == TypeConversion.Copy))
				{
					methodData.Copy();
					// Check if there are any internal methods that are candidate to be async and are invoked inside this method
					// If there are, then we need to copy them
					foreach (var bodyReference in methodData.BodyMethodReferences.Where(o => o.ReferenceFunctionData != null))
					{
						var invokedMethodData = bodyReference.ReferenceFunctionData;
						if (invokedMethodData.Conversion != MethodConversion.Ignore)
						{
							invokedMethodData.Conversion |= MethodConversion.Copy;
						}
					}
				}
				else
				{
					methodData.Ignore("All abstract/virtual related methods are ignored");
				}
				return;
			}


			var methodBody = methodData.GetBodyNode();
			methodData.RewriteYields = methodBody?.DescendantNodes().OfType<YieldStatementSyntax>().Any() == true;
			methodData.MustRunSynchronized = methodData.Symbol.GetAttributes()
				.Where(o => o.AttributeClass.Name == "MethodImplAttribute")
				.Any(o => ((MethodImplOptions)(int)o.ConstructorArguments.First().Value).HasFlag(MethodImplOptions.Synchronized));
			if (methodBody == null)
			{
				methodData.OmitAsync = true;
			}

			// Order by descending so we are sure that methods passed by argument will be processed before the invoked method with those arguments
			foreach (var reference in methodData.BodyMethodReferences.OrderByDescending(o => o.ReferenceNameNode.SpanStart))
			{
				AnalyzeMethodReference(documentData, reference);
			}

			foreach (var reference in methodData.CrefMethodReferences)
			{
				AnalyzeCrefMethodReference(documentData, methodData, reference);
			}

			// Ignore all candidate arguments that are not an argument of an async invocation candidate
			foreach (var reference in methodData.BodyMethodReferences.Where(o => o.ReferenceNode.IsKind(SyntaxKind.Argument) && o.ArgumentOfFunctionInvocation == null))
			{
				reference.Ignore("The invoked method does not have an async counterpart");
			}

			if (methodData.Conversion.HasFlag(MethodConversion.ToAsync))
			{
				return;
			}

			// If a method is never invoked and there is no invocations inside the method body that can be async and there is no related methods we can ignore it.
			// Methods with Unknown may not have InvokedBy populated so we cannot ignore them here
			// Do not ignore methods that are inside a type with conversion NewType as ExternalRelatedMethods may not be populated
			if (
				!methodData.Dependencies.Any() && 
				methodData.BodyMethodReferences.All(o => o.Conversion == ReferenceConversion.Ignore) && 
				methodData.Conversion.HasFlag(MethodConversion.Smart) &&
			    methodData.TypeData.GetSelfAndAncestorsTypeData().All(o => o.Conversion != TypeConversion.NewType) &&
				!methodData.ExternalRelatedMethods.Any()
			)
			{
				methodData.Ignore("Method is never used and has no async invocations");
				LogIgnoredReason(methodData);
			}
		}

		private void AnalyzeAnonymousFunctionData(DocumentData documentData, ChildFunctionData functionData)
		{
			// Ignore if the anonymous function is passed as an argument to a non async candidate
			if (functionData.GetNode().Parent.IsKind(SyntaxKind.Argument) && functionData.ArgumentOfFunctionInvocation == null)
			{
				functionData.Ignore("Function is passed as an argument to a non async invocation");
			}

			// Order by descending so we are sure that methods passed by argument will be processed before the invoked method with those arguments
			foreach (var reference in functionData.BodyMethodReferences.OrderByDescending(o => o.ReferenceNameNode.SpanStart))
			{
				AnalyzeMethodReference(documentData, reference);
			}

			// Ignore all candidate arguments that are not an argument of an async invocation candidate
			foreach (var reference in functionData.BodyMethodReferences.Where(o => o.ReferenceNode.IsKind(SyntaxKind.Argument) && o.ArgumentOfFunctionInvocation == null))
			{
				reference.Ignore("The invoked method does not have an async counterpart");
			}

			functionData.RewriteYields = functionData.GetBodyNode()?.DescendantNodes().OfType<YieldStatementSyntax>().Any() == true;
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
						AnalyzeArgumentExpression((ArgumentSyntax)currNode, nameNode, refData);
						break;
					case SyntaxKind.AddAssignmentExpression:
						refData.Ignore($"Cannot attach an async method to an event (void async is not an option as cannot be awaited):\r\n{nameNode.Parent}\r\n");
						Logger.Warn(refData.IgnoredReason);
						break;
					case SyntaxKind.VariableDeclaration:
						refData.Ignore($"Assigning async method to a variable is not supported:\r\n{nameNode.Parent}\r\n");
						Logger.Warn(refData.IgnoredReason);
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
				refData.AwaitInvocation = refData.Conversion != ReferenceConversion.Ignore;
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
				functionReferenceData.Ignore($"Cannot await async method in a query expression:\r\n{queryExpression}\r\n");
				Logger.Warn(functionReferenceData.IgnoredReason);
				return;
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
					Logger.Info($"Cannot await invocation of a method that returns a Task without be synchronously awaited:\r\n{methodSymbol}\r\n");
				}
				else
				{
					functionReferenceData.SynchronouslyAwaited = true;
				}
			}

			FindAndSetAsyncCounterparts(functionReferenceData);

			for (var i = 0; i < node.ArgumentList.Arguments.Count; i++)
			{
				var argument = node.ArgumentList.Arguments[i];
				var argumentExpression = argument.Expression;
				// We have to process anonymous funcions as they will not be analyzed as arguments
				if (argumentExpression.IsFunction())
				{
					var anonFunction = (AnonymousFunctionData)functionData.ChildFunctions[argumentExpression];
					functionReferenceData.AddFunctionArgument(new FunctionArgumentData(anonFunction, i));
					anonFunction.ArgumentOfFunctionInvocation = functionReferenceData;
					continue;
				}
				if (argumentExpression.IsKind(SyntaxKind.IdentifierName) ||
				    argumentExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
				{
					var argRefFunction = functionData.BodyMethodReferences.FirstOrDefault(o => argument.Equals(o.ReferenceNode));
					if (argRefFunction == null)
					{
						// Ignore only if the async argument does not match
						// TODO: internal methods
						if (functionReferenceData.ReferenceFunctionData == null && functionReferenceData.AsyncCounterpartSymbol != null)
						{
							// If the invocation has at least one method argument that is not a candidate to be async we have to ignore it
							var argRefMethod = documentData.SemanticModel.GetSymbolInfo(argumentExpression).Symbol as IMethodSymbol;
							if (argRefMethod != null)
							{
								var paramDelegate = (IMethodSymbol)functionReferenceData.AsyncCounterpartSymbol.Parameters[i].Type.GetMembers("Invoke").First();
								// TODO: check parameters
								if (!paramDelegate.ReturnType.Equals(argRefMethod.ReturnType))
								{
									functionReferenceData.Ignore("One of the argument does not have an async counterpart");
									return;
								}
							}
						}

						
						continue;
					}
					functionReferenceData.AddFunctionArgument(new FunctionArgumentData(argRefFunction, i));
					argRefFunction.ArgumentOfFunctionInvocation = functionReferenceData;
				}
			}

			if (functionReferenceData.FunctionArguments != null && ProjectData.Contains(methodSymbol) && functionReferenceData.FunctionArguments.Any())
			{
				functionReferenceData.Ignore($"Internal invoked method {methodSymbol} contains at least one argument that is a delegate which is currently not supported");
				return;
			}

			CalculateLastInvocation(node, functionReferenceData);

			foreach (var analyzer in _configuration.InvocationExpressionAnalyzers)
			{
				analyzer.Analyze(node, functionReferenceData, documentData.SemanticModel);
			}

			PropagateCancellationToken(functionReferenceData);
		}

		/// <summary>
		/// Propagate CancellationTokenRequired to the method data only if the invocation can be async and the method does not have any external related methods (eg. external interface)
		/// </summary>
		private void PropagateCancellationToken(BodyFunctionReferenceData functionReferenceData)
		{
			if (functionReferenceData.CancellationTokenRequired && functionReferenceData.Conversion == ReferenceConversion.ToAsync)
			{
				// We need to set CancellationTokenRequired to true for the method that contains this invocation
				var methodData = functionReferenceData.FunctionData.GetMethodData();
				if (!methodData.ExternalRelatedMethods.Any())
				{
					methodData.CancellationTokenRequired = true;
				}
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
			if (functionReferenceData.ReferenceFunctionData == null && functionReferenceData.AsyncCounterpartSymbol != null)
			{
				functionReferenceData.UseAsReturnValue = functionReferenceData.AsyncCounterpartSymbol.ReturnType.IsTaskType();
			}
		}

		private void AnalyzeArgumentExpression(ArgumentSyntax node, SimpleNameSyntax nameNode, BodyFunctionReferenceData result)
		{
			var documentData = result.FunctionData.TypeData.NamespaceData.DocumentData;
			var methodArgTypeInfo = documentData.SemanticModel.GetTypeInfo(nameNode);
			if (methodArgTypeInfo.ConvertedType?.TypeKind != TypeKind.Delegate)
			{
				// TODO: debug and document
				return;
			}
			result.AwaitInvocation = false; // we cannot await something that is not invoked

			var delegateMethod = (IMethodSymbol)methodArgTypeInfo.ConvertedType.GetMembers("Invoke").First();
			if (!delegateMethod.IsAsync)
			{
				if (!FindAndSetAsyncCounterparts(result))
				{
					return;
				}
				PropagateCancellationToken(result);

				// Check if the method is passed as an argument to a candidate method
				//var invokedByMethod = result.FunctionData.BodyMethodReferences
				//	.Where(o => o.ReferenceNode.IsKind(SyntaxKind.InvocationExpression))
				//	.Select(o => new
				//	{
				//		Reference = o,
				//		Index = ((InvocationExpressionSyntax) o.ReferenceNode).ArgumentList.Arguments.IndexOf(node)
				//	})
				//	.FirstOrDefault(o => o.Index >= 0);
				//if (invokedByMethod == null)
				//{
				//	result.Ignore($"Cannot pass an async method as argument to a non async Delegate argument:\r\n{delegateMethod}\r\n");
				//	Logger.Warn(result.IgnoredReason);
				//	return;
				//}
				//invokedByMethod.Reference.FunctionArguments.Add(new FunctionArgumentData(result, invokedByMethod.Index));
				//result.ArgumentOfFunctionInvocation = invokedByMethod.Reference;

			}
			else
			{
				result.Ignore("The method is already used as async");

				//var argumentMethodSymbol = (IMethodSymbol)documentData.SemanticModel.GetSymbolInfo(nameNode).Symbol;
				//if (!argumentMethodSymbol.ReturnType.IsAwaitRequired(delegateMethod.ReturnType)) // i.e IList<T> -> IEnumerable<T>
				//{
				//	result.AwaitInvocation = true;
				//}
			}
		}

		private bool FindAndSetAsyncCounterparts(BodyFunctionReferenceData functionReferenceData)
		{
			var methodSymbol = functionReferenceData.ReferenceSymbol;

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
				functionReferenceData.Conversion = ReferenceConversion.ToAsync;
			}
			else if (!ProjectData.Contains(methodSymbol))
			{
				// If we are dealing with an external method and there are no async counterparts for it, we cannot convert it to async
				functionReferenceData.Ignore($"Method {methodSymbol} can not be async as there is no async counterparts for it");
				Logger.Info(functionReferenceData.IgnoredReason);
				return false;
			}
			else if (functionReferenceData.ReferenceFunctionData != null)
			{
				functionReferenceData.AsyncCounterpartName = methodSymbol.Name + "Async";
				functionReferenceData.AsyncCounterpartSymbol = methodSymbol;
				switch (functionReferenceData.Conversion)
				{
					case ReferenceConversion.ToAsync:
						functionReferenceData.Conversion = ReferenceConversion.ToAsync;
						break;
					case ReferenceConversion.Ignore:
						functionReferenceData.Conversion = ReferenceConversion.Ignore;
						break;
				}
				
			}
			return true;
		}
	}
}
