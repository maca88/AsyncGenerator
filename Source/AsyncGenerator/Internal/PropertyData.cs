using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class PropertyData : AbstractData, IPropertyAnalyzationResult
	{
		public PropertyData(TypeData typeData, IPropertySymbol symbol, PropertyDeclarationSyntax node)
		{
			TypeData = typeData ?? throw new ArgumentNullException(nameof(typeData));
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
			Node = node ?? throw new ArgumentNullException(nameof(node));
			if (Symbol.GetMethod != null)
			{
				GetAccessorData = new AccessorData(this, Symbol.GetMethod, Node.AccessorList != null
					? (SyntaxNode)Node.AccessorList.Accessors.First(o => o.Keyword.IsKind(SyntaxKind.GetKeyword))
					: Node.ExpressionBody);
			}
			if (Symbol.SetMethod != null)
			{
				SetAccessorData = new AccessorData(this, Symbol.SetMethod, Node.AccessorList.Accessors.First(o => o.Keyword.IsKind(SyntaxKind.SetKeyword)));
			}
		}

		public IPropertySymbol Symbol { get; }

		public PropertyConversion Conversion { get; set; }

		public TypeData TypeData { get; }

		public PropertyDeclarationSyntax Node { get; }

		public AccessorData GetAccessorData { get; }

		public AccessorData SetAccessorData { get; }

		public override ISymbol GetSymbol()
		{
			return Symbol;
		}

		public IEnumerable<AccessorData> GetAccessors()
		{
			if (GetAccessorData != null)
			{
				yield return GetAccessorData;
			}
			if (SetAccessorData != null)
			{
				yield return SetAccessorData;
			}
		}

		#region IPropertyAnalyzationResult

		IEnumerable<IAccessorAnalyzationResult> IPropertyAnalyzationResult.GetAccessors() => GetAccessors();

		#endregion

		public override SyntaxNode GetNode()
		{
			return Node;
		}

		public override void Ignore(string reason, bool explicitlyIgnored = false)
		{
			Conversion = PropertyConversion.Ignore;
			IgnoredReason = reason;
			ExplicitlyIgnored = explicitlyIgnored;
			IgnoreAccessors("Cascade ignored.");
		}

		public void IgnoreAccessors(string reason)
		{
			GetAccessorData?.Ignore(reason);
			SetAccessorData?.Ignore(reason);
		}

		public void Copy()
		{
			IgnoredReason = null;
			ExplicitlyIgnored = false;
			Conversion = PropertyConversion.Copy;
		}
	}
}
