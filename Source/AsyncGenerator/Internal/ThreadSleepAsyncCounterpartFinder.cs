using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Extensions;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Plugins.Internal;
using Microsoft.CodeAnalysis;
using static AsyncGenerator.Core.AsyncCounterpartsSearchOptions;

namespace AsyncGenerator.Internal
{
	internal class ThreadSleepAsyncCounterpartFinder : AbstractPlugin, IAsyncCounterpartsFinder
	{
		private List<IMethodSymbol> _taskDelayMethods;

		public override Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			var taskSymbol =
				compilation.References
					.Select(compilation.GetAssemblyOrModuleSymbol)
					.OfType<IAssemblySymbol>()
					.Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName("System.Threading.Tasks.Task"))
					.FirstOrDefault(o => o != null);
			if (taskSymbol == null)
			{
				throw new InvalidOperationException("Unable to find System.Threading.Tasks.Task type");
			}
			_taskDelayMethods = taskSymbol.GetMembers("Delay").OfType<IMethodSymbol>().ToList();
			return Task.CompletedTask;
		}

		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol syncMethodSymbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options)
		{
			if (syncMethodSymbol.Name != "Sleep" || syncMethodSymbol.ContainingType.ToString() != "System.Threading.Thread")
			{
				yield break;
			}

			foreach (var taskDelayMethod in _taskDelayMethods)
			{
				if (syncMethodSymbol.IsAsyncCounterpart(invokedFromType, taskDelayMethod, options.HasFlag(EqualParameters),
					options.HasFlag(HasCancellationToken), options.HasFlag(IgnoreReturnType)))
				{
					yield return taskDelayMethod;
				}
			}
		}
	}
}
