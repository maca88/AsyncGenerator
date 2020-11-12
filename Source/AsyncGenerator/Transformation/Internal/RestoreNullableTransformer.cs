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
	internal class RestoreNullableTransformer : IDocumentTransformer
	{
		private bool _enabled;

		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			_enabled = ((CSharpCompilationOptions) project.CompilationOptions).NullableContextOptions != NullableContextOptions.Disable;
			return Task.CompletedTask;
		}

		public CompilationUnitSyntax Transform(IDocumentTransformationResult transformationResult)
		{
			if (!_enabled)
			{
				return null;
			}

			var transformed = transformationResult.Transformed;
			var firstNode = transformed.DescendantNodes().FirstOrDefault();
			if (firstNode == null)
			{
				return null;
			}

			var directive = NullableDirectiveTrivia(
				Token(SyntaxKind.HashToken),
				Token(TriviaList(), SyntaxKind.NullableKeyword, TriviaList(Space)),
				Token(SyntaxKind.RestoreKeyword),
#if !LEGACY
				default,
#endif
				Token(TriviaList(), SyntaxKind.EndOfDirectiveToken, TriviaList(transformationResult.EndOfLineTrivia)), true);

			return transformed.ReplaceNode(firstNode,
				firstNode.WithLeadingTrivia(firstNode.GetLeadingTrivia().Add(Trivia(directive))));
		}
	}
}