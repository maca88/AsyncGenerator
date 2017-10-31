using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public interface IFunctionReferenceTransformer : IPlugin
	{
		T TransformFunctionReference<T>(T node, IFunctionReferenceAnalyzationResult funReferenceResult, INamespaceTransformationMetadata namespaceMetadata)
			where T : SyntaxNode;
	}
}
