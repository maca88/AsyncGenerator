#if TEST
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.NUnit.Input
{
	[TestFixture]
	public class SimpleFixture
	{
		[Test]
		public void Test()
		{
			SimpleFile.Read();
		}

		[TestCaseSource(nameof(SetUp))]
		public void TestCaseSource()
		{
			SimpleFile.Read();
		}

		[TestCase("test")]
		public void TestCase(string test)
		{
			SimpleFile.Read();
		}

		[SetUp]
		public void SetUp()
		{
		}

		[TearDown]
		public void TearDown()
		{
		}
	}
}
#endif