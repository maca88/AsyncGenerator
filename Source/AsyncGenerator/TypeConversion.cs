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
		/// Conversion will be decided by the analyzer. The final conversion can be <see cref="Partial"/> if the type contains at least one
		/// method with the conversion <see cref="MethodConversion.ToAsync"/> otherwise <see cref="Ignore"/>
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// The type will not be modified
		/// </summary>
		Ignore = 1,
		/// <summary>
		/// A partial type will be created that could contain one or more async methods
		/// </summary>
		Partial = 2,
		/// <summary>
		/// A new type will be created with an Async postfix that could contain one or more async methods
		/// </summary>
		NewType = 3,
		/// <summary>
		/// The type will be copied into the new one
		/// </summary>
		Copy = 4
	}
}
