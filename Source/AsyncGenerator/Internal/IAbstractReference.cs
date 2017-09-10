using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;

namespace AsyncGenerator.Internal
{
	internal interface IAbstractReference : IReferenceAnalyzationResult
	{
		AbstractData Data { get; }

		AbstractData ReferenceData { get; }
	}
}
