using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Github.Issue46.Input
{
	public abstract class InternalReaderWithToken
	{
		public virtual bool Read()
		{
			SimpleFile.Read();
			return true;
		}

		public virtual async Task<bool> ReadAsync(CancellationToken cancellationToken)
		{
			await SimpleFile.ReadAsync(cancellationToken);
			return true;
		}
	}

	public class TestCaseWithToken : InternalReaderWithToken
	{
		public override bool Read()
		{
			return false;
		}
	}

	public class TestCaseWithTokenExternal : ExternalReaderWithToken
	{
		public override bool Read()
		{
			return false;
		}
	}

	public class TestCaseBaseCallWithToken : InternalReaderWithToken
	{
		public override bool Read()
		{
			base.Read();
			return false;
		}
	}

	public class TestCaseBaseCallWithTokenExternal : ExternalReaderWithToken
	{
		public override bool Read()
		{
			base.Read();
			return false;
		}
	}
}
