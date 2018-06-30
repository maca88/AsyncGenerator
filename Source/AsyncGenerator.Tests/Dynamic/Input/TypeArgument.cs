using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Dynamic.Input
{
	public class TypeArgument
	{
		public void Test(Database database)
		{
			dynamic value = "test";
			database.Save("test", value);
			Assert.That(database.Save("test", value), Is.True);

			Assert.That(Database.IsInitialized(value.Test), Is.True);
			Assert.That(() => Database.IsInitialized(value.Test), Is.True);

			Assert.That(value.Length, Is.EqualTo(4));
		}
	}
}
