using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IMethodAnalyzationResult : IMethodOrAccessorAnalyzationResult
	{
		MethodDeclarationSyntax Node { get; }
	}
}
