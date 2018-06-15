using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Github.Issue105.Input
{
	public class TestCase
	{
		public void Interface(IAsyncChild c)
		{
			c.SomeMethod();
		}

		public void Class(BothSyncAndAsync c)
		{
			c.SomeMethod();
		}
	}

	public interface IParent
	{
		void SomeMethod();
	}

	public interface IAsyncChild : IParent
	{
		Task SomeMethodAsync();
	}

	public class BothSyncAndAsync : IAsyncChild
	{
		public void SomeMethod()
		{
		}

		public Task SomeMethodAsync()
		{
			return Task.CompletedTask;
		}
	}
}
