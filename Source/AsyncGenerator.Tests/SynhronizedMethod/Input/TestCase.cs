using System.Runtime.CompilerServices;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.SynhronizedMethod.Input
{
	public class TestCase
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Synhronized()
		{
			SimpleFile.Read();
		}

		[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.Synchronized)]
		public void NoOptimizationSynhronized()
		{
			SimpleFile.Read();
		}

	}
}
