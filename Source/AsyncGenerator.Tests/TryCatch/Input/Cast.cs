using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.TryCatch.Input
{
	public class Cast
	{
		private readonly long _maxLong = long.MaxValue;

		public int Test()
		{
			return (int)_maxLong;
		}
	}
}
