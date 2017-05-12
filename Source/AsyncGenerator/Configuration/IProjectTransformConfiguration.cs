using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration
{
	public interface IProjectTransformConfiguration
	{
		string AsyncFolder { get; }

		bool LocalFunctions { get; }

		string AsyncLockFullTypeName { get; }

		string AsyncLockMethodName { get; }
	}
}
