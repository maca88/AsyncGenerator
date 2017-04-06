using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Analyzation
{
	public partial class ProjectAnalyzer
	{
		/// <summary>
		/// Set all method data dependencies to be also async
		/// </summary>
		/// <param name="asyncMethodData">Method data that is marked to be async</param>
		/// <param name="toProcessMethodData">All method data that needs to be processed</param>
		private void PostAnalyzeAsyncMethodData(MethodData asyncMethodData, ISet<MethodData> toProcessMethodData)
		{
			if (!toProcessMethodData.Contains(asyncMethodData))
			{
				return;
			}
			var processingMetodData = new Queue<MethodData>();
			processingMetodData.Enqueue(asyncMethodData);
			while (processingMetodData.Any())
			{
				var currentMethodData = processingMetodData.Dequeue();
				toProcessMethodData.Remove(currentMethodData);
				foreach (var depFunctionData in currentMethodData.Dependencies)
				{
					var depMethodData = depFunctionData as MethodData;
					if (depMethodData != null)
					{
						if (!toProcessMethodData.Contains(depMethodData))
						{
							continue;
						}
						processingMetodData.Enqueue(depMethodData);
					}
					if (depFunctionData.Conversion == MethodConversion.Ignore)
					{
						Logger.Info($"Ignored method {depFunctionData.Symbol} has a method invocation that can be async");
						continue;
					}
					depFunctionData.Conversion = MethodConversion.ToAsync;
					
				}
			}
		}

		/// <summary>
		/// Calculates the final conversion for all currently not ignored method data
		/// </summary>
		/// <param name="documentData">All project documents</param>
		private void PostAnalyze(IEnumerable<DocumentData> documentData)
		{
			// If a type data is ignored then also its method data are ignored
			var allTypeData = documentData
				.SelectMany(o => o.GetAllTypeDatas())
				.Where(o => o.Conversion != TypeConversion.Ignore)
				.ToList();
			var toProcessMethodData = new HashSet<MethodData>(allTypeData
				.SelectMany(o => o.MethodData.Values.Where(m => m.Conversion != MethodConversion.Ignore)));

			// 1. Step - Go through all async methods and set their dependencies to be also async
			var asyncMethodDatas = toProcessMethodData.Where(o => o.Conversion == MethodConversion.ToAsync).ToList();
			foreach (var asyncMethodData in asyncMethodDatas)
			{
				if (toProcessMethodData.Count == 0)
				{
					break;
				}
				PostAnalyzeAsyncMethodData(asyncMethodData, toProcessMethodData);
			}

			// 2. Step - Go through remaing methods and set them to be async if there is at least one method invocation that has an async counterpart
			var remainingMethodData = toProcessMethodData.ToList();
			foreach (var methodData in remainingMethodData)
			{
				if (methodData.MethodReferenceData
					.Any(o => o.CanBeAsync && (
						o.ReferenceAsyncSymbols?.Count > 0 ||
						o.ReferenceFunctionData?.Conversion == MethodConversion.ToAsync)))
				{
					if (methodData.Conversion == MethodConversion.Ignore)
					{
						Logger.Info($"Ignored method {methodData.Symbol} has a method invocation that can be async");
						continue;
					}
					methodData.Conversion = MethodConversion.ToAsync;
					// Set all dependencies to be async for the newly discovered async method
					PostAnalyzeAsyncMethodData(methodData, toProcessMethodData);
					if (toProcessMethodData.Count == 0)
					{
						break;
					}
				}
			}

			// 3. Step - Mark all remaining method to be ignored
			foreach (var methodData in toProcessMethodData)
			{
				methodData.Conversion = MethodConversion.Ignore;
			}

			// 4. Step - Calculate the final type conversion
			foreach (var typeData in allTypeData)
			{
				if (typeData.Conversion != TypeConversion.Unknown)
				{
					continue;
				}
				// A type can be ignored only if it has no async methods and no dependencies that will get converted
				//TODO: take care of circular dependencies (related types and nested/parent)
				/*
				if(typeData.MethodData.Values.All(o => o.Conversion == MethodConversion.Ignore) &&
					typeData.
					)*/
			}

		}
	}
}
