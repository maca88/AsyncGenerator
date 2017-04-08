using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Configuration
{
	public interface IAsyncCodeConfiguration
	{
		IAsyncCodeConfiguration ConfigureSolution(string solutionFilePath, Action<ISolutionConfiguration> action);
	}
}
