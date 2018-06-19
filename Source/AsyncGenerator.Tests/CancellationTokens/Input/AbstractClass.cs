using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.CancellationTokens.Input
{
	public abstract class AbstractClass
	{
		public virtual void Read()
		{
			SimpleFile.Read();
		}

		public virtual bool Write(string value)
		{
			return SimpleFile.Write(value);
		}

		public abstract void Clear();
	}

	public class DerivedClass : AbstractClass
	{
		public override void Read()
		{
			base.Read();
		}

		public override bool Write(string value)
		{
			return base.Write(value);
		}

		public override void Clear()
		{
			SimpleFile.Read();
		}
	}
}
