using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Plugins
{
	public interface IInvocationExpressionAnalyzer : IPlugin
	{
		void AnalyzeInvocationExpression(InvocationExpressionSyntax invocation, IBodyFunctionReferenceAnalyzation functionReferenceAnalyzation, SemanticModel semanticModel);
	}
}
