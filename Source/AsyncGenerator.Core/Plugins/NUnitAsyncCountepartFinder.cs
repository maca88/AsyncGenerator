using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public class NUnitAsyncCounterpartsFinder : IAsyncCounterpartsFinder
	{
		public Task Initialize(Project project, IProjectConfiguration configuration)
		{
			return Task.CompletedTask;
		}

		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol syncMethodSymbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options)
		{
			if (syncMethodSymbol.Name != "That" || syncMethodSymbol.ContainingType.Name != "Assert" || syncMethodSymbol.ContainingType.ContainingNamespace.ToString() != "NUnit.Framework")
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
