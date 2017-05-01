using System.ComponentModel;
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

		[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining | MethodImplOptions.Synchronized | MethodImplOptions.PreserveSig)]
		public void NoOptimizationNoInliningSynhronized()
		{
			SimpleFile.Read();
		}

		[MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.NoOptimization), ReadOnly(false)]
		public static void ReadonlySynhronized()
		{
			SimpleFile.Read();
		}

	}
}
