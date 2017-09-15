using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Plugins
{
	public interface ITypeTransformer : IPlugin
	{
		TypeDeclarationSyntax Transform(TypeDeclarationSyntax transformedNode, ITypeTransformationResult transformationResult,
			INamespaceTransformationMetadata namespaceMetadata, bool missingMembers);
	}
}
