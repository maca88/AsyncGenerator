using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Plugins
{
	public interface IPreconditionChecker : IPlugin
	{
		bool IsPrecondition(StatementSyntax statement);
	}
}
