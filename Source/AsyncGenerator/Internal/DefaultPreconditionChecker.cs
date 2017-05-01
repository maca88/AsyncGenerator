using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Plugins;
using AsyncGenerator.Plugins.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class DefaultPreconditionChecker : AbstractPlugin, IPreconditionChecker
	{
		public bool IsPrecondition(StatementSyntax statement, SemanticModel semanticModel)
		{
			return statement.IsPrecondition();
		}
	}
}
