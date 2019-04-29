using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	internal class FieldVariableTransformationResult : TransformationResult<VariableDeclaratorSyntax>
	{
		public FieldVariableTransformationResult(IFieldVariableDeclaratorResult variableAnalyzationResult) : base(variableAnalyzationResult.Node)
		{
			AnalyzationResult = variableAnalyzationResult;
		}

		public IFieldVariableDeclaratorResult AnalyzationResult { get; }
	}
}
