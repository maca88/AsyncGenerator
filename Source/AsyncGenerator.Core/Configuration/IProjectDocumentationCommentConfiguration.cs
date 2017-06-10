using System;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration
{
	public interface IProjectDocumentationCommentConfiguration
	{
		Func<IMethodSymbol, string> AddOrReplaceMethodSummary { get; }

		Predicate<IMethodSymbol> CanRemoveMethodSummary { get; }

		Func<IMethodSymbol, string> AddOrReplaceMethodRemarks { get; }

		Predicate<IMethodSymbol> CanRemoveMethodRemarks { get; }
	}
}
