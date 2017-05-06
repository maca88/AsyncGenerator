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

		bool Write(string content);
	}


	public abstract class AbstractTest : ITestInteraface
	{
		public virtual void Read()
		{
		}

		public abstract bool Write(string content);
	}

	public class TestCase : AbstractTest
	{
		public override void Read()
		{
		}

		public override bool Write(string content)
		{
			return SimpleFile.Write(content);
		}
	}
}
