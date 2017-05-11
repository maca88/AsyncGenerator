using System.Collections.Generic;
using System.Linq;
using AsyncGenerator.Extensions;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Analyzation.Internal
{
	internal partial class ProjectAnalyzer
	{
		/// <summary>
		/// Set all method data dependencies to be also async
		/// </summary>
		/// <param name="asyncMethodData">Method data that is marked to be async</param>
		/// <param name="toProcessMethodData">All method data that needs to be processed</param>
		private void PostAnalyzeAsyncMethodData(MethodData asyncMethodData, ISet<MethodData> toProcessMethodData)
		{
			if (!toProcessMethodData.Contains(asyncMethodData))
			{
				return;
			}
			var processingMetodData = new Queue<MethodData>();
			processingMetodData.Enqueue(asyncMethodData);
			while (processingMetodData.Any())
			{
				var currentMethodData = processingMetodData.Dequeue();
				toProcessMethodData.Remove(currentMethodData);

				if (_configuration.UseCancellationTokens)
				{
					// Permit the consumer to decide require the cancellation parameter
					currentMethodData.CancellationTokenRequired =
						_configuration.CancellationTokens.RequireCancellationToken(currentMethodData.Symbol) ??
						currentMethodData.CancellationTokenRequired;
				}
				if (currentMethodData.CancellationTokenRequired)
				{
					currentMethodData.MethodCancellationToken = _configuration.CancellationTokens.MethodGeneration(currentMethodData);
					currentMethodData.AddCancellationTokenGuards = _configuration.CancellationTokens.Guards;
				}

				foreach (var depFunctionData in currentMethodData.Dependencies)
				{
					var depMethodData = depFunctionData as MethodData;
					var bodyReferences = depFunctionData.BodyMethodReferences.Where(o => o.ReferenceFunctionData == currentMethodData).ToList();
					if (depMethodData != null)
					{
						// Before setting the dependency to async we need to check if there is at least one invocation that will be converted to async
						// Here we also need to consider that a method can be a dependency because is a related method
						if (depMethodData.RelatedMethods.All(o => o != currentMethodData) && bodyReferences.All(o => o.GetConversion() == ReferenceConversion.Ignore))
						{
							continue;
						}

						if (!toProcessMethodData.Contains(depMethodData))
						{
							continue;
						}
						processingMetodData.Enqueue(depMethodData);
					}
					if (depFunctionData.Conversion == MethodConversion.Ignore)
					{
						Logger.Warn($"Ignored method {depFunctionData.Symbol} has a method invocation that can be async");
						continue;
					}
					depFunctionData.Conversion = MethodConversion.ToAsync;

					if (!currentMethodData.CancellationTokenRequired)
					{
						continue;
					}
					// Propagate the CancellationTokenRequired for the dependency method data
					if (depMethodData != null)
					{
						depMethodData.CancellationTokenRequired |= currentMethodData.CancellationTokenRequired;
					}
				}
			}
		}

		/// <summary>
		/// Skip wrapping a method into a try/catch only when we have one statement (except preconditions) that is an invocation
		/// which returns a Task. This statement must have only one invocation.
		/// </summary>
		/// <param name="methodData"></param>
		private void CalculateWrapInTryCatch(MethodData methodData)
		{
			var methodDataBody = methodData.Node.Body;
			if (methodDataBody == null || !methodDataBody.Statements.Any() || methodData.SplitTail)
			{
				return;
			}
			if (methodDataBody.Statements.Count != methodData.Preconditions.Count + 1)
			{
				methodData.WrapInTryCatch = true;
				return;
			}
			// Do not look into child functions
			var statements = methodDataBody.Statements
				.First(o => !methodData.Preconditions.Contains(o))
				.DescendantNodesAndSelf(o => !o.IsFunction())
				.OfType<StatementSyntax>()
				.ToList();
			if (statements.Count != 1)
			{
				methodData.WrapInTryCatch = true;
				return;
			}
			var lastStatement = statements[0];
			var invocationExps = lastStatement?.DescendantNodes(o => !o.IsFunction()).OfType<InvocationExpressionSyntax>().ToList();
			if (invocationExps?.Count != 1)
			{
				methodData.WrapInTryCatch = true;
				return;
			}
			var invocationExpr = invocationExps[0];
			var refData = methodData.BodyMethodReferences.FirstOrDefault(o => o.ReferenceNode == invocationExpr);
			if (refData == null)
			{
				methodData.WrapInTryCatch = true;
				return;
			}
			if (refData.GetConversion() == ReferenceConversion.Ignore || refData.ReferenceAsyncSymbols.Any(o => o.ReturnsVoid || !o.ReturnType.IsTaskType()))
			{
				methodData.WrapInTryCatch = true;
			}
		}

		/// <summary>
		/// Calculates the final conversion for all currently not ignored method/type/namespace data
		/// </summary>
		/// <param name="documentData">All project documents</param>
		private void PostAnalyze(IEnumerable<DocumentData> documentData)
		{
			var allNamespaceData = documentData
				.SelectMany(o => o.GetAllNamespaceDatas(m => m.Conversion != NamespaceConversion.Ignore))
				.ToList();

			// We need to take care of explictly ignored methods as we have to implicitly ignore also the related methods.
			// Here the conversion for the type is not yet calculated so we have to process all types
			foreach (var methodData in allNamespaceData
				.SelectMany(o => o.Types.Values)
				.SelectMany(o => o.GetSelfAndDescendantsTypeData())
				.SelectMany(o => o.Methods.Values.Where(m => m.ExplicitlyIgnored)))
			{
				// TODO: what to do if an abstract method is explictly ignored, should we implicitly ignore all overrides or just remove the override keyword?.
				// TODO: if an override implements an interface that is not ignored, we would need to remove the override method and not ignore it
				if (!methodData.InterfaceMethod)
				{
					continue;
				}
				// If an interface method is explictly ignored then we need to implicitly ignore the related methods, 
				// but only if a related method implements just the ignored one.
				// TODO: remove override keyword if exist for the non removed related methods
				foreach (var relatedMethodData in methodData.RelatedMethods.Where(i => !i.RelatedMethods.Any(r => r != methodData && r.InterfaceMethod)))
				{
					relatedMethodData.Ignore($"Implicitly ignored because of the explictly ignored method {methodData.Symbol}");
					WarnLogIgnoredReason(relatedMethodData);
				}
			}

			// If a type data is ignored then also its method data are ignored
			var allTypeData = allNamespaceData
				.SelectMany(o => o.Types.Values)
				.SelectMany(o => o.GetSelfAndDescendantsTypeData(t => t.Conversion != TypeConversion.Ignore))
				//.Where(o => o.Conversion != TypeConversion.Ignore)
				.ToList();
			// TODO: nested functions
			var toProcessMethodData = new HashSet<MethodData>(allTypeData
				.SelectMany(o => o.Methods.Values.Where(m => m.Conversion != MethodConversion.Ignore)));
			//TODO: optimize steps for better performance

			// 0. Step - If cancellation tokens are enabled we should start from methods that requires a cancellation token in order to correctly propagate CancellationTokenRequired
			// to dependency methods
			if (_configuration.UseCancellationTokens)
			{
				var tokenMethodDatas = toProcessMethodData.Where(o => o.CancellationTokenRequired).ToList();
				foreach (var tokenMethodData in tokenMethodDatas)
				{
					if (toProcessMethodData.Count == 0)
					{
						break;
					}
					tokenMethodData.Conversion = MethodConversion.ToAsync;
					PostAnalyzeAsyncMethodData(tokenMethodData, toProcessMethodData);
				}
			}
			
			// 1. Step - Go through all async methods and set their dependencies to be also async
			// TODO: should we start from the bottom/leaf method that is async? how do we know if the method is a leaf (consider circular calls)?
			var asyncMethodDatas = toProcessMethodData.Where(o => o.Conversion == MethodConversion.ToAsync).ToList();
			foreach (var asyncMethodData in asyncMethodDatas)
			{
				if (toProcessMethodData.Count == 0)
				{
					break;
				}
				PostAnalyzeAsyncMethodData(asyncMethodData, toProcessMethodData);
			}

			// 2. Step - Go through remaining methods and set them to be async if there is at least one method invocation that will get converted
			// TODO: should we start from the bottom/leaf method that is async? how do we know if the method is a leaf (consider circular calls)?
			var remainingMethodData = toProcessMethodData.ToList();
			foreach (var methodData in remainingMethodData)
			{
				if (methodData.BodyMethodReferences.Any(o => o.GetConversion() == ReferenceConversion.ToAsync))
				{
					if (methodData.Conversion == MethodConversion.Ignore)
					{
						Logger.Warn($"Ignored method {methodData.Symbol} has a method invocation that can be async");
						continue;
					}
					methodData.Conversion = MethodConversion.ToAsync;
					// Set all dependencies to be async for the newly discovered async method
					PostAnalyzeAsyncMethodData(methodData, toProcessMethodData);
					if (toProcessMethodData.Count == 0)
					{
						break;
					}
				}
			}

			// 3. Step - Mark all remaining method to be ignored
			foreach (var methodData in toProcessMethodData)
			{
				methodData.Ignore("Method is never used.");
				LogIgnoredReason(methodData);
			}

			// Update CancellationTokenRequired for all body function references that requires a cancellation token
			if (_configuration.UseCancellationTokens)
			{
				foreach (var methodData in allTypeData
					.SelectMany(o => o.Methods.Values.Where(m => m.Conversion != MethodConversion.Ignore)))
				{
					foreach (var functionRefData in methodData.BodyMethodReferences
						.Where(o => o.ReferenceFunctionData != null && o.ReferenceFunctionData.Conversion == MethodConversion.ToAsync &&
									o.ReferenceFunctionData.GetMethodData().CancellationTokenRequired))
					{
						functionRefData.CancellationTokenRequired = true;
					}
				}
			}

			// 4. Step - Calculate the final type conversion
			foreach (var typeData in allTypeData)
			{
				if (typeData.Conversion != TypeConversion.Unknown)
				{
					continue;
				}
				// A type can be ignored only if it has no async methods that will get converted
				if (typeData.GetSelfAndDescendantsTypeData().All(t => t.Methods.Values.All(o => o.Conversion == MethodConversion.Ignore)))
				{
					typeData.Conversion = TypeConversion.Ignore;
				}
				else
				{
					typeData.Conversion = TypeConversion.Partial;
				}
			}

			// 5. Step - Calculate the final namespace conversion
			foreach (var namespaceData in allNamespaceData)
			{
				if (namespaceData.Conversion != NamespaceConversion.Unknown)
				{
					continue;
				}
				// A type can be ignored only if it has no async methods that will get converted
				if (namespaceData.GetSelfAndDescendantsNamespaceData().All(t => t.Types.Values.All(o => o.Conversion == TypeConversion.Ignore)))
				{
					namespaceData.Conversion = NamespaceConversion.Ignore;
				}
				else
				{
					namespaceData.Conversion = NamespaceConversion.Generate;
				}
			}

			// 6. Step - For all async methods check for preconditions. Search only statements that its end location is lower that the first async method reference
			foreach (var methodData in allTypeData.Where(o => o.Conversion != TypeConversion.Ignore)
				.SelectMany(o => o.Methods.Values.Where(m => m.Conversion != MethodConversion.Ignore)))
			{
				if (methodData.GetBodyNode() == null)
				{
					continue;
				}

				var asyncMethodReferences = methodData.BodyMethodReferences
					.Where(o => o.GetConversion() == ReferenceConversion.ToAsync)
					.ToList();
				// Calculate the final reference AwaitInvocation, we can skip await if all async invocations are returned and the return type matches
				// or we have only one async invocation that is the last to be invoked
				// Invocations in synchronized methods must be awaited to mimic the same behavior as their sync counterparts
				if (!methodData.MustRunSynchronized)
				{
					var canSkipAwaits = true;
					foreach (var methodReference in methodData.BodyMethodReferences)
					{
						if (methodReference.GetConversion() == ReferenceConversion.Ignore)
						{
							methodReference.AwaitInvocation = false;
							continue;
						}

						if (!methodReference.UseAsReturnValue && !methodReference.LastInvocation)
						{
							canSkipAwaits = false;
							break;
						}
						var functionData = methodReference.FunctionData;

						if (methodReference.LastInvocation && functionData.Symbol.ReturnsVoid && (
							    (methodReference.ReferenceAsyncSymbols.Any() && methodReference.ReferenceAsyncSymbols.All(o => o.ReturnType.IsTaskType())) ||
							    methodReference.ReferenceFunctionData?.Conversion == MethodConversion.ToAsync
						    ))
						{
							continue;
						}

						var isReturnTypeTask = methodReference.ReferenceSymbol.ReturnType.IsTaskType();
						// We need to check the return value of the async counterpart
						// eg. Task<IList<string>> to Task<IEnumerable<string>>, Task<long> -> Task<int> are not valid
						// eg. Task<int> to Task is valid
						if (!isReturnTypeTask &&
						    (
							    (
								    methodReference.ReferenceAsyncSymbols.Any() &&
								    !methodReference.ReferenceAsyncSymbols.All(o =>
								    {
									    var returnType = o.ReturnType as INamedTypeSymbol;
									    if (returnType == null || !returnType.IsGenericType)
									    {
										    return o.ReturnType.IsAwaitRequired(functionData.Symbol.ReturnType);
									    }
									    return returnType.TypeArguments.First().IsAwaitRequired(functionData.Symbol.ReturnType);
								    })
							    ) ||
							    (
								    methodReference.ReferenceFunctionData != null &&
								    !methodReference.ReferenceFunctionData.Symbol.ReturnType.IsAwaitRequired(functionData.Symbol.ReturnType)
							    )
						    )
						)
						{
							canSkipAwaits = false;
							break;
						}
					}
					if (canSkipAwaits)
					{
						foreach (var methodReference in asyncMethodReferences)
						{
							methodReference.AwaitInvocation = false;
							methodReference.UseAsReturnValue = true;
						}
					}
				}

				// If the method has a block body
				if (methodData.Node.Body != null)
				{
					// Some async methods may not have any async invocations because were forced to be async (overloads)
					var methodRefSpan = asyncMethodReferences
						.Select(o => o.ReferenceLocation.Location)
						.OrderBy(o => o.SourceSpan.Start)
						.FirstOrDefault();
					var semanticModel = methodData.TypeData.NamespaceData.DocumentData.SemanticModel;
					// Search for preconditions until a statement has not been qualified as a precondition or we encounter an async invocation
					// The faulted property is set to true when the first statement is a throw statement
					foreach (var statement in methodData.Node.Body.Statements)
					{
						if (methodRefSpan != null && statement.Span.End > methodRefSpan.SourceSpan.Start)
						{
							break;
						}
						if (!_configuration.PreconditionCheckers.Any(o => o.IsPrecondition(statement, semanticModel)))
						{
							methodData.Faulted = statement.IsKind(SyntaxKind.ThrowStatement);
							break;
						}
						methodData.Preconditions.Add(statement);
					}

					// A method shall be tail splitted when has at least one precondition and there is at least one awaitable invocation
					if (methodData.Preconditions.Any() && methodData.BodyMethodReferences.Any(o => o.AwaitInvocation == true))
					{
						methodData.SplitTail = true;
					}
				}
				else
				{
					methodData.Faulted = methodData.Node.ExpressionBody.IsKind(SyntaxKind.ThrowExpression);
				}

				// The async keyword shall be omitted when the method does not have any awaitable invocation or we have to tail split
				if (methodData.SplitTail || !methodData.BodyMethodReferences.Any(o => o.GetConversion() == ReferenceConversion.ToAsync && o.AwaitInvocation == true))
				{
					methodData.OmitAsync = true;
				}
				// When the async keyword is omitted and the method is not faulted we need to calculate if the method body shall be wrapped in a try/catch block
				if (!methodData.Faulted && methodData.OmitAsync)
				{
					// For sync forwarding we will always wrap into try catch
					if (methodData.BodyMethodReferences.All(o => o.GetConversion() == ReferenceConversion.Ignore) && _configuration.CallForwarding(methodData.Symbol))
					{
						methodData.WrapInTryCatch = true;
						methodData.ForwardCall = true;
					}
					else
					{
						CalculateWrapInTryCatch(methodData);
					}
					
				}
				
			}
		}
	}
}
