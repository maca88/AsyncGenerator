using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;

namespace AsyncGenerator.Extensions
{
	public static class AnalyzationResultExtensions
	{
		public static IEnumerable<INamespaceAnalyzationResult> GetSelfAndDescendantsNamespaces(this INamespaceAnalyzationResult namespaceResult,
			Func<INamespaceAnalyzationResult, bool> predicate = null)
		{
			return GetSelfAndDescendantsNamespacesRecursively(namespaceResult, predicate);
		}

		private static IEnumerable<INamespaceAnalyzationResult> GetSelfAndDescendantsNamespacesRecursively(INamespaceAnalyzationResult namespaceResult,
			Func<INamespaceAnalyzationResult, bool> predicate = null)
		{
			if (predicate?.Invoke(namespaceResult) == false)
			{
				yield break;
			}
			yield return namespaceResult;
			foreach (var subNamespace in namespaceResult.NestedNamespaces)
			{
				if (predicate?.Invoke(subNamespace) == false)
				{
					continue;
				}
				foreach (var td in GetSelfAndDescendantsNamespacesRecursively(subNamespace, predicate))
				{
					if (predicate?.Invoke(td) == false)
					{
						continue;
					}
					yield return td;
				}
			}
		}

		public static IEnumerable<ITypeAnalyzationResult> GetSelfAndDescendantsTypes(this ITypeAnalyzationResult typeResult, Func<ITypeAnalyzationResult, bool> predicate = null)
		{
			return GetSelfAndDescendantsTypesRecursively(typeResult, predicate);
		}

		private static IEnumerable<ITypeAnalyzationResult> GetSelfAndDescendantsTypesRecursively(ITypeAnalyzationResult typeData, Func<ITypeAnalyzationResult, bool> predicate = null)
		{
			if (predicate?.Invoke(typeData) == false)
			{
				yield break;
			}
			yield return typeData;
			foreach (var subTypeData in typeData.NestedTypes)
			{
				if (predicate?.Invoke(subTypeData) == false)
				{
					continue;
				}
				foreach (var td in GetSelfAndDescendantsTypesRecursively(subTypeData, predicate))
				{
					if (predicate?.Invoke(td) == false)
					{
						continue;
					}
					yield return td;
				}
			}
		}

		public static IEnumerable<IFunctionAnalyzationResult> GetSelfAndDescendantsFunctions(this IFunctionAnalyzationResult funcResult, Func<IFunctionAnalyzationResult, bool> predicate = null)
		{
			return GetSelfAndDescendantsFunctionsRecursively(funcResult, predicate);
		}

		private static IEnumerable<IFunctionAnalyzationResult> GetSelfAndDescendantsFunctionsRecursively(IFunctionAnalyzationResult fucData, Func<IFunctionAnalyzationResult, bool> predicate = null)
		{
			if (predicate?.Invoke(fucData) == false)
			{
				yield break;
			}
			yield return fucData;
			foreach (var subFuncData in fucData.ChildFunctions)
			{
				if (predicate?.Invoke(subFuncData) == false)
				{
					continue;
				}
				foreach (var td in GetSelfAndDescendantsFunctionsRecursively(subFuncData, predicate))
				{
					if (predicate?.Invoke(td) == false)
					{
						continue;
					}
					yield return td;
				}
			}
		}
	}
}
