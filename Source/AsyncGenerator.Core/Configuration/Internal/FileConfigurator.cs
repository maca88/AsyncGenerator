using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration.Internal
{
	internal class FileConfigurator
	{
		public void Configure(AsyncGenerator configuration, IFluentSolutionConfiguration solutionConfiguration)
		{
			if (configuration.Solution.ApplyChanges.HasValue)
			{
				solutionConfiguration.ApplyChanges(configuration.Solution.ApplyChanges.Value);
			}
			if (configuration.Solution.ConcurrentRun.HasValue)
			{
				solutionConfiguration.ConcurrentRun(configuration.Solution.ConcurrentRun.Value);
			}

			// Configure projects
			foreach (var projectConfig in configuration.Solution.Projects)
			{
				solutionConfiguration.ConfigureProject(projectConfig.Name, o => Configure(configuration, projectConfig, o));
			}
		}

		private static void Configure(AsyncGenerator globalConfig, Project config, IFluentProjectConfiguration fluentConfig)
		{
			fluentConfig.ConfigureAnalyzation(o => Configure(globalConfig,config.Analyzation, o));
			fluentConfig.ConfigureTransformation(o => Configure(config.Transformation, o));
		}

		private static void Configure(AsyncGenerator globalConfig, Analyzation config, IFluentProjectAnalyzeConfiguration fluentConfig)
		{
			if (config.CallForwarding.HasValue)
			{
				fluentConfig.CallForwarding(config.CallForwarding.Value);
			}
			if (config.ScanMethodBody.HasValue)
			{
				fluentConfig.ScanMethodBody(config.ScanMethodBody.Value);
			}
			fluentConfig.CancellationTokens(o => Configure(config.CancellationTokens, o));

			if (config.DocumentSelection.Any())
			{
				fluentConfig.DocumentSelection(CreateDocumentSelectionPredicate(config.DocumentSelection));
			}
			if (config.MethodConversion.Any())
			{
				fluentConfig.MethodConversion(CreateMethodConversionFunction(globalConfig, config.MethodConversion));
			}
			if (config.PreserveReturnType.Any())
			{
				fluentConfig.PreserveReturnType(CreateMethodPredicate(globalConfig, config.PreserveReturnType, false));
			}
			if (config.SearchForAsyncCounterparts.Any())
			{
				fluentConfig.SearchForAsyncCounterparts(CreateMethodPredicate(globalConfig, config.SearchForAsyncCounterparts, true));
			}
			if (config.TypeConversion.Any())
			{
				fluentConfig.TypeConversion(CreateTypeConversionFunction(globalConfig, config.TypeConversion));
			}
		}

		private static void Configure(CancellationTokens config, IFluentProjectCancellationTokenConfiguration fluentConfig)
		{
		}

		private static void Configure(Transformation config, IFluentProjectTransformConfiguration fluentConfig)
		{

		}

		private static Func<INamedTypeSymbol, Core.TypeConversion> CreateTypeConversionFunction(AsyncGenerator globalConfig, IList<TypeConversionFilter> filters)
		{
			var rules = globalConfig.TypeRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return Convert(filter.Conversion);
					}
				}
				return Core.TypeConversion.Unknown;
			};
		}

		private static Predicate<IMethodSymbol> CreateMethodPredicate(AsyncGenerator globalConfig, IList<MethodSearchFilter> filters, bool defaultValue)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return filter.Search;
					}
				}
				return defaultValue;
			};
		}

		private static Predicate<IMethodSymbol> CreateMethodPredicate(AsyncGenerator globalConfig, IList<MethodConversionFilter> filters, bool defaultValue)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return true;
					}
				}
				return defaultValue;
			};
		}

		private static Func<IMethodSymbol, Core.MethodConversion> CreateMethodConversionFunction(AsyncGenerator globalConfig, IList<MethodConversionFilter> filters)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return Convert(filter.Conversion);
					}
				}
				return Core.MethodConversion.Unknown; // Default value
			};
		}

		private static Core.MethodConversion Convert(MethodConversion conversion)
		{
			switch (conversion)
			{
				case MethodConversion.Ignore:
					return Core.MethodConversion.Ignore;
				case MethodConversion.ToAsync:
					return Core.MethodConversion.ToAsync;
				case MethodConversion.Smart:
					return Core.MethodConversion.Smart;
				default:
					return Core.MethodConversion.Unknown;
			}
		}

		private static Core.TypeConversion Convert(TypeConversion conversion)
		{
			switch (conversion)
			{
				case TypeConversion.Ignore:
					return Core.TypeConversion.Ignore;
				case TypeConversion.NewType:
					return Core.TypeConversion.NewType;
				case TypeConversion.Partial:
					return Core.TypeConversion.Partial;
				default:
					return Core.TypeConversion.Unknown;
			}
		}

		private static bool CanApply(IMethodSymbol symbol, MethodFilter filter, Dictionary<string, List<MethodFilter>> rules)
		{
			if (!CanApply(symbol, filter))
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.Rule) && !rules[filter.Rule].Any(o => CanApply(symbol, o, rules)))
			{
				return false;
			}
			return true;
		}

		private static bool CanApply(INamedTypeSymbol symbol, TypeFilter filter, IReadOnlyDictionary<string, List<TypeFilter>> rules)
		{
			if (!CanApply(symbol, filter))
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.Rule) && !rules[filter.Rule].Any(o => CanApply(symbol, o, rules)))
			{
				return false;
			}
			return true;
		}

		private static bool CanApply(ISymbol symbol, MemberFilter filter)
		{
			if (!string.IsNullOrEmpty(filter.Name) && filter.Name != symbol.Name)
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.ContainingNamespace) && filter.ContainingNamespace != symbol.ContainingType.ContainingNamespace.ToString())
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.ContainingType) && filter.ContainingType != symbol.ContainingType.ToString())
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.ContainingTypeName) && filter.ContainingTypeName != symbol.ContainingType.Name)
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.HasAttribute) && !symbol.GetAttributes().Any(o => o.AttributeClass.ToString() == filter.HasAttribute))
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.HasAttributeName) && !symbol.GetAttributes().Any(o => o.AttributeClass.Name == filter.HasAttributeName))
			{
				return false;
			}
			return true;
		}

		private static Predicate<Document> CreateDocumentSelectionPredicate(IList<DocumentFilter> filters)
		{
			return document =>
			{
				foreach (var filter in filters)
				{
					if (!string.IsNullOrEmpty(filter.Name) && filter.Name != document.Name)
					{
						continue;
					}
					if (!string.IsNullOrEmpty(filter.FilePath) && filter.FilePath != document.FilePath)
					{
						continue;
					}
					if (!string.IsNullOrEmpty(filter.FilePathEndsWith) && !document.FilePath.EndsWith(filter.FilePathEndsWith))
					{
						continue;
					}
					return filter.Select;
				}
				return true; // Default value
			};
		}

		
	}
}
