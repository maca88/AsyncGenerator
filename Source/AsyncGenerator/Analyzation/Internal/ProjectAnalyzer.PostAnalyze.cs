using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Extensions.Internal;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
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
		private void PostAnalyzeAsyncMethodData(MethodOrAccessorData asyncMethodData, ISet<MethodOrAccessorData> toProcessMethodData)
		{
			if (!toProcessMethodData.Contains(asyncMethodData))
			{
				return;
			}
			var processingMetodData = new Queue<MethodOrAccessorData>();
			processingMetodData.Enqueue(asyncMethodData);
			while (processingMetodData.Any())
			{
				var currentMethodData = processingMetodData.Dequeue();
				toProcessMethodData.Remove(currentMethodData);

				// Missing methods have already calculated the CancellationTokenRequired and MethodCancellationToken in the scanning step
				if (!currentMethodData.Missing && _configuration.UseCancellationTokens)
				{
					// Permit the consumer to decide require the cancellation parameter
					currentMethodData.CancellationTokenRequired =
						_configuration.CancellationTokens.RequiresCancellationToken(currentMethodData.Symbol) ??
						currentMethodData.CancellationTokenRequired || currentMethodData.RelatedMethods.Any(r => r.CancellationTokenRequired);
				}
				// TODO: support params
				if (currentMethodData.Symbol.Parameters.LastOrDefault()?.IsParams == true)
				{
					currentMethodData.CancellationTokenRequired = false;
					currentMethodData.AddDiagnostic(
						"Cancellation token parameter will not be added, because the last parameter is declared as an array which is currently not supported",
						DiagnosticSeverity.Hidden);
				}

				CalculatePreserveReturnType(currentMethodData);

				foreach (var depFunctionData in currentMethodData.Dependencies.Where(o => o.CanBeAsync))
				{
					var depMethodData = depFunctionData as MethodOrAccessorData;
					var bodyReferences = depFunctionData.BodyFunctionReferences.Where(o => o.ReferenceFunctionData == currentMethodData).ToList();

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
							currentMethodData.CancellationTokenRequired |= depMethodData.CancellationTokenRequired;
							continue;
						}
						processingMetodData.Enqueue(depMethodData);
					}
					if (depFunctionData.Conversion == MethodConversion.Ignore)
					{
						depFunctionData.AddDiagnostic("Has a method invocation that can be async", DiagnosticSeverity.Warning);
						continue;
					}
					depFunctionData.ToAsync();

					// Propagate the CancellationTokenRequired for the dependency method data or
					// from the dependency method to the current one
					if (depMethodData != null)
					{
						var requiresToken = _configuration.CancellationTokens.RequiresCancellationToken(depMethodData.Symbol);
						if (!requiresToken.HasValue)
						{
							depMethodData.CancellationTokenRequired |= currentMethodData.CancellationTokenRequired;
						}
						else
						{
							currentMethodData.CancellationTokenRequired |= requiresToken.Value;
						}
					}
					else if (depFunctionData is ChildFunctionData childFunction)
					{
						var methodOrAccessorData = childFunction.GetMethodOrAccessorData();
						var requiresToken = _configuration.CancellationTokens.RequiresCancellationToken(methodOrAccessorData.Symbol);
						if (requiresToken == null)
						{
							methodOrAccessorData.CancellationTokenRequired |= currentMethodData.CancellationTokenRequired;
						}
						else
						{
							currentMethodData.CancellationTokenRequired |= requiresToken.Value;
						}
					}
				}

				if (currentMethodData.CancellationTokenRequired)
				{
					if (!currentMethodData.Missing)
					{
						currentMethodData.MethodCancellationToken = _configuration.CancellationTokens.MethodGeneration(currentMethodData);
					}

					currentMethodData.AddCancellationTokenGuards = _configuration.CancellationTokens.Guards;
					foreach (var relatedMethod in currentMethodData.RelatedMethods)
					{
						relatedMethod.CancellationTokenRequired = true;
					}
				}
			}
		}

		private bool ShouldWrapInTryCatch(List<StatementSyntax> statements)
		{
			var totalInvocations = 0;
			for (var i = 0; i < statements.Count; i++)
			{
				var statement = statements[i];
				// Do not look into child functions
				foreach (var expression in statement.DescendantNodes(o => !o.IsFunction()).OfType<ExpressionSyntax>())
				{
					if (new[]
					{
						SyntaxKind.ElementAccessExpression,
						SyntaxKind.CastExpression,
						//SyntaxKind.SimpleMemberAccessExpression
					}.Contains(expression.Kind()))
					{
						return true;
					}
					if (expression is InvocationExpressionSyntax invocation && 
						invocation.Expression.ToString() != "nameof")
					{
						if (i != statements.Count - 1)
						{
							return true;
						}
						totalInvocations++;
					}
					/*else if (expression is ObjectCreationExpressionSyntax objectCreation &&
						objectCreation.ArgumentList?.Arguments.Count > 0)
					{
						return true;
					}*/
				}
			}
			return totalInvocations > 1;
		}

		/// <summary>
		/// Skip wrapping a method into a try/catch only when we have one statement (except preconditions) that has one invocation
		/// which is the last statement that returns a Task.
		/// </summary>
		private void CalculateWrapInTryCatch(FunctionData functionData)
		{
			if (!(functionData.GetBodyNode() is BlockSyntax functionDataBody) || 
				!functionDataBody.Statements.Any() || 
				functionData.SplitTail ||
				functionData.WrapInTryCatch)
			{
				return;
			}

			foreach (var handler in _configuration.MethodExceptionHandlers)
			{
				var result = handler.CatchMethodBody(functionData.Symbol, functionData.ArgumentOfFunctionInvocation?.ReferenceSymbol);
				if (!result.HasValue)
				{
					continue;
				}
				functionData.WrapInTryCatch = result.Value;
				return;
			}

			var customResult = _configuration.ExceptionHandling.CatchFunctionBody?.Invoke(functionData.Symbol);
			if (customResult.HasValue)
			{
				functionData.WrapInTryCatch = customResult.Value;
				return;
			}

			var lastPrecondition = functionData.Preconditions.LastOrDefault();
			if ((lastPrecondition == null && functionData.CatchPropertyGetterCalls.Count > 0) || 
				(lastPrecondition != null && functionData.CatchPropertyGetterCalls.Any(o => o.SpanStart > lastPrecondition.Span.End)))
			{
				functionData.WrapInTryCatch = true;
				return;
			}
			
			var statements = functionDataBody.Statements
				.Where(o => !functionData.Preconditions.Contains(o))
				.ToList();
			if (ShouldWrapInTryCatch(statements))
			{
				functionData.WrapInTryCatch = true;
				return;
			}

			var lastStatement = statements.LastOrDefault();
			var invocationExpr = lastStatement?.DescendantNodes(o => !o.IsFunction())
				.OfType<InvocationExpressionSyntax>()
				.FirstOrDefault();

			if (invocationExpr == null)
			{
				return;
			}
			var refData = functionData.BodyFunctionReferences.FirstOrDefault(o => o.ReferenceNode == invocationExpr);
			if (refData == null)
			{
				functionData.WrapInTryCatch = true;
				return;
			}
			if (refData.GetConversion() == ReferenceConversion.Ignore || refData.ReferenceAsyncSymbols.Any(o => o.ReturnsVoid || !o.ReturnType.IsTaskType()))
			{
				functionData.WrapInTryCatch = true;
			}
		}

		private void CalculatePreserveReturnType(MethodOrAccessorData methodData)
		{
			// Shall not wrap the return type into Task when all async invocations do not return a task. Here we mark only methods that do not contain 
			// any references to internal methods
			if (!methodData.RelatedMethods.Any())
			{
				CalculatePreserveReturnType((FunctionData)methodData);
			}
		}

		private void CalculatePreserveReturnType(FunctionData methodData)
		{
			// Shall not wrap the return type into Task when all async invocations do not return a task. Here we mark only methods that do not contain 
			// any references to internal methods
			if (methodData.BodyFunctionReferences
				.Where(o => o.ArgumentOfFunctionInvocation == null)
				.All(o =>
					o.GetConversion() == ReferenceConversion.Ignore ||
					o.GetConversion() == ReferenceConversion.ToAsync &&
					(
						o.ReferenceFunctionData != null &&
						o.ReferenceFunctionData.PreserveReturnType
					) ||
					(
						o.ReferenceFunctionData == null &&
						!o.AsyncCounterpartSymbol.ReturnType.SupportsTaskType()
					)
					))
			{
				methodData.PreserveReturnType = _configuration.CanPreserveReturnType(methodData.Symbol);
			}
		}

		private void ValidateMethodCancellationToken(MethodOrAccessorData methodData)
		{
			var methodGeneration = methodData.MethodCancellationToken.GetValueOrDefault();
			if (methodGeneration == default(MethodCancellationToken))
			{
				methodGeneration = methodData.Symbol.ExplicitInterfaceImplementations.Length > 0
					? MethodCancellationToken.Required 
					: MethodCancellationToken.Optional;
			}
			else if (
				!methodGeneration.HasAnyFlag(MethodCancellationToken.Optional, MethodCancellationToken.Required) &&
				methodGeneration.HasAnyFlag(MethodCancellationToken.ForwardNone, MethodCancellationToken.SealedForwardNone))
			{
				methodGeneration |= MethodCancellationToken.Required;
				methodData.AddDiagnostic(
					$"'{MethodCancellationToken.Optional}' or '{MethodCancellationToken.Required}' ParameterGeneration option for method '{methodData.Symbol}' were not set, " +
					$"'{MethodCancellationToken.Required}' option will be added.",
					DiagnosticSeverity.Hidden);
			}
			else if (methodGeneration.HasFlag(MethodCancellationToken.Optional) &&
					 methodGeneration.HasFlag(MethodCancellationToken.Required))
			{
				methodGeneration &= ~MethodCancellationToken.Required;
				methodData.AddDiagnostic(
					$"Invalid ParameterGeneration option for method '{methodData.Symbol}'. " +
					$"The method cannot have '{MethodCancellationToken.Required}' and '{MethodCancellationToken.Optional}' options set at once. " +
					$"'{MethodCancellationToken.Required}' option will be removed.",
					DiagnosticSeverity.Info);
			}
			if (methodGeneration.HasFlag(MethodCancellationToken.ForwardNone) &&
			    methodGeneration.HasFlag(MethodCancellationToken.SealedForwardNone))
			{
				methodGeneration &= ~MethodCancellationToken.ForwardNone;
				methodData.AddDiagnostic(
					$"Invalid ParameterGeneration option for method '{methodData.Symbol}'. " +
					$"The method cannot have '{MethodCancellationToken.ForwardNone}' and '{MethodCancellationToken.SealedForwardNone}' options set at once. " +
					$"'{MethodCancellationToken.ForwardNone}' option will be removed.",
					DiagnosticSeverity.Info);
			}
			if (methodGeneration.HasFlag(MethodCancellationToken.Optional) &&
			    methodGeneration.HasAnyFlag(MethodCancellationToken.ForwardNone, MethodCancellationToken.SealedForwardNone))
			{
				methodGeneration = MethodCancellationToken.Optional;
				methodData.AddDiagnostic(
					$"Invalid ParameterGeneration option for method '{methodData.Symbol}'. " +
					$"The '{MethodCancellationToken.Optional}' option cannot be combined " +
					$"with '{MethodCancellationToken.ForwardNone}' or '{MethodCancellationToken.SealedForwardNone}' option. " +
					$"'{MethodCancellationToken.ForwardNone}' and '{MethodCancellationToken.SealedForwardNone}' options will be removed.",
					DiagnosticSeverity.Info);
			}

			// Explicit implementor can have only Parameter combined with NoParameterForward or SealedNoParameterForward
			if (methodData.Symbol.ExplicitInterfaceImplementations.Length > 0 && !methodGeneration.HasFlag(MethodCancellationToken.Required))
			{
				methodData.MethodCancellationToken = MethodCancellationToken.Required;
				methodData.AddDiagnostic(
					$"Invalid ParameterGeneration option '{methodGeneration}' for method '{methodData.Symbol}'. " +
					$"Explicit implementor can have only '{MethodCancellationToken.Required}' option combined with " +
					$"'{MethodCancellationToken.ForwardNone}' or '{MethodCancellationToken.SealedForwardNone}' option. " +
					$"The ParameterGeneration will be set to '{methodData.MethodCancellationToken}'",
					DiagnosticSeverity.Info);
				return;
			}

			// Interface method can have only Parameter or DefaultParameter
			if (methodData.InterfaceMethod && methodGeneration.HasFlag(MethodCancellationToken.Required) &&
			    methodGeneration.HasAnyFlag(MethodCancellationToken.ForwardNone, MethodCancellationToken.SealedForwardNone))
			{
				methodData.MethodCancellationToken = MethodCancellationToken.Required;
				methodData.AddDiagnostic(
					$"Invalid ParameterGeneration option '{methodGeneration}' for method '{methodData.Symbol}'. " +
					$"Interface method can have '{MethodCancellationToken.ForwardNone}' or '{MethodCancellationToken.SealedForwardNone}' option. " +
					$"The ParameterGeneration will be set to '{methodData.MethodCancellationToken}'",
					DiagnosticSeverity.Info);
				return;
			}

			if (methodGeneration.HasFlag(MethodCancellationToken.Required) && methodData.Symbol.Parameters.LastOrDefault()?.IsOptional == true)
			{
				methodData.MethodCancellationToken &= ~MethodCancellationToken.Required;
				methodData.MethodCancellationToken |= MethodCancellationToken.Optional;
				methodData.AddDiagnostic(
					$"Invalid ParameterGeneration option '{methodGeneration}' for method '{methodData.Symbol}'. " +
					$"Method can not have '{MethodCancellationToken.Required}' when the last parameter is optional. " +
					$"The ParameterGeneration will be set to '{methodData.MethodCancellationToken}'",
					DiagnosticSeverity.Info);
				return;
			}
			methodData.MethodCancellationToken = methodGeneration;
		}

		private void CalculateFinalFunctionConversion(FunctionData functionData, ICollection<MethodOrAccessorData> asyncMethodDatas)
		{
			// Before checking the conversion of method references we have to calculate the conversion of invocations that 
			// have one or more methods passed as an argument as the current calculated conversion may be wrong
			// (eg. one of the arguments may be ignored in the post-analyze step)
			foreach (var bodyRefData in functionData.BodyFunctionReferences.Where(o => o.DelegateArguments != null && o.Conversion != ReferenceConversion.Ignore))
			{
				var asyncCounterpart = bodyRefData.AsyncCounterpartSymbol;
				if (asyncCounterpart == null)
				{
					// TODO: define
					throw new InvalidOperationException($"AsyncCounterpartSymbol is null {bodyRefData.ReferenceNode}");
				}
				bodyRefData.CalculateFunctionArguments();
				foreach (var analyzer in _configuration.BodyFunctionReferencePostAnalyzers)
				{
					analyzer.PostAnalyzeBodyFunctionReference(bodyRefData);
				}
			}

			if (functionData.Conversion != MethodConversion.Ignore && functionData.Conversion != MethodConversion.Copy &&
			    functionData.BodyFunctionReferences.All(o => o.GetConversion() == ReferenceConversion.Ignore))
			{
				// A method may be already calculated to be async, but we will ignore it if the method does not have any dependency and was not explicitly set to be async
				var methodData = functionData as MethodOrAccessorData;
				if (methodData == null ||
				    (
					    !methodData.Missing && methodData.Dependencies.All(o => o.Conversion.HasFlag(MethodConversion.Ignore)) &&
					    !asyncMethodDatas.Contains(methodData)
				    ))
				{
					if (functionData.ParentFunction != null)
					{
						functionData.Copy();
					}
					else
					{
						functionData.Ignore(IgnoreReason.NoAsyncInvocations);
					}
				}
				return;
			}

			// For copied functions only nameof and typeof body references should be converted to async
			if (functionData.Conversion == MethodConversion.Copy)
			{
				foreach (var bodyReference in functionData.BodyFunctionReferences
					.Where(o => o.GetConversion() == ReferenceConversion.ToAsync)
					.Where(o => !o.IsNameOf && !o.IsTypeOf))
				{
					bodyReference.Ignore(IgnoreReason.MethodIsCopied);
				}
			}

			if (functionData.Conversion.HasAnyFlag(MethodConversion.ToAsync, MethodConversion.Ignore, MethodConversion.Copy))
			{
				return;
			}

			if (functionData.CanBeAsync && functionData.BodyFunctionReferences.Any(o => o.GetConversion() == ReferenceConversion.ToAsync))
			{
				functionData.ToAsync();
			}
			else
			{
				if (functionData.ParentFunction != null)
				{
					functionData.Copy();
				}
				else
				{
					functionData.Ignore(IgnoreReason.NoAsyncInvocations);
				}
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
				.SelectMany(o => o.MethodsAndAccessors.Where(m => m.ExplicitlyIgnored))
				)
			{
				// Correct if the user ignored a method that is used by other methods
				if (methodData.TypeData.Conversion == TypeConversion.NewType &&
				    methodData.Dependencies.Any(o => o.Conversion.HasAnyFlag(MethodConversion.Copy, MethodConversion.ToAsync)))
				{
					methodData.Copy();
					methodData.AddDiagnostic("Explicitly ignored method will be copied as it is used in one or many methods that will be generated", DiagnosticSeverity.Hidden);
				}

				// If an abstract method is ignored we have to ignore also the overrides otherwise we may break the functionality and the code from compiling (eg. base.Call())
				if (methodData.Symbol.IsAbstract || methodData.Symbol.IsVirtual)
				{
					foreach (var relatedMethodData in methodData.RelatedMethods.Where(i => i.RelatedMethods.All(r => r == methodData)))
					{
						if (relatedMethodData.TypeData.Conversion == TypeConversion.NewType ||
						    relatedMethodData.TypeData.Conversion == TypeConversion.Copy)
						{
							relatedMethodData.Copy();
						}
						else
						{
							relatedMethodData.Ignore(IgnoreReason.Custom($"Implicitly ignored because of the explictly ignored method {methodData.Symbol}", DiagnosticSeverity.Hidden));
						}
					}
				}
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
					relatedMethodData.Ignore(IgnoreReason.Custom($"Implicitly ignored because of the explictly ignored method {methodData.Symbol}", DiagnosticSeverity.Hidden));
				}
			}

			// If a type data is ignored then also its method data are ignored
			var allTypeData = allNamespaceData
				.SelectMany(o => o.Types.Values)
				.SelectMany(o => o.GetSelfAndDescendantsTypeData(t => t.Conversion != TypeConversion.Ignore))
				//.Where(o => o.Conversion != TypeConversion.Ignore)
				.ToList();
			var toProcessMethodData = new HashSet<MethodOrAccessorData>(allTypeData
				.SelectMany(o => o.MethodsAndAccessors.Where(m => m.Conversion.HasAnyFlag(MethodConversion.ToAsync, MethodConversion.Smart, MethodConversion.Unknown))));
			//TODO: optimize steps for better performance

			// 0. Step - If cancellation tokens are enabled we should start from methods that requires a cancellation token in order to correctly propagate CancellationTokenRequired
			// to dependency methods
			if (_configuration.UseCancellationTokens || _configuration.CanScanForMissingAsyncMembers != null)
			{
				var tokenMethodDatas = toProcessMethodData.Where(o => o.CancellationTokenRequired && o.CanBeAsync).ToList();
				foreach (var tokenMethodData in tokenMethodDatas)
				{
					if (toProcessMethodData.Count == 0)
					{
						break;
					}
					tokenMethodData.ToAsync();
					PostAnalyzeAsyncMethodData(tokenMethodData, toProcessMethodData);
				}
			}
			
			// 1. Step - Go through all async methods and set their dependencies to be also async
			// TODO: should we start from the bottom/leaf method that is async? how do we know if the method is a leaf (consider circular calls)?
			var asyncMethodDatas = toProcessMethodData.Where(o => o.Conversion.HasFlag(MethodConversion.ToAsync)).ToList();
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
			var postponedMethodData = new List<MethodOrAccessorData>();
			foreach (var methodData in remainingMethodData.Where(o => o.CanBeAsync))
			{
				// Start from the bottom child function in case the root function calls them
				foreach (var childFunction in methodData.GetDescendantsChildFunctions()
					.OrderByDescending(o => o.GetBodyNode().SpanStart)
					.Where(o => o.Conversion != MethodConversion.Ignore && o.Conversion != MethodConversion.Copy))
				{
					if (childFunction.BodyFunctionReferences.Where(o => o.ArgumentOfFunctionInvocation == null)
						.Any(o => o.GetConversion() == ReferenceConversion.ToAsync))
					{
						childFunction.ToAsync();
					}
				}

				if (methodData.BodyFunctionReferences.Where(o => o.ArgumentOfFunctionInvocation == null).All(o => o.GetConversion() != ReferenceConversion.ToAsync))
				{
					// Postpone the conversion calculation of methods that have nested functions as one of the function may be async after all remaining methods are processed
					if (methodData.ChildFunctions.Values.Any(o => o.Conversion != MethodConversion.Ignore && o.Conversion != MethodConversion.Copy))
					{
						postponedMethodData.Add(methodData);
					}
					continue;
				}
				if (methodData.Conversion == MethodConversion.Ignore)
				{
					methodData.AddDiagnostic("Has a method invocation that can be async", DiagnosticSeverity.Warning);
					continue;
				}
				methodData.ToAsync();
				// Set all dependencies to be async for the newly discovered async method
				PostAnalyzeAsyncMethodData(methodData, toProcessMethodData);
				if (toProcessMethodData.Count == 0)
				{
					break;
				}
			}

			// 2.1 Step - Retry to calculate the conversion of the postponed methods
			foreach (var methodData in postponedMethodData)
			{
				foreach (var bodyMethodRef in methodData.BodyFunctionReferences.Where(o => o.ReferenceAsyncSymbols.Any() && o.AsyncCounterpartSymbol == null))
				{
					SetAsyncCounterpart(bodyMethodRef);
				}
				if (methodData.BodyFunctionReferences.Where(o => o.ArgumentOfFunctionInvocation == null).Any(o => o.GetConversion() == ReferenceConversion.ToAsync))
				{
					methodData.ToAsync();
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
				if (methodData.TypeData.GetSelfAndAncestorsTypeData().Any(o => o.Conversion == TypeConversion.NewType))
				{
					// For smart methods we will have filled dependencies so we can ignore it if is not used
					if (
						methodData.Conversion.HasFlag(MethodConversion.Smart) && 
						methodData.Dependencies.All(o => o.Conversion.HasFlag(MethodConversion.Ignore)) &&
						!methodData.HasAnyActiveReference()
						)
					{
						methodData.Ignore(IgnoreReason.NeverUsed);
					}
					else
					{
						methodData.Copy();
					}
				}
				else
				{
					methodData.Ignore(IgnoreReason.NeverUsed);
				}
			}

			// We need to calculate the final conversion for the local/anonymous functions
			foreach (var methodData in allTypeData.SelectMany(o => o.MethodsAndAccessors.Where(m => m.Conversion.HasFlag(MethodConversion.ToAsync))))
			{
				// We have to calculate the conversion from bottom to top as a body reference may depend on a child function (passed by argument)
				foreach (var childFunction in methodData.GetSelfAndDescendantsFunctions().Where(o => o.GetBodyNode() != null).OrderByDescending(o => o.GetBodyNode().SpanStart))
				{
					CalculateFinalFunctionConversion(childFunction, asyncMethodDatas);
				}

				// Update PassCancellationToken for all body function references that requires a cancellation token
				if (_configuration.UseCancellationTokens || _configuration.CanScanForMissingAsyncMembers != null)
				{
					ValidateMethodCancellationToken(methodData);

					foreach (var functionRefData in methodData.GetSelfAndDescendantsFunctions()
						.SelectMany(o => o.BodyFunctionReferences.Where(r => r.GetConversion() == ReferenceConversion.ToAsync)))
					{
						// Child functions don't need a cancellation token as they can use the one from the root method
						if (functionRefData.ReferenceFunctionData != null && !(functionRefData.ReferenceFunctionData is ChildFunctionData))
						{
							// Only update if AsyncCounterpartSymbol is the sync version of method.
							if (functionRefData.AsyncCounterpartSymbol.OriginalDefinition
								.EqualTo(functionRefData.ReferenceFunctionData.Symbol.OriginalDefinition))
							{
								var refMethodData = functionRefData.ReferenceFunctionData.GetMethodOrAccessorData();
								functionRefData.PassCancellationToken = refMethodData.CancellationTokenRequired;
							}
						}
						if (!methodData.CancellationTokenRequired && functionRefData.CanSkipCancellationTokenArgument() == true)
						{
							functionRefData.PassCancellationToken = false; // Do not pass CancellationToken.None if the parameter is optional
						}
					}
				}

				CalculatePreserveReturnType(methodData);
			}

			// Ignore unused private methods
			var methodOrAccessorQueue = new Queue<MethodOrAccessorData>(allTypeData
				.SelectMany(o => o.MethodsAndAccessors.Where(m => m.Conversion != MethodConversion.Ignore && m.IsPrivate && !m.ForceAsync)));
			var postopnedMethods = new List<MethodOrAccessorData>();
			var lastPostponedMethods = 0;
			while (methodOrAccessorQueue.Count > 0 || postopnedMethods.Count > 0)
			{
				if (methodOrAccessorQueue.Count == 0)
				{
					if (lastPostponedMethods == postopnedMethods.Count)
					{
						break;
					}
					lastPostponedMethods = postopnedMethods.Count;
					foreach (var postopnedMethod in postopnedMethods)
					{
						methodOrAccessorQueue.Enqueue(postopnedMethod);
					}
					postopnedMethods.Clear();
					continue;
				}

				var methodOrAccessor = methodOrAccessorQueue.Dequeue();
				var usage = GetUsage(methodOrAccessor);
				switch (usage)
				{
					case MethodUsage.None:
						// Ignore only if there is no CS0103 error related to the current function.
						// e.g. The name 'identifier' does not exist in the current context
						if (!methodOrAccessor.TypeData.NamespaceData.DocumentData.SemanticModel.GetDiagnostics(methodOrAccessor.TypeData.Node.Span)
							.Any(o => o.Id == "CS0103" && o.GetMessage(CultureInfo.InvariantCulture).Contains($"'{methodOrAccessor.AsyncCounterpartName}'")))
						{
							methodOrAccessor.Ignore(IgnoreReason.NeverUsed);
						}
						continue;
					// We have to postpone the calculation of a private method that is marked to be async when it is references only by 
					// private methods. We could later discover that methods that are referencing this one will be ignored.
					case MethodUsage.Async when methodOrAccessor.ReferencedBy.OfType<MethodOrAccessorData>().All(o => o.IsPrivate):
						postopnedMethods.Add(methodOrAccessor);
						continue;
				}
				if (methodOrAccessor.TypeData.GetSelfAndAncestorsTypeData().All(o => o.Conversion != TypeConversion.NewType))
				{
					if (usage == MethodUsage.Sync)
					{
						methodOrAccessor.Ignore(IgnoreReason.NeverUsedAsAsync);
					}
					continue;
				}
				if (usage == MethodUsage.Sync)
				{
					methodOrAccessor.Copy();
				}
				else if (usage.HasFlag(MethodUsage.Sync))
				{
					methodOrAccessor.SoftCopy();
				}
			}

			// 4. Step - Calculate the final type conversion
			foreach (var typeData in allTypeData)
			{
				if (typeData.Conversion == TypeConversion.Ignore)
				{
					continue;
				}

				// If any of the base types will be generated as a new type and have at least one async member, create the derived type even 
				// if it has no async memebers or was not marked to be a new type
				if ((typeData.Conversion == TypeConversion.NewType || typeData.Conversion == TypeConversion.Unknown) && 
					typeData.BaseTypes.Any(t => t.Conversion == TypeConversion.NewType && t.MethodsAndAccessors.Any(o => o.Conversion.HasFlag(MethodConversion.ToAsync))))
				{
					typeData.Conversion = TypeConversion.NewType;
					foreach (var property in typeData.Properties.Values)
					{
						property.Copy(); // TODO: copy only if needed
					}
					// TODO: what else should we do here?
					continue;
				}

				// A type can be ignored only if it has no async methods that will get converted
				if (typeData.GetSelfAndDescendantsTypeData().All(t => t.MethodsAndAccessors.All(o => o.Conversion == MethodConversion.Ignore || o.Conversion == MethodConversion.Copy)))
				{
					if (typeData.ParentTypeData?.GetSelfAndAncestorsTypeData().Any(o => o.Conversion == TypeConversion.NewType) == true)
					{
						typeData.Copy();
					}
					else
					{
						typeData.Ignore(IgnoreReason.NoAsyncMembers);
					}
				}
				else if(typeData.Conversion == TypeConversion.Unknown)
				{
					typeData.Conversion = TypeConversion.Partial;
				}
			}

			foreach (var type in allTypeData)
			{
				// 5.1 Step - Ignore or copy private fields of new types whether they are used or not
				var postopnedVariables = new List<FieldVariableDeclaratorData>();
				foreach (var variable in type.Fields.Values.SelectMany(o => o.Variables).Where(o => o.Conversion != FieldVariableConversion.Ignore))
				{
					if (type.Conversion == TypeConversion.Partial)
					{
						variable.Conversion = FieldVariableConversion.Ignore;
						continue;
					}
					// From here on the type can be or a new one or copied
					if (!variable.FieldData.Node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
					{
						variable.Conversion = FieldVariableConversion.Copy;
						continue;
					}

					variable.Conversion = variable.HasAnyActiveReference()
						? FieldVariableConversion.Copy
						: FieldVariableConversion.Ignore;
					if (variable.Conversion == FieldVariableConversion.Ignore && variable.ReferencedBy.OfType<FieldVariableDeclaratorData>().Any())
					{
						postopnedVariables.Add(variable);
					}
				}
				foreach (var variable in postopnedVariables)
				{
					variable.Conversion = variable.ReferencedBy.OfType<FieldVariableDeclaratorData>().Any(o => o.Conversion == FieldVariableConversion.Copy)
						? FieldVariableConversion.Copy
						: FieldVariableConversion.Ignore;
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
					namespaceData.Ignore(IgnoreReason.NoAsyncMembers);
				}
				else
				{
					namespaceData.Conversion = NamespaceConversion.Generate;
				}
			}

			// 6. Step - For all async methods check for preconditions. Search only statements that its end location is lower that the first async method reference
			foreach (var functionData in allTypeData.Where(o => o.Conversion != TypeConversion.Ignore)
				.SelectMany(o => o.MethodsAndAccessors.Where(m => m.Conversion.HasFlag(MethodConversion.ToAsync)))
				.SelectMany(o => o.GetSelfAndDescendantsFunctions().Where(m => m.Conversion.HasFlag(MethodConversion.ToAsync))))
			{
				CalculateFinalFlags(functionData);
			}
		}

		[Flags]
		private enum MethodUsage
		{
			Unknown = 1,
			None = 4,
			Async = 8,
			Sync = 16,
			Nameof = 32,
			Cref = 64
		}

		/// <summary>
		/// Retrieve the method usage
		/// </summary>
		private MethodUsage GetUsage(MethodOrAccessorData methodOrAccessorData)
		{
			// If the method was not scanned we don't know if is used or not
			if (!_scannedMethodOrAccessors.Contains(methodOrAccessorData))
			{
				return MethodUsage.Unknown;
			}

			var usage = MethodUsage.None;
			// A method is unused when is private and never used
			foreach (var reference in methodOrAccessorData.SelfReferences)
			{
				switch (reference)
				{
					case BodyFunctionDataReference bodyRef:
						if (bodyRef.GetConversion() == ReferenceConversion.ToAsync)
						{
							usage |= MethodUsage.Async;
							continue;
						}
						if (bodyRef.GetConversion() != ReferenceConversion.Ignore ||
						    bodyRef.Data.Conversion.HasAnyFlag(MethodConversion.Smart, MethodConversion.Unknown))
						{
							usage |= MethodUsage.Unknown; // We don't know yet
						}
						// A reference may be ignored because will get copied
						if (bodyRef.Data.Conversion.HasAnyFlag(MethodConversion.Copy, MethodConversion.ToAsync))
						{
							usage |= MethodUsage.Sync;
						}
						break;
					case NameofFunctionDataReference _:
						usage |= MethodUsage.Nameof;
						break;
					case CrefFunctionDataReference _:
						usage |= MethodUsage.Cref;
						break;
				}
			}
			if (usage != MethodUsage.None)
			{
				usage &= ~MethodUsage.None;
			}
			return usage;
		}

		private void CalculateFinalFlags(FunctionData functionData)
		{
			if (functionData.GetBodyNode() == null)
			{
				return;
			}
			var methodData = functionData as MethodData;
			var asyncMethodReferences = functionData.BodyFunctionReferences
				.Where(o => o.GetConversion() == ReferenceConversion.ToAsync)
				.ToList();
			var nonArgumentReferences = functionData.BodyFunctionReferences
				.Where(o => o.ArgumentOfFunctionInvocation == null)
				.ToList();
			// Calculate the final reference AwaitInvocation, we can skip await if all async invocations are returned and the return type matches
			// or we have only one async invocation that is the last to be invoked
			// Invocations in synchronized methods must be awaited to mimic the same behavior as their sync counterparts
			if (!_configuration.CanAlwaysAwait(functionData.Symbol) && (methodData == null || !methodData.MustRunSynchronized))
			{
				var canSkipAwaits = true;
				// Skip functions that are passed as arguments 
				foreach (var methodReference in nonArgumentReferences)
				{
					if (methodReference.GetConversion() == ReferenceConversion.Ignore)
					{
						methodReference.AwaitInvocation = false;
						continue;
					}
					if (methodReference.AwaitInvocation == false)
					{
						continue;
					}

					if (!methodReference.UseAsReturnValue && !methodReference.LastInvocation)
					{
						canSkipAwaits = false;
						break;
					}
					// We cannot skip await keyword if the result of the invocation is assigned to something
					if (methodReference.LastInvocation && methodReference.ReferenceNode.Parent is AssignmentExpressionSyntax)
					{
						canSkipAwaits = false;
						break;
					}

					// We must await the invocation before returning when located inside a using or try statement
					if (methodReference.UseAsReturnValue && methodReference.ReferenceNode.Ancestors()
						    .TakeWhile(o => o != functionData.GetNode())
						    .Any(o => o.IsKind(SyntaxKind.UsingStatement) || o.IsKind(SyntaxKind.TryStatement) || o.IsKind(SyntaxKind.LockStatement)))
					{
						canSkipAwaits = false;
						break;
					}

					var referenceFunctionData = methodReference.Data;

					if (methodReference.LastInvocation && referenceFunctionData.Symbol.ReturnsVoid && (
						    (methodReference.ReferenceAsyncSymbols.Any() && methodReference.ReferenceAsyncSymbols.All(o => o.ReturnType.IsTaskType())) ||
						    methodReference.ReferenceFunctionData?.Conversion.HasFlag(MethodConversion.ToAsync) == true
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
									    return o.ReturnType.IsAwaitRequired(referenceFunctionData.Symbol.ReturnType);
								    }
								    return returnType.TypeArguments.First().IsAwaitRequired(referenceFunctionData.Symbol.ReturnType);
							    })
						    ) ||
						    (
							    methodReference.ReferenceFunctionData != null &&
							    !methodReference.ReferenceFunctionData.Symbol.ReturnType.IsAwaitRequired(referenceFunctionData.Symbol.ReturnType)
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
					foreach (var methodReference in asyncMethodReferences.Where(o => o.ArgumentOfFunctionInvocation == null))
					{
						// If the async counterpart of a method reference do not return a task we cannot set UseAsReturnValue to true
						if (methodReference.AwaitInvocation != false)
						{
							methodReference.UseAsReturnValue = true;
						}
						methodReference.AwaitInvocation = false;
					}
				}
			}

			var functionBody = functionData.GetBodyNode();
			// If the method has a block body
			if (functionBody is BlockSyntax functionBlockBody)
			{
				// Some async methods may not have any async invocations because were forced to be async (overloads)
				var methodRefSpan = asyncMethodReferences
					.Select(o => o.ReferenceLocation.Location)
					.OrderBy(o => o.SourceSpan.Start)
					.FirstOrDefault();
				var semanticModel = functionData.TypeData.NamespaceData.DocumentData.SemanticModel;
				// Search for preconditions until a statement has not been qualified as a precondition or we encounter an async invocation
				// The faulted property is set to true when the first statement is a throw statement
				foreach (var statement in functionBlockBody.Statements)
				{
					if (methodRefSpan != null && statement.Span.End > methodRefSpan.SourceSpan.Start)
					{
						break;
					}
					if (!_configuration.PreconditionCheckers.Any(o => o.IsPrecondition(statement, semanticModel)))
					{
						functionData.Faulted = statement.IsKind(SyntaxKind.ThrowStatement);
						break;
					}
					functionData.Preconditions.Add(statement);
				}

				// A method shall be tail splitted when has at least one precondition and there is at least one awaitable invocation
				if (functionData.Preconditions.Any() && functionData.BodyFunctionReferences.Any(o => o.AwaitInvocation == true))
				{
					functionData.SplitTail = true;
				}
			}
			else if(functionBody is ArrowExpressionClauseSyntax functionArrowBody)
			{
				functionData.Faulted = functionArrowBody.IsKind(SyntaxKind.ThrowExpression);
			}

			// The async keyword shall be omitted when the method does not have any awaitable invocation or we have to tail split
			if (functionData.SplitTail || !nonArgumentReferences.Any(o => o.GetConversion() == ReferenceConversion.ToAsync && o.AwaitInvocation == true))
			{
				functionData.OmitAsync = true;
			}

			// TODO: what about anonymous functions
			if (methodData != null && functionData.OmitAsync)
			{
				CalculatePreserveReturnType(methodData);
			}

			// When the async keyword is omitted and the method is not faulted we need to calculate if the method body shall be wrapped in a try/catch block
			// Also we do need to wrap into a try/catch when the return type remains the same
			if (!functionData.Faulted && !functionData.PreserveReturnType && functionData.OmitAsync)
			{
				// For sync forwarding we will always wrap into try catch
				if (methodData != null && methodData.BodyFunctionReferences
					.All(o => o.GetConversion() == ReferenceConversion.Ignore) && _configuration.CanForwardCall(functionData.Symbol))
				{
					methodData.WrapInTryCatch = true;
					methodData.ForwardCall = true;
				}
				else
				{
					CalculateWrapInTryCatch(functionData);
				}
			}
			else
			{
				functionData.WrapInTryCatch = false;
			}
		}
	}
}
