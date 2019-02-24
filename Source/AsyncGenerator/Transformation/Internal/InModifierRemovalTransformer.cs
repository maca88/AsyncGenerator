using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	internal class InModifierRemovalTransformer : IMethodOrAccessorTransformer, IFunctionTransformer
	{
		private readonly InModifierRemovalRewriter _rewriter = new InModifierRemovalRewriter();

		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			return Task.CompletedTask;
		}

		public SyntaxNode Transform(IFunctionTransformationResult transformResult,
			ITypeTransformationMetadata typeMetadata,
			INamespaceTransformationMetadata namespaceMetadata)
		{
			return transformResult.AnalyzationResult.OmitAsync ? null : _rewriter.VisitFunction(transformResult.Transformed);
		}

		public MethodTransformerResult Transform(IMethodOrAccessorTransformationResult transformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			if (transformResult.AnalyzationResult.OmitAsync)
			{
				return MethodTransformerResult.Skip;
			}

			var result = _rewriter.VisitMethod(transformResult.Transformed);
			return result == null
				? MethodTransformerResult.Skip
				: MethodTransformerResult.Update(result);
		}

		private class InModifierRemovalRewriter : CSharpSyntaxRewriter
		{
			public override SyntaxNode VisitParameter(ParameterSyntax node)
			{
				return node.Modifiers.Count == 0
					? node
					: node.WithModifiers(TokenList(node.Modifiers.Where(o => !o.IsKind(SyntaxKind.InKeyword))));
			}

			internal MethodDeclarationSyntax VisitMethod(MethodDeclarationSyntax method)
			{
				if (method.ParameterList == null ||
					method.ParameterList.DescendantTokens().All(o => !o.IsKind(SyntaxKind.InKeyword)))
				{
					return null;
				}

				return method.WithParameterList((ParameterListSyntax)VisitParameterList(method.ParameterList));
			}

			internal SyntaxNode VisitFunction(SyntaxNode node)
			{
				switch (node.Kind())
				{
					case SyntaxKind.SimpleLambdaExpression:
						var lambda = (SimpleLambdaExpressionSyntax)node;
						if (lambda.Parameter == null || lambda.Parameter.Modifiers.All(o => !o.IsKind(SyntaxKind.InKeyword)))
						{
							return null;
						}

						return lambda.WithParameter((ParameterSyntax)VisitParameter(lambda.Parameter));
					case SyntaxKind.ParenthesizedLambdaExpression:
						var parenLambda = (ParenthesizedLambdaExpressionSyntax)node;
						if (parenLambda.ParameterList == null ||
							parenLambda.ParameterList.DescendantTokens().All(o => !o.IsKind(SyntaxKind.InKeyword)))
						{
							return null;
						}

						return parenLambda.WithParameterList((ParameterListSyntax)VisitParameterList(parenLambda.ParameterList));
					case SyntaxKind.LocalFunctionStatement:
						var localFunction = (LocalFunctionStatementSyntax)node;
						if (localFunction.ParameterList == null ||
							localFunction.ParameterList.DescendantTokens().All(o => !o.IsKind(SyntaxKind.InKeyword)))
						{
							return null;
						}

						return localFunction.WithParameterList((ParameterListSyntax)VisitParameterList(localFunction.ParameterList));
					case SyntaxKind.MethodDeclaration:
						return VisitMethod((MethodDeclarationSyntax)node);
					default:
						return null;
				}
			}
		}
	}
}
