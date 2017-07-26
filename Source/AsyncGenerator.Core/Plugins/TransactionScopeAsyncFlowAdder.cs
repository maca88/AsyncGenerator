using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Plugins
{
	/// <summary>
	/// Adds TransactionScopeAsyncFlowOption option for TransactionScope
	/// </summary>
	public class TransactionScopeAsyncFlowAdder : CSharpSyntaxRewriter, IMethodOrAccessorTransformer
	{
		public Task Initialize(Project project, IProjectConfiguration configuration)
		{
			return Task.CompletedTask;
		}

		public MethodTransformerResult Transform(IMethodOrAccessorTransformationResult methodTransformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			return MethodTransformerResult.Update((MethodDeclarationSyntax) VisitMethodDeclaration(methodTransformResult.Transformed));
		}

		public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
		{
			if (node.Type.ToString() != "TransactionScope")
			{
				return base.VisitObjectCreationExpression(node);
			}
			if (node.ArgumentList.Arguments.Any(o => o.Expression.ToString().StartsWith("TransactionScopeAsyncFlowOption")))
			{
				//TODO: what to do for TransactionScopeAsyncFlowOption.Suppress?
				return node; // argument is already there
			}
			var argument = SyntaxFactory.Argument(
				SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					SyntaxFactory.IdentifierName("TransactionScopeAsyncFlowOption"),
					SyntaxFactory.IdentifierName("Enabled")));
			var arguments = node.ArgumentList.Arguments.GetWithSeparators()
				.Union(node.ArgumentList.Arguments.Count > 0
					? new SyntaxNodeOrToken[] { SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.CommaToken, SyntaxFactory.TriviaList(SyntaxFactory.Space)), argument }
					: new SyntaxNodeOrToken[] { argument });
			return node.WithArgumentList(
				node.ArgumentList.WithArguments(SyntaxFactory.SeparatedList<ArgumentSyntax>(arguments)));
		}
	}
}
