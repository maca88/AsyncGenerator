using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	internal class FunctionReferenceTransformationResult : TransformationResult<SimpleNameSyntax>, IFunctionReferenceTransformationResult
	{
		public FunctionReferenceTransformationResult(IFunctionReferenceAnalyzationResult analyzationResult) : base(analyzationResult.ReferenceNameNode)
		{
			AnalyzationResult = analyzationResult;
		}

		public IFunctionReferenceAnalyzationResult AnalyzationResult { get; }
	}
}
