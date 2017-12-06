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

#pragma warning disable CS0168 // Variable is declared but never used
#pragma warning disable CS8321 // Local function is declared but never used
			void LocalDoubleRead()
#pragma warning restore CS8321 // Local function is declared but never used
#pragma warning restore CS0168 // Variable is declared but never used
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
