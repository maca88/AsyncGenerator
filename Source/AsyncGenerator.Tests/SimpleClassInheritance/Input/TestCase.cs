using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.SimpleClassInheritance.Input
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

	public class TestCase
	{
		public void Read()
		{
			new DerivedClass().ReadFile();
		}
	}
}
