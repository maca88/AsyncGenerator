using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Tests.ExternalProjects.NHibernate
{
	public class NUnitAsyncCountepartFinder : IAsyncCounterpartsFinder
	{
		public Task Initialize(Project project, IProjectConfiguration configuration)
		{
			return Task.CompletedTask;
		}

		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol syncMethodSymbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options)
		{
			if (syncMethodSymbol.Name != "That" || syncMethodSymbol.ContainingType.Name != "Assert" ||syncMethodSymbol.ContainingType.ContainingNamespace.ToString() != "NUnit.Framework")
			{
				yield break;
			}

			var firstParamType = syncMethodSymbol.Parameters.First().Type;
			if (firstParamType.Name == "ActualValueDelegate" || firstParamType.Name == "TestDelegate")
			{
				yield return syncMethodSymbol;
			}
		}
	}
}
