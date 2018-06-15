using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.SearchInheritedTypes.Input
{
	public abstract class InternalReader
	{
		public virtual bool Read()
		{
			SimpleFile.Read();
			return true;
		}

		public virtual async Task<bool> ReadAsync()
		{
			await SimpleFile.ReadAsync();
			return true;
		}
	}

	public class MissingAsyncMembers : InternalReader
	{
		public override bool Read()
		{
			return false;
		}
	}

	public class MissingAsyncMembersWithExternal : ExternalReader
	{
		public override bool Read()
		{
			return false;
		}
	}

	public class MissingAsyncMembersBaseCall : InternalReader
	{
		public override bool Read()
		{
			base.Read();
			return false;
		}
	}

	public class MissingAsyncMembersBaseCallWithExternal : ExternalReader
	{
		public override bool Read()
		{
			base.Read();
			return false;
		}
	}
}
