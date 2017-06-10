using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.DocumentationComments.Input
{
	public class Remarks
	{

		public void Test()
		{
			SimpleFile.Read();
		}

		/// <remarks>
		/// Sync remarks
		/// </remarks>
		public void Test2()
		{
			SimpleFile.Read();
		}

		/// <summary>
		/// My summary
		/// </summary>
		public void Test3()
		{
			SimpleFile.Read();
		}

		/// <summary>
		/// My summary
		/// </summary>
		/// <param name="content"></param>
		public void Test4(string content)
		{
			SimpleFile.Write(content);
		}

		/// <summary>
		/// My summary
		/// </summary>
		/// <param name="content"></param>
		/// <remarks>
		/// Sync remarks
		/// multi line
		/// </remarks>
		public void Test5(string content)
		{
			SimpleFile.Write(content);
		}

	}
}
