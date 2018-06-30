using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.HiddenMethods.Input
{
	public interface IFutureEnumerable<T> : IEnumerable<T>
	{
		[Obsolete]
		new IEnumerator<T> GetEnumerator();

		IEnumerable<T> GetEnumerable();

		Task<IEnumerable<T>> GetEnumerableAsync(CancellationToken cancellationToken = default(CancellationToken));
	}

	public class FutureEnumerable
	{
	}

	public class FutureEnumerable<TResult> : IFutureEnumerable<TResult>
	{
		public IEnumerable<TResult> GetEnumerable()
		{
			return null;
		}

		public Task<IEnumerable<TResult>> GetEnumerableAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return null;
		}

		public IEnumerator<TResult> GetEnumerator()
		{
			return GetEnumerable().GetEnumerator();
		}
		
		IEnumerator<TResult> IFutureEnumerable<TResult>.GetEnumerator()
		{
			return GetEnumerable().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerable().GetEnumerator();
		}
	}

	public class DelayedEnumerator<T> : IFutureEnumerable<T>
	{
		public IEnumerable<T> GetEnumerable()
		{
			return null;
		}

		public Task<IEnumerable<T>> GetEnumerableAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return null;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return GetEnumerable().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)GetEnumerable()).GetEnumerator();
		}
	}
}
