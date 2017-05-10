using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Configuration
{
	public interface IProjectAnalyzeConfiguration
	{
		bool ScanMethodBody { get; }

		bool ScanForMissingAsyncMembers { get; }

		bool UseCancellationTokens { get; }

		IProjectCancellationTokenConfiguration CancellationTokens { get; }
	}
}
