﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Extensions.Internal
{
	internal static class SymbolExtensions
	{
		private static readonly Func<IMethodSymbol, IEnumerable> GetHiddenMembersFunc;

		static SymbolExtensions()
		{
			const string methodSymbolFullName = "Microsoft.CodeAnalysis.CSharp.Symbols.MethodSymbol, Microsoft.CodeAnalysis.CSharp";
			var type = Type.GetType(methodSymbolFullName);
			if (type == null)
			{
				throw new InvalidOperationException($"Type {methodSymbolFullName} does not exist");
			}
			var overriddenOrHiddenMembersGetter = type.GetProperty("OverriddenOrHiddenMembers", BindingFlags.NonPublic | BindingFlags.Instance)?.GetMethod;
			if (overriddenOrHiddenMembersGetter == null)
			{
				throw new InvalidOperationException($"Property OverriddenOrHiddenMembers of type {methodSymbolFullName} does not exist.");
			}

			var hiddenMembersGetter = overriddenOrHiddenMembersGetter.ReturnType.GetProperty("HiddenMembers")?.GetMethod;
			if (hiddenMembersGetter == null)
			{
				throw new InvalidOperationException($"Property HiddenMembers of type {overriddenOrHiddenMembersGetter.ReturnType} does not exist.");
			}

			var param1 = Expression.Parameter(typeof(IMethodSymbol), "methodSymbol");
			var convertToMethodSymbol = Expression.Convert(param1, type);
			var callOverriddenOrHiddenMembersGetter = Expression.Call(convertToMethodSymbol, overriddenOrHiddenMembersGetter);
			var callHiddenMembersGetter = Expression.Call(callOverriddenOrHiddenMembersGetter, hiddenMembersGetter);
			var lambdaParams = new List<ParameterExpression>
			{
				param1
			};
			GetHiddenMembersFunc = Expression.Lambda<Func<IMethodSymbol, IEnumerable>>(
					Expression.Convert(callHiddenMembersGetter, typeof(IEnumerable)), lambdaParams)
				.Compile();
		}

		internal static IEnumerable<IMethodSymbol> GetHiddenMethods(this IMethodSymbol methodSymbol)
		{
			return GetHiddenMembersFunc(methodSymbol).OfType<IMethodSymbol>();
		}

		/// <summary>
		/// Check if the definition (excluding method name) of both methods matches
		/// </summary>
		/// <param name="method"></param>
		/// <param name="toMatch"></param>
		/// <param name="skipCancellationToken"></param>
		/// <returns></returns>
		internal static bool MatchesDefinition(this IMethodSymbol method, IMethodSymbol toMatch, bool skipCancellationToken)
		{
			if (method.IsExtensionMethod)
			{
				// System.Linq extensions
				method = method.ReducedFrom ?? method;
			}

			if (method.Parameters.Length != toMatch.Parameters.Length)
			{
				if (!skipCancellationToken || method.Parameters.Length != toMatch.Parameters.Length - 1)
				{
					return false;
				}
			}

			if (!method.ReturnType.AreEqual(toMatch.ReturnType, method.ReturnType.OriginalDefinition)) // Here the generic arguments will be checked if any.
				// The return type of the toMatch parameter can be a base type of the return type
				// of the method parameter
			{
				return false;
			}
			// Do not check the method type parameters as toMatch method may have the type parameters from the type
			// (the generic arguments will be checked for the return type and parameters separately)
			for (var i = 0; i < method.Parameters.Length; i++)
			{
				var param = method.Parameters[i];
				var candidateParam = toMatch.Parameters[i];
				if (skipCancellationToken && candidateParam.Type.IsCancellationToken())
				{
					continue;
				}
				if (param.IsOptional != candidateParam.IsOptional ||
					param.IsParams != candidateParam.IsParams ||
					param.RefKind != candidateParam.RefKind)
				{
					return false;
				}
				if (!param.Type.AreEqual(candidateParam.Type)) // Here the generic arguments will be checked if any
				{
					return false;
				}
			}
			return true;
		}

		public static bool IsCancellationToken(this ITypeSymbol typeSymbol)
		{
			return typeSymbol.ToString() == "System.Threading.CancellationToken";
		}

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

		internal static List<int> GetAsyncDelegateArgumentIndexes(this IMethodSymbol syncMethod, IMethodSymbol asyncMethod)
		{
			// Parallel.For -> Task.WaitAll
			if (syncMethod.Parameters.Length > asyncMethod.Parameters.Length)
			{
				return null;
			}
			var result = new List<int>();
			for (var i = 0; i < syncMethod.Parameters.Length; i++)
			{
				var param = syncMethod.Parameters[i];
				var candidateParam = asyncMethod.Parameters[i];
				if (!(param.Type is INamedTypeSymbol typeSymbol))
				{
					continue;
				}
				var origDelegate = typeSymbol.DelegateInvokeMethod;
				if (origDelegate == null)
				{
					continue;
				}
				// Candidate delegate argument must be equal or has to be an async candidate
				if (!(candidateParam.Type is INamedTypeSymbol candidateTypeSymbol))
				{
					continue;
				}
				var candidateDelegate = candidateTypeSymbol.DelegateInvokeMethod;
				if (origDelegate.Equals(candidateDelegate))
				{
					continue;
				}
				result.Add(i);
			}

			return result;
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

			if (type.TypeKind == TypeKind.Array && toCompare.TypeKind == TypeKind.Array)
			{
				var arrayType = (IArrayTypeSymbol)type;
				var toCompareArrayType = (IArrayTypeSymbol)toCompare;
				return arrayType.Rank == toCompareArrayType.Rank &&
				       arrayType.IsSZArray == toCompareArrayType.IsSZArray &&
				       AreEqual(arrayType.ElementType, toCompareArrayType.ElementType);
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
