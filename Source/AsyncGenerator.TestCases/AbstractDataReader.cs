using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncGenerator.TestCases
{
	public abstract class AbstractDataReader : DbDataReader
	{
		public override bool NextResult()
		{
			throw new NotSupportedException();
		}

		public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public new Task<bool> NextResultAsync()
		{
			throw new NotSupportedException();
		}
	}
}
