using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Configuration
{
	public class AsyncCodeConfiguration
	{
		private AsyncCodeConfiguration() {}

		public static AsyncCodeConfiguration Create()
		{
			return new AsyncCodeConfiguration();
		}

		internal List<SolutionConfiguration> SolutionConfigurations { get; } = new List<SolutionConfiguration>();

		public AsyncCodeConfiguration ConfigureSolution(string solutionFilePath, Action<IFluentSolutionConfiguration> action)
		{
			if (solutionFilePath == null)
			{
				throw new ArgumentNullException(nameof(solutionFilePath));
			}
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			if (!File.Exists(solutionFilePath))
			{
				throw new FileNotFoundException($"Solution not found. Path:'{solutionFilePath}'");
			}
			var solutionConfig = new SolutionConfiguration(solutionFilePath);
			SolutionConfigurations.Add(solutionConfig);
			action(solutionConfig);
			return this;
		}
	}
}
