using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public interface IFunctionTransformer : IPlugin
	{
		SyntaxNode Transform(IFunctionTransformationResult functionTransformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata);

	}
}
