using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Transformation;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Plugins
{
	public interface IDocumentTransformer: IPlugin
	{
		CompilationUnitSyntax Transform(IDocumentTransformationResult transformationResult);
	}
}
