using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Exceptions.Input
{
	public class TestCase
	{
		public void NotImplemented()
		{
			throw new NotImplementedException();
		}

		public void NotImplementedExpression() => throw new NotImplementedException();

		public void InvalidOperationException()
		{
			throw new InvalidOperationException("Invalid");
#pragma warning disable 162
			SimpleFile.Read();
#pragma warning restore 162
		}

		public void PreconditionOperationException(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}
			throw new InvalidOperationException("Invalid");
		}

	}
}
