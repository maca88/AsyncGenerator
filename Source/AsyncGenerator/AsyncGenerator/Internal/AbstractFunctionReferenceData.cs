using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal abstract class AbstractFunctionReferenceData : AbstractReferenceData<IMethodSymbol>, IFunctionReferenceAnalyzationResult
	{
		protected AbstractFunctionReferenceData(ReferenceLocation reference, SimpleNameSyntax referenceNameNode,
			IMethodSymbol referenceSymbol, FunctionData referenceFunctionData)
			: base(reference, referenceNameNode, referenceSymbol)
		{
			ReferenceFunctionData = referenceFunctionData;
		}

		public FunctionData ReferenceFunctionData { get; }

		public SyntaxNode ReferenceNode { get; internal set; }

		public abstract ReferenceConversion GetConversion();

		#region IReferenceAnalyzationResult

		IFunctionAnalyzationResult IFunctionReferenceAnalyzationResult.ReferenceFunctionData => ReferenceFunctionData;

		#endregion
	}
}
