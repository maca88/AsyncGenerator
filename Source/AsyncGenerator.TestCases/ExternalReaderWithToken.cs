using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncGenerator.TestCases
{
	public class ExternalReaderWithToken
	{
		public virtual bool Read()
		{
			SimpleFile.Read();
			return true;
		}

		public virtual async Task<bool> ReadAsync(CancellationToken cancellationToken)
		{
			await SimpleFile.ReadAsync(cancellationToken);
			return true;
		}
	}
}
