using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator
{
	public enum MethodConversion
	{
		/// <summary>
		/// The method conversion will be decided by the analyzer
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// The method will not be modified
		/// </summary>
		Ignore = 1,
		/// <summary>
		/// The method will be converted to async only if there is at least one method within the method body that has an async counterpart
		/// </summary>
		Smart = 2,
		/// <summary>
		/// The method will be converted to async only if the analyzer will be able to convert it
		/// </summary>
		ToAsync = 3
	}
}
