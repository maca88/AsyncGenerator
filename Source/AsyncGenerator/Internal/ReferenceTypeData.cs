using System;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal class ReferenceTypeData : Reference<AbstractData, INamedTypeSymbol, TypeData>, ITypeReferenceAnalyzationResult
	{
		public ReferenceTypeData(AbstractData data, ReferenceLocation reference, SimpleNameSyntax referenceNameNode, INamedTypeSymbol referenceSymbol, TypeData referenceData)
			: base(data, reference, referenceNameNode, referenceSymbol, referenceData)
		{
		}

		#region ITypeReferenceAnalyzationResult

		ITypeAnalyzationResult ITypeReferenceAnalyzationResult.TypeAnalyzationResult => ReferenceData;

		#endregion
	}
}
