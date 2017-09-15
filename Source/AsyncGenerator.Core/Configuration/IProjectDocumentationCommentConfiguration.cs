using System;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration
{
	public interface IProjectDocumentationCommentConfiguration
	{
		Func<INamedTypeSymbol, string> AddOrReplacePartialTypeComments { get; }

		Predicate<INamedTypeSymbol> RemovePartialTypeComments { get; }

		Func<INamedTypeSymbol, string> AddOrReplaceNewTypeComments { get; }

		Predicate<INamedTypeSymbol> RemoveNewTypeComments { get; }

		Func<IMethodSymbol, string> AddOrReplaceMethodSummary { get; }

		Predicate<IMethodSymbol> CanRemoveMethodSummary { get; }

		Func<IMethodSymbol, string> AddOrReplaceMethodRemarks { get; }

		Predicate<IMethodSymbol> CanRemoveMethodRemarks { get; }
	}
}
