using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.ExceptionHandling.Input
{
	public class DoNotPropagateOperationCanceledException
	{
		public void MethodThatCatchesTargetInvocationException()
		{
			try
			{
				SimpleFile.Read();
			}
			catch (TargetInvocationException ex)
			{
				throw new Exception("My wrapped exception", ex);
			}
		}

		public void MethodThatCatchesOperationCanceledException()
		{
			try
			{
				SimpleFile.Read();
			}
			catch (OperationCanceledException ex)
			{
				throw new Exception("My wrapped exception", ex);
			}
		}

		public void MethodWithoutCatch()
		{
			try
			{
				SimpleFile.Read();
			}
			finally
			{
			}
		}

		public void LocalFunctionThatCatchesOperationCanceledException()
		{
			Internal();

			void Internal()
			{
				try
				{
					SimpleFile.Read();
				}
				catch (OperationCanceledException ex)
				{
					throw new Exception("My wrapped exception", ex);
				}
			}
		}

		public void MethodCatchNotWrappingAsyncCall()
		{
			SimpleFile.Read();
			try
			{
				; //no async calls here
			}
			catch
			{
			}
		}
	}
}