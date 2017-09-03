using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	public class BaseNonAsyncClass
	{
		public void Test()
		{
			
		}
	}

	public class DerivedAsyncClass : BaseNonAsyncClass
	{
		public BaseNonAsyncClass BaseTypeField;

		public BaseNonAsyncClass BaseTypeProperty { get; set; }

		public void Read()
		{
			if (BaseTypeField == null)
			{
				BaseTypeField = new BaseNonAsyncClass();
			}
			if (BaseTypeProperty != null)
			{
				SimpleFile.Read();
			}
		}
	}
}
