namespace AsyncGenerator.Core.Configuration
{
	public interface ISolutionConfiguration
	{
		bool RunInParallel { get; }

		bool ApplyChanges { get; }
	}
}
