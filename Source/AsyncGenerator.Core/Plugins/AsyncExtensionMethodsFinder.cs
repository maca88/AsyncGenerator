using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Extensions;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Core.Plugins
{
	public class AsyncExtensionMethodsFinder : IAsyncCounterpartsFinder, IDocumentTransformer
	{
		private HashSet<IMethodSymbol> _linqMethods;
		private ILookup<string, IMethodSymbol> _linqMethodsLookup;
		private readonly string _fileName;
		private readonly string _projectName;

		public AsyncExtensionMethodsFinder(string projectName, string fileName)
		{
			_projectName = projectName;
			_fileName = fileName;
		}

		public async Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			var extProject = project.Solution.Projects.First(o => o.Name == _projectName);
			var doc = extProject.Documents.First(o => o.Name == _fileName);
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
			var requiredNamespaces = transformationResult.AnalyzationResult.AllTypes
				.SelectMany(o => o.GetSelfAndDescendantsTypes())
				.SelectMany(o => o.MethodsAndAccessors.SelectMany(m => m.FunctionReferences.Where(r => _linqMethods.Contains(r.AsyncCounterpartSymbol))))
				.Select(o => o.AsyncCounterpartSymbol.ContainingNamespace.ToString())
				.Distinct()
				.ToList();

			if (!requiredNamespaces.Any() || requiredNamespaces.All(o => 
				transformationResult.AnalyzationResult.GlobalNamespace.NestedNamespaces.Any(n => n.Symbol.ToString().StartsWith(o)) || 
				transformationResult.Transformed.Usings.Any(u => u.Name.ToString() == o)))
			{
				return null;
			}
			var transformed = transformationResult.Transformed;
			foreach (var requiredNamespace in requiredNamespaces.Where(o => transformationResult.Transformed.Usings.All(u => u.Name.ToString() != o)))
			{
				transformed = transformed
					.AddUsings(
						UsingDirective(ConstructNameSyntax(requiredNamespace))
							.WithUsingKeyword(Token(TriviaList(), SyntaxKind.UsingKeyword, TriviaList(Space)))
							.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(transformationResult.EndOfLineTrivia))));
			}
			return transformed;
		}

		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol symbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options)
		{
			if (invokedFromType == null)
			{
				yield break;
			}
			var asyncName = symbol.GetAsyncName();
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

		private static NameSyntax ConstructNameSyntax(string name)
		{
			var names = name.Split('.').ToList();
			if (names.Count < 2)
			{
				return IdentifierName(name);
			}
			var result = QualifiedName(IdentifierName(names[0]), IdentifierName(names[1]));
			for (var i = 2; i < names.Count; i++)
			{
				result = QualifiedName(result, IdentifierName(names[i]));
			}
			return result;
		}
	}
}
