using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Plugins.Internal
{
	internal abstract class AbstractPlugin : IPlugin
	{
		public virtual Task Initialize(Project project, IProjectConfiguration configuration)
		{
			return Task.CompletedTask;
		}
	}
}
