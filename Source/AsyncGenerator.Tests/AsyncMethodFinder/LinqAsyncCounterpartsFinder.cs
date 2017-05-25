using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Plugins;
using AsyncGenerator.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Tests.AsyncMethodFinder
{
	public class LinqAsyncCounterpartsFinder : IAsyncCounterpartsFinder
	{
		private HashSet<IMethodSymbol> _linqMethods;
		private ILookup<string, IMethodSymbol> _linqMethodsLookup;

		public async Task Initialize(Project project, IProjectConfiguration configuration)
		{
			var nhProject = project.Solution.Projects.First(o => o.Name == "AsyncGenerator.Tests");
			var doc = nhProject.Documents.First(o => o.Name == "LinqExtensions.cs");
			var rootNode = await doc.GetSyntaxRootAsync().ConfigureAwait(false);
			var semanticModel = await doc.GetSemanticModelAsync().ConfigureAwait(false);
			_linqMethods = new HashSet<IMethodSymbol>(rootNode.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.Where(o => o.Identifier.ValueText.EndsWith("Async"))
				.Select(o => semanticModel.GetDeclaredSymbol(o)));
			_linqMethodsLookup = _linqMethods.ToLookup(o => o.Name);
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
			if (!_linqMethodsLookup.Contains(asyncName))
			{
				yield break;
			}
			foreach (var asyncCandidate in _linqMethodsLookup[asyncName])
			{
				if (symbol.IsAsyncCounterpart(invokedFromType, asyncCandidate, true, true, false))
				{
					yield return asyncCandidate;
					yield break;
				}
			}
		}
	}
}
