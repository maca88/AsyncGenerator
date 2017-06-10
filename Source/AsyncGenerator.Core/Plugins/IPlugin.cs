using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public interface IPlugin
	{
		Task Initialize(Project project, IProjectConfiguration configuration);
	}
}
