using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.SimpleClassInheritance.Input
{
	public interface IClass
	{
		void ReadFile();
	}

	public class BaseClass : IClass
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
