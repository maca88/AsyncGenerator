using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.OmitAsync.Input
{
	public class TestCase
	{
		public void ReadAsyncNotOmitted(string path)
		{
			if (path != "")
			{
				SimpleFile.Read();
			}
			else if (path == "/")
			{
				for (int i = 0; i < 10; i++)
				{
					SimpleFile.Read();
				}
			}
			else if(SimpleFile.Write(""))
			{
				SimpleFile.Write("");
			}
		}

		public void ReadAsyncOmitted(string path)
		{
			if (path == "")
			{
				SimpleFile.Read();
			}
			else if (path == "/")
			{
				if (path == "")
				{
					SimpleFile.Read();
				}
			}
			else
			{
				SimpleFile.Read();
			}
		}

		public void BlockAsyncOmitted(string path)
		{
			{
				{
					if(path == "")
						SimpleFile.Read();
				}
			}
		}

		public bool IfAsyncOmitted(string content)
		{
			{
				if (content == null)
				{
					return SimpleFile.Write(null);
				}
				return content == "" ? SimpleFile.Write(content) : false;
			}
		}

		public bool ConditionAsyncOmitted2(string content)
		{
			return content == "" ? SimpleFile.Write(null) : SimpleFile.Write(content);
		}

		public void IfElseAsyncOmitted(string content)
		{
			// Verrryyyyyyy looooooooooong
			// comeeeeeeeeent
			if (content == null)
			{
				SimpleFile.Write(null);
			}
			else
			{
				SimpleFile.Write(null);
			}
		}

		public void IfElseNoBlockAsyncOmitted(string content)
		{
			// Verrryyyyyyy looooooooooong
			// comeeeeeeeeent
			if (content == null)
				SimpleFile.Write(null);
			else
				SimpleFile.Write(null);
		}

		public void IfElseNoBlockAsyncOmitted2(string content)
		{
			if (content == null)
				Console.WriteLine();
			else
				SimpleFile.Write(null);
		}
	}
}
