using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.TryCatch.Input
{
	public class VirtualAutoProperty
	{
		public virtual bool Success { get; set; }

		public int Test()
		{
			return Success ? 1 : 0;
		}

		public void Test2()
		{
			Success = true;
		}

		public VirtualAutoProperty Create()
		{
			return new VirtualAutoProperty { Success = true };
		}
	}
}
