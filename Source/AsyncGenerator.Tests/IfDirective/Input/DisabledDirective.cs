using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.IfDirective.Input
{
	public class DisabledDirective
	{
		public void Read()
		{
			ReadInternal();
		}

#if DISABLED
		public void Read2()
		{
			ReadInternal();
		}
#elif TEST
		public void Read2()
		{
			ReadInternal();
		}
#else
		internal class Test2
		{

		}
#endif

		private void ReadInternal()
		{
			SimpleFile.Read();
		}

#if DISABLED
		public void Read3()
		{
			ReadInternal();
		}
#endif

		internal class Test
		{

		}

#if DISABLED
		public void Read4()
		{
			ReadInternal();
		}
#endif

		[Custom]
		public void Write()
		{
			SimpleFile.Write("");
		}

#if DISABLED
		public void Read5()
		{
			ReadInternal();
		}
#endif
	}
}
