using System;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal class TypeDataReference : DataReference<AbstractData, INamedTypeSymbol>, ITypeReferenceAnalyzationResult
	{
		public TypeDataReference(AbstractData data, ReferenceLocation reference, SimpleNameSyntax referenceNameNode, INamedTypeSymbol referenceSymbol, TypeData referenceData)
			: base(data, reference, referenceNameNode, referenceSymbol)
		{
			ReferenceTypeData = referenceData;
		}

		public TypeData ReferenceTypeData { get; }

		#region ITypeReferenceAnalyzationResult

		ITypeAnalyzationResult ITypeReferenceAnalyzationResult.TypeAnalyzationResult => ReferenceTypeData;

		#endregion
	}
}
