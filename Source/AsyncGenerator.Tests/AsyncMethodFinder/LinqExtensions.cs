using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.AsyncMethodFinder
{
	public static class LinqExtensions
	{
		public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source)
		{
			return null;
		}

		public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken)
		{
			return null;
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source)
		{
			return null;
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
		{
			return null;
		}

		public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
		{
			return null;
		}

		public static int Delete<TSource>(this IQueryable<TSource> source)
		{
			return 0;
		}

		public static Task<int> DeleteAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken)
		{
			return Task.FromResult(0);
		}
	}
}
