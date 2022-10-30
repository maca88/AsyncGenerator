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
			//Ignore init setters
			if (Symbol.SetMethod != null && Node.AccessorList.Accessors.FirstOrDefault(o => o.Keyword.IsKind(SyntaxKind.SetKeyword)) is AccessorDeclarationSyntax accessorDeclaration)
			{
				SetAccessorData = new AccessorData(this, Symbol.SetMethod, accessorDeclaration);
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

		protected override void Ignore()
		{
			Conversion = PropertyConversion.Ignore;
			IgnoreAccessors(IgnoreReason.Cascade);
		}

		public void IgnoreAccessors(IgnoreReason reason)
		{
			GetAccessorData?.Ignore(reason);
			SetAccessorData?.Ignore(reason);
		}

		public override void Copy()
		{
			base.Copy();
			Conversion = PropertyConversion.Copy;
			GetAccessorData?.SoftCopy();
			SetAccessorData?.SoftCopy();
		}
	}
}
