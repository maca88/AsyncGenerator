using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.CSharpFeatures.Input
{
	public interface ICustomEnumerable<T> : IEnumerable<T>
	{
		Task<IEnumerator<T>> GetEnumeratorAsync();
	}

	public class CustomEnumerable<T> : ICustomEnumerable<T>
	{
		private readonly List<T> _list;

		public CustomEnumerable(IEnumerable<T> enumerable)
		{
			_list = new List<T>(enumerable);
		}

		public Task<IEnumerator<T>> GetEnumeratorAsync()
		{
			return Task.FromResult<IEnumerator<T>>(_list.GetEnumerator());
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _list.GetEnumerator();
		}
	}

	public class CustomEnumerable
	{
		public int Sum()
		{
			var list = new CustomEnumerable<int>(Enumerable.Range(1, 10));
			var sum = 0;
			foreach (var item in list)
			{
				sum += item;
			}
			var enumerator = list.GetEnumerator();
			enumerator.Dispose();
			return sum;
		}
	}
}
