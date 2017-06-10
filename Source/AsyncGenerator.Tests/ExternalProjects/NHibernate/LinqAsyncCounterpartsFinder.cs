using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Plugins;
using AsyncGenerator.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Tests.ExternalProjects.NHibernate
{
	public class LinqAsyncCounterpartsFinder : IAsyncCounterpartsFinder, IDocumentTransformer
	{
		private HashSet<IMethodSymbol> _linqMethods;
		private ILookup<string, IMethodSymbol> _linqMethodsLookup;

		public async Task Initialize(Project project, IProjectConfiguration configuration)
		{
			var nhProject = project.Solution.Projects.First(o => o.Name == "NHibernate");
			var doc = nhProject.Documents.First(o => o.Name == "LinqExtensionMethods.cs");
			var rootNode = await doc.GetSyntaxRootAsync().ConfigureAwait(false);
			var semanticModel = await doc.GetSemanticModelAsync().ConfigureAwait(false);
			_linqMethods = new HashSet<IMethodSymbol>(rootNode.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.Where(o => o.Identifier.ValueText.EndsWith("Async"))
				.Select(o => semanticModel.GetDeclaredSymbol(o)));
			_linqMethodsLookup = _linqMethods.ToLookup(o => o.Name);
		}

		public CompilationUnitSyntax Transform(IDocumentTransformationResult transformationResult)
		{
			if (!transformationResult.AnalyzationResult.GetAllTypes()
				.SelectMany(o => o.GetSelfAndDescendantsTypes())
				.Any(o => o.Methods.Any(m => m.MethodReferences.Any(r => _linqMethods.Contains(r.AsyncCounterpartSymbol)))) ||
				transformationResult.Transformed.Usings.Any(o => o.Name.ToString() == "NHibernate.Linq"))
			{
				return null;
			}
			return transformationResult.Transformed
				.AddUsings(
					UsingDirective(QualifiedName(IdentifierName("NHibernate"), IdentifierName("Linq")))
						.WithUsingKeyword(Token(TriviaList(), SyntaxKind.UsingKeyword, TriviaList(Space)))
						.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(transformationResult.EndOfLineTrivia))));
		}

		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol symbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options)
		{
			var ns = symbol.ContainingNamespace?.ToString() ?? "";
			var isToList = symbol.ContainingType.Name == "Enumerable" && symbol.Name == "ToList";
			if (!ns.StartsWith("System.Linq") || (!isToList && symbol.ContainingType.Name != "Queryable"))
			{
				yield break;
			}
			var asyncName = symbol.Name + "Async";
			foreach (var asyncCandidate in _linqMethodsLookup[asyncName])
			{
				if (!symbol.IsAsyncCounterpart(invokedFromType, asyncCandidate, true, true, false))
				{
					continue;
				}
				yield return asyncCandidate;
				yield break;
			}
		}
	}
}
