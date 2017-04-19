using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal class CrefFunctionReferenceData : AbstractFunctionReferenceData
	{
		public CrefFunctionReferenceData(ReferenceLocation reference, SimpleNameSyntax referenceNameNode,
			IMethodSymbol referenceSymbol, FunctionData referenceFunctionData)
			: base(reference, referenceNameNode, referenceSymbol, referenceFunctionData)
		{
		}

		public List<InvokeFunctionReferenceData> RelatedInvokeFunctionReferences { get; } = new List<InvokeFunctionReferenceData>();

		public override ReferenceConversion GetConversion()
		{
			return (ReferenceFunctionData?.Conversion == MethodConversion.ToAsync) || RelatedInvokeFunctionReferences.Any(o => o.GetConversion() == ReferenceConversion.ToAsync)
				? ReferenceConversion.ToAsync
				: ReferenceConversion.Ignore;
		}

		public override string AsyncCounterpartName
		{
			get
			{
				return (ReferenceFunctionData?.Conversion == MethodConversion.ToAsync)
					? ReferenceFunctionData.Symbol.Name + "Async"
					: RelatedInvokeFunctionReferences.FirstOrDefault()?.AsyncCounterpartName;
			}
			set
			{
				throw new NotSupportedException($"Setting {nameof(AsyncCounterpartName)} for {nameof(CrefFunctionReferenceData)} is not supported");
			}
		}
	}
}
