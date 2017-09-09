using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Github.Issue49.Input
{
	public class TestCase
	{
		private const string Content = "test";

		public void Write()
		{
			SimpleFile.Write(GetContent());
		}

		private static string GetContent() => $"_{Content}_";
	}
}
