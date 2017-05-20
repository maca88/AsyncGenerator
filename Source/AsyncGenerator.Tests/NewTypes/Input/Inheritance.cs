using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.NewTypes.Input
{
	public class Inheritance
	{
		public virtual void Read()
		{
			
		}
	}

	/// <summary>
	/// Derives from <see cref="Inheritance"/>
	/// </summary>
	public class Derived : Inheritance
	{
		private Type _type = typeof(Derived);
		private Type _baseType;

		public Derived()
		{
			_baseType = typeof(Inheritance);
		}

		public override void Read()
		{
			SimpleFile.Read();
		}
	}
}
