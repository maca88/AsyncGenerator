using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectDocumentationCommentConfiguration : IFluentProjectDocumentationCommentConfiguration, IProjectDocumentationCommentConfiguration
	{
		public Func<INamedTypeSymbol, string> AddOrReplacePartialTypeComments { get; private set; }

		public Func<INamedTypeSymbol, string> AddOrReplaceNewTypeComments { get; private set; }

		public Predicate<INamedTypeSymbol> RemovePartialTypeComments { get; private set; } = symbol => true;

		public Predicate<INamedTypeSymbol> RemoveNewTypeComments { get; private set; }

		public Func<IMethodSymbol, string> AddOrReplaceMethodSummary { get; private set; }

		public Func<IMethodSymbol, string> AddOrReplaceMethodRemarks { get; private set; }

		public Predicate<IMethodSymbol> CanRemoveMethodSummary { get; private set; }

		public Predicate<IMethodSymbol> CanRemoveMethodRemarks { get; private set; }

		IFluentProjectDocumentationCommentConfiguration IFluentProjectDocumentationCommentConfiguration.AddOrReplacePartialTypeComments(Func<INamedTypeSymbol, string> addOrReplaceFunc)
		{
			AddOrReplacePartialTypeComments = addOrReplaceFunc ?? throw new ArgumentNullException(nameof(addOrReplaceFunc));
			return this;
		}

		IFluentProjectDocumentationCommentConfiguration IFluentProjectDocumentationCommentConfiguration.RemovePartialTypeComments(Predicate<INamedTypeSymbol> predicate)
		{
			RemovePartialTypeComments = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectDocumentationCommentConfiguration IFluentProjectDocumentationCommentConfiguration.AddOrReplaceNewTypeComments(Func<INamedTypeSymbol, string> addOrReplaceFunc)
		{
			AddOrReplaceNewTypeComments = addOrReplaceFunc ?? throw new ArgumentNullException(nameof(addOrReplaceFunc));
			return this;
		}

		IFluentProjectDocumentationCommentConfiguration IFluentProjectDocumentationCommentConfiguration.RemoveNewTypeComments(Predicate<INamedTypeSymbol> predicate)
		{
			RemoveNewTypeComments = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectDocumentationCommentConfiguration IFluentProjectDocumentationCommentConfiguration.AddOrReplaceMethodSummary(Func<IMethodSymbol, string> addOrReplaceFunc)
		{
			AddOrReplaceMethodSummary = addOrReplaceFunc ?? throw new ArgumentNullException(nameof(addOrReplaceFunc));
			return this;
		}

		IFluentProjectDocumentationCommentConfiguration IFluentProjectDocumentationCommentConfiguration.RemoveMethodSummary(Predicate<IMethodSymbol> predicate)
		{
			CanRemoveMethodSummary = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectDocumentationCommentConfiguration IFluentProjectDocumentationCommentConfiguration.AddOrReplaceMethodRemarks(Func<IMethodSymbol, string> addOrReplaceFunc)
		{
			AddOrReplaceMethodRemarks = addOrReplaceFunc ?? throw new ArgumentNullException(nameof(addOrReplaceFunc));
			return this;
		}

		IFluentProjectDocumentationCommentConfiguration IFluentProjectDocumentationCommentConfiguration.RemoveMethodRemarks(Predicate<IMethodSymbol> predicate)
		{
			CanRemoveMethodRemarks = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}
	}
}
