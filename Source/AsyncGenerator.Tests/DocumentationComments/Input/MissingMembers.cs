namespace AsyncGenerator.Tests.DocumentationComments.Input
{
	#if TEST
	public class MissingMembers : IExternalInterface
	{
		public class NestedClass : IExternalInterface
		{
			public void Method()
			{
			}

			public bool Method2()
			{
				return SimpleFile.Write("");
			}

			public bool Method3()
			{
				return false;
			}
		}

		public void Method()
		{
			SimpleFile.Read();
		}

		public bool Method2()
		{
			return SimpleFile.Write("");
		}

		public bool Method3()
		{
			return Method2();
		}

		public void Read()
		{
			Method();
		}
	}
	#endif
}
