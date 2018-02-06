using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.TryCatch.Input
{
	public interface IInterfaceAutoProperty
	{
		bool Success { get; set; }
	}

	public class InterfaceAutoProperty : IInterfaceAutoProperty
	{
		private static readonly IInterfaceAutoProperty Singleton = new InterfaceAutoProperty();

		public bool Success { get; set; }

		public int Test()
		{
			return Singleton.Success ? 1 : 0;
		}

		public void Test2()
		{
			Singleton.Success = true;
		}

		public IInterfaceAutoProperty Create()
		{
			return new InterfaceAutoProperty
			{
				Success = true
			};
		}
	}
}
