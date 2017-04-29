using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;

namespace AsyncGenerator.Transformation
{
	public interface IMemberTransformationResult : ITransformationResult
	{
		IMemberAnalyzationResult GetAnalyzationResult();
	}
}
