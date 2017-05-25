using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Extensions.Internal
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
			       parameterSymbol.ConstraintTypes.Length == candParamType.ConstraintTypes.Length &&
			       parameterSymbol.ConstraintTypes.All(o => candParamType.ConstraintTypes.Contains(o));
		}

		/// <summary>
		/// Check if the given type satisfies the constraints of the type parameter
		/// </summary>
		/// <param name="parameterSymbol"></param>
		/// <param name="typeSymbol"></param>
		/// <returns></returns>
		internal static bool CanApply(this ITypeParameterSymbol parameterSymbol, ITypeSymbol typeSymbol)
		{
			// Check if the given type symbol is also a type parameter
			if (parameterSymbol.AreEqual(typeSymbol))
			{
				return true;
			}
			// We have to check if all constraints can be applied to the given type
			if (parameterSymbol.HasConstructorConstraint)
			{
				// TODO: check if is a public ctor
				if (typeSymbol is INamedTypeSymbol namedTypeSymbol && !namedTypeSymbol.Constructors.Any(o => !o.Parameters.Any()))
				{
					return false;
				}
				return false;
			}
			if (parameterSymbol.HasReferenceTypeConstraint && !typeSymbol.IsReferenceType)
			{
				return false;
			}
			if (parameterSymbol.HasReferenceTypeConstraint && !typeSymbol.IsReferenceType)
			{
				return false;
			}
			return parameterSymbol.ConstraintTypes.All(typeSymbol.InheritsFromOrEquals);
		}

		/// <summary>
		/// Check if the retrived type can be returned without having an await. We need to consider that the given types will be wrapped in a <see cref="Task{T}"/>.
		/// Also when dealing with type parameters we need to check if the they can be applied to the given type
		/// </summary>
		/// <param name="retrivedType"></param>
		/// <param name="toReturnType"></param>
		/// <returns></returns>
		internal static bool IsAwaitRequired(this ITypeSymbol retrivedType, ITypeSymbol toReturnType)
		{
			if (retrivedType.Equals(toReturnType))
			{
				return true;
			}
			var retrivedNamedType = retrivedType as INamedTypeSymbol;
			var toReturnNamedType = toReturnType as INamedTypeSymbol;
			if (retrivedNamedType == null)
			{
				if (retrivedType is ITypeParameterSymbol retrivedParamType)
				{
					return retrivedParamType.CanApply(toReturnType);
				}
				return false;
			}
			if (toReturnNamedType == null)
			{
				if (toReturnType is ITypeParameterSymbol toReturnParamType)
				{
					return toReturnParamType.CanApply(retrivedType);
				}
				return false;
			}

			if (!retrivedType.OriginalDefinition.Equals(toReturnType.OriginalDefinition))
			{
				return false;
			}
			// If the original definitions are equal then we need to check the type arguments if they match
			for (var i = 0; i < retrivedNamedType.TypeArguments.Length; i++)
			{
				if (!retrivedNamedType.TypeArguments[i].IsAwaitRequired(toReturnNamedType.TypeArguments[i]))
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
				equals = new []{ canBeDerivedFromType }.Union(canBeDerivedFromType.AllInterfaces).Any(o => toCompareNamedType.OriginalDefinition.Equals(o.OriginalDefinition));
			}
			return equals;
		}

		/// <summary>
		/// Searches for an async counterpart methods within containing and its bases types
		/// </summary>
		/// <param name="methodSymbol"></param>
		/// <param name="invokedFromType"></param>
		/// <param name="equalParameters"></param>
		/// <param name="searchInheritedTypes"></param>
		/// <param name="hasCancellationToken"></param>
		/// <param name="ignoreReturnType"></param>
		/// <returns></returns>
		public static IEnumerable<IMethodSymbol> GetAsyncCounterparts(this IMethodSymbol methodSymbol, ITypeSymbol invokedFromType, 
			bool equalParameters, bool searchInheritedTypes, bool hasCancellationToken, bool ignoreReturnType)
		{
			var asyncName = methodSymbol.Name + "Async";
			if (searchInheritedTypes)
			{
				return methodSymbol.ContainingType.GetBaseTypesAndThis()
					.SelectMany(o => o.GetMembers().Where(m => asyncName == m.Name || !equalParameters && m.Name == methodSymbol.Name && !methodSymbol.Equals(m)))
					.OfType<IMethodSymbol>()
					.Where(o => methodSymbol.IsAsyncCounterpart(invokedFromType, o, equalParameters, hasCancellationToken, ignoreReturnType));
			} 
			return methodSymbol.ContainingType.GetMembers().Where(m => asyncName == m.Name || !equalParameters && m.Name == methodSymbol.Name && !methodSymbol.Equals(m))
							   .OfType<IMethodSymbol>()
							   .Where(o => methodSymbol.IsAsyncCounterpart(invokedFromType, o, equalParameters, hasCancellationToken, ignoreReturnType));
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

		internal static bool IsTaskType(this ITypeSymbol typeSymbol)
		{
			return typeSymbol.Name == nameof(Task) &&
			       typeSymbol.ContainingNamespace.ToString() == "System.Threading.Tasks";
		}

		internal static bool IsAccessor(this ISymbol symbol)
		{
			return symbol.IsPropertyAccessor() || symbol.IsEventAccessor();
		}

		internal static bool IsPropertyAccessor(this ISymbol symbol)
		{
			return (symbol as IMethodSymbol)?.MethodKind.IsPropertyAccessor() == true;
		}

		internal static bool IsEventAccessor(this ISymbol symbol)
		{
			var method = symbol as IMethodSymbol;
			return method != null &&
				(method.MethodKind == MethodKind.EventAdd ||
				 method.MethodKind == MethodKind.EventRaise ||
				 method.MethodKind == MethodKind.EventRemove);
		}

		internal static bool IsNullable(this ITypeSymbol symbol)
		{
			return symbol?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
		}

		internal static TypeSyntax CreateTypeSyntax(this ITypeSymbol symbol, bool insideCref = false, bool onlyName = false)
		{
			var predefinedType = symbol.SpecialType.ToPredefinedType();
			if (predefinedType != null)
			{
				return symbol.IsNullable()
					? (TypeSyntax)SyntaxFactory.NullableType(predefinedType)
					: predefinedType;
			}
			return SyntaxNodeExtensions.ConstructNameSyntax(symbol.ToString(), insideCref: insideCref, onlyName: onlyName);
		}
	}
}
