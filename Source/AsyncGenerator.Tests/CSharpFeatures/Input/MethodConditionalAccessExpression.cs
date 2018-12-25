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

		public bool Argument()
		{
			var reader = GetReader();
			Transform(reader?.ReadContentAsObject());
			return true;
		}

		public bool ValueType()
		{
			var reader = GetReader();
			var val = reader?.ReadContentAsBase64(new byte[0], 0, 0).ToString();
			return val == "1";
		}

		public bool Void()
		{
			var reader = GetReader();
			reader?.ReadContentAsString().CopyTo(0, new char[0], 0, 0);
			return true;
		}

		public bool Void2()
		{
			var reader = GetReader();
			reader?.ReadContentAsString();
			return true;
		}

		private XmlReader GetReader()
		{
			return new XmlTextReader("");
		}

		private object Transform(object value)
		{
			return value;
		}
	}
}
