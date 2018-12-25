using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.CSharpFeatures.Input
{
	public class ConditionalAccessWithNullCoalescing
	{
		public void Variables()
		{
			var reader = GetReader();
			var result1 = reader?.ReadContentAsString().ToString().Length.ToString() ?? "1";
			var result2 = reader?.ReadContentAsString().Length.ToString() ?? "2";
			var result3 = reader?.ReadContentAsString()[0].ToString() ?? "3";
		}

		public void Accessors()
		{
			var reader = GetReader();
			var readers = new[] { GetReader() };
			var result1 = readers[0]?.ReadContentAsString().ToString().Length.ToString() ?? "1";
			var result2 = reader?.ReadContentAsString().Length.ToString() ?? "2";
			var result3 = GetReader()?.ReadContentAsString()[0].ToString() ?? "3";
			var result4 = (reader as XmlReader)?.ReadContentAsString()[0].ToString() ?? "3";
		}

		public object Return()
		{
			var reader = GetReader();
			return reader?.ReadContentAsString().Trim().Length;
		}

		public bool NestedExpression()
		{
			var reader = GetReader();
			{
				Transform(reader?.ReadContentAsString()?.Substring(0) ?? "test");
			}
			return true;
		}

		public bool Expression()
		{
			var reader = GetReader();
			Transform(reader?.ReadContentAsString()?.Substring(0) ?? "test");
			return true;
		}

		public void ExpressionVoid()
		{
			var reader = GetReader();
			Transform(reader?.ReadContentAsString()?.Substring(0) ?? "test");
		}

		public object ShouldIgnore()
		{
			var reader = GetReader();
			return reader.ReadContentAsString().Trim()?.Length ?? 0;
		}

		public string Trivia()
		{
			var reader = GetReader();
			return reader?.ReadContentAsString() ?? "test";
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
