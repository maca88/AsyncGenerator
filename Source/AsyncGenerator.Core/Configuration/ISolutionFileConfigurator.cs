using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Core.Configuration
{
	public interface ISolutionFileConfigurator
	{
		void Parse(string filePath);

		string GetSolutionPath();

		void Configure(IFluentSolutionConfiguration solutionConfiguration);
	}
}
