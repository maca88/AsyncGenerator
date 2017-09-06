using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	public abstract class InternalReaderWithToken
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

	public class NestedDerivedAsyncWithToken
	{
		public void Write()
		{
			SimpleFile.Write("");
		}

		public class Nested : InternalReaderWithToken
		{
			public override bool Read()
			{
				return false;
			}
		}

		public class Nested2 : ExternalReaderWithToken
		{
			public override bool Read()
			{
				return false;
			}
		}
	}
}
