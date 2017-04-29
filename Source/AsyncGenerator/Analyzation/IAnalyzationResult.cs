using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Analyzation
{
	public interface IAnalyzationResult
	{
		/// <summary>
		/// Get the member node
		/// </summary>
		/// <returns></returns>
		SyntaxNode GetNode();
	}
}
