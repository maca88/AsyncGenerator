using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AbstractClass.Input
{
	public interface ITestInteraface
	{
		void Read();
	}


	public abstract class AbstractTest : ITestInteraface
	{
		public abstract void Read();
	}

	public class TestCase : AbstractTest
	{
		public override void Read()
		{
		}

		public bool Write(string content)
		{
			return SimpleFile.Write(content);
		}
	}
}
