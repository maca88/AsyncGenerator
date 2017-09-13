using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class FieldVariableDeclaratorData : AbstractData, IFieldVariableDeclaratorResult
	{
		public FieldVariableDeclaratorData(BaseFieldData fieldData, ISymbol symbol, VariableDeclaratorSyntax node)
		{
			FieldData = fieldData ?? throw new ArgumentNullException(nameof(fieldData));
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
			Node = node ?? throw new ArgumentNullException(nameof(node));
		}

		public BaseFieldData FieldData { get; }

		/// <summary>
		/// Can be a IFieldSymbol or IEventSymbol
		/// </summary>
		public ISymbol Symbol { get; }

		public VariableDeclaratorSyntax Node { get; }

		public FieldVariableConversion Conversion { get; internal set; }

		public override SyntaxNode GetNode()
		{
			return Node;
		}

		public override ISymbol GetSymbol()
		{
			return Symbol;
		}

		public override void Ignore(string reason, bool explicitlyIgnored = false)
		{
			if (Conversion == FieldVariableConversion.Ignore)
			{
				return;
			}
			Conversion = FieldVariableConversion.Ignore;
			base.Ignore(reason, explicitlyIgnored);
		}
	}

}
