using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class DelegatePreconditionChecker : AbstractPlugin, IPreconditionChecker
	{
		private readonly Predicate<StatementSyntax> _predicate;

		public DelegatePreconditionChecker(Predicate<StatementSyntax> predicate)
		{
			_predicate = predicate;
		}

		public bool IsPrecondition(StatementSyntax statement)
		{
			return _predicate(statement);
		}
	}
}
