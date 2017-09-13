using System.ComponentModel;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Nameof.Input
{
	public class Method
	{
		[DefaultValue(nameof(Write))]
		public void Write()
		{
			SimpleFile.Write(nameof(DoSomething));
		}

		private void DoSomething()
		{
			
		}

		[DefaultValue(nameof(DoSomething2))]
		public void Read()
		{
			SimpleFile.Read();
		}

		private void DoSomething2()
		{

		}
	}
}
