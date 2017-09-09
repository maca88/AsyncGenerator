using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Github.Issue51.Input
{
	public class TestCase : AbstractTest
	{
		public override void Read()
		{
			SimpleFile.Read();
		}

		public override bool Write(string content)
		{
			return SimpleFile.Write(content);
		}
	}

	public abstract class AbstractTest : ITestInterface
	{
		public virtual void Read()
		{
			SimpleFile.Read();
		}

		public abstract bool Write(string content);
	}

	public interface ITestInterface
	{
		void Read();

		bool Write(string content);
	}
}
