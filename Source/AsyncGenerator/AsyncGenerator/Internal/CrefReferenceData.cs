using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal class CrefReferenceData : AbstractFunctionReferenceData
	{
		public CrefReferenceData(ReferenceLocation reference, SimpleNameSyntax referenceNameNode,
			IMethodSymbol referenceSymbol, FunctionData referenceFunctionData)
			: base(reference, referenceNameNode, referenceSymbol, referenceFunctionData)
		{
		}

		public override ReferenceConversion GetConversion()
		{
			return ReferenceFunctionData?.Conversion == MethodConversion.ToAsync
				? ReferenceConversion.ToAsync
				: ReferenceConversion.Ignore;
		}
	}
}
