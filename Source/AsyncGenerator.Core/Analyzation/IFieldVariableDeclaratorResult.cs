using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IFieldVariableDeclaratorResult
	{
		ISymbol Symbol { get; }

		VariableDeclaratorSyntax Node { get; }

		FieldVariableConversion Conversion { get; }
	}
}
