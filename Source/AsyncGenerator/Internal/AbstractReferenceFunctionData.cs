using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal abstract class AbstractReferenceFunctionData<TData> : AbstractReference<TData, IMethodSymbol, FunctionData>, IFunctionReferenceAnalyzationResult
		where TData : AbstractData
	{
		protected AbstractReferenceFunctionData(TData data, ReferenceLocation referenceLocation, SimpleNameSyntax referenceNameNode,
			IMethodSymbol referenceSymbol, FunctionData referenceFunctionData)
			: base(data, referenceLocation, referenceNameNode, referenceSymbol, referenceFunctionData)
		{
			ReferenceFunctionData = referenceFunctionData;
		}

		public FunctionData ReferenceFunctionData { get; }

		public SyntaxNode ReferenceNode { get; internal set; }

		public abstract string AsyncCounterpartName { get; set; }

		public abstract IMethodSymbol AsyncCounterpartSymbol { get; set; }

		public abstract FunctionData AsyncCounterpartFunction { get; set; }

		public abstract ReferenceConversion GetConversion();

		public abstract ReferenceConversion Conversion { get; set; }

		#region IReferenceAnalyzationResult

		IFunctionAnalyzationResult IFunctionReferenceAnalyzationResult.ReferenceFunction => ReferenceFunctionData;

		IFunctionAnalyzationResult IFunctionReferenceAnalyzationResult.AsyncCounterpartFunction => AsyncCounterpartFunction;

		#endregion

		public override bool IsTypeOf => false;

		public override bool IsNameOf => false;

		public override bool IsCref => false;
	}
}
