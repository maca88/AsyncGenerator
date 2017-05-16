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

		public List<BodyFunctionReferenceData> RelatedBodyFunctionReferences { get; } = new List<BodyFunctionReferenceData>();

		public override ReferenceConversion GetConversion()
		{
			return (ReferenceFunctionData?.Conversion == MethodConversion.ToAsync) || RelatedBodyFunctionReferences.Any(o => o.GetConversion() == ReferenceConversion.ToAsync)
				? ReferenceConversion.ToAsync
				: ReferenceConversion.Ignore;
		}

		public override ReferenceConversion Conversion { get; set; }

		public override string AsyncCounterpartName
		{
			get => ReferenceFunctionData?.Conversion == MethodConversion.ToAsync
				? ReferenceFunctionData.Symbol.Name + "Async"
				: RelatedBodyFunctionReferences.FirstOrDefault()?.AsyncCounterpartName;
			set => throw new NotSupportedException($"Setting {nameof(AsyncCounterpartName)} for {nameof(CrefFunctionReferenceData)} is not supported");
		}

		public override IMethodSymbol AsyncCounterpartSymbol
		{
			get => ReferenceFunctionData?.Conversion == MethodConversion.ToAsync
				? ReferenceFunctionData.Symbol
				: RelatedBodyFunctionReferences.FirstOrDefault()?.AsyncCounterpartSymbol;
			set => throw new NotSupportedException($"Setting {nameof(AsyncCounterpartSymbol)} for {nameof(CrefFunctionReferenceData)} is not supported");
		}

		public override FunctionData AsyncCounterpartFunction
		{
			get => ReferenceFunctionData?.Conversion == MethodConversion.ToAsync
				? ReferenceFunctionData
				: null;
			set => throw new NotSupportedException($"Setting {nameof(AsyncCounterpartFunction)} for {nameof(CrefFunctionReferenceData)} is not supported");
		}
	}
}
