using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// Remove disabled text in directives that are defined outside a method
	/// </summary>
	internal class DisabledTextTypeTransformer : ITypeTransformer
	{
		private readonly TriviaRemover _triviaRemover;

		public DisabledTextTypeTransformer()
		{
			_triviaRemover = new TriviaRemover(
				trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia),
				node => node != null && node.ContainsDirectives && (node.Parent?.IsFunction() != true || node.Parent?.GetFunctionBody() != node));
		}

		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			return Task.CompletedTask;
		}

		public TypeDeclarationSyntax Transform(TypeDeclarationSyntax transformedNode, ITypeTransformationResult transformationResult,
			INamespaceTransformationMetadata namespaceMetadata, bool missingMembers)
		{
			if (!transformedNode.ContainsDirectives)
			{
				return transformedNode;
			}

			return (TypeDeclarationSyntax) _triviaRemover.Visit(transformedNode);
		}
	}
}
