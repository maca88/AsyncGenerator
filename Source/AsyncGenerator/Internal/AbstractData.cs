using System.Collections.Generic;
using System.Linq;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal abstract class AbstractData : IAnalyzationResult
	{
		/// <summary>
		/// References of members that are used inside this data (eg. variable declaration, typeof, cref, nameof)
		/// </summary>
		public ConcurrentSet<IAbstractReference> ReferencedMembers { get; } = new ConcurrentSet<IAbstractReference>();

		/// <summary>
		/// References to other members that are using this data (eg. variable declaration, typeof, cref, nameof)
		/// </summary>
		public ConcurrentSet<IAbstractReference> SelfReferences { get; } = new ConcurrentSet<IAbstractReference>();

		/// <summary>
		/// An enumerable of all members inside the project that are referencing this data. This can be used only when references of this data were scanned.
		/// </summary>
		public IEnumerable<AbstractData> ReferencedBy => SelfReferences.Select(o => o.Data);

		public bool IsAnyActiveReference()
		{
			foreach (var data in ReferencedBy)
			{
				if (data is AccessorData accessorData)
				{
					if (accessorData.Conversion.HasAnyFlag(MethodConversion.ToAsync, MethodConversion.Copy) ||
					    accessorData.PropertyData.Conversion == PropertyConversion.Copy)
					{
						return true;
					}
				}
				else if (data is BaseMethodData baseMethodData)
				{
					if (baseMethodData.Conversion.HasAnyFlag(MethodConversion.ToAsync, MethodConversion.Copy))
					{
						return true;
					}
				}
				else if (data is PropertyData propertyData)
				{
					if (propertyData.Conversion == PropertyConversion.Copy)
					{
						return true;
					}
				}
				else if (data is BaseFieldData field)
				{
					if (field.Variables.Any(o => o.Conversion == FieldVariableConversion.Copy))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Get the syntax node of the function
		/// </summary>
		public abstract SyntaxNode GetNode();

		public abstract ISymbol GetSymbol();

		public string IgnoredReason { get; protected set; }

		public bool ExplicitlyIgnored { get; set; }

		public virtual void Ignore(string reason, bool explicitlyIgnored = false)
		{
			IgnoredReason = reason;
			ExplicitlyIgnored = explicitlyIgnored;
		}
	}
}
