using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator
{
	public abstract class FunctionData : IFunctionAnalyzationResult
	{
		protected FunctionData(IMethodSymbol methodSymbol)
		{
			Symbol = methodSymbol;
		}

		public bool IsAsync { get; set; }

		public IMethodSymbol Symbol { get; }

		public abstract TypeData TypeData { get; }

		public MethodConversion Conversion { get; set; }

		/// <summary>
		/// References of types that are used inside this method
		/// </summary>
		public ConcurrentSet<ReferenceLocation> TypeReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		/// <summary>
		/// References to other methods that are invoked inside this method and are candidates to be async
		/// </summary>
		public ConcurrentSet<ReferenceLocation> MethodReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		public abstract SyntaxNode GetNode();

		public abstract IEnumerable<AnonymousFunctionData> GetAnonymousFunctionData();

		#region Analyze step

		public ConcurrentSet<FunctionReferenceData> MethodReferenceData { get; } = new ConcurrentSet<FunctionReferenceData>();

		public bool CanBeAsnyc { get; set; }

		#endregion

		#region IFunctionAnalyzationResult

		IReadOnlyList<ReferenceLocation> IFunctionAnalyzationResult.MethodReferences => MethodReferences.ToImmutableArray();

		IReadOnlyList<ReferenceLocation> IFunctionAnalyzationResult.TypeReferences => TypeReferences.ToImmutableArray();

		#endregion
	}
}
