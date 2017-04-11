using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TestCases
{
	public class SynhronizedMethod
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
