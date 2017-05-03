using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Extensions
{
	internal static class SymbolExtensions
	{
		private static bool AreEqual(this ITypeParameterSymbol parameterSymbol, ITypeSymbol typeSymbol)
		{
			var candParamType = typeSymbol as ITypeParameterSymbol;
			if (candParamType == null)
			{
				return false;
			}
			return parameterSymbol.HasConstructorConstraint == candParamType.HasConstructorConstraint &&
			       parameterSymbol.HasReferenceTypeConstraint == candParamType.HasReferenceTypeConstraint &&
			       parameterSymbol.HasValueTypeConstraint == candParamType.HasValueTypeConstraint &&
			       parameterSymbol.TypeParameterKind == candParamType.TypeParameterKind &&
			       parameterSymbol.Ordinal == candParamType.Ordinal &&
			       parameterSymbol.Variance == candParamType.Variance &&
			       parameterSymbol.ConstraintTypes.Length == candParamType.ConstraintTypes.Length &&
			       parameterSymbol.ConstraintTypes.All(o => candParamType.ConstraintTypes.Contains(o));
		}

		internal static bool IsTaskType(this ITypeSymbol typeSymbol)
		{
			return typeSymbol.Name == nameof(Task) &&
					typeSymbol.ContainingNamespace.ToString() == "System.Threading.Tasks";
		}

		/// <summary>
		/// Check if the return type matches, valid cases: <see cref="Void"/> to <see cref="System.Threading.Tasks.Task"/> Task, TResult to <see cref="System.Threading.Tasks.Task{TResult}"/> and
		/// also equals return types are ok when there is at least one delegate that can be converted to async (eg. Task.Run(<see cref="Action"/>) and Task.Run(<see cref="Func{Task}"/>))
		/// </summary>
		/// <param name="syncMethod"></param>
		/// <param name="candidateAsyncMethod"></param>
		/// <returns></returns>
		private static bool IsAsyncCandidateForReturnType(this IMethodSymbol syncMethod, IMethodSymbol candidateAsyncMethod)
		{
			// Original definition is used for matching generic types
			if (syncMethod.ReturnType.OriginalDefinition.Equals(candidateAsyncMethod.ReturnType.OriginalDefinition))
			{
				return true;
			}
			var candidateReturnType = candidateAsyncMethod.ReturnType as INamedTypeSymbol;
			if (candidateReturnType == null)
			{
				return false;
			}
			if (syncMethod.ReturnsVoid)
			{
				if (candidateReturnType.Name != nameof(Task) || candidateReturnType.TypeArguments.Any())
				{
					return false;
				}
			}
			else
			{
				if (candidateReturnType.Name != nameof(Task) || candidateReturnType.TypeArguments.Length != 1)
				{
					return false;
				}
				if (!candidateReturnType.TypeArguments.First().Equals(syncMethod.ReturnType))
				{
					// Check if the return type is a type parameter (generic argument) and if is equal as the candidate
					var paramType = syncMethod.ReturnType as ITypeParameterSymbol;
					return paramType != null && paramType.AreEqual(candidateReturnType.TypeArguments.First());
				}
			}
			return true;
		}

		/// <summary>
		/// Check if the candidate asynchronous method is an asynchronous counterpart of the synchronous method.
		/// </summary>
		/// <param name="syncMethod">The synchronous method</param>
		/// <param name="candidateAsyncMethod">The candidate asynchronous method</param>
		/// <param name="equalParameters">When true, both methods must have the same parameters, except when <see cref="hasCancellationToken"/> is set to true</param>
		/// <param name="hasCancellationToken">When true, the asynchronous method can contain one more parameter which must be of type <see cref="System.Threading.CancellationToken"/>
		/// and has to be the last parameter</param>
		/// <param name="ignoreReturnType">When true, the return type is not checked</param>
		/// <returns></returns>
		public static bool IsAsyncCounterpart(this IMethodSymbol syncMethod, IMethodSymbol candidateAsyncMethod, bool equalParameters, bool hasCancellationToken, bool ignoreReturnType)
		{
			// Check if the length of the parameters matches
			if (syncMethod.Parameters.Length != candidateAsyncMethod.Parameters.Length)
			{
				// For symplicity, we suppose that the sync method does not have a cancellation token as a parameter
				if (!hasCancellationToken || syncMethod.Parameters.Length + 1 != candidateAsyncMethod.Parameters.Length)
				{
					return false;
				}
			}
			// Check if the generic arguments are the same
			if (syncMethod.TypeParameters.Length  != candidateAsyncMethod.TypeParameters.Length)
			{
				return false;
			}
			if (syncMethod.TypeParameters.Length > 0)
			{
				for (var i = 0; i < syncMethod.TypeParameters.Length; i++)
				{
					var param = syncMethod.TypeParameters[i];
					var candidateParam = candidateAsyncMethod.TypeParameters[i];
					if (param.Variance != candidateParam.Variance ||
					    param.TypeParameterKind != candidateParam.TypeParameterKind ||
					    param.ConstraintTypes.Length != candidateParam.ConstraintTypes.Length)
					{
						return false;
					}
					if (param.ConstraintTypes.Where((t, j) => !t.Equals(candidateParam.ConstraintTypes[j])).Any())
					{
						return false;
					}
				}
			}

			// Check if the return type matches
			if (!ignoreReturnType && !IsAsyncCandidateForReturnType(syncMethod, candidateAsyncMethod))
			{
				return false;
			}
			// Both methods can have the same return type only if we have at least one delegate argument that can be async
			var result = !syncMethod.ReturnType.OriginalDefinition.Equals(candidateAsyncMethod.ReturnType.OriginalDefinition);
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
				var typeSymbol = param.Type as INamedTypeSymbol;
				if (typeSymbol == null)
				{
					if (param.Type.Equals(candidateParam.Type))
					{
						continue;
					}
					return false;
				}
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
				if (!origDelegate.IsAsyncCounterpart(candidateDelegate, false, hasCancellationToken, ignoreReturnType))
				{
					return false;
				}
				result = true;
			}
			if (syncMethod.Parameters.Length >= candidateAsyncMethod.Parameters.Length)
			{
				return result;
			}
			return candidateAsyncMethod.Parameters.Last().Type.Name == nameof(CancellationToken) && result;
		}

		/// <summary>
		/// Searches for an async counterpart methods within containing and its bases types
		/// </summary>
		/// <param name="methodSymbol"></param>
		/// <param name="equalParameters"></param>
		/// <param name="searchInheritedTypes"></param>
		/// <param name="hasCancellationToken"></param>
		/// <param name="ignoreReturnType"></param>
		/// <returns></returns>
		public static IEnumerable<IMethodSymbol> GetAsyncCounterparts(this IMethodSymbol methodSymbol, bool equalParameters, bool searchInheritedTypes, bool hasCancellationToken, bool ignoreReturnType)
		{
			var asyncName = methodSymbol.Name + "Async";
			if (searchInheritedTypes)
			{
				return methodSymbol.ContainingType.EnumerateBaseTypesAndSelf()
					.SelectMany(o => o.GetMembers().Where(m => asyncName == m.Name || !equalParameters && m.Name == methodSymbol.Name && !methodSymbol.Equals(m)))
					.OfType<IMethodSymbol>()
					.Where(o => methodSymbol.IsAsyncCounterpart(o, equalParameters, hasCancellationToken, ignoreReturnType));
			} 
			return methodSymbol.ContainingType.GetMembers().Where(m => asyncName == m.Name || !equalParameters && m.Name == methodSymbol.Name && !methodSymbol.Equals(m))
							   .OfType<IMethodSymbol>()
							   .Where(o => methodSymbol.IsAsyncCounterpart(o, equalParameters, hasCancellationToken, ignoreReturnType));
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

		public static bool IsNullable(this ITypeSymbol symbol)
		{
			return symbol?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
		}

		public static TypeSyntax CreateTypeSyntax(this ITypeSymbol symbol, bool insideCref = false, bool onlyName = false)
		{
			var predefinedType = symbol.SpecialType.ToPredefinedType();
			if (predefinedType != null)
			{
				return symbol.IsNullable()
					? (TypeSyntax)NullableType(predefinedType)
					: predefinedType;
			}
			return SyntaxNodeExtensions.ConstructNameSyntax(symbol.ToString(), insideCref: insideCref, onlyName: onlyName);
		}
	}
}
