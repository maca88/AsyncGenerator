using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	internal class MethodTransformationResult : TransformationResult<MethodDeclarationSyntax>, IMethodTransformationResult
	{
		public MethodTransformationResult(IMethodAnalyzationResult result) : base(result.Node)
		{
			AnalyzationResult = result;
		}

		public IMethodAnalyzationResult AnalyzationResult { get; }

		public List<FieldDeclarationSyntax> Fields { get; set; }

		public List<MethodDeclarationSyntax> Methods { get; private set; }

		public List<FunctionReferenceTransformationResult> TransformedFunctionReferences { get; } = new List<FunctionReferenceTransformationResult>();

		public SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia BodyLeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia EndOfLineTrivia { get; set; }

		public SyntaxTrivia IndentTrivia { get; set; }

		// TODO: find a better approach
		public string TaskReturnedAnnotation { get; set; } = "TaskReturned";

		#region IMethodTransformationResult

		private IReadOnlyList<IFunctionReferenceTransformationResult> _cachedTransformedFunctionReferences;
		IReadOnlyList<IFunctionReferenceTransformationResult> IMethodTransformationResult.TransformedFunctionReferences =>
			_cachedTransformedFunctionReferences ?? (_cachedTransformedFunctionReferences = TransformedFunctionReferences.Where(o => o.Transformed != null).ToImmutableList());

		#endregion


		public void AddMethod(MethodDeclarationSyntax node)
		{
			Methods = Methods ?? new List<MethodDeclarationSyntax>();
			Methods.Add(node);
		}

		public void AddMethods(IEnumerable<MethodDeclarationSyntax> nodes)
		{
			Methods = Methods ?? new List<MethodDeclarationSyntax>();
			Methods.AddRange(nodes);
		}

		public override IEnumerable<SyntaxNode> GetTransformedNodes()
		{
			if (Fields != null)
			{
				foreach (var field in Fields)
				{
					yield return field;
				}
			}

			if (Transformed != null)
			{
				yield return Transformed;
			}
			if (Methods == null)
			{
				yield break;
			}
			foreach (var method in Methods)
			{
				yield return method;
			}
		}

		public IMemberAnalyzationResult GetAnalyzationResult()
		{
			return AnalyzationResult;
		}

		IMemberAnalyzationResult IMemberTransformationResult.GetAnalyzationResult()
		{
			throw new NotImplementedException();
		}
	}
}
