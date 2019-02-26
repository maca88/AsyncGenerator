using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal class FieldVariableDataReference : DataReference<AbstractData, ISymbol>
	{
		public FieldVariableDataReference(AbstractData data, ReferenceLocation reference, SimpleNameSyntax referenceNameNode, ISymbol referenceSymbol)
			: base(data, reference, referenceNameNode, referenceSymbol, false)
		{
		}
	}
}
