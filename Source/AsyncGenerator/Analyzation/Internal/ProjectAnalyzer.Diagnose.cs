using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace AsyncGenerator.Analyzation.Internal
{
	partial class ProjectAnalyzer
	{
		public void Diagnose(IEnumerable<DocumentData> documents)
		{
			var config = _configuration.Diagnostics;
			foreach (var document in documents.Where(o => config.CanDiagnoseDocument(o.Document)).OrderBy(o => o.FilePath))
			{
				var logs = new List<KeyValuePair<DiagnosticSeverity, string>>();
				
				foreach (var diagnostic in document.GetDiagnostics()
					.OrderByDescending(o => o.DiagnosticSeverity)
					.ThenBy(o => o.Description))
				{
					logs.Add(new KeyValuePair<DiagnosticSeverity, string>(diagnostic.DiagnosticSeverity, diagnostic.Description));
				}

				foreach (var namespaceData in document.GetAllNamespaceDatas().OrderBy(o => o.Node?.SpanStart))
				{
					if (namespaceData.IsGlobal)
					{
						LogDocumentDiagnostics(namespaceData, logs);
					}
					else
					{
						LogDiagnostics("Namespace", namespaceData, logs);
					}

					foreach (var typeData in namespaceData.Types.Values.SelectMany(o => o.GetSelfAndDescendantsTypeData())
						.Where(o => config.CanDiagnoseType(o.Symbol))
						.OrderBy(o => o.Node.SpanStart))
					{
						var diagnosedFunctions = new List<FunctionData>(typeData.Methods.Count);
						foreach (var functionData in typeData.Methods.Values.Where(o => config.CanDiagnoseMethod(o.Symbol))
							.OrderBy(o => o.Node.SpanStart)
							.SelectMany(o => o.GetSelfAndDescendantsFunctions()))
						{
							DiagnoseFunction(functionData);
							diagnosedFunctions.Add(functionData);
						}
						foreach (var functionData in diagnosedFunctions.OrderBy(o => o.GetNode().SpanStart))
						{
							if (functionData is MethodData methodData)
							{
								LogDiagnostics("Method", methodData, logs);
							}
							else
							{
								LogDiagnostics("Function", functionData, logs);
							}

							foreach (var bodyReference in functionData.BodyFunctionReferences
								.OrderBy(o => o.ReferenceNameNode.SpanStart))
							{
								LogReferenceDiagnostics(bodyReference, logs);
							}
						}

						foreach (var methodData in typeData.SpecialMethods.Values
							.Where(o => config.CanDiagnoseMethod(o.Symbol))
							.OrderBy(o => o.GetNode().SpanStart))
						{
							LogDiagnostics("Method", methodData, logs);
						}

						foreach (var propertyData in typeData.Properties.Values.OrderBy(o => o.Node.SpanStart))
						{
							LogDiagnostics("Property", propertyData, logs);

							foreach (var accessorData in propertyData.GetAccessors()
								.Where(o => config.CanDiagnoseMethod(o.Symbol))
								.OrderBy(o => o.Node.SpanStart))
							{
								DiagnoseFunction(accessorData);
								LogDiagnostics("Property accessor", accessorData, logs);
							}
						}

						foreach (var fieldData in typeData.Fields.Values.OrderBy(o => o.Node.SpanStart))
						{
							LogDiagnostics("Field", fieldData, logs);
						}
					}
				}

				if (logs.Count == 0)
				{
					continue;
				}

				var highestSeverity = logs.Select(o => o.Key).OrderByDescending(o => o).First();
				logs.Insert(0, new KeyValuePair<DiagnosticSeverity, string>(highestSeverity,
					$"Diagnostics for document: {Path.Combine(document.Document.Folders.Concat(new[] {document.Document.Name}).ToArray())}"));
				foreach (var log in logs)
				{
					switch (log.Key)
					{
						case DiagnosticSeverity.Hidden:
							_diagnosticsLogger.LogDebug(log.Value);
							break;
						case DiagnosticSeverity.Info:
							_diagnosticsLogger.LogInformation(log.Value);
							break;
						case DiagnosticSeverity.Warning:
							_diagnosticsLogger.LogWarning(log.Value);
							break;
						case DiagnosticSeverity.Error:
							_diagnosticsLogger.LogError(log.Value);
							break;
					}
				}
			}
		}

		private void DiagnoseFunction(FunctionData functionData)
		{
			if (functionData.Conversion != Core.MethodConversion.Ignore ||
			    functionData.ExplicitlyIgnored ||
			    new[]
			    {
				    IgnoreReason.NeverUsed,
				    IgnoreReason.NoAsyncInvocations,
				    IgnoreReason.AllRelatedMethodsIgnored,
				    IgnoreReason.AlreadyAsync,
				    IgnoreReason.AsyncCounterpartExists
				}.Contains(functionData.IgnoredReason))
			{
				return;
			}
			var asyncInvocations = functionData.BodyFunctionReferences.Where(o =>
				o.ReferenceFunctionData?.Conversion.HasFlag(Core.MethodConversion.ToAsync) == true ||
				GetAsyncCounterparts(o.ReferenceSymbol, _searchOptions).Any()).ToList();
			if (asyncInvocations.Count <= 0)
			{
				return;
			}

			var methodData = functionData.GetMethodOrAccessorData();
			if (methodData.IgnoredReason != null)
			{
				methodData.SetIgnoreSeverity(DiagnosticSeverity.Warning);
			}
			functionData.SetIgnoreSeverity(DiagnosticSeverity.Warning);
			foreach (var asyncInvocation in asyncInvocations)
			{
				asyncInvocation.SetIgnoreSeverity(DiagnosticSeverity.Warning);
				asyncInvocation.AddDiagnostic("Has an async counterpart that could't been used", DiagnosticSeverity.Warning);
			}
		}

		private static void LogDocumentDiagnostics(AbstractData data, List<KeyValuePair<DiagnosticSeverity, string>> logs)
		{
			if (data.IgnoredReason != null)
			{
				logs.Add(new KeyValuePair<DiagnosticSeverity, string>(data.IgnoredReason.DiagnosticSeverity,
					$"Document ignored reason: {data.IgnoredReason.Reason}"));
			}

			foreach (var diagnostic in data.GetDiagnostics())
			{
				logs.Add(new KeyValuePair<DiagnosticSeverity, string>(diagnostic.DiagnosticSeverity,
					$"Document: {diagnostic.Description}"));
			}
		}

		private static void LogDiagnostics(string dataType, AbstractData data, List<KeyValuePair<DiagnosticSeverity, string>> logs)
		{
			if (data.IgnoredReason != null)
			{
				logs.Add(new KeyValuePair<DiagnosticSeverity, string>(data.IgnoredReason.DiagnosticSeverity,
					$"{dataType} {data.GetSymbol().Name} {data.GetLineSpan().Span.Format()} ignored reason: {data.IgnoredReason.Reason}"));
			}

			foreach (var diagnostic in data.GetDiagnostics())
			{
				logs.Add(new KeyValuePair<DiagnosticSeverity, string>(diagnostic.DiagnosticSeverity,
					$"{dataType} {data.GetSymbol().Name} {data.GetLineSpan().Span.Format()}: {diagnostic.Description}"));
			}
		}

		private static void LogReferenceDiagnostics(BodyFunctionDataReference data, List<KeyValuePair<DiagnosticSeverity, string>> logs)
		{
			if (data.IgnoredReason != null)
			{
				logs.Add(new KeyValuePair<DiagnosticSeverity, string>(data.IgnoredReason.DiagnosticSeverity,
					$"Method reference {data.ReferenceSymbol.Name} {data.ReferenceLocation.Location.GetLineSpan().Span.Format()} ignored reason: {data.IgnoredReason.Reason}"));
			}

			foreach (var diagnostic in data.GetDiagnostics())
			{
				logs.Add(new KeyValuePair<DiagnosticSeverity, string>(diagnostic.DiagnosticSeverity,
					$"Method reference {data.ReferenceSymbol.Name} {data.ReferenceLocation.Location.GetLineSpan().Span.Format()}: {diagnostic.Description}"));
			}
		}
	}
}
