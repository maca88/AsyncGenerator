using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Extensions;
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
		private readonly string[] _taskResultMethods = {"Wait", "GetResult", "RunSynchronously"};

		private void AnalyzeDocumentData(DocumentData documentData)
		{
			foreach (var typeData in documentData.GetAllTypeDatas(o => o.Conversion != TypeConversion.Ignore))
			{
				// Ignore or copy properties of new types
				foreach (var property in typeData.Properties.Values)
				{
					if (typeData.IsNewType)
					{
						property.Copy(); // TODO: copy only if needed
						SoftCopyAllDependencies(property.GetAccessorData);
						SoftCopyAllDependencies(property.SetAccessorData);
					}
					// A type with Unknown conversion may be converted to a new type if one of its base classes
					// is a new type and has at least one async member
					else if (typeData.BaseTypes.All(o => o.Conversion != TypeConversion.NewType))
					{
						property.Conversion = PropertyConversion.Ignore;
					}
				}

				foreach (var methodData in typeData.MethodsAndAccessors.Where(o => o.Conversion != MethodConversion.Ignore))
				{
					foreach (var functionData in methodData.GetDescendantsChildFunctions(o => o.Conversion != MethodConversion.Ignore)
						.OrderByDescending(o => o.GetNode().SpanStart))
					{
						AnalyzeAnonymousFunctionData(documentData, functionData);
					}
					AnalyzeMethodData(documentData, methodData);
				}
			}
		}

		private void SoftCopyAllDependencies(MethodOrAccessorData methodOrAccessor)
		{
			if (methodOrAccessor == null)
			{
				return;
			}
			var processedMethods = new HashSet<MethodOrAccessorData>();
			var processingMetods = new Queue<MethodOrAccessorData>();
			processingMetods.Enqueue(methodOrAccessor);

			while (processingMetods.Count > 0)
			{
				var currentOrAccessor = processingMetods.Dequeue();
				processedMethods.Add(currentOrAccessor);
				foreach (var referencedMethodOrAccessor in currentOrAccessor.BodyFunctionReferences
					.Select(o => o.ReferenceFunctionData)
					.Where(o => o != null)
					.Where(o => o.Conversion != MethodConversion.Ignore && o.TypeData.IsNewType)
					.Where(o => !processedMethods.Contains(o))
					.OfType<MethodOrAccessorData>())
				{
					referencedMethodOrAccessor.SoftCopy();
					processingMetods.Enqueue(referencedMethodOrAccessor);
				}
			}
		}

		private void AnalyzeMethodData(DocumentData documentData, MethodOrAccessorData methodAccessorData)
		{
			if (methodAccessorData.Conversion == MethodConversion.Copy)
			{
				// If the method will be copied then we have to copy all methods that are used inside this one and do this recursively
				SoftCopyAllDependencies(methodAccessorData);
				return; // We do not want to analyze method that will be only copied
			}
			
			// If all abstract/virtual related methods are ignored then ignore also this one (IsAbstract includes also interface members)
			// Skip methods that were ignored because having an async counterpart in order to have overrides generated
			var baseMethods = methodAccessorData.RelatedMethods
				.Where(o => !o.HasAsyncCounterpart || o.ExplicitlyIgnored)
				.Where(o => o.Symbol.IsAbstract || o.Symbol.IsVirtual).ToList();
			if (!methodAccessorData.Conversion.HasFlag(MethodConversion.ToAsync) && baseMethods.Any() && baseMethods.All(o => o.Conversion == MethodConversion.Ignore))
			{
				if (methodAccessorData.TypeData.GetSelfAndAncestorsTypeData().Any(o => o.Conversion == TypeConversion.NewType))
				{
					methodAccessorData.Copy();
					// Check if there are any internal methods that are candidate to be async and are invoked inside this method
					// If there are, then we need to copy them
					SoftCopyAllDependencies(methodAccessorData);
				}
				else
				{
					methodAccessorData.Ignore(IgnoreReason.AllRelatedMethodsIgnored);
				}
				return;
			}


			var methodBody = methodAccessorData.GetBodyNode();
			methodAccessorData.HasYields = methodBody?.DescendantNodes().OfType<YieldStatementSyntax>().Any() == true;
			methodAccessorData.MustRunSynchronized = methodAccessorData.Symbol.GetAttributes()
				.Where(o => o.AttributeClass.Name == "MethodImplAttribute")
				.Any(o => ((MethodImplOptions)(int)o.ConstructorArguments.First().Value).HasFlag(MethodImplOptions.Synchronized));

			if (methodBody == null)
			{
				methodAccessorData.OmitAsync = true;
			}

			// Order by descending so we are sure that methods passed by argument will be processed before the invoked method with those arguments
			foreach (var reference in methodAccessorData.BodyFunctionReferences.OrderByDescending(o => o.ReferenceNameNode.SpanStart))
			{
				AnalyzeMethodReference(documentData, reference);
			}

			foreach (var reference in methodAccessorData.CrefFunctionReferences)
			{
				AnalyzeCrefMethodReference(documentData, methodAccessorData, reference);
			}

			foreach (var reference in methodAccessorData.NameofFunctionReferences)
			{
				AnalyzeNameofMethodReference(documentData, methodAccessorData, reference);
			}

			// Ignore all candidate arguments that are not an argument of an async invocation candidate
			// Do not ignore accessor as they are executed prior passing
			foreach (var reference in methodAccessorData.BodyFunctionReferences
				.Where(o => !o.ReferenceSymbol.IsPropertyAccessor() && o.ReferenceNode.IsKind(SyntaxKind.Argument) && o.ArgumentOfFunctionInvocation == null))
			{
				reference.Ignore(IgnoreReason.InvokedMethodNoAsyncCounterpart);
			}

			if (methodAccessorData.Conversion.HasFlag(MethodConversion.ToAsync))
			{
				return;
			}

			// If a method is never invoked and there is no invocations inside the method body that can be async and there is no related methods we can ignore it.
			// Methods with Unknown may not have InvokedBy populated so we cannot ignore them here
			// Do not ignore methods that are inside a type with conversion NewType as ExternalRelatedMethods may not be populated
			// Do not ignore a method if any of its related methods has an async counterpart that was not ignored by user so that we can gnerate (e.g. async override)
			if (
				methodAccessorData.Dependencies.All(o => o.Conversion.HasFlag(MethodConversion.Ignore)) &&
				methodAccessorData.RelatedMethods.All(o => !o.HasAsyncCounterpart || o.ExplicitlyIgnored) &&
				methodAccessorData.BodyFunctionReferences.All(o => o.Conversion == ReferenceConversion.Ignore) && 
				methodAccessorData.Conversion.HasFlag(MethodConversion.Smart) &&
			    methodAccessorData.TypeData.GetSelfAndAncestorsTypeData().All(o => o.Conversion != TypeConversion.NewType) &&
				!methodAccessorData.ExternalRelatedMethods.Any()
			)
			{
				methodAccessorData.Ignore(IgnoreReason.NeverUsedAndNoAsyncInvocations);
			}
		}

		private void AnalyzeAnonymousFunctionData(DocumentData documentData, ChildFunctionData functionData)
		{
			var parentNode = functionData.GetNode().Parent;
			// Ignore if the anonymous function is passed as an argument to a non async candidate.
			// We cannot use ArgumentOfFunctionInvocation as it is populated when the main method is analyzed
			if (parentNode.IsKind(SyntaxKind.Argument))
			{
				var parentFunction = functionData.ParentFunction;
				var callNode = parentNode.Parent.Parent;
				if (callNode is InvocationExpressionSyntax invocationNode)
				{
					// If the parent function does not contain the invoked function with our argument then ignore it
					if (parentFunction.BodyFunctionReferences.All(
						o => !invocationNode.Expression.Span.Contains(o.ReferenceNameNode.Span)))
					{
						// A child function can be ignored only when the method that contains it is ignored
						functionData.Copy();
						if (functionData.TypeData.GetSelfAndAncestorsTypeData().Any(o => o.Conversion == TypeConversion.NewType))
						{
							functionData.AddDiagnostic("Function is passed as an argument to a non async invocation", DiagnosticSeverity.Hidden);
						}
						return;
					}
				}
				else
				{
					functionData.Copy();
					if (functionData.TypeData.GetSelfAndAncestorsTypeData().Any(o => o.Conversion == TypeConversion.NewType))
					{
						functionData.AddDiagnostic($"Anonymous function is passed as argument to a non supported node {callNode}", DiagnosticSeverity.Hidden);
					}
					return;
				}
			}

			// Order by descending so we are sure that methods passed by argument will be processed before the invoked method with those arguments
			foreach (var reference in functionData.BodyFunctionReferences.OrderByDescending(o => o.ReferenceNameNode.SpanStart))
			{
				AnalyzeMethodReference(documentData, reference);
			}

			// Ignore all candidate arguments that are not an argument of an async invocation candidate
			foreach (var reference in functionData.BodyFunctionReferences
				.Where(o => !o.ReferenceSymbol.IsPropertyAccessor() && o.ReferenceNode.IsKind(SyntaxKind.Argument) && o.ArgumentOfFunctionInvocation == null))
			{
				reference.Ignore(IgnoreReason.InvokedMethodNoAsyncCounterpart);
			}

			functionData.HasYields = functionData.GetBodyNode()?.DescendantNodes().OfType<YieldStatementSyntax>().Any() == true;
		}

		private void AnalyzeCrefMethodReference(DocumentData documentData, MethodOrAccessorData methoData, CrefFunctionDataReference crefData)
		{
			crefData.RelatedBodyFunctionReferences.AddRange(
				methoData.BodyFunctionReferences.Where(o => o.ReferenceSymbol.EqualTo(crefData.ReferenceSymbol)));
		}

		private void AnalyzeNameofMethodReference(DocumentData documentData, MethodOrAccessorData methoData, NameofFunctionDataReference nameofData)
		{
			nameofData.RelatedBodyFunctionReferences.AddRange(
				methoData.BodyFunctionReferences.Where(o => o.ReferenceSymbol.EqualTo(nameofData.ReferenceSymbol)));
		}

		private void AnalyzeMethodReference(DocumentData documentData, BodyFunctionDataReference refData)
		{
			var nameNode = refData.ReferenceNameNode;

			// Find the actual usage of the method
			SyntaxNode currNode = nameNode;
			var ascend = true;

			if (refData.ReferenceSymbol.IsPropertyAccessor())
			{
				ascend = false;
				AnalyzeAccessor(documentData, nameNode, refData);
			}
			else
			{
				currNode = nameNode.Parent;
			}

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
						refData.Ignore(IgnoreReason.Custom(
							$"Cannot attach an async method to an event (void async is not an option as cannot be awaited)", DiagnosticSeverity.Info));
						break;
					case SyntaxKind.SubtractAssignmentExpression:
						refData.Ignore(IgnoreReason.Custom($"Cannot detach an async method to an event", DiagnosticSeverity.Info));
						break;
					case SyntaxKind.VariableDeclaration:
						refData.Ignore(IgnoreReason.NotSupported($"Assigning async method to a variable is not supported"));
						break;
					case SyntaxKind.CastExpression:
						refData.AwaitInvocation = true;
						ascend = true;
						break;
					case SyntaxKind.ReturnStatement:
						break;
					case SyntaxKind.ArrayInitializerExpression:
					case SyntaxKind.CollectionInitializerExpression:
					case SyntaxKind.ComplexElementInitializerExpression:
						refData.Ignore(IgnoreReason.NotSupported($"Async method inside an array/collection initializer is not supported"));
						break;
					// skip
					case SyntaxKind.VariableDeclarator:
					case SyntaxKind.EqualsValueClause:
					case SyntaxKind.SimpleMemberAccessExpression:
					case SyntaxKind.ArgumentList:
					case SyntaxKind.ObjectCreationExpression:
					case SyntaxKind.MemberBindingExpression: // ?.
						ascend = true;
						break;
					default:
						throw new NotSupportedException(
							$"Unknown node kind: {currNode.Kind()} at {currNode?.SyntaxTree.GetLineSpan(currNode.Span)}. Node:{Environment.NewLine}{currNode}");
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

		private void AnalyzeAccessor(DocumentData documentData, SimpleNameSyntax node, BodyFunctionDataReference functionReferenceData)
		{
			var functionData = functionReferenceData.Data;
			var functionNode = functionData.GetNode();

			if (IgnoreIfInvalidAncestor(node, functionNode, functionReferenceData))
			{
				return;
			}
			functionReferenceData.InvokedFromType = functionData.Symbol.ContainingType;
			FindAsyncCounterparts(functionReferenceData);
			SetAsyncCounterpart(functionReferenceData);
			CalculateLastInvocation(node, functionReferenceData);
			PropagateCancellationToken(functionReferenceData);
		}

		private bool IgnoreIfInvalidAncestor(SyntaxNode node, SyntaxNode endNode, BodyFunctionDataReference functionReferenceData)
		{
			var currAncestor = node.Parent;
			while (!currAncestor.Equals(endNode))
			{
				if (currAncestor.IsKind(SyntaxKind.QueryExpression))
				{
					functionReferenceData.Ignore(IgnoreReason.Custom("Cannot await async method in a query expression", DiagnosticSeverity.Info));
					return true;
				}
				currAncestor = currAncestor.Parent;
			}
			return false;
		}

		private void AnalyzeInvocationExpression(DocumentData documentData, InvocationExpressionSyntax node, BodyFunctionDataReference functionReferenceData)
		{
			var functionData = functionReferenceData.Data;
			var methodSymbol = functionReferenceData.ReferenceSymbol;
			var functionNode = functionData.GetNode();

			if (IgnoreIfInvalidAncestor(node, functionNode, functionReferenceData))
			{
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
					functionReferenceData.AddDiagnostic("Cannot await invocation that returns a Task without being synchronously awaited", DiagnosticSeverity.Info);
				}
				else
				{
					functionReferenceData.SynchronouslyAwaited = true;
				}
			}
			if (node.Expression is SimpleNameSyntax)
			{
				functionReferenceData.InvokedFromType = functionData.Symbol.ContainingType;
			}
			else if (node.Expression is MemberAccessExpressionSyntax memberAccessExpression)
			{
				functionReferenceData.InvokedFromType = documentData.SemanticModel.GetTypeInfo(memberAccessExpression.Expression).Type;
			}

			FindAsyncCounterparts(functionReferenceData);

			var delegateParams = methodSymbol.Parameters.Select(o => o.Type.TypeKind == TypeKind.Delegate).ToList();
			for (var i = 0; i < node.ArgumentList.Arguments.Count; i++)
			{
				var argument = node.ArgumentList.Arguments[i];
				var argumentExpression = argument.Expression;
				// We have to process anonymous funcions as they will not be analyzed as arguments
				if (argumentExpression.IsFunction())
				{
					var anonFunction = (AnonymousFunctionData)functionData.ChildFunctions[argumentExpression];
					functionReferenceData.AddDelegateArgument(new DelegateArgumentData(anonFunction, i));
					anonFunction.ArgumentOfFunctionInvocation = functionReferenceData;
					continue;
				}
				if (argumentExpression.IsKind(SyntaxKind.IdentifierName) ||
				    argumentExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
				{
					var argRefFunction = functionData.BodyFunctionReferences.FirstOrDefault(o => argument.Equals(o.ReferenceNode));
					if (argRefFunction == null)
					{
						// Ignore only if the async argument does not match
						// TODO: internal methods, unify with CalculateFunctionArguments
						if (functionReferenceData.ReferenceFunctionData == null && delegateParams[i]) // If the parameter is a delegate check the symbol of the argument
						{
							var argSymbol = documentData.SemanticModel.GetSymbolInfo(argumentExpression).Symbol;
							if (argSymbol is ILocalSymbol arglocalSymbol)
							{
								// TODO: local arguments
								functionReferenceData.Ignore(IgnoreReason.NotSupported("Local delegate arguments are currently not supported"));
								return;
							}
							if (argSymbol is IMethodSymbol argMethodSymbol)
							{
								// TODO: support custom async counterparts that have different parameters
								// If the invocation has at least one argument that does not fit into any async counterparts we have to ignore it
								if (functionReferenceData.ReferenceAsyncSymbols
									.Where(o => o.Parameters.Length >= methodSymbol.Parameters.Length) // The async counterpart may have less parameters. e.g. Parallel.For -> Task.WhenAll
									.All(o => !((IMethodSymbol) o.Parameters[i].Type.GetMembers("Invoke").First()).ReturnType.EqualTo(argMethodSymbol.ReturnType)))
								{
									functionReferenceData.Ignore(IgnoreReason.Custom("The delegate argument does not fit to any async counterparts", DiagnosticSeverity.Hidden));
									return;
								}
							}
						}
						continue;
					}
					functionReferenceData.AddDelegateArgument(new DelegateArgumentData(argRefFunction, i));
					argRefFunction.ArgumentOfFunctionInvocation = functionReferenceData;
				}
			}

			SetAsyncCounterpart(functionReferenceData);

			CalculateLastInvocation(node, functionReferenceData);

			foreach (var analyzer in _configuration.InvocationExpressionAnalyzers)
			{
				analyzer.AnalyzeInvocationExpression(node, functionReferenceData, documentData.SemanticModel);
			}

			PropagateCancellationToken(functionReferenceData);
		}

		/// <summary>
		/// Propagate CancellationTokenRequired to the method data only if the invocation can be async and the method does not have any external related methods (eg. external interface)
		/// </summary>
		private void PropagateCancellationToken(BodyFunctionDataReference functionReferenceData)
		{
			var methodData = functionReferenceData.Data.GetMethodOrAccessorData();
			if (functionReferenceData.Conversion != ReferenceConversion.ToAsync || methodData.ExternalRelatedMethods.Any())
			{
				return;
			}
			if (functionReferenceData.PassCancellationToken)
			{
				methodData.CancellationTokenRequired = true;
			}
			// Propagate if there is at least one async invocation inside an anoymous function that is passed as an argument to this invocation
			else if (functionReferenceData.DelegateArguments != null && functionReferenceData.DelegateArguments
				.Where(o => o.FunctionData != null)
				.Any(o => o.FunctionData.BodyFunctionReferences.Any(r => r.GetConversion() == ReferenceConversion.ToAsync && r.PassCancellationToken)))
			{
				methodData.CancellationTokenRequired = true;
			}
		}

		private void CalculateLastInvocation(SyntaxNode node, BodyFunctionDataReference functionReferenceData)
		{
			var functionData = functionReferenceData.Data;
			var methodSymbol = functionReferenceData.ReferenceSymbol;
			var functionBodyNode = functionData.GetBodyNode();
			if (functionBodyNode == null)
			{
				return;
			}
			// Check if the invocation node is returned in an expression body
			if (node.Parent.Equals(functionBodyNode) || //eg. bool ExpressionReturn() => SimpleFile.Write();
			    node.Equals(functionBodyNode) || // eg. Func<bool> fn = () => SimpleFile.Write();
				(
					node.IsKind(SyntaxKind.IdentifierName) && 
					node.Parent.Parent.IsKind(SyntaxKind.ArrowExpressionClause) &&
					node.Parent.Parent.Equals(functionBodyNode)
				) // eg. bool Prop => StaticClass.Property;
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
			if (functionReferenceData.ReferenceFunctionData == null && functionReferenceData.AsyncCounterpartSymbol != null)
			{
				functionReferenceData.UseAsReturnValue = !_configuration.CanAlwaysAwait(methodSymbol) && functionReferenceData.AsyncCounterpartSymbol.ReturnType.IsTaskType();
			}
			else if (!methodSymbol.ReturnsVoid)
			{
				functionReferenceData.UseAsReturnValue = !_configuration.CanAlwaysAwait(methodSymbol); // here we don't now if the method will be converted to async or not
			}
		}

		private void AnalyzeArgumentExpression(ArgumentSyntax node, SimpleNameSyntax nameNode, BodyFunctionDataReference result)
		{
			var documentData = result.Data.TypeData.NamespaceData.DocumentData;
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
				FindAsyncCounterparts(result);
				if (!SetAsyncCounterpart(result))
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
				result.Ignore(IgnoreReason.AlreadyAsync);

				//var argumentMethodSymbol = (IMethodSymbol)documentData.SemanticModel.GetSymbolInfo(nameNode).Symbol;
				//if (!argumentMethodSymbol.ReturnType.IsAwaitRequired(delegateMethod.ReturnType)) // i.e IList<T> -> IEnumerable<T>
				//{
				//	result.AwaitInvocation = true;
				//}
			}
		}

		private void FindAsyncCounterparts(BodyFunctionDataReference functionReferenceData)
		{
			var methodSymbol = functionReferenceData.ReferenceSymbol;
			methodSymbol = methodSymbol.ReducedFrom ?? methodSymbol; // System.Linq extensions

			functionReferenceData.ReferenceAsyncSymbols = new HashSet<IMethodSymbol>(GetAsyncCounterparts(methodSymbol.OriginalDefinition,
				functionReferenceData.InvokedFromType, _searchOptions)
				.Where(o => _configuration.IgnoreAsyncCounterpartsPredicates.All(p => !p(o))));
		}

		private bool SetAsyncCounterpart(BodyFunctionDataReference functionReferenceData)
		{
			var methodSymbol = functionReferenceData.ReferenceSymbol;
			methodSymbol = methodSymbol.ReducedFrom ?? methodSymbol; // System.Linq extensions
			var useTokens = _configuration.UseCancellationTokens | _configuration.CanScanForMissingAsyncMembers != null;
			if (functionReferenceData.ReferenceAsyncSymbols.Any())
			{
				if (functionReferenceData.ReferenceAsyncSymbols.All(o => o.ReturnsVoid || !o.ReturnType.IsTaskType()))
				{
					functionReferenceData.AwaitInvocation = false;
					functionReferenceData.AddDiagnostic("Cannot await method that is either void or do not return a Task", DiagnosticSeverity.Hidden);
				}

				var passToken = false;
				var analyzationResult = AnalyzeAsyncCandidates(functionReferenceData, functionReferenceData.ReferenceAsyncSymbols.ToList(), useTokens);
				if (analyzationResult.AsyncCandidate != null)
				{
					passToken = analyzationResult.AsyncCandidate.Parameters.Any(o => o.Type.IsCancellationToken());
				}

				if (analyzationResult.IgnoreDelegateArgumentsReason != null)
				{
					foreach (var delegateArgument in functionReferenceData.DelegateArguments)
					{
						delegateArgument.FunctionData?.Copy();
						delegateArgument.FunctionReference?.Ignore(analyzationResult.IgnoreDelegateArgumentsReason);
					}
				}

				if (analyzationResult.IgnoreBodyFunctionDataReferenceReason != null)
				{
					functionReferenceData.Ignore(analyzationResult.IgnoreBodyFunctionDataReferenceReason);
					return false;
				}

				if (analyzationResult.AsyncCandidate != null)
				{
					functionReferenceData.PassCancellationToken = passToken;
					functionReferenceData.AsyncCounterpartSymbol = analyzationResult.AsyncCandidate;
					functionReferenceData.AsyncCounterpartName = analyzationResult.AsyncCandidate.Name;
				}
				else
				{
					return false;
				}

				if (functionReferenceData.AsyncCounterpartSymbol != null &&
				    functionReferenceData.ArgumentOfFunctionInvocation == null &&
				    analyzationResult.CanBeAsync)
				{
					if (functionReferenceData.AsyncCounterpartSymbol.IsObsolete())
					{
						functionReferenceData.Ignore(IgnoreReason.CallObsoleteMethod);
					}
					else
					{
						functionReferenceData.ToAsync();
					}
				}

				// Ignore the method if we found its async counterpart
				if (functionReferenceData.ReferenceFunctionData is MethodOrAccessorData methodOrAccessorData)
				{
					if (passToken)
					{
						methodOrAccessorData.AsyncCounterpartWithTokenSymbol = analyzationResult.AsyncCandidate;
					}
					else
					{
						methodOrAccessorData.AsyncCounterpartSymbol = analyzationResult.AsyncCandidate;
					}
					methodOrAccessorData.Ignore(IgnoreReason.AsyncCounterpartExists);
				}
			}
			else if (!ProjectData.Contains(methodSymbol))
			{
				// If we are dealing with an external method and there are no async counterparts for it, we cannot convert it to async
				functionReferenceData.Ignore(IgnoreReason.NoAsyncCounterparts);
				return false;
			}
			else if (functionReferenceData.ReferenceFunctionData != null)
			{
				functionReferenceData.AsyncCounterpartName = functionReferenceData.ReferenceFunctionData.AsyncCounterpartName;
				functionReferenceData.AsyncCounterpartSymbol = methodSymbol;
			}
			return true;
		}
		
		internal class AnalyzationCandidateResult
		{
			public IMethodSymbol AsyncCandidate { get; set; }

			public bool CanBeAsync { get; set; }

			public IgnoreReason IgnoreDelegateArgumentsReason { get; set; }

			public IgnoreReason IgnoreBodyFunctionDataReferenceReason { get; set; }


		}

		private AnalyzationCandidateResult AnalyzeAsyncCandidate(BodyFunctionDataReference functionReferenceData,
			IMethodSymbol asyncCandidate, bool useCancellationToken)
		{
			var canBeAsync = true;
			var asnycDelegateIndexes = functionReferenceData.ReferenceSymbol.GetAsyncDelegateArgumentIndexes(asyncCandidate);

			if (asnycDelegateIndexes != null)
			{
				if (asnycDelegateIndexes.Count == 0 && functionReferenceData.DelegateArguments != null)
				{
					return new AnalyzationCandidateResult
					{
						AsyncCandidate = asyncCandidate,
						CanBeAsync = true,
						IgnoreDelegateArgumentsReason =
							IgnoreReason.Custom("Argument is not async.", DiagnosticSeverity.Hidden)
					};
				}
				if (asnycDelegateIndexes.Count > 0 && functionReferenceData.DelegateArguments == null)
				{
					return new AnalyzationCandidateResult
					{
						AsyncCandidate = null,
						CanBeAsync = false,
						IgnoreBodyFunctionDataReferenceReason =
							IgnoreReason.Custom("Delegate argument is not async.", DiagnosticSeverity.Hidden)
					};
				}

			}

			if (functionReferenceData.DelegateArguments == null)
			{
				return new AnalyzationCandidateResult
				{
					AsyncCandidate = asyncCandidate,
					CanBeAsync = true
				};
			}

			if (asnycDelegateIndexes != null)
			{
				var delegateIndexes = functionReferenceData.DelegateArguments.Select(o => o.Index).ToList();
				if (delegateIndexes.Count != asnycDelegateIndexes.Count ||
				    asnycDelegateIndexes.Any(o => !delegateIndexes.Contains(o)))
				{
					return new AnalyzationCandidateResult
					{
						AsyncCandidate = null,
						CanBeAsync = false,
						IgnoreBodyFunctionDataReferenceReason =
							IgnoreReason.Custom("Delegate arguments do not match with the async counterpart.", DiagnosticSeverity.Hidden)
					};
				}
			}

			foreach (var functionArgument in functionReferenceData.DelegateArguments)
			{
				var funcData = functionArgument.FunctionData;
				
				if (funcData == null)
				{
					var bodyRef = functionArgument.FunctionReference;
					funcData = bodyRef.ReferenceFunctionData;
					
					//if (!result.CanBeAsync)
					//{
					//	return new AnalyzationCandidateResult
					//	{
					//		AsyncCandidate = null,
					//		CanBeAsync = false,
					//		IgnoreBodyFunctionDataReferenceReason =
					//			IgnoreReason.Custom("Delegate argument cannot be async.", DiagnosticSeverity.Hidden)
					//	};
					//}

					if (funcData == null)
					{
						var result = AnalyzeAsyncCandidates(bodyRef, bodyRef.ReferenceAsyncSymbols.ToList(), useCancellationToken);
						if (result.AsyncCandidate != null && functionArgument.Index < asyncCandidate.Parameters.Length)
						{
							var delegateSymbol = (IMethodSymbol)asyncCandidate.Parameters[functionArgument.Index].Type.GetMembers("Invoke").First();
							if (!delegateSymbol.MatchesDefinition(result.AsyncCandidate, true))
							{
								return new AnalyzationCandidateResult
								{
									AsyncCandidate = null,
									CanBeAsync = false,
									IgnoreBodyFunctionDataReferenceReason =
										IgnoreReason.Custom("Delegate argument async counterpart does not match.", DiagnosticSeverity.Hidden)
								};
							}
						}

						continue;
					}
				}
				if (funcData.BodyFunctionReferences.All(o => o.GetConversion() == ReferenceConversion.Ignore))
				{
					return new AnalyzationCandidateResult
					{
						AsyncCandidate = null,
						CanBeAsync = false,
						IgnoreBodyFunctionDataReferenceReason =
							IgnoreReason.Custom("The delegate argument does not have any async invocation.", DiagnosticSeverity.Hidden)
					};
				}

				canBeAsync &= funcData.BodyFunctionReferences.Any(o => o.GetConversion() == ReferenceConversion.ToAsync);
				//if (funcData.Symbol.MethodKind != MethodKind.AnonymousFunction)
				//{
				//	CalculatePreserveReturnType(funcData);
				//}

				//// Check if the return type of the delegate parameter matches with the calculated return type of the anonymous function
				//if (
				//	(delegateSymbol.ReturnType.SupportsTaskType() && !funcData.PreserveReturnType) ||
				//	(!delegateSymbol.ReturnType.SupportsTaskType() && funcData.PreserveReturnType && !funcData.Symbol.ReturnType.SupportsTaskType())
				//)
				//{
				//	continue;
				//}

				//return new AnalyzationCandidateResult
				//{
				//	AsyncCandidate = null,
				//	CanBeAsync = false,
				//	IgnoreBodyFunctionDataReferenceReason =
				//		IgnoreReason.Custom("Return type of the delegate argument does not match.", DiagnosticSeverity.Hidden)
				//};
			}

			return new AnalyzationCandidateResult
			{
				AsyncCandidate = asyncCandidate,
				CanBeAsync = canBeAsync
			};
		}


		private class MethodCancellationTokenComparer : IComparer<IMethodSymbol>
		{
			public static readonly MethodCancellationTokenComparer Instance = new MethodCancellationTokenComparer();

			public int Compare(IMethodSymbol x, IMethodSymbol y)
			{
				var xHasToken = x.Parameters.Any(p => p.Type.IsCancellationToken());
				var yHasToken = y.Parameters.Any(p => p.Type.IsCancellationToken());
				if (xHasToken == yHasToken)
				{
					return 0;
				}

				return xHasToken ? -1 : 1;
			}
		}

		private AnalyzationCandidateResult AnalyzeAsyncCandidates(BodyFunctionDataReference functionReferenceData, IEnumerable<IMethodSymbol> asyncCandidates, bool preferCancellationToken)
		{
			var orderedCandidates = preferCancellationToken
				? asyncCandidates.OrderBy(o => o, MethodCancellationTokenComparer.Instance).ToList()
				: asyncCandidates.OrderByDescending(o => o, MethodCancellationTokenComparer.Instance).ToList();

			if (orderedCandidates.Count == 0)
			{
				return new AnalyzationCandidateResult
				{
					AsyncCandidate = null,
					CanBeAsync = false,
					IgnoreBodyFunctionDataReferenceReason = IgnoreReason.NoAsyncCounterparts
				};
			}

			// More than one
			// By default we will get here when there are multiple overloads of an async function (e.g. Task.Run<T>(Func<T>, CancellationToken) and Task.Run<T>(Func<Task<T>>, CancellationToken))
			// In the Task.Run case we have to check the delegate argument if it can be async or not (the delegate argument will be processed before the invocation)
			//if (functionReferenceData.DelegateArguments == null)
			//{
			//	return new AnalyzationCandidateResult
			//	{
			//		AsyncCandidate = null,
			//		CanBeAsync = false,
			//		IgnoreBodyFunctionDataReferenceReason = IgnoreReason.Custom("Multiple async counterparts without delegate arguments.", DiagnosticSeverity.Info)
			//	};
			//}

			var validCandidates = new List<AnalyzationCandidateResult>();
			foreach (var asyncCandidate in orderedCandidates)
			{
				var result = AnalyzeAsyncCandidate(functionReferenceData, asyncCandidate, preferCancellationToken);
				if (result.AsyncCandidate != null)
				{
					validCandidates.Add(result);
				}
			}

			if (validCandidates.Count == 0)
			{
				return new AnalyzationCandidateResult
				{
					AsyncCandidate = null,
					CanBeAsync = false,
					IgnoreBodyFunctionDataReferenceReason = IgnoreReason.Custom("No async counterparts matches delegate arguments.", DiagnosticSeverity.Info)
				};
			}

			return validCandidates[0];
		}
	}
}
