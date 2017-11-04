using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;

namespace AsyncGenerator.Core.Plugins
{
	public interface IBodyFunctionReferencePostAnalyzer : IPlugin
	{
		void PostAnalyzeBodyFunctionReference(IBodyFunctionReferenceAnalyzation functionReferenceAnalyzation);
	}
}
