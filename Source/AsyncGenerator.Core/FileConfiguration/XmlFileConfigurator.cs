using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using AsyncGenerator.Core.Configuration;

namespace AsyncGenerator.Core.FileConfiguration
{
	public class XmlFileConfigurator : ISolutionFileConfigurator
	{
		public AsyncGenerator Parse(string content)
		{
			return Deserialize<AsyncGenerator>(content);
		}

		public string GetSolutionPath(AsyncGenerator configuration)
		{
			return configuration.Solution.FilePath;
		}

		public void Configure(AsyncGenerator configuration, IFluentSolutionConfiguration solutionConfiguration)
		{
			FileConfigurator.Configure(configuration, solutionConfiguration);
		}

		private static T Deserialize<T>(string xmlString, Encoding encoding = null)
		{
			var type = typeof(T);
			var xmlRoot = typeof(T).GetCustomAttributes(typeof(XmlRootAttribute), false).FirstOrDefault() as XmlRootAttribute;
			var serializer = new XmlSerializer(type, new XmlAttributeOverrides(), new Type[0], xmlRoot, xmlRoot?.Namespace);

			if (encoding == null) //The XmlReader automatically detects and uses the encoding specified by the XML document.
			{
				using (var reader = XmlReader.Create(new StringReader(xmlString)))
				{
					return (T)serializer.Deserialize(reader);
				}
			}
			using (var memoryStream = new MemoryStream(encoding.GetBytes(xmlString)))
			{
				return (T)serializer.Deserialize(memoryStream);
			}
		}
	}
}
