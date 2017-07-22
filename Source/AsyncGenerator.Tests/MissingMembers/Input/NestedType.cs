using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.MissingMembers.Input
{
#if TEST
	public partial interface INestedClass
	{
		Task MethodAsync();

		Task<bool> Method2Async(CancellationToken cancellationToken);

		Task<bool> Method3Async(CancellationToken cancellationToken = default(CancellationToken));
	}
#endif

	public partial interface INestedClass
	{
		void Method();
	}

	public class NestedType
	{
		public bool IsSuccess { get; set; }

		public bool Write()
		{
			return SimpleFile.Write("");
		}

		public class NestedClass : INestedClass
		{
			public bool Executed { get; set; }

			public void Method()
			{
			}
		}
	}
}
