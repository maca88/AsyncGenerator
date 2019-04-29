using AsyncGenerator.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public interface IProjectConfigurator
	{
		void Configure(Project project, IFluentProjectConfiguration configuration);
	}
}
