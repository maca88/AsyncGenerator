using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.TryCatch.Input
{
	public class ExternalProperty
	{
		public string Test()
		{
			return typeof(AutoProperty).FullName;
		}
	}
}
