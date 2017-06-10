using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace AsyncGenerator.Core.FileConfiguration.Internal
{
	internal class XmlValidator
	{
		private readonly XmlSchemaSet _xmlSchemaSet;

		public XmlValidator()
		{
			_xmlSchemaSet = new XmlSchemaSet();
		}

		public void LoadEmbeddedSchemas(Assembly assembly, params string[] schemaBasePaths)
		{
			foreach (var basePath in schemaBasePaths)
			{
				foreach (var schemaPath in GetAllXsdIn(basePath, assembly))
				{
					using (var stream = assembly.GetManifestResourceStream(schemaPath))
					using (var reader = XmlReader.Create(stream))
					{
						var xmlSchema = XmlSchema.Read(reader, SchemaValidationCallback);
						_xmlSchemaSet.Add(xmlSchema);
					}
				}
			}
		}

		private List<string> GetAllXsdIn(string nameSpace, Assembly assembly)
		{
			return assembly.GetManifestResourceNames().Where(r => r.StartsWith(nameSpace) && r.EndsWith(".xsd")).ToList();
		}

		private void SchemaValidationCallback(object sender, ValidationEventArgs args)
		{
		}

		public XmlValidationResult Validate(string xml)
		{
			return Validate(XDocument.Parse(xml));
		}

		public XmlValidationResult Validate(XDocument xml)
		{
			var result = new XmlValidationResult();
			xml.Validate(_xmlSchemaSet, (sender, args) =>
			{
				switch (args.Severity)
				{
					case XmlSeverityType.Warning:
						result.AddWarning(args.Exception);
						break;
					case XmlSeverityType.Error:
						result.AddError(args.Exception);
						break;
				}
			});
			return result;
		}
	}
}
