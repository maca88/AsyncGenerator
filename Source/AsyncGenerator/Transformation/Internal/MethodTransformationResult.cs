using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	internal class MethodTransformationResult : TransformationResult, IMethodTransformationResult
	{
		public MethodTransformationResult(IMethodAnalyzationResult result) : base(result.Node)
		{
			AnalyzationResult = result;
		}

		public IMethodAnalyzationResult AnalyzationResult { get; }

		public MethodDeclarationSyntax TailMethodNode { get; set; }

		public FieldDeclarationSyntax AsyncLockField { get; set; }

		public SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia BodyLeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia EndOfLineTrivia { get; set; }

		public SyntaxTrivia IndentTrivia { get; set; }

		public string TaskReturnedAnnotation { get; set; } = "TaskReturned";

		public override IEnumerable<SyntaxNode> GetTransformedNodes()
		{
			if (TransformedNode != null)
			{
				yield return TransformedNode;
			}
			if (TailMethodNode != null)
			{
				yield return TailMethodNode;
			}
		}

		public IMemberAnalyzationResult GetAnalyzationResult()
		{
			return AnalyzationResult;
		}
	}
}
