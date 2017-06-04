using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Formatting.Input
{
	public class Async
	{
		public void Test()
		{
			Assert.DoesNotThrow(
				() =>
				{
					SimpleFile.Read();
					SimpleFile.Read();
				});

			Assert.DoesNotThrow(
				delegate ()
				{
					SimpleFile.Read();
					SimpleFile.Read();
				});

			Runner.RunWithParameter(
				obj =>
				{
					SimpleFile.Read();
					SimpleFile.Read();
				});

			void LocalDoubleRead()
			{
				SimpleFile.Read();
				SimpleFile.Read();
			}
		}

		void DoubleRead()
		{
			SimpleFile.Read();
			SimpleFile.Read();
		}

		static void StaticDoubleRead()
		{
			SimpleFile.Read();
			SimpleFile.Read();
		}

		public void PublicDoubleRead()
		{
			SimpleFile.Read();
			SimpleFile.Read();
		}
	}
}
