using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Extensions;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static AsyncGenerator.Core.AsyncCounterpartsSearchOptions;

namespace AsyncGenerator.Core.Plugins
{
	public class AsyncExtensionMethodsFinder : IAsyncCounterpartsFinder, IDocumentTransformer
	{
		private HashSet<IMethodSymbol> _extensionMethods;
		private ILookup<string, IMethodSymbol> _extensionMethodsLookup;
		private readonly string _fileName;
		private readonly string _projectName;
		private readonly bool _findByReference;

		public AsyncExtensionMethodsFinder(string projectName, string fileName, bool findByReference)
		{
			_projectName = projectName;
			_fileName = fileName;
			_findByReference = findByReference;
		}

		public async Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			_extensionMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
			if (_findByReference)
			{
				var test = compilation.References
					.Select(compilation.GetAssemblyOrModuleSymbol)
					.OfType<IAssemblySymbol>()
					.FirstOrDefault(o => o.Name == _projectName);
				var type = test?.GetTypeByMetadataName(_fileName);
				if (type == null)
				{
					throw new InvalidOperationException($"Type {_fileName} was not found in assembly {_projectName}");
				}

				foreach (var asyncMethod in type.GetMembers().OfType<IMethodSymbol>()
					         .Where(o => o.Name.EndsWith("Async") && o.IsExtensionMethod))
				{
					_extensionMethods.Add(asyncMethod);
				}
			}
			else
			{
				var extProject = project.Solution.Projects.First(o => o.Name == _projectName);
				var docs = extProject.Documents.Where(o => o.Name == _fileName);
				foreach (var doc in docs)
				{
					var rootNode = await doc.GetSyntaxRootAsync().ConfigureAwait(false);
					var semanticModel = await doc.GetSemanticModelAsync().ConfigureAwait(false);
					var asyncMethods = rootNode.DescendantNodes()
						.OfType<MethodDeclarationSyntax>()
						.Where(o => o.Identifier.ValueText.EndsWith("Async"))
						.Select(o => semanticModel.GetDeclaredSymbol(o))
						.Where(o => o?.IsExtensionMethod == true);
					foreach (var asyncMethod in asyncMethods)
					{
						_extensionMethods.Add(asyncMethod);
					}
				}
			}

			_extensionMethodsLookup = _extensionMethods.ToLookup(o => o.Name);
		}

		public CompilationUnitSyntax Transform(IDocumentTransformationResult transformationResult)
		{
			var requiredNamespaces = transformationResult.AnalyzationResult.AllTypes
				.SelectMany(o => o.GetSelfAndDescendantsTypes())
				.SelectMany(o => o.MethodsAndAccessors
					.SelectMany(m => m.GetSelfAndDescendantsFunctions()
						.SelectMany(f =>  f.FunctionReferences.Where(r => _extensionMethods.Contains(r.AsyncCounterpartSymbol)))))
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
			foreach (var asyncCandidate in _extensionMethodsLookup[asyncName])
			{
				if (!symbol.IsAsyncCounterpart(
					    invokedFromType,
					    asyncCandidate, 
					    true,
					    options.HasFlag(HasCancellationToken),
					    options.HasFlag(IgnoreReturnType)))
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
