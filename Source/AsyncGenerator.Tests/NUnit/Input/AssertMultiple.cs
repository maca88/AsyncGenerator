using System;
using System.Collections.Generic;
using System.Text;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.NUnit.Input
{
	public class AssertMultiple
	{
		public void MultipleWithDoesNotThrow()
		{
			Assert.Multiple(() =>
			{
				Assert.DoesNotThrow(() => SimpleFile.Read());
				Assert.DoesNotThrow(() => SimpleFile.Read(), "test");
			});

			Assert.Multiple(() =>
			{
				Assert.DoesNotThrow(() => SimpleFile.Read());
				Assert.DoesNotThrow(() => SimpleFile.Clear(), "test");
			});

			Assert.Multiple(() =>
			{
				Assert.DoesNotThrow(() => SimpleFile.Clear());
				Assert.DoesNotThrow(() => SimpleFile.Clear(), "test");
			});
		}
	}
}
