using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.SimpleOmitAsync.Input
{
	public class TestCase
	{
		public bool SimpleReturn()
		{
			return SimpleFile.Write("");
		}

		public bool DoubleCallReturn()
		{
			return SimpleFile.Write(ReadFile());
		}

		public string SyncReturn()
		{
			return SyncReadFile();
		}

		public void SimpleVoid()
		{
			SimpleFile.Read();
		}

		public void DoubleCallVoid()
		{
			SyncReadFile();
			SimpleFile.Read();
		}

		public void ExpressionVoid() => SimpleFile.Read();

		public bool ExpressionReturn() => SimpleFile.Write("");

		public string ReadFile()
		{
			SimpleFile.Read();
			return "";
		}

		public string SyncReadFile()
		{
			return "";
		}

		public string SimpleReturnString()
		{
			return "";
		}

		public string SimpleReturnDefaultOfString()
		{
			return default(string);
		}

		public decimal SimpleReturnDecimal()
		{
			return 25m;
		}

		public decimal ReturnDecimalConstructor()
		{
			return new decimal(25);
		}
	}
}
