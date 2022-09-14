using AsyncGenerator.Core.FileConfiguration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AsyncGenerator.Configuration.Yaml
{
	public class YamlFileConfigurator : FileConfigurator
	{
		public override Core.FileConfiguration.AsyncGenerator Parse(string content)
		{
			return Deserialize<Core.FileConfiguration.AsyncGenerator>(content);
		}

		private static T Deserialize<T>(string content)
		{
			var deserializer = new DeserializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
				.Build();
			return deserializer.Deserialize<T>(content);
		}
	}
}
