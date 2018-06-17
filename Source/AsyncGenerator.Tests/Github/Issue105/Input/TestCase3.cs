using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Github.Issue105.Input
{
	public abstract class MyDataReader : DbDataReader
	{
		public override bool IsDBNull(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
		{
			return base.IsDBNullAsync(ordinal, cancellationToken);
		}
	}

	public class TestCase3
	{
		public bool Test(DbDataReader reader)
		{
			return reader.IsDBNull(0);
		}
	}
}
