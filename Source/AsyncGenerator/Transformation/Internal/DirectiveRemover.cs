using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxNodeExtensions = AsyncGenerator.Extensions.Internal.SyntaxNodeExtensions;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// Removes all directive in the given node
	/// </summary>
	internal class DirectiveRemover : CSharpSyntaxRewriter
	{
		public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
		{
			if (trivia.IsDirective)
			{
				return default(SyntaxTrivia);
			}
			return base.VisitTrivia(trivia);
		}
	}
}
