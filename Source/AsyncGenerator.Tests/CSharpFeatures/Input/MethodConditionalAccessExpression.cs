using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.CSharpFeatures.Input
{
	public class MethodConditionalAccessExpression
	{
		public object ReturnVariable()
		{
			var reader = GetReader();
			var result = reader?.ReadContentAsObject().ToString().ToUpper();
			if (result == "Test")
			{
				return null;
			}
			return result;
		}

		public bool Expression()
		{
			var reader = GetReader();
			reader?.ReadContentAsObject().ToString();
			return true;
		}

		private XmlReader GetReader()
		{
			return new XmlTextReader("");
		}
	}
}
