using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation
{
	public interface IDocumentTransformationResult
	{
		/// <summary>
		/// Contains the original document prior the transformation
		/// </summary>
		CompilationUnitSyntax Original { get; }

		/// <summary>
		/// Contains the modified original document that will be set only if the there were some changes needed to be done (eg. added partial keyword)
		/// </summary>
		CompilationUnitSyntax OriginalModified { get; }

		/// <summary>
		/// Contains the transformed document
		/// </summary>
		CompilationUnitSyntax Transformed { get; }
	}
}
