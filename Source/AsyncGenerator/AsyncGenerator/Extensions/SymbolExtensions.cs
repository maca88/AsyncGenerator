using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Extensions
{
	internal static class SymbolExtensions
	{
		/// <summary>
		/// Check if the return type matches, valid cases: <see cref="Void"/> to <see cref="System.Threading.Tasks.Task"/> Task, TResult to <see cref="System.Threading.Tasks.Task{TResult}"/> and
		/// also equals return types are ok when there is at least one delegate that can be converted to async (eg. Task.Run(<see cref="Action"/>) and Task.Run(<see cref="Func{Task}"/>))
		/// </summary>
		/// <param name="syncMethod"></param>
		/// <param name="candidateAsyncMethod"></param>
		/// <returns></returns>
		private static bool IsAsyncCandidateForReturnType(this IMethodSymbol syncMethod, IMethodSymbol candidateAsyncMethod)
		{
			if (syncMethod.ReturnType.Equals(candidateAsyncMethod.ReturnType))
			{
				return true;
			}
			var candidateReturnType = (INamedTypeSymbol)candidateAsyncMethod.ReturnType;
			if (syncMethod.ReturnsVoid)
			{
				if (candidateReturnType.Name != "Task" || candidateReturnType.TypeArguments.Any())
				{
					return false;
				}
			}
			else
			{
				if (candidateReturnType.Name != "Task" || candidateReturnType.TypeArguments.Length != 1 || !candidateReturnType.TypeArguments.First().Equals(syncMethod.ReturnType))
				{
					return false;
				}
			}
			return true;
		}

		public static bool IsAsyncCounterpart(this IMethodSymbol syncMethod, IMethodSymbol candidateAsyncMethod, bool equalParameters)
		{
			if (syncMethod.Parameters.Length != candidateAsyncMethod.Parameters.Length || !IsAsyncCandidateForReturnType(syncMethod, candidateAsyncMethod))
			{
				return false;
			}
			// Both methods can have the same return type only if we have at least one delegate argument that can be async
			var result = !syncMethod.ReturnType.Equals(candidateAsyncMethod.ReturnType);
			if (!result && equalParameters)
			{
				return false;
			}

			for (var i = 0; i < syncMethod.Parameters.Length; i++)
			{
				var param = syncMethod.Parameters[i];
				var candidateParam = candidateAsyncMethod.Parameters[i];
				if (param.IsOptional != candidateParam.IsOptional || 
					param.IsParams != candidateParam.IsParams || 
					param.RefKind != candidateParam.RefKind)
				{
					return false;
				}
				if (equalParameters)
				{
					if (param.Type.Equals(candidateParam.Type))
					{
						continue;
					}
					return false;
				}
				var typeSymbol = (INamedTypeSymbol)param.Type;
				var origDelegate = typeSymbol.DelegateInvokeMethod;
				if (origDelegate == null)
				{
					if (param.Type.Equals(candidateParam.Type))
					{
						continue;
					}
					return false;
				}
				// Candidate delegate argument must be equal or has to be an async candidate
				var candidateTypeSymbol = (INamedTypeSymbol)candidateParam.Type;
				var candidateDelegate = candidateTypeSymbol.DelegateInvokeMethod;
				if (candidateDelegate == null)
				{
					return false;
				}
				if (origDelegate.Equals(candidateDelegate))
				{
					continue;
				}
				if (!origDelegate.IsAsyncCounterpart(candidateDelegate, false))
				{
					return false;
				}
				result = true;
			}
			return result;
		}

		/// <summary>
		/// Searches for an async counterpart methods within containing and its bases types
		/// </summary>
		/// <param name="methodSymbol"></param>
		/// <param name="equalParameters"></param>
		/// <param name="searchInheritedTypes"></param>
		/// <returns></returns>
		public static IEnumerable<IMethodSymbol> GetAsyncCounterparts(this IMethodSymbol methodSymbol, bool equalParameters, bool searchInheritedTypes)
		{
			var asyncName = methodSymbol.Name + "Async";
			if (searchInheritedTypes)
			{
				return methodSymbol.ContainingType.EnumerateBaseTypesAndSelf()
					.SelectMany(o => o.GetMembers().Where(m => asyncName == m.Name || !equalParameters && m.Name == methodSymbol.Name && !methodSymbol.Equals(m)))
					.OfType<IMethodSymbol>()
					.Where(o => methodSymbol.IsAsyncCounterpart(o, equalParameters));
			} 
			return methodSymbol.ContainingType.GetMembers().Where(m => asyncName == m.Name || !equalParameters && m.Name == methodSymbol.Name && !methodSymbol.Equals(m))
							   .OfType<IMethodSymbol>()
							   .Where(o => methodSymbol.IsAsyncCounterpart(o, equalParameters));
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
