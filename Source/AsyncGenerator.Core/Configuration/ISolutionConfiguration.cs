namespace AsyncGenerator.Core.Configuration
{
	public interface ISolutionConfiguration
	{
		bool ConcurrentRun { get; }

		bool ApplyChanges { get; }
	}
}
