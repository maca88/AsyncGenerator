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
		public void RunTasks()
		{
			// Actions
			Task.Run(() => SimpleFile.Read());
			Task.Run(() => SimpleFile.Read()).Wait();
			Task.Run(() => SimpleFile.Read()).GetAwaiter().GetResult();
			Task.Run(() => SimpleFile.Read()).ConfigureAwait(false).GetAwaiter().GetResult();

			// Functions
			Task.Run(() =>
			{
				SimpleFile.Read();
				return true;
			});
			var result = Task.Run(() =>
			{
				SimpleFile.Read();
				return true;
			}).Result;
			var result2 = Task.Run(() =>
			{
				SimpleFile.Read();
				return true;
			}).GetAwaiter().GetResult();
			var result3 = Task.Run(() =>
			{
				SimpleFile.Read();
				return true;
			}).ConfigureAwait(false).GetAwaiter().GetResult();
		}
	}
}
