using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator
{
	public class MethodData : IMethodAnalyzationResult
	{
		public MethodData(TypeData typeData, IMethodSymbol symbol, MethodDeclarationSyntax node)
		{
			TypeData = typeData;
			Symbol = symbol;
			Node = node;
			InterfaceMethod = Symbol.ContainingType.TypeKind == TypeKind.Interface;
		}

		public bool InterfaceMethod { get; }

		/// <summary>
		/// References of types that are used inside this method
		/// </summary>
		public ConcurrentSet<ReferenceLocation> TypeReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		/// <summary>
		/// References to other methods that are invoked inside this method and are candidates to be async
		/// </summary>
		public ConcurrentSet<ReferenceLocation> MethodReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		/// <summary>
		/// Methods 
		/// </summary>
		public ConcurrentSet<IMethodSymbol> ExternalAsyncMethods { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Interface members within project that the method implements (internal and external)
		/// </summary>
		public ConcurrentSet<IMethodSymbol> ImplementedInterfaces { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Implementation/derived/base/interface methods
		/// </summary>
		public ConcurrentSet<MethodData> RelatedMethods { get; } = new ConcurrentSet<MethodData>();

		/// <summary>
		/// External Base/derivered or interface/implementation methods
		/// </summary>
		public ConcurrentSet<IMethodSymbol> ExternalRelatedMethods { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Methods within project that the method overrides
		/// </summary>
		public ConcurrentSet<IMethodSymbol> OverridenMethods { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Method datas that invokes this method
		/// </summary>
		public ConcurrentSet<MethodData> InvokedBy { get; } = new ConcurrentSet<MethodData>();

		/// <summary>
		/// The base method that is overriden
		/// </summary>
		public IMethodSymbol BaseOverriddenMethod { get; set; }

		/// <summary>
		/// Reference to the async counterpart for this method
		/// </summary>
		public IMethodSymbol AsyncCounterpartSymbol { get; set; }

		public bool IsAsync { get; set; }

		public MethodConversion Conversion { get; internal set; }

		public TypeData TypeData { get; }

		public IMethodSymbol Symbol { get; }

		public MethodDeclarationSyntax Node { get; }

		#region IMethodAnalyzationResult

		IEnumerable<IMethodAnalyzationResult> IMethodAnalyzationResult.InvokedBy => InvokedBy.ToImmutableArray();

		IEnumerable<ReferenceLocation> IMethodAnalyzationResult.MethodReferences => MethodReferences.ToImmutableArray();

		IEnumerable<ReferenceLocation> IMethodAnalyzationResult.TypeReferences => TypeReferences.ToImmutableArray();

		#endregion

	}
}
