using System;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal abstract class AbstractReference<TData, TReferenceSymbol, TReferenceData> : IReferenceAnalyzationResult<TReferenceSymbol>, IAbstractReference
		where TReferenceSymbol : ISymbol
		where TData : AbstractData
		where TReferenceData : AbstractData
	{
		protected AbstractReference(TData data, ReferenceLocation referenceLocation, SimpleNameSyntax referenceNameNode,
			TReferenceSymbol referenceSymbol, TReferenceData referenceData = null)
		{
			Data = data ?? throw new ArgumentNullException(nameof(data));
			if (referenceSymbol == null)
			{
				throw new ArgumentNullException(nameof(referenceSymbol));
			}
			ReferenceLocation = referenceLocation;
			ReferenceNameNode = referenceNameNode ?? throw new ArgumentNullException(nameof(referenceNameNode));
			ReferenceSymbol = referenceSymbol;
			ReferenceData = referenceData;
		}

		public TData Data { get; }

		public ReferenceLocation ReferenceLocation { get; }

		public abstract bool IsCref { get; }

		public abstract bool IsTypeOf { get; }

		public abstract bool IsNameOf { get; }

		public SimpleNameSyntax ReferenceNameNode { get; }

		public TReferenceSymbol ReferenceSymbol { get; }

		#region IAbstractReference

		AbstractData IAbstractReference.Data => Data;

		AbstractData IAbstractReference.ReferenceData => ReferenceData;

		#endregion

		public TReferenceData ReferenceData { get; }

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
