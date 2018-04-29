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

		public void MethodThatCatchesExceptionsNoDeclaration()
		{
			try
			{
				SimpleFile.Read();
			}
			catch
			{
				throw new Exception("My exception");
			}
		}

		public void MethodThatCatchesExceptionsNested()
		{
			try
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
			catch (Exception ex)
			{
				throw new Exception("My wrapped exception", ex);
			}
		}

		public void LocalFunctionThatCatchesExceptions()
		{
			Internal();

			void Internal()
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
}