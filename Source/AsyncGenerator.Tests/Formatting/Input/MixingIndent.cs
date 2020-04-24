using System;
using System.Collections.Generic;
using System.Text;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Formatting.Input
{
    public class MixingIndent
    {
		[Obsolete]
        public void Test(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

         	SimpleFile
         		.Read();
         	value = value == "null" ? "test" : value;
         	value = value != "test"
         		? "val" : value;
         	value = value != "test"
         		? "val"
         		: value;
        }
    }
}
