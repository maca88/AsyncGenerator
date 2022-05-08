using System.Data.Common;

namespace AsyncGenerator.Tests.AsyncReturnType.Input
{
	public class NullCoalescing
	{
		public void Variables()
		{
			var connection = GetConnection();
			var trx = connection?.BeginTransaction();
			trx?.Dispose();
		}

		private DbConnection GetConnection()
		{
			return default;
		}
	}
}
