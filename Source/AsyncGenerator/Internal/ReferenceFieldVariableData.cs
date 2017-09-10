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
	internal class ReferenceFieldVariableData : Reference<AbstractData, ISymbol, FieldVariableDeclaratorData>
	{
		public ReferenceFieldVariableData(AbstractData data, ReferenceLocation reference, SimpleNameSyntax referenceNameNode,
			ISymbol referenceSymbol, FieldVariableDeclaratorData referenceData)
			: base(data, reference, referenceNameNode, referenceSymbol, referenceData)
		{
		}
	}
}
