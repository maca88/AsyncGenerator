using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.DocumentationComments.Input
{
	/// <summary>
	/// My documentation comment
	/// </summary>
	public class Comments
	{
		public class Nested
		{
			public void Write(string content)
			{
				SimpleFile.Write(content);
			}
		}

		public void Read()
		{
			SimpleFile.Read();
		}

		// My comment
		public class Nested2
		{
			public void Write(string content)
			{
				SimpleFile.Write(content);
			}
		}
	}
}
