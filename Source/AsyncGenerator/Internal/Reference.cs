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
	internal class Reference<TData, TReferenceSymbol, TReferenceData> : AbstractReference<TData, TReferenceSymbol, TReferenceData>
		where TReferenceSymbol : ISymbol
		where TData : AbstractData
		where TReferenceData : AbstractData
	{
		public Reference(TData data, ReferenceLocation referenceLocation, SimpleNameSyntax referenceNameNode, TReferenceSymbol referenceSymbol,
			TReferenceData referenceData = null) : base(data, referenceLocation, referenceNameNode, referenceSymbol, referenceData)
		{
			IsCref = referenceNameNode.IsInsideCref();
			IsTypeOf = referenceNameNode.IsInsideTypeOf();
			IsNameOf = referenceNameNode.IsInsideNameOf();
		}

		public override bool IsCref { get; }
		public override bool IsTypeOf { get; }
		public override bool IsNameOf { get; }
	}
}
