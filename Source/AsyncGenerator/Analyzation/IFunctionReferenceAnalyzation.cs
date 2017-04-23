using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Analyzation
{
	/// <summary>
	/// Used by <see cref="IInvocationExpressionAnalyzer"/> in order to customize the analyzation process of a reference
	/// </summary>
	public interface IFunctionReferenceAnalyzation 
	{
		IFunctionAnalyzationResult ReferenceFunctionData { get; }

		SimpleNameSyntax ReferenceNameNode { get; }

		ReferenceLocation ReferenceLocation { get; }

		IMethodSymbol ReferenceSymbol { get; }

		IReadOnlyList<IMethodSymbol> ReferenceAsyncSymbols { get; }

		bool Ignore { get; set; }
	}
}
