using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using AsyncGenerator.Core.FileConfiguration;
using AsyncGenerator.Core.FileConfiguration.Internal;

namespace AsyncGenerator.Core.Configuration
{
	public class XmlFileConfigurator : FileConfigurator
	{
		private readonly XmlValidator _xmlValidator;

		public XmlFileConfigurator()
		{
			_xmlValidator = new XmlValidator();
			_xmlValidator.LoadEmbeddedSchemas(typeof(XmlFileConfigurator).Assembly, "AsyncGenerator.Core.FileConfiguration");
		}

		public override FileConfiguration.AsyncGenerator Parse(string content)
		{
			Validate(content);
			return Deserialize<FileConfiguration.AsyncGenerator>(content);
		}

		private void Validate(string content)
		{
			var result = _xmlValidator.Validate(content);
			if (!result.IsValid)
			{
				throw new XmlSchemaValidationException(result.ToString());
			}
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
