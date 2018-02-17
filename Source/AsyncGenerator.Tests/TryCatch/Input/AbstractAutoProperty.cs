using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.TryCatch.Input
{
	public class AbstractAutoPropertyImpl : AbstractAutoProperty
	{
		public override bool Success { get; set; }
	}

	public abstract class AbstractAutoProperty
	{
		public abstract bool Success { get; set; }

		public int Test()
		{
			return Success ? 1 : 0;
		}

		public void Test2()
		{
			Success = true;
		}

		public AbstractAutoProperty Create()
		{
			return new AbstractAutoPropertyImpl
			{
				Success = true
			};
		}
	}
}
