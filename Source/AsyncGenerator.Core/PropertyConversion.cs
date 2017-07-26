using System;

namespace AsyncGenerator.Core
{
	public enum PropertyConversion
	{
		/// <summary>
		/// The property will not be modified
		/// </summary>
		Ignore = 1,
		/// <summary>
		/// The property will be copied into the new type, only type references may be modified. This option is only valid when the type conversion is set to <see cref="TypeConversion.NewType"/>
		/// or <see cref="TypeConversion.Copy"/>. 
		/// </summary>
		Copy = 2
	}
}
