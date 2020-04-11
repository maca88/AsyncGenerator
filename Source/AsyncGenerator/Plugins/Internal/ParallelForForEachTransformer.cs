using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Extensions.Internal;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Plugins.Internal
{
	internal class ParallelForForEachTransformer : AbstractPlugin,
		IAsyncCounterpartsFinder,
		IFunctionReferenceTransformer,
		IInvocationExpressionAnalyzer,
		IBodyFunctionReferencePostAnalyzer
	{
		private IMethodSymbol _whenAllMethod;
		private IMethodSymbol _forEachMethod;
		private IMethodSymbol _forMethod;

		public override Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			var taskSymbol =
				compilation.References
					.Select(compilation.GetAssemblyOrModuleSymbol)
					.OfType<IAssemblySymbol>()
					.Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName("System.Threading.Tasks.Task"))
					.FirstOrDefault(o => o != null);
			if (taskSymbol == null)
			{
				throw new InvalidOperationException("Unable to find System.Threading.Tasks.Task type");
			}
			// Get member:
			// Task WhenAll(IEnumerable<Task> tasks)
			_whenAllMethod = taskSymbol.GetMembers("WhenAll").OfType<IMethodSymbol>()
				.First(o => o.Parameters.Length == 1 &&
				            !o.IsGenericMethod &&
				            !o.Parameters[0].IsParams);

			var parallelSymbol =
				compilation.References
					.Select(compilation.GetAssemblyOrModuleSymbol)
					.OfType<IAssemblySymbol>()
					.Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName("System.Threading.Tasks.Parallel"))
					.FirstOrDefault(o => o != null);
			if (parallelSymbol == null)
			{
				return Task.CompletedTask;
			}

			// Try to get member:
			// ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
			_forEachMethod = parallelSymbol.GetMembers("ForEach").OfType<IMethodSymbol>()
				.FirstOrDefault(o =>
					o.Parameters.Length == 2 &&
					o.Parameters[0].Type.Name == "IEnumerable" &&
					o.Parameters[1].Type is INamedTypeSymbol namedType &&
					namedType.IsGenericType &&
					namedType.TypeArguments.Length == 1 // Action<TSource>
				);

			// Try to get member
			// ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int> body)
			_forMethod = parallelSymbol.GetMembers("For").OfType<IMethodSymbol>()
				.FirstOrDefault(o =>
					o.Parameters.Length == 3 &&
					o.Parameters[0].Type.Name == "Int32" &&
					o.Parameters[2].Type is INamedTypeSymbol namedType &&
					namedType.IsGenericType &&
					namedType.TypeArguments.Length == 1 // Action<int> or Action<long>
				);

			return Task.CompletedTask;
		}

		public void PostAnalyzeBodyFunctionReference(IBodyFunctionReferenceAnalyzation functionReferenceAnalyzation)
		{
			if (functionReferenceAnalyzation.AsyncCounterpartSymbol?.EqualTo(_whenAllMethod) != true)
			{
				return;
			}
			var delegateArgument = functionReferenceAnalyzation.DelegateArguments.FirstOrDefault(o => o.Function != null);
			if (delegateArgument?.Function.Conversion.HasAnyFlag(MethodConversion.Ignore, MethodConversion.Copy) == true)
			{
				functionReferenceAnalyzation.Ignore("Delegate argument is not async");
			}
		}

		public void AnalyzeInvocationExpression(InvocationExpressionSyntax invocation, IBodyFunctionReferenceAnalyzation funcReferenceResult,
			SemanticModel semanticModel)
		{
			if (funcReferenceResult.AsyncCounterpartSymbol?.EqualTo(_whenAllMethod) != true)
			{
				return;
			}
			// Ignore if the Parallel.For/ForEach return value is used
			if (!invocation.Parent.IsKind(SyntaxKind.ExpressionStatement))
			{
				funcReferenceResult.Ignore("Unable to convert to Task.WaitAll, because the return value is used");
			}
		}

		public SyntaxNode TransformFunctionReference(SyntaxNode node, IFunctionAnalyzationResult funcResult,
			IFunctionReferenceAnalyzationResult funcReferenceResult,
			INamespaceTransformationMetadata namespaceMetadata)
		{
			if (!funcReferenceResult.AsyncCounterpartSymbol.EqualTo(_whenAllMethod))
			{
				return node;
			}
			if (!(node is InvocationExpressionSyntax invokeNode) ||
			    !(funcReferenceResult is IBodyFunctionReferenceAnalyzationResult bodyReference))
			{
				return node; // Cref
			}

			// Here are some examples of expected nodes
			// Task.WhenAll(Results, ReadAsync)
			// Task.WhenAll(1, 100, ReadAsync)
			// Task.WhenAll(Enumerable.Empty<string>(), ReadAsync)
			// Task.WhenAll(GetStringList(), i =>
			// {
			//	return SimpleFile.ReadAsync();
			// })

			// For Parallel.ForEach, we need to combine the two arguments into one, using the Select Linq extension e.g.
			// Task.WhenAll(Results.Select(i => ReadAsync(i))
			// For Parallel.For, we need to move the first two parameters into Enumerable.Range and then apply the same logic as for
			// Parallel.ForEach

			var actionParam = bodyReference.ReferenceSymbol.Parameters.Last();
			var actionType = actionParam.Type as INamedTypeSymbol;
			var actionMethod = actionType?.DelegateInvokeMethod;

			if (actionMethod == null)
			{
				throw new InvalidOperationException(
					$"Unable to transform Parallel.{bodyReference.ReferenceSymbol.Name} to Task.WaitAll. " +
					$"The second Parallel.{bodyReference.ReferenceSymbol.Name} argument is not a delegate, but is {actionParam.Type}");
			}
			namespaceMetadata.AddUsing("System.Linq");
			var newExpression = invokeNode.ArgumentList.Arguments.Last().Expression;
			if (!(newExpression is AnonymousFunctionExpressionSyntax))
			{
				var delArgument = bodyReference.DelegateArguments.Last();
				var cancellationTokenParamName = funcResult.GetMethodOrAccessor().CancellationTokenRequired
					? "cancellationToken"
					: null; // TODO: find a way to not have this duplicated and fix naming colision
				newExpression = newExpression.WrapInsideFunction(actionMethod, false, namespaceMetadata.TaskConflict,
					invoke => invoke.AddCancellationTokenArgumentIf(cancellationTokenParamName, delArgument.BodyFunctionReference));
			}
			ExpressionSyntax enumerableExpression;
			if (bodyReference.ReferenceSymbol.EqualTo(_forMethod))
			{
				// Construct an Enumerable.Range(1, 10 - 1), where 1 and 10 are the first two arguments of Parallel.For method
				var startArg = invokeNode.ArgumentList.Arguments.First();
				enumerableExpression = InvocationExpression(
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName("Enumerable").WithLeadingTrivia(startArg.GetLeadingTrivia()),
						Token(SyntaxKind.DotToken),
						IdentifierName("Range")),
					ArgumentList(
						SeparatedList<ArgumentSyntax>(
							new SyntaxNodeOrToken[]
							{
								startArg.WithoutTrivia(),
								Token(TriviaList(), SyntaxKind.CommaToken, TriviaList(Space)),
								Argument(
									BinaryExpression(SyntaxKind.SubtractExpression,
										invokeNode.ArgumentList.Arguments.Skip(1).First().Expression.WithoutTrivia().WithTrailingTrivia(Space),
										Token(TriviaList(), SyntaxKind.MinusToken, TriviaList(Space)),
										startArg.WithoutTrivia().Expression))
							}
						)
					)
				);
			}
			else
			{
				enumerableExpression = invokeNode.ArgumentList.Arguments.First().Expression; // For ForEach take the first parmeter e.g. Enumerable.Range(1, 10)
			}

			var memberAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
				enumerableExpression,
				Token(SyntaxKind.DotToken),
				IdentifierName("Select"));
			var argument = InvocationExpression(memberAccess)
				.WithArgumentList(
					ArgumentList(SingletonSeparatedList(Argument(newExpression.WithoutTrivia())))
						.WithCloseParenToken(Token(TriviaList(), SyntaxKind.CloseParenToken, newExpression.GetTrailingTrivia()))
				);
			return invokeNode.WithArgumentList(
				invokeNode.ArgumentList.WithArguments(SingletonSeparatedList(Argument(argument))));
		}

		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol syncMethodSymbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options)
		{
			switch (syncMethodSymbol.Name)
			{
				case "ForEach" when syncMethodSymbol.EqualTo(_forEachMethod):
					yield return _whenAllMethod;
					break;
				case "For" when _forMethod.EqualTo(syncMethodSymbol):
					yield return _whenAllMethod;
					break;
			}
		}
	}
}
