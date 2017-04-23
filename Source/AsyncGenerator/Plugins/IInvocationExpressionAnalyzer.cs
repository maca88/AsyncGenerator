using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Plugins
{
	public interface IInvocationExpressionAnalyzer
	{
		void Analyze(InvocationExpressionSyntax invocation, IFunctionReferenceAnalyzation functionReferenceAnalyzation, SemanticModel semanticModel);
	}
}
