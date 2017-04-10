using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.TestCases
{
	public class VariousTaskRunUsages
	{
		public void NotAwaitedActionTask()
		{
			Task.Run(() => SimpleFile.Read());
		}

		public void NotAwaitedFunctionTask()
		{
			Task.Run(() =>
			{
				SimpleFile.Read();
				return true;
			});
		}

		public void WaitActionTask()
		{
			Task.Run(() => SimpleFile.Read()).Wait();
		}

		public void WaitFunctionTask()
		{
			var result = Task.Run(() =>
			{
				SimpleFile.Read();
				return true;
			}).Result;
		}
		public void WaitFunctionTaskNoResult()
		{
			Task.Run(() =>
			{
				SimpleFile.Read();
				return true;
			}).Wait();
		}

		public void AwaitedActionTask()
		{
			Task.Run(() => SimpleFile.Read()).GetAwaiter().GetResult();
		}

		public void AwaitedFunctionTask()
		{
			var result2 = Task.Run(() =>
			{
				SimpleFile.Read();
				return true;
			}).GetAwaiter().GetResult();
		}

		public void ConfiguratedAwaitedActionTask()
		{
			Task.Run(() => SimpleFile.Read()).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public void ConfiguratedAwaitedFunctionTask()
		{
			var result3 = Task.Run(() =>
			{
				SimpleFile.Read();
				return true;
			}).ConfigureAwait(false).GetAwaiter().GetResult();
		}
	}
}
