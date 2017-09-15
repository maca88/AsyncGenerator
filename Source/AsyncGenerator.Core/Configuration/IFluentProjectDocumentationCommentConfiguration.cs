using System;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFluentProjectDocumentationCommentConfiguration
	{
		/// <summary>
		/// Set a function that will add or replace comments of a generated partial type.
		/// Comments will only get added/replaced if the returned string is not null or empty.
		/// </summary>
		IFluentProjectDocumentationCommentConfiguration AddOrReplacePartialTypeComments(Func<INamedTypeSymbol, string> replaceFunc);

		/// <summary>
		/// Set a predicate that will decide whether to remove comments of a generated partial type or not.
		/// Comments will not be removed if <see cref="AddOrReplacePartialTypeComments" /> returns a not null or empty string for the same type.
		/// <para>Default comments for all types will be removed</para>
		/// </summary>
		IFluentProjectDocumentationCommentConfiguration RemovePartialTypeComments(Predicate<INamedTypeSymbol> predicate);

		/// <summary>
		/// Set a function that will add or replace comments of a generated new type.
		/// Comments will only get added/replaced if the returned string is not null or empty.
		/// </summary>
		IFluentProjectDocumentationCommentConfiguration AddOrReplaceNewTypeComments(Func<INamedTypeSymbol, string> replaceFunc);

		/// <summary>
		/// Set a predicate that will decide whether to remove comments of a generated new type or not.
		/// Comments will not be removed if <see cref="AddOrReplaceNewTypeComments" /> returns a not null or empty string for the same type.
		/// </summary>
		IFluentProjectDocumentationCommentConfiguration RemoveNewTypeComments(Predicate<INamedTypeSymbol> predicate);

		/// <summary>
		/// Set a function that will add or replace the summary content of a method. The summary will only get added/replaced if the returned string is not null or empty.
		/// </summary>
		IFluentProjectDocumentationCommentConfiguration AddOrReplaceMethodSummary(Func<IMethodSymbol, string> replaceFunc);

		/// <summary>
		/// Set a predicate that will decide whether to remove the method summary or not.
		/// </summary>
		IFluentProjectDocumentationCommentConfiguration RemoveMethodSummary(Predicate<IMethodSymbol> predicate);

		/// <summary>
		/// Set a function that will add or replace the remark of the method. The remark will only get added/replaced if the returned string is not null or empty.
		/// </summary>
		IFluentProjectDocumentationCommentConfiguration AddOrReplaceMethodRemarks(Func<IMethodSymbol, string> replaceFunc);

		/// <summary>
		/// Set a predicate that will decide whether to remove the method remark or not.
		/// </summary>
		IFluentProjectDocumentationCommentConfiguration RemoveMethodRemarks(Predicate<IMethodSymbol> predicate);
	}
}
