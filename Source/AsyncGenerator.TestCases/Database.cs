using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.TestCases
{
	public interface IDatabase : IDatabaseAsync
	{
		void Save();
	}

	public interface IDatabaseAsync
	{
		Task SaveAsync();
	}

	public class Database : IDatabase
	{
		public void Save()
		{

		}

		public Task SaveAsync()
		{
			return Task.CompletedTask;
		}
	}
}
