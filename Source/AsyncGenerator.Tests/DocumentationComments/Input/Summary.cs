using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.DocumentationComments.Input
{
	public class Summary
	{
		public void Test()
		{
			SimpleFile.Read();
		}

		/// <summary>
		/// Sync summary
		/// </summary>
		public void Test2()
		{
			SimpleFile.Read();
		}

		/// <remarks>
		/// My remarks
		/// </remarks>
		public void Test3()
		{
			SimpleFile.Read();
		}

		/// <param name="content"></param>
		/// <remarks>
		/// My remarks
		/// </remarks>
		public void Test4(string content)
		{
			SimpleFile.Write(content);
		}

		/// <summary>
		/// My summary
		/// multi line
		/// </summary>
		/// <param name="content"></param>
		/// <remarks>
		/// Sync remarks
		/// </remarks>
		public void Test5(string content)
		{
			SimpleFile.Write(content);
		}
	}
}
