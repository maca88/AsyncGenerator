using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.FileConfiguration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AsyncGenerator.Yaml
{
	public class YamlFileConfigurator : ISolutionFileConfigurator
	{
		public Core.FileConfiguration.AsyncGenerator Parse(string content)
		{
			return Deserialize<Core.FileConfiguration.AsyncGenerator>(content);
		}

		public string GetSolutionPath(Core.FileConfiguration.AsyncGenerator configuration)
		{
			return configuration.Solution.FilePath;
		}

		public void Configure(Core.FileConfiguration.AsyncGenerator configuration, IFluentSolutionConfiguration solutionConfiguration)
		{
			FileConfigurator.Configure(configuration, solutionConfiguration);
		}

		private static T Deserialize<T>(string content)
		{
			var deserializer = new DeserializerBuilder()
				.WithNamingConvention(new CamelCaseNamingConvention())
				.Build();
			return deserializer.Deserialize<T>(content);
		}
	}
}
