using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Plugins;
using AsyncGenerator.Plugins.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class DelegatePreconditionChecker : AbstractPlugin, IPreconditionChecker
	{
		private readonly Func<StatementSyntax, SemanticModel, bool> _func;

		public DelegatePreconditionChecker(Func<StatementSyntax, SemanticModel, bool> func)
		{
			_func = func;
		}

		public bool IsPrecondition(StatementSyntax statement, SemanticModel semanticModel)
		{
			return _func(statement, semanticModel);
		}
	}
}
