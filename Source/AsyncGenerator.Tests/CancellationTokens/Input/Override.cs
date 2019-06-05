using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.CancellationTokens.Input
{
	public class Override : Base
	{
		public override void WriteMany()
		{
			ExecuteWrite();
		}

#if TEST
		public override Task ExecuteWriteManyAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
#endif
	}

	public class Base
	{
		public virtual void Write()
		{
			ExecuteWrite();
		}

		public virtual void ExecuteWrite()
		{
			SimpleFile.Write("");
		}

		public virtual void WriteMany()
		{
			ExecuteWriteMany();
		}

		public virtual void ExecuteWriteMany()
		{
			SimpleFile.Write("");
		}
	}
}
