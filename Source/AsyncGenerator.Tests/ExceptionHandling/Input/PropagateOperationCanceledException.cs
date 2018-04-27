using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.ExceptionHandling.Input
{
	public class PropagateOperationCanceledException
	{
		public void MethodThatCatchesExceptions()
		{
			try
			{
				SimpleFile.Read();
			}
			catch (Exception ex)
			{
				throw new Exception("My wrapped exception", ex);
			}
		}
	}
}