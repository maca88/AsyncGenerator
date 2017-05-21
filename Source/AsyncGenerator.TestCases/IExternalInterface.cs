using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncGenerator.TestCases
{
	public interface IExternalInterface
	{
		void Method();

		bool Method2();

		bool Method3();

		Task MethodAsync();

		Task<bool> Method2Async(CancellationToken cancellationToken);

		Task<bool> Method3Async(CancellationToken cancellationToken = default(CancellationToken));
	}
}
