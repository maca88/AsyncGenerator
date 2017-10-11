using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Github.Issue57.Input
{
	public interface IInterface
	{
		void Read();
	}

	public class ExplicitInterface : IInterface
	{
		void IInterface.Read()
		{
			SimpleFile.Read();
		}
	}
}
