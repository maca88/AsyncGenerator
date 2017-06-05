using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal class TypeReferenceData : AbstractReferenceData<INamedTypeSymbol>, ITypeReferenceAnalyzationResult
	{
		public TypeReferenceData(ReferenceLocation reference, SimpleNameSyntax referenceNameNode, INamedTypeSymbol referenceSymbol)
			: base(reference, referenceNameNode, referenceSymbol)
		{
		}

		public bool IsCref { get; set; }
	}
}
