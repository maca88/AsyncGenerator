using System.Collections.Generic;
using System.Collections.Immutable;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal abstract class FunctionData : AbstractData, IFunctionAnalyzationResult
	{
		protected FunctionData(IMethodSymbol methodSymbol)
		{
			Symbol = methodSymbol;
		}

		//TODO: remove if not needed
		public bool IsAsync { get; set; }

		public IMethodSymbol Symbol { get; }

		public abstract TypeData TypeData { get; }

		public MethodConversion Conversion { get; set; }

		/// <summary>
		/// References to other methods that are invoked inside this method and are candidates to be async
		/// </summary>
		public ConcurrentSet<InvokeFunctionReferenceData> InvokedMethodReferences { get; } = new ConcurrentSet<InvokeFunctionReferenceData>();

		public ConcurrentSet<CrefFunctionReferenceData> CrefMethodReferences { get; } = new ConcurrentSet<CrefFunctionReferenceData>();

		public List<StatementSyntax> Preconditions { get; } = new List<StatementSyntax>();

		public abstract SyntaxNode GetNode();

		public abstract SyntaxNode GetBodyNode();

		public abstract IEnumerable<AnonymousFunctionData> GetAnonymousFunctionData();

		public abstract MethodData GetMethodData();

		#region Analyze step

		public bool HasYields { get; set; }

		#endregion

		#region Post analyze step

		public bool SplitTail { get; set; }

		public bool OmitAsync { get; set; }

		public bool WrapInTryCatch { get; set; }

		#endregion

		#region IFunctionAnalyzationResult

		private IReadOnlyList<IInvokeFunctionReferenceAnalyzationResult> _cachedMethodReferences;
		IReadOnlyList<IInvokeFunctionReferenceAnalyzationResult> IFunctionAnalyzationResult.MethodReferences => _cachedMethodReferences ?? (_cachedMethodReferences = InvokedMethodReferences.ToImmutableArray());

		private IReadOnlyList<ITypeReferenceAnalyzationResult> _cachedTypeReferences;
		IReadOnlyList<ITypeReferenceAnalyzationResult> IFunctionAnalyzationResult.TypeReferences => _cachedTypeReferences ?? (_cachedTypeReferences = TypeReferences.ToImmutableArray());

		private IReadOnlyList<StatementSyntax> _cachedPreconditions;
		IReadOnlyList<StatementSyntax> IFunctionAnalyzationResult.Preconditions => _cachedPreconditions ?? (_cachedPreconditions = Preconditions.ToImmutableArray());

		#endregion
	}
}
