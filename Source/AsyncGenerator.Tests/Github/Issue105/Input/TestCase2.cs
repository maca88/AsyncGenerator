using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Github.Issue105.Input
{
	public class TestCase2
	{
		private readonly List<IAsyncSynchronization> _synchronizations = new List<IAsyncSynchronization>();

		public void Test(bool successful)
		{
			NotifyLocalSynchsAfterTransactionCompletion(successful);
		}

		private void NotifyLocalSynchsAfterTransactionCompletion(bool success)
		{
			if (_synchronizations != null)
			{
				for (int i = 0; i < _synchronizations.Count; i++)
				{
					var sync = _synchronizations[i];
					sync.AfterCompletion(success);
				}
			}
		}
	}

	public interface ISynchronization
	{
		void BeforeCompletion();
		void AfterCompletion(bool success);
	}

	public interface IAsyncSynchronization : ISynchronization
	{
		Task BeforeCompletionAsync(CancellationToken cancellationToken = default(CancellationToken));
		Task AfterCompletionAsync(bool success, CancellationToken cancellationToken = default(CancellationToken));
	}
#if TEST
	public class SynchronizationWrapper : IAsyncSynchronization
	{
		private readonly ISynchronization _inner;

		public SynchronizationWrapper(ISynchronization inner)
		{
			_inner = inner;
		}

		public void BeforeCompletion()
		{
			_inner.BeforeCompletion();
		}

		public void AfterCompletion(bool success)
		{
			_inner.AfterCompletion(success);
		}
#if ASYNC
		public Task BeforeCompletionAsync(
			CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
				return Task.FromCanceled(cancellationToken);

			try
			{
				BeforeCompletion();
				return Task.CompletedTask;
			}
			catch (Exception e)
			{
				return Task.FromException(e);
			}
		}

		public Task AfterCompletionAsync(
			bool success,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
				return Task.FromCanceled(cancellationToken);

			try
			{
				AfterCompletion(success);
				return Task.CompletedTask;
			}
			catch (Exception e)
			{
				return Task.FromException(e);
			}
		}
#endif
	}
#endif
}
