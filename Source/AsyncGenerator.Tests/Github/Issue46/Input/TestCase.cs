using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Github.Issue46.Input
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

	public class TestCase : InternalReader
	{
		public override bool Read()
		{
			return false;
		}
	}

	public class TestCaseWithExternal : ExternalReader
	{
		public override bool Read()
		{
			return false;
		}
	}
}
