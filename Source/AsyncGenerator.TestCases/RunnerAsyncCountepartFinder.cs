using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.TestCases
{
	public class RunnerAsyncCountepartFinder : IAsyncCounterpartsFinder
	{
		private IMethodSymbol _asyncCounterpart;

		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			var assertSymbol =
				compilation.References
					.Select(compilation.GetAssemblyOrModuleSymbol)
					.OfType<IAssemblySymbol>()
					.Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName("AsyncGenerator.TestCases.Runner"))
					.FirstOrDefault(o => o != null);
			if (assertSymbol == null)
			{
				throw new InvalidOperationException("Unable to find NUnit.Framework.Assert type");
			}
			_asyncCounterpart = assertSymbol.GetMembers("RunGeneric").OfType<IMethodSymbol>().First();

			return Task.CompletedTask;
		}

		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol syncMethodSymbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options)
		{
			if ((syncMethodSymbol.Name != "RunVoid" && syncMethodSymbol.Name != "RunGeneric") ||
				syncMethodSymbol.ContainingType.Name != "Runner" ||
				syncMethodSymbol.ContainingType.ContainingNamespace.ToString() != "AsyncGenerator.TestCases")
			{
				yield break;
			}
			yield return _asyncCounterpart;
		}
	}
}
