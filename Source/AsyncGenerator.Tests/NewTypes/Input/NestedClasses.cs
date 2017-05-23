using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	public class NestedClasses
	{
		private readonly NestedClass _nestedClass;

		public NestedClasses()
		{
			_nestedClass = new NestedClass();
		}

		public class NestedClass : IDisposable
		{
			public bool Disposed { get; set; }

			public NestedClass()
			{
				SimpleFile.Read();
			}

			public void Nop() { }

			public void Read()
			{
				SimpleFile.Read();
			}

			public void Dispose()
			{
				Disposed = true;
				Read();
			}
		}

		[Custom]
		public void Read()
		{
			DoSomething();
			SimpleFile.Read();
		}

		public void DoSomething()
		{
			_nestedClass.Nop();
		}

		public void Read2()
		{
			Read();
		}

		public void Read3()
		{
			_nestedClass.Read();
		}

	}
}
