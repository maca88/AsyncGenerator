using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Extensions.Internal
{
	internal static class SymbolExtensions
	{
		/// <summary>
		/// Check if the type parameters are equals
		/// </summary>
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
			       //parameterSymbol.TypeParameterKind == candParamType.TypeParameterKind && we do not care if the type parameter is form a method or a type
			       //parameterSymbol.Ordinal == candParamType.Ordinal && // we do not care about the position index of the type parameter
			       parameterSymbol.Variance == candParamType.Variance &&
			       AreEqual(parameterSymbol.ConstraintTypes, candParamType.ConstraintTypes);
		}

		private static bool AreEqual(ImmutableArray<ITypeSymbol> types, ImmutableArray<ITypeSymbol> toCompareTypes)
		{
			if (types.Length != toCompareTypes.Length)
			{
				return false;
			}

			for (var i = 0; i < types.Length; i++)
			{
				if (!types[i].AreEqual(toCompareTypes[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Check if the return type matches, valid cases: <see cref="Void"/> to <see cref="System.Threading.Tasks.Task"/> Task, TResult to <see cref="System.Threading.Tasks.Task{TResult}"/> and
		/// also equals return types are ok when there is at least one delegate that can be converted to async (eg. Task.Run(<see cref="Action"/>) and Task.Run(<see cref="Func{Task}"/>))
		/// </summary>
		/// <param name="syncMethod"></param>
		/// <param name="candidateAsyncMethod"></param>
		/// <returns></returns>
		internal static bool IsAsyncCandidateForReturnType(this IMethodSymbol syncMethod, IMethodSymbol candidateAsyncMethod)
		{
			// Original definition is used for matching generic types
			if (syncMethod.ReturnType.OriginalDefinition.Equals(candidateAsyncMethod.ReturnType.OriginalDefinition))
			{
				return true;
			}
			var candidateReturnType = candidateAsyncMethod.ReturnType as INamedTypeSymbol;
			if (candidateReturnType == null)
			{
				var candParamType = candidateAsyncMethod.ReturnType as ITypeParameterSymbol;
				if (candParamType != null)
				{
					return candParamType.AreEqual(syncMethod.ReturnType);
				}
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
				if (!candidateReturnType.TypeArguments.First().AreEqual(syncMethod.ReturnType))
				{
					// Check if the return type is a type parameter (generic argument) and if is equal as the candidate
					var paramType = syncMethod.ReturnType as ITypeParameterSymbol;
					return paramType != null && paramType.AreEqual(candidateReturnType.TypeArguments.First());
				}
			}
			return true;
		}

		/// <summary>
		/// Checks if the given types are equal
		/// </summary>
		/// <param name="type"></param>
		/// <param name="toCompare"></param>
		/// <param name="canBeDerivedFromType"></param>
		/// <returns></returns>
		internal static bool AreEqual(this ITypeSymbol type, ITypeSymbol toCompare, ITypeSymbol canBeDerivedFromType = null)
		{
			if (type.Equals(toCompare))
			{
				return true;
			}
			var typeNamedType = type as INamedTypeSymbol;
			var toCompareNamedType = toCompare as INamedTypeSymbol;
			if (typeNamedType == null)
			{
				if (type is ITypeParameterSymbol retrivedParamType)
				{
					return retrivedParamType.AreEqual(toCompare);
				}
				return false;
			}

			if (typeNamedType.TypeArguments.Length != toCompareNamedType?.TypeArguments.Length)
			{
				return false;
			}

			for (var i = 0; i < typeNamedType.TypeArguments.Length; i++)
			{
				var typeArgument = typeNamedType.TypeArguments[i];
				if (!typeArgument.AreEqual(toCompareNamedType.TypeArguments[i]))
				{
					return false;
				}
			}
			var equals = typeNamedType.OriginalDefinition.Equals(toCompareNamedType.OriginalDefinition);
			if (!equals && canBeDerivedFromType != null)
			{
				equals = new []{ canBeDerivedFromType }.Concat(canBeDerivedFromType.AllInterfaces).Any(o => toCompareNamedType.OriginalDefinition.Equals(o.OriginalDefinition));
			}
			return equals;
		}

		

		// Determine if "type" inherits from "baseType", ignoring constructed types, optionally including interfaces,
		// dealing only with original types.
		internal static bool InheritsFromOrEquals(this ITypeSymbol type, ITypeSymbol baseType, bool includeInterfaces)
		{
			if (!includeInterfaces)
			{
				return InheritsFromOrEquals(type, baseType);
			}

			return type.GetBaseTypesAndThis().Concat(type.AllInterfaces).Any(t => t.Equals(baseType));
		}

		// Determine if "type" inherits from "baseType", ignoring constructed types and interfaces, dealing
		// only with original types.
		internal static bool InheritsFromOrEquals(this ITypeSymbol type, ITypeSymbol baseType)
		{
			return type.GetBaseTypesAndThis().Any(t => t.Equals(baseType));
		}

		// Determine if "type" inherits from "baseType", ignoring constructed types, and dealing
		// only with original types.
		internal static bool InheritsFromOrEqualsIgnoringConstruction(this ITypeSymbol type, ITypeSymbol baseType)
		{
			var originalBaseType = baseType.OriginalDefinition;
			return type.GetBaseTypesAndThis().Any(t => t.OriginalDefinition.Equals(originalBaseType));
		}

		internal static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
		{
			var current = type;
			while (current != null)
			{
				yield return current;
				current = current.BaseType;
			}
		}
	}
}
