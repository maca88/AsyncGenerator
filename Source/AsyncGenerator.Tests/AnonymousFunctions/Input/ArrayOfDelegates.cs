using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AnonymousFunctions.Input
{
	public class ArrayOfDelegates
	{
		public void Test()
		{
			var dict = new Dictionary<Action, bool>()
			{
				{() => { Read(); }, true},
				{() => Read(), true},
				{Read, true}
			};
			var array = new List<Action>()
			{
				() => { Read(); },
				() => Read(),
				Read,
			};
			var array2 = new Action[]
			{
				() => { Read(); },
				() => Read(),
				Read,
			};
		}

		public void Read()
		{
			SimpleFile.Read();
		}
	}
}
