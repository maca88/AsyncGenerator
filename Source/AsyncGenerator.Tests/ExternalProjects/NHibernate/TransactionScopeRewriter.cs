using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Plugins;
using AsyncGenerator.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Tests.ExternalProjects.NHibernate
{
	public class TransactionScopeRewriter : CSharpSyntaxRewriter, IMethodTransformer
	{
		public Task Initialize(Project project, IProjectConfiguration configuration)
		{
			return Task.CompletedTask;
		}

		public MethodTransformerResult Transform(IMethodTransformationResult methodTransformResult,
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
			return node.WithArgumentList(
				node.ArgumentList.WithArguments(
					node.ArgumentList.Arguments.Add(
						Argument(
							MemberAccessExpression(
								SyntaxKind.SimpleMemberAccessExpression,
								IdentifierName("TransactionScopeAsyncFlowOption"),
								IdentifierName("Enabled"))))));
		}
	}
}
