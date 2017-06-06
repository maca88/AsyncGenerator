using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Core.Configuration
{
	public interface ISolutionFileConfigurator
	{
		object Parse(string filePath);

		string GetSolutionPath(object configuration);

		void Configure(object configuration, IFluentSolutionConfiguration solutionConfiguration);
	}
}
