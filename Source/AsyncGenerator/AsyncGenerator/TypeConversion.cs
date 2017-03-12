using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator
{
	public enum TypeConversion
	{
		/// <summary>
		/// The type conversion will be decided by the analyzer
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// A partial type will be created that will contains the async counterparts
		/// </summary>
		Partial = 1,
		/// <summary>
		/// A new type will be created with an Async postfix that will contains the async counterparts
		/// </summary>
		NewType = 2
	}
}
