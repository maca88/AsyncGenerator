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
		public static bool HaveSameParameters(this IMethodSymbol m1, IMethodSymbol m2, Func<IParameterSymbol, IParameterSymbol, int, bool> paramCompareFunc = null)
		{
			if (m1.Parameters.Length != m2.Parameters.Length)
			{
				return false;
			}

			for (var i = 0; i < m1.Parameters.Length; i++)
			{
				if (paramCompareFunc != null)
				{
					if (!paramCompareFunc(m1.Parameters[i], m2.Parameters[i], i))
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
		/// <param name="indexOfArgument"></param>
		/// <param name="inherit"></param>
		/// <returns></returns>
		public static IMethodSymbol GetAsyncCounterpart(this IMethodSymbol methodSymbol, int? indexOfArgument = null, bool inherit = false)
		{
			Func<IParameterSymbol, IParameterSymbol, int, bool> paramCompareFunc = null;
			if (indexOfArgument.HasValue)
			{
				paramCompareFunc = (candidateParam, param, index) =>
				{
					if (indexOfArgument != index)
					{
						return param.Type.Equals(candidateParam.Type);
					}
					var typeSymbol = (INamedTypeSymbol)param.Type;
					var candidateTypeSymbol = (INamedTypeSymbol)candidateParam.Type;
					var origDelegate = typeSymbol.DelegateInvokeMethod;
					var candidateDelegate = candidateTypeSymbol.DelegateInvokeMethod;
					if (origDelegate == null ||
						candidateDelegate == null ||
						!origDelegate.HaveSameParameters(candidateDelegate))
					{
						return false;
					}
					var candidateReturnType = (INamedTypeSymbol)candidateDelegate.ReturnType;
					return candidateReturnType.Name == "Task" &&
					       (
						       (
							       origDelegate.ReturnsVoid && !candidateReturnType.TypeArguments.Any()
						       ) ||
						       (
							       candidateReturnType.TypeArguments.Length == 1 &&
							       candidateReturnType.TypeArguments.First().Equals(origDelegate.ReturnType)
						       )
					       );
				};
			}
			IMethodSymbol asyncSymbol;
			if (inherit)
			{
				asyncSymbol = methodSymbol.ContainingType.EnumerateBaseTypesAndSelf()
								   .SelectMany(o => o.GetMembers(methodSymbol.Name + "Async"))
								   .OfType<IMethodSymbol>()
								   .Where(o => o.TypeParameters.Length == methodSymbol.TypeParameters.Length)
								   .FirstOrDefault(o => o.HaveSameParameters(methodSymbol, paramCompareFunc));
				if (asyncSymbol == null && indexOfArgument.HasValue)
				{
					asyncSymbol = methodSymbol.ContainingType.EnumerateBaseTypesAndSelf()
								   .SelectMany(o => o.GetMembers(methodSymbol.Name))
								   .OfType<IMethodSymbol>()
								   .Where(o => o.TypeParameters.Length == methodSymbol.TypeParameters.Length)
								   .FirstOrDefault(o => o.HaveSameParameters(methodSymbol, paramCompareFunc));
				}
				return asyncSymbol;
			} 
			asyncSymbol = methodSymbol.ContainingType.GetMembers(methodSymbol.Name + "Async")
							   .OfType<IMethodSymbol>()
							   .Where(o => o.TypeParameters.Length == methodSymbol.TypeParameters.Length)
							   .FirstOrDefault(o => o.HaveSameParameters(methodSymbol, paramCompareFunc));
			if (asyncSymbol == null && indexOfArgument.HasValue)
			{
				asyncSymbol = methodSymbol.ContainingType.GetMembers(methodSymbol.Name)
							   .OfType<IMethodSymbol>()
							   .Where(o => o.TypeParameters.Length == methodSymbol.TypeParameters.Length)
							   .FirstOrDefault(o => o.HaveSameParameters(methodSymbol, paramCompareFunc));
			}
			return asyncSymbol;
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

		public static bool IsAccessor(this ISymbol symbol)
		{
			return symbol.IsPropertyAccessor() || symbol.IsEventAccessor();
		}

		public static bool IsPropertyAccessor(this ISymbol symbol)
		{
			return (symbol as IMethodSymbol)?.MethodKind.IsPropertyAccessor() == true;
		}

		public static bool IsEventAccessor(this ISymbol symbol)
		{
			var method = symbol as IMethodSymbol;
			return method != null &&
				(method.MethodKind == MethodKind.EventAdd ||
				 method.MethodKind == MethodKind.EventRaise ||
				 method.MethodKind == MethodKind.EventRemove);
		}
	}
}
