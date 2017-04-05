using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Analyzation
{
	[Flags]
	public enum AsyncCounterpartsSearchOptions
	{
		Default = 1,
		EqualParamaters = 2,
		SeachInheritTypes = 4
	}
}
