using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IPropertyAnalyzationResult : IMemberAnalyzationResult
	{
		PropertyDeclarationSyntax Node { get; }

		/// <summary>
		/// Symbol of the property
		/// </summary>
		IPropertySymbol Symbol { get; }

		/// <summary>
		/// Returns getter and setter accessors of the property.
		/// </summary>
		IEnumerable<IAccessorAnalyzationResult> GetAccessors();

		PropertyConversion Conversion { get; }
	}
}
