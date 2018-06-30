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

		object Save(string name, object obj);

		void Save(object name, object obj);
	}

	public interface IDatabaseAsync
	{
		Task SaveAsync();

		Task<object> SaveAsync(string name, object obj);

		Task SaveAsync(object name, object obj);
	}

	public class Database : IDatabase
	{
		public void Save()
		{

		}

		public object Save(string name, object obj)
		{
			return null;
		}

		public void Save(object name, object obj)
		{
		}

		public Task SaveAsync()
		{
			return Task.CompletedTask;
		}

		public Task<object> SaveAsync(string name, object obj)
		{
			throw new NotImplementedException();
		}

		public Task SaveAsync(object name, object obj)
		{
			throw new NotImplementedException();
		}

		public static bool IsInitialized(object obj)
		{
			return true;
		}

		public static Task<bool> IsInitializedAsync(object obj)
		{
			throw new NotImplementedException();
		}
	}
}
