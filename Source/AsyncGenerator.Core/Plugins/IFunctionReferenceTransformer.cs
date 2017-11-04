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
		SyntaxNode TransformFunctionReference(SyntaxNode node, IFunctionAnalyzationResult funcResult, 
			IFunctionReferenceAnalyzationResult funcReferenceResult, INamespaceTransformationMetadata namespaceMetadata);
	}
}
