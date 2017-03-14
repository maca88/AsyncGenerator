using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class AsyncCounterpartMethod
	{
		public ExpressionSyntax MethodNode { get; set; }

		public IMethodSymbol MethodSymbol { get; set; }

		public IMethodSymbol AsyncMethodSymbol { get; set; }
	}
}
