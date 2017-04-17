using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;

namespace AsyncGenerator.Extensions
{
	public static class TypeAnalyzationResultExtensions
	{
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
					yield break;
				}
				foreach (var td in GetSelfAndDescendantsTypesRecursively(subTypeData, predicate))
				{
					if (predicate?.Invoke(td) == false)
					{
						yield break;
					}
					yield return td;
				}
			}
		}
	}
}
