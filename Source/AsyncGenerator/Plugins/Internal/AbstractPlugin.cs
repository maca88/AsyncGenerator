using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Plugins.Internal
{
	internal abstract class AbstractPlugin : IPlugin
	{
		public virtual Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			return Task.CompletedTask;
		}
	}
}
