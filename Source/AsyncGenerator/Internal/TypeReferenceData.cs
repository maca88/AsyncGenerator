using System;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal class TypeReferenceData : AbstractReferenceData<INamedTypeSymbol>, ITypeReferenceAnalyzationResult
	{
		public TypeReferenceData(TypeData typeData, ReferenceLocation reference, SimpleNameSyntax referenceNameNode, INamedTypeSymbol referenceSymbol)
			: base(reference, referenceNameNode, referenceSymbol)
		{
			TypeData = typeData ?? throw new ArgumentNullException(nameof(typeData));
		}

		public bool IsCref { get; set; }

		public TypeData TypeData { get; }

		#region ITypeReferenceAnalyzationResult

		ITypeAnalyzationResult ITypeReferenceAnalyzationResult.TypeAnalyzationResult => TypeData;

		#endregion
	}
}
