namespace AsyncGenerator.Core
{
	public class PreprocessorDirectives
	{
		public PreprocessorDirectives(string startDirective, string endDirective = null)
		{
			StartDirective = startDirective;
			EndDirective = endDirective;
		}

		public string StartDirective { get; }

		public string EndDirective { get; }
	}
}
