using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using AsyncGenerator.Core.Extensions.Internal;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Extensions
{
	public static class SymbolExtensions
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

		/// <summary>
		/// Check if the definition (excluding method name) of both methods matches
		/// </summary>
		/// <param name="method"></param>
		/// <param name="toMatch"></param>
		/// <returns></returns>
		public static bool MatchesDefinition(this IMethodSymbol method, IMethodSymbol toMatch)
		{
			if (method.IsExtensionMethod)
			{
				// System.Linq extensions
				method = method.ReducedFrom ?? method;
			}
			if (method.Parameters.Length != toMatch.Parameters.Length ||
				!method.ReturnType.AreEqual(toMatch.ReturnType, method.ReturnType.OriginalDefinition)) // Here the generic arguments will be checked if any.
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

		/// <summary>
		/// Check if the candidate asynchronous method is an asynchronous counterpart of the synchronous method.
		/// </summary>
		/// <param name="syncMethod">The synchronous method</param>
		/// <param name="invokedFromType"></param>
		/// <param name="candidateAsyncMethod">The candidate asynchronous method</param>
		/// <param name="equalParameters">When true, both methods must have the same parameters, except when <see cref="hasCancellationToken"/> is set to true</param>
		/// <param name="hasCancellationToken">When true, the asynchronous method can contain one more parameter which must be of type <see cref="System.Threading.CancellationToken"/>
		/// and has to be the last parameter</param>
		/// <param name="ignoreReturnType">When true, the return type is not checked</param>
		/// <returns></returns>
		public static bool IsAsyncCounterpart(this IMethodSymbol syncMethod, ITypeSymbol invokedFromType, IMethodSymbol candidateAsyncMethod,
			bool equalParameters, bool hasCancellationToken, bool ignoreReturnType)
		{
			if (syncMethod.IsExtensionMethod)
			{
				// System.Linq extensions
				syncMethod = syncMethod.ReducedFrom ?? syncMethod;
			}

			if (syncMethod.OverriddenMethod != null && candidateAsyncMethod.Equals(syncMethod.OverriddenMethod))
			{
				return false;
			}

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
			if (syncMethod.TypeParameters.Length != candidateAsyncMethod.TypeParameters.Length)
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
			if (!ignoreReturnType && !syncMethod.IsAsyncCandidateForReturnType(candidateAsyncMethod))
			{
				return false;
			}
			// Both methods can have the same return type only if we have at least one delegate argument that can be async or the ignoreReturnType
			// is set to true
			var result = ignoreReturnType || !syncMethod.ReturnType.OriginalDefinition.Equals(candidateAsyncMethod.ReturnType.OriginalDefinition);

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
					if (param.Type.AreEqual(candidateParam.Type))
					{
						continue;
					}
					if (i != 0 || !syncMethod.IsExtensionMethod || !candidateAsyncMethod.IsExtensionMethod || invokedFromType == null)
					{
						return false;
					}
					// Check if the candidate extension method can be called from the type that was invoked
					if (param.Type.AreEqual(candidateParam.Type, invokedFromType))
					{
						continue;
					}
					return false;
				}
				var typeSymbol = param.Type as INamedTypeSymbol;
				if (typeSymbol == null)
				{
					if (param.Type.AreEqual(candidateParam.Type))
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
				var candidateTypeSymbol = candidateParam.Type as INamedTypeSymbol;
				var candidateDelegate = candidateTypeSymbol?.DelegateInvokeMethod;
				if (candidateDelegate == null)
				{
					return false;
				}
				if (origDelegate.Equals(candidateDelegate))
				{
					continue;
				}
				if (!origDelegate.IsAsyncCounterpart(null, candidateDelegate, false, hasCancellationToken, ignoreReturnType))
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

		internal static IEnumerable<IMethodSymbol> GetHiddenMethods(this IMethodSymbol methodSymbol)
		{
			return GetHiddenMembersFunc(methodSymbol).OfType<IMethodSymbol>();
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
			var asyncName = methodSymbol.GetAsyncName();
			if (searchInheritedTypes)
			{
				// If the containing type is not an interface we don't have to search in interfaces as all interface members
				// should be defined in one of the types.
				var types = methodSymbol.ContainingType.TypeKind == TypeKind.Interface
					? new ITypeSymbol[] {methodSymbol.ContainingType}.Concat(methodSymbol.ContainingType.AllInterfaces)
					: methodSymbol.ContainingType.GetBaseTypesAndThis();
				var asyncCounterparts = types
					.SelectMany(o => o.GetMembers().Where(m => asyncName == m.Name || !equalParameters && m.Name == methodSymbol.Name && !methodSymbol.Equals(m)))
					.OfType<IMethodSymbol>()
					.Where(o => methodSymbol.IsAsyncCounterpart(invokedFromType, o, equalParameters, hasCancellationToken, ignoreReturnType));
				// We have to return only unique async counterparts, skip overriden and hidden methods
				return FilterOutHiddenAndOverridenMethods(asyncCounterparts);
			}

			return methodSymbol.ContainingType.GetMembers().Where(m => asyncName == m.Name || !equalParameters && m.Name == methodSymbol.Name && !methodSymbol.Equals(m))
				.OfType<IMethodSymbol>()
				.Where(o => methodSymbol.IsAsyncCounterpart(invokedFromType, o, equalParameters, hasCancellationToken, ignoreReturnType));
		}

		private static IEnumerable<IMethodSymbol> FilterOutHiddenAndOverridenMethods(IEnumerable<IMethodSymbol> methodSymbols)
		{
			var overridenOrHiddenMethods = new HashSet<IMethodSymbol>();
			foreach (var methodSymbol in methodSymbols)
			{
				if (overridenOrHiddenMethods.Contains(methodSymbol))
				{
					continue;
				}
				foreach (var hiddenMethod in methodSymbol.GetHiddenMethods())
				{
					overridenOrHiddenMethods.Add(hiddenMethod);
				}
				if (methodSymbol.OverriddenMethod != null)
				{
					overridenOrHiddenMethods.Add(methodSymbol.OverriddenMethod);
				}

				yield return methodSymbol;
			}
		}

		/// <summary>
		/// Generate an async name for the given method smybol
		/// </summary>
		public static string GetAsyncName(this IMethodSymbol methodSymbol, string postfix = "Async")
		{
			var asyncName = $"{methodSymbol.Name.Split('.').Last()}{postfix}"; // Split is needed for explicit methods
			if (methodSymbol.MethodKind == MethodKind.PropertyGet || methodSymbol.MethodKind == MethodKind.PropertySet)
			{
				// We have to rename async name from eg. get_SomethingAsync to GetSomethingAsync by capitalize the first char and remove the underscore
				asyncName = char.ToUpperInvariant(asyncName[0]) + asyncName.Substring(1).Replace("_", "");
			}
			return asyncName;
		}

		/// <summary>
		/// Check if the given type satisfies the constraints of the type parameter
		/// </summary>
		/// <param name="parameterSymbol"></param>
		/// <param name="typeSymbol"></param>
		/// <returns></returns>
		public static bool CanApply(this ITypeParameterSymbol parameterSymbol, ITypeSymbol typeSymbol)
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
	}
}
