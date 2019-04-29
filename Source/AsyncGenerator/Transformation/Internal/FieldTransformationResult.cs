using System.Collections.Generic;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	internal class FieldTransformationResult : TransformationResult<BaseFieldDeclarationSyntax>
	{
		public FieldTransformationResult(IFieldAnalyzationResult fieldAnalyzationResult) : base(fieldAnalyzationResult.Node)
		{
			AnalyzationResult = fieldAnalyzationResult;
		}

		public IFieldAnalyzationResult AnalyzationResult { get; }

		public List<FieldVariableTransformationResult> TransformedVariables { get; } = new List<FieldVariableTransformationResult>();

		public SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia EndOfLineTrivia { get; set; }

		public SyntaxTrivia IndentTrivia { get; set; }
	}
}
