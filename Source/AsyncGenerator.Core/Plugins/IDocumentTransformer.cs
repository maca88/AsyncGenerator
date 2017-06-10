using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Plugins
{
	public interface IDocumentTransformer: IPlugin
	{
		CompilationUnitSyntax Transform(IDocumentTransformationResult transformationResult);
	}
}
