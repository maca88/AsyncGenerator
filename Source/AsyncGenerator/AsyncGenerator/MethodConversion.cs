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
		/// The method wont be changed
		/// </summary>
		None = 0,
		/// <summary>
		/// The method will be converted to async
		/// </summary>
		ToAsync = 1,
		/// <summary>
		/// The method will be converted to async only if there is at least one method within the method body that has an async counterpart
		/// </summary>
		Smart = 3
	}
}
