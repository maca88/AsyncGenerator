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
		/// Conversion will be decided by the analyzer. The method will be converted to async only if there is at
		/// least one method invocation within the method body that has an async counterpart.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// The method will not be modified
		/// </summary>
		Ignore = 1,
		/// <summary>
		/// Works like <see cref="Unknown"/> with the addition of scanning the method body for method invocations that have an async counterpart.
		/// This can be used for methods that invokes external methods with an async counterpart. 
		/// Methods with this conversion can affect also methods with <see cref="Unknown"/> conversion, as the analyzer will find all usages of the 
		/// found external methods with an async counterpart.
		/// </summary>
		Smart = 2,
		/// <summary>
		/// The method will be converted to async, but only if the analyzer will be able to convert it.
		/// Also the method body will get scanned for method invocations that have an async counterpart.
		/// Use this conversion for methods that need to be async even if there is no method invocations with async counterpart inside the method body.
		/// </summary>
		ToAsync = 3,
		/// <summary>
		/// The method will be copied into the new type, only type references may be modified. This option is only valid when the type conversion is set to <see cref="TypeConversion.NewType"/>
		/// or <see cref="TypeConversion.Copy"/>. 
		/// </summary>
		Copy = 4
	}
}
