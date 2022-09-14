﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFluentProjectAsyncExtensionMethodsConfiguration
	{
		/// <summary>
		/// Add a project file that contains async extension methods 
		/// </summary>
		/// <param name="projectName">Name of the project where async extension methods are located</param>
		/// <param name="fileName">Name of the file which contains the async extension methods</param>
		IFluentProjectAsyncExtensionMethodsConfiguration ProjectFile(string projectName, string fileName);

		/// <summary>
		/// Add an external type that contains async extension methods
		/// </summary>
		/// <param name="assemblyName">Name of the assembly where async extension methods are located</param>
		/// <param name="fullTypeName">Full name of the type which contains the async extension methods</param>
		IFluentProjectAsyncExtensionMethodsConfiguration ExternalType(string assemblyName, string fullTypeName);
	}
}
