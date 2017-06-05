namespace AsyncGenerator.Core.Configuration
{
	public interface IProjectConfiguration
	{
		/// <summary>
		/// Name of the project
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Analyzation configurations for the project
		/// </summary>
		IProjectAnalyzeConfiguration AnalyzeConfiguration { get; }

		/// <summary>
		/// Transformation configurations for the project
		/// </summary>
		IProjectTransformConfiguration TransformConfiguration { get; }

		/// <summary>
		/// Compilation configurations for the project
		/// </summary>
		IProjectCompileConfiguration CompileConfiguration { get; }

		bool RunInParallel { get; }
	}
}
