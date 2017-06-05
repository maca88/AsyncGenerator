using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal abstract class AbstractReferenceData<TSymbol> : IReferenceAnalyzationResult<TSymbol> where TSymbol : ISymbol
	{
		protected AbstractReferenceData(ReferenceLocation reference, SimpleNameSyntax referenceNameNode, TSymbol referenceSymbol)
		{
			ReferenceLocation = reference;
			ReferenceNameNode = referenceNameNode;
			ReferenceSymbol = referenceSymbol;
		}

		public ReferenceLocation ReferenceLocation { get; }

		public SimpleNameSyntax ReferenceNameNode { get; }

		public TSymbol ReferenceSymbol { get; }

		public override int GetHashCode()
		{
			return ReferenceLocation.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != GetType())
			{
				return false;
			}
			return ReferenceLocation.Equals(((IReferenceAnalyzationResult)obj).ReferenceLocation);
		}
	}
}
