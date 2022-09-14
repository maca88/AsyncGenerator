using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	internal class PreprocessorDirectivesMethodTransformer : IMethodOrAccessorTransformer
	{
		private IProjectPreprocessorDirectivesConfiguration _configuration;
		private bool _enabled;

		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			_configuration = configuration.TransformConfiguration.PreprocessorDirectives;
			_enabled = _configuration.AddForMethod != null;

			return Task.CompletedTask;
		}

		public MethodTransformerResult Transform(IMethodOrAccessorTransformationResult methodTransformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			if (!_enabled)
			{
				return MethodTransformerResult.Skip;
			}

			var directives = _configuration.AddForMethod(methodTransformResult.AnalyzationResult.Symbol);
			if (directives == null)
			{
				return MethodTransformerResult.Skip;
			}

			var transformed = methodTransformResult.Transformed;
			var start = directives.StartDirective;
			var end = directives.EndDirective;
			var eol = methodTransformResult.EndOfLineTrivia.ToFullString();
			var nodes = CSharpSyntaxTree.ParseText($"{start}{eol}{end}{eol}").GetRoot().GetLeadingTrivia();
			// Add directives after other processor directives
			var methodLeadingTrivia = transformed.GetLeadingTrivia();
			var leadingTriviaIndex = 0;
			for (var i = methodLeadingTrivia.Count - 1; i >= 0; i--)
			{
				if (methodLeadingTrivia[i].GetStructure() is DirectiveTriviaSyntax)
				{
					leadingTriviaIndex = i + 1;
					break;
				}
			}

			// Add directives before other processor directives
			var methodTrailingTrivia = transformed.GetTrailingTrivia();
			var trailingTriviaIndex = methodTrailingTrivia.Count;
			for (var i = 0; i < methodTrailingTrivia.Count; i++)
			{
				if (methodTrailingTrivia[i].GetStructure() is DirectiveTriviaSyntax)
				{
					trailingTriviaIndex = i;
					break;
				}
			}

			return MethodTransformerResult.Update(transformed
				.WithLeadingTrivia(methodLeadingTrivia.Insert(leadingTriviaIndex, nodes.First()))
				.WithTrailingTrivia(methodTrailingTrivia.Insert(trailingTriviaIndex, nodes.Last())));
		}
	}
}
