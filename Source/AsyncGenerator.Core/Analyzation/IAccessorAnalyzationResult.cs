using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IAccessorAnalyzationResult : IMethodOrAccessorAnalyzationResult
	{
		SyntaxNode Node { get; }
	}
}
