namespace AsyncGenerator.Core.Configuration
{
	public interface IProjectTransformConfiguration
	{
		string AsyncFolder { get; }

		bool LocalFunctions { get; }

		string AsyncLockFullTypeName { get; }

		string AsyncLockMethodName { get; }

		bool ConcurrentRun { get; }

		IProjectDocumentationCommentConfiguration DocumentationComments { get; }

		IProjectPreprocessorDirectivesConfiguration PreprocessorDirectives { get; }
	}
}
