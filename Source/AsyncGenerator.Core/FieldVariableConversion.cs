using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Core
{
	public enum FieldVariableConversion
	{
		Unknown = 0,
		/// <summary>
		/// The field variable will not be modified or copied
		/// </summary>
		Ignore = 1,
		/// <summary>
		/// The field variable will be copied into the new type. This option is only valid when the type conversion is set to <see cref="TypeConversion.NewType"/>
		/// or <see cref="TypeConversion.Copy"/>. 
		/// </summary>
		Copy = 2,
		/// <summary>
		/// The field variable will be copied only when the type that contains this field will be converted into a new type and is used by a converted method.
		/// </summary>
		Smart = 3
	}
}
