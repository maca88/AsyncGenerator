using System;
using AsyncGenerator.Analyzation;

namespace AsyncGenerator.Configuration
{
	public interface IProjectAnalyzeCallbacksConfiguration
	{
		/// <summary>
		/// Appends a callback that will be called after the analyzation step
		/// </summary>
		IProjectAnalyzeCallbacksConfiguration AfterAnalyzation(Action<IProjectAnalyzationResult> action);
	}
}
