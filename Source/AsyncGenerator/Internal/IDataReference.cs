using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Internal
{
	internal interface IDataReference : IReferenceAnalyzationResult
	{
		AbstractData Data { get; }

		AbstractData ReferenceData { get; }
	}
}
