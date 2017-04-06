using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TestCases
{
	public class BaseClass
	{
		public virtual void ReadFile()
		{
		}
	}

	public class DerivedClass : BaseClass
	{
		public override void ReadFile()
		{
			SimpleFile.Read();
		}
	}

	public class SimpleClassInheritance
	{
		public void Read()
		{
			new DerivedClass().ReadFile();
		}
	}
}
