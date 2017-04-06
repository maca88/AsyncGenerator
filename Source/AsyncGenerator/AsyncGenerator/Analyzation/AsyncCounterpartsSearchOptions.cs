using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncGenerator.Analyzation
{
	[Flags]
	public enum AsyncCounterpartsSearchOptions
	{
		/// <summary>
		/// With this option a method should be qualified as an async counterpart if the following conditions are met:
		/// 1. The async method is defined in the same type
		/// 2. The async method has the same number of parameters and they should be the same, except for delegates that should be an async counterpart of the original
		/// 3. The async method return type should be the same type as the original wrapped in a <see cref="Task"/>
		/// </summary>
		Default = 1,
		/// <summary>
		/// With this option a method should be qualified as an async counterpart only if the async method parameters are exactly the same as the original ones.
		/// This option should limit the <see cref="Default"/> constraints.
		/// </summary>
		EqualParameters = 2,
		/// <summary>
		/// With this option a method should be qualified as an async counterpart if it is defined in an inherited type.
		/// This option should extend the <see cref="Default"/> constraints.
		/// </summary>
		SearchInheritTypes = 4,
		/// <summary>
		/// With this option a method should be qualified as an async counterpart if has a parameter of type <see cref="System.Threading.CancellationToken"/>.
		/// This options should extend the <see cref="Default"/> constraints.
		/// </summary>
		HasCancellationToken = 8
	}
}
