using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Extensions
{
	internal static class SymbolExtensions
	{
		public static bool HaveSameParameters(this IMethodSymbol m1, IMethodSymbol m2, Func<IParameterSymbol, IParameterSymbol, bool> paramCompareFunc = null)
		{
			if (m1.Parameters.Length != m2.Parameters.Length)
			{
				return false;
			}

			for (var i = 0; i < m1.Parameters.Length; i++)
			{
				if (paramCompareFunc != null)
				{
					if (!paramCompareFunc(m1.Parameters[i], m2.Parameters[i]))
					{
						return false;
					}
				}
				else
				{
					if (!m1.Parameters[i].Type.Equals(m2.Parameters[i].Type))
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Searches for an async counterpart method within containing type and its bases without checking the return type
		/// </summary>
		/// <param name="methodSymbol"></param>
		/// <param name="inherit"></param>
		/// <returns></returns>
		public static IMethodSymbol GetAsyncCounterpart(this IMethodSymbol methodSymbol, bool inherit = false)
		{
			if (inherit)
			{
				return methodSymbol.ContainingType.EnumerateBaseTypesAndSelf()
								   .SelectMany(o => o.GetMembers(methodSymbol.Name + "Async"))
								   .OfType<IMethodSymbol>()
								   .Where(o => o.TypeParameters.Length == methodSymbol.TypeParameters.Length)
								   .FirstOrDefault(o => o.HaveSameParameters(methodSymbol));
			}
			return methodSymbol.ContainingType.GetMembers(methodSymbol.Name + "Async")
							   .OfType<IMethodSymbol>()
							   .Where(o => o.TypeParameters.Length == methodSymbol.TypeParameters.Length)
							   .FirstOrDefault(o => o.HaveSameParameters(methodSymbol));
		}

		public static IEnumerable<INamedTypeSymbol> EnumerateBaseTypesAndSelf(this INamedTypeSymbol type)
		{
			yield return type;
			var currType = type.BaseType;
			while (currType != null)
			{
				yield return currType;
				currType = currType.BaseType;
			}
		}
	}
}
