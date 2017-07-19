using System;

namespace AsyncGenerator.Core
{
	[Flags]
	public enum PropertyConversion
	{
		/// <summary>
		/// The property will not be modified
		/// </summary>
		Ignore = 1,
		/// <summary>
		/// Conversion will be decided by the analyzer. The property will be converted to async only if there is at
		/// least one method invocation within the accessors that has an async counterpart.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Works like <see cref="Unknown"/> with the addition of scanning the accessors body for method invocations that have an async counterpart.
		/// This can be used for properties that invokes external methods with an async counterpart. 
		/// Properties with this conversion can affect also properties/methods with <see cref="Unknown"/> conversion, as the analyzer will find all usages of the 
		/// found external methods with an async counterpart.
		/// </summary>
		Smart = 2,
		/// <summary>
		/// The property will be converted to async, but only if the analyzer will be able to convert it.
		/// Also the accessors body will get scanned for method invocations that have an async counterpart.
		/// Use this conversion for properties that need to be async even if there is no method invocations with async counterpart inside accessors.
		/// </summary>
		ToAsync = 8,
		/// <summary>
		/// The property will be copied into the new type, only type references may be modified. This option is only valid when the type conversion is set to <see cref="TypeConversion.NewType"/>
		/// or <see cref="TypeConversion.Copy"/>. 
		/// </summary>
		Copy = 16
	}
}
