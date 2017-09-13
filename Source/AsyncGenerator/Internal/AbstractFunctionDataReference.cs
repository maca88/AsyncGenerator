using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal abstract class AbstractFunctionDataReference<TData> : AbstractDataReference<TData, IMethodSymbol, FunctionData>, IFunctionReferenceAnalyzationResult
		where TData : AbstractData
	{
		protected AbstractFunctionDataReference(TData data, ReferenceLocation referenceLocation, SimpleNameSyntax referenceNameNode,
			IMethodSymbol referenceSymbol, FunctionData referenceFunctionData, bool insideMethodBody)
			: base(data, referenceLocation, referenceNameNode, referenceSymbol, referenceFunctionData)
		{
			ReferenceFunctionData = referenceFunctionData;
			InsideMethodBody = insideMethodBody;
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

		public bool InsideMethodBody { get; }
	}
}
