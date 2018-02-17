using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.TryCatch.Input
{
	public class CustomProperty
	{
		public bool Success
		{
			get => false;
			set => throw new NotSupportedException();
		}

		public int Test()
		{
			return Success ? 1 : 0;
		}

		public void Test2()
		{
			Success = true;
		}

		public CustomProperty Create()
		{
			return new CustomProperty { Success = true };
		}
	}
}
