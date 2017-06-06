using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using AsyncGenerator.Core.Configuration.Internal;

namespace AsyncGenerator.Core.Configuration
{
	public class XmlFileConfigurator : ISolutionFileConfigurator
	{
		public object Parse(string filePath)
		{
			var xmlContent = File.ReadAllText(filePath);
			return Deserialize<Internal.AsyncGenerator>(xmlContent);
		}

		public string GetSolutionPath(object configuration)
		{
			return ((Internal.AsyncGenerator)configuration).Solution.FilePath;
		}

		public void Configure(object configuration, IFluentSolutionConfiguration solutionConfiguration)
		{
			var configurator = new FileConfigurator();
			configurator.Configure((Internal.AsyncGenerator)configuration, solutionConfiguration);
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
