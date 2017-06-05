using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Plugins
{
	public interface IPreconditionChecker : IPlugin
	{
		bool IsPrecondition(StatementSyntax statement, SemanticModel semanticModel);
	}
}
