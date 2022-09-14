using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.PreprocessorDirectives.Input
{
	public class TestCase
	{
		/// <summary>
		/// Test
		/// </summary>
		public void Read()
		{
			SimpleFile.Read();
		}

		public void Read2()
		{
			SimpleFile.Read();
		}

		#region MyRegion
		#endregion

		public void Read3()
		{
			SimpleFile.Read();
		}

		#region MyRegion2
		#endregion
	}
}
