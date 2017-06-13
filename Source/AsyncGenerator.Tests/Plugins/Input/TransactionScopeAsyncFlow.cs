using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Plugins.Input
{
	public class TransactionScopeAsyncFlow
	{
		public void Test()
		{
			using (new TransactionScope())
			{
				SimpleFile.Read();
			}
			using (new TransactionScope(TransactionScopeOption.Required))
			{
				SimpleFile.Read();
			}
			using (new TransactionScope(TransactionScopeOption.Suppress))
			{
				SimpleFile.Read();
			}
			using (new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress))
			{
				SimpleFile.Read();
			}
		}
	}
}
