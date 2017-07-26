using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;

namespace AsyncGenerator.Core.Transformation
{
	public interface IAccessorTransformationResult : IMethodOrAccessorTransformationResult
	{
		new IAccessorAnalyzationResult AnalyzationResult { get; }
	}
}
