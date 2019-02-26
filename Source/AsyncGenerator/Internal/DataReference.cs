using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal abstract class DataReference<TData, TReferenceSymbol> : AbstractDataReference<TData, TReferenceSymbol>
		where TReferenceSymbol : ISymbol
		where TData : AbstractData
	{
		protected DataReference(TData data, ReferenceLocation referenceLocation, SimpleNameSyntax referenceNameNode, TReferenceSymbol referenceSymbol,
			bool? isTypeOf = null) : base(data, referenceLocation, referenceNameNode, referenceSymbol)
		{
			IsCref = referenceNameNode.IsInsideCref();
			IsNameOf = referenceNameNode.IsInsideNameOf();
			IsTypeOf = isTypeOf ?? referenceNameNode.IsInsideTypeOf();
		}

		public override bool IsCref { get; }
		public override bool IsTypeOf { get; }
		public override bool IsNameOf { get; }
	}
}
