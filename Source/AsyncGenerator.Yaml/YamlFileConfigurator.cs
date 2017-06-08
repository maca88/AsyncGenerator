using System;
using System.Reflection;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.FileConfiguration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AsyncGenerator.Yaml
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
				.WithNamingConvention(new CamelCaseNamingConvention())
				.Build();
			return deserializer.Deserialize<T>(content);
		}
	}
}
