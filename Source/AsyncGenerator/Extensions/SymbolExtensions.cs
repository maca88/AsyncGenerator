using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Extensions
{
	public static class SymbolExtensions
	{
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
	}
}
