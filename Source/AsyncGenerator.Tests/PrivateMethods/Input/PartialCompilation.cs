using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.PrivateMethods.Input
{
	public class PartialCompilationDerived : PartialCompilation
	{
		protected override IList List()
		{
			SimpleFile.Read();
			return null;
		}
	}

	public class PartialCompilation
	{
		public class FutureValue<T>
		{
			public delegate IEnumerable<T> GetResult();

			public delegate Task<IEnumerable<T>> GetResultAsync(CancellationToken cancellationToken);

			private readonly GetResult _getResult;

			private readonly GetResultAsync _getResultAsync;

			public FutureValue(GetResult result, GetResultAsync resultAsync)
			{
				_getResult = result;
				_getResultAsync = resultAsync;
			}
		}

#if TEST

		public FutureValue<TResult> GetFutureValue<TResult>()
		{
			var future = new FutureValue<TResult>(
				() => GetCurrentResult<TResult>(0),
				cancellationToken => GetCurrentResultAsync<TResult>(0, cancellationToken));
			return future;
		}
#endif

		private IList GetResults()
		{
			return List();
		}

		private IEnumerable<TResult> GetCurrentResult<TResult>(int currentIndex)
		{
			return ((IList)GetResults()[currentIndex]).Cast<TResult>();
		}

		protected virtual IList List()
		{
			throw new NotSupportedException();
		}
	}
}
