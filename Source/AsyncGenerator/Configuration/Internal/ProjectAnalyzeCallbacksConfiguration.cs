using System;
using System.Collections.Generic;
using AsyncGenerator.Analyzation;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectAnalyzeCallbacksConfiguration : IProjectAnalyzeCallbacksConfiguration
	{
		public List<Action<IProjectAnalyzationResult>> AfterAnalyzation { get; } = new List<Action<IProjectAnalyzationResult>>();

		IProjectAnalyzeCallbacksConfiguration IProjectAnalyzeCallbacksConfiguration.AfterAnalyzation(Action<IProjectAnalyzationResult> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			AfterAnalyzation.Add(action);
			return this;
		}
	}
}
