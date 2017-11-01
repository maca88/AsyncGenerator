using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Plugins.Internal
{
	internal class ParallelForForEachTransformer : AbstractPlugin, IAsyncCounterpartsFinder, IFunctionReferenceTransformer,
		IInvocationExpressionAnalyzer
	{
		private IMethodSymbol _whenAllMethod;
		private IMethodSymbol _forEachMethod;
		private List<IMethodSymbol> _forMethods;

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
					o.Parameters[1].Type is INamedTypeSymbol namedType &&
					namedType.IsGenericType &&
					namedType.TypeArguments.Length == 1 // Action<TSource>
				);

			// Try to get members
			// ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int> body)
			// ParallelLoopResult For(long fromInclusive, long toExclusive, Action<long> body)
			_forMethods = parallelSymbol.GetMembers("For").OfType<IMethodSymbol>()
				.Where(o =>
					o.Parameters.Length == 3 &&
					o.Parameters[2].Type is INamedTypeSymbol namedType &&
					namedType.IsGenericType &&
					namedType.TypeArguments.Length == 1 // Action<int> or Action<long>
				).ToList();

			return Task.CompletedTask;
		}

		public void Analyze(InvocationExpressionSyntax invocation, IFunctionReferenceAnalyzation funReferenceResult,
			SemanticModel semanticModel)
		{
			if (funReferenceResult.AsyncCounterpartSymbol?.Equals(_whenAllMethod) != true)
			{
				return;
			}
			// Ignore if the Parallel.For/ForEach return value is used
			if (!invocation.Parent.IsKind(SyntaxKind.ExpressionStatement))
			{
				funReferenceResult.Ignore("Unable to convert to Task.WaitAll, because the return value is used");
			}
		}

		public SyntaxNode TransformFunctionReference(SyntaxNode node, IFunctionReferenceAnalyzationResult funReferenceResult,
			INamespaceTransformationMetadata namespaceMetadata)
		{
			if (!funReferenceResult.AsyncCounterpartSymbol.Equals(_whenAllMethod))
			{
				return node;
			}
			if (!(node is InvocationExpressionSyntax invokeNode) ||
			    !(funReferenceResult is IBodyFunctionReferenceAnalyzationResult bodyReference))
			{
				return node; // Should not happen
			}

			// Here are some examples of expected nodes
			// Task.WhenAll(Results, ReadAsync)
			// Task.WhenAll(Enumerable.Empty<string>(), ReadAsync)
			// Task.WhenAll(GetStringList(), i =>
			// {
			//	return SimpleFile.ReadAsync();
			// })

			// We then need to combine the two arguments into one, using the Select Linq extension e.g.
			// Task.WhenAll(Results.Select(i => ReadAsync(i))

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
				newExpression = newExpression.WrapInsideFunction(actionMethod, false, namespaceMetadata.TaskConflict);
			}
			var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
				invokeNode.ArgumentList.Arguments.First().Expression,
				SyntaxFactory.Token(SyntaxKind.DotToken),
				SyntaxFactory.IdentifierName("Select"));
			var argument = SyntaxFactory.InvocationExpression(memberAccess)
				.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(newExpression))));
			return invokeNode.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(argument))));
		}

		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol syncMethodSymbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options)
		{
			switch (syncMethodSymbol.Name)
			{
				case "ForEach" when syncMethodSymbol.Equals(_forEachMethod):
					yield return _whenAllMethod;
					break;
				case "For" when _forMethods.Any(o => o.Equals(syncMethodSymbol)):
					yield return _whenAllMethod;
					break;
			}
		}
	}
}
