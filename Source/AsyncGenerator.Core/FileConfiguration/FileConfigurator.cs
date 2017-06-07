using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.FileConfiguration
{
	public static class FileConfigurator
	{
		public static void Configure(AsyncGenerator configuration, IFluentSolutionConfiguration solutionConfiguration)
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

			if (!config.RegisterPlugin.Any())
			{
				return;
			}
			var assemblies = AppDomain.CurrentDomain.GetAssemblies()
				.Where(o => !o.IsDynamic)
				.Distinct()
				.ToDictionary(o => o.GetName().Name);

			foreach (var plugin in config.RegisterPlugin)
			{
				if (!string.IsNullOrEmpty(plugin.AssemblyName) && !assemblies.ContainsKey(plugin.AssemblyName))
				{
					throw new DllNotFoundException($"Assembly with name {plugin.AssemblyName} was not found.");
				}
				var assembly = string.IsNullOrEmpty(plugin.AssemblyName)
					? Assembly.GetExecutingAssembly()
					: assemblies[plugin.AssemblyName];

				var type = assembly.GetExportedTypes().FirstOrDefault(o => o.FullName == plugin.FullTypeName);
				if (type == null)
				{
					throw new InvalidOperationException($"Type {plugin.FullTypeName} was not found inside assembly {plugin.AssemblyName}. Hint: Make sure that the type is public.");
				}
				var pluginInstance = Activator.CreateInstance(type) as IPlugin;
				if (pluginInstance == null)
				{
					throw new InvalidOperationException($"Type {plugin.FullTypeName} from assembly {plugin.AssemblyName} does not implement IPlugin interaface");
				}
				fluentConfig.RegisterPlugin(pluginInstance);
			}
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
			if (config.ScanForMissingAsyncMembers.Any())
			{
				fluentConfig.ScanForMissingAsyncMembers(CreateTypePredicate(globalConfig, config.ScanForMissingAsyncMembers));
			}
			fluentConfig.CancellationTokens(o => Configure(globalConfig, config.CancellationTokens, o));

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
				fluentConfig.PreserveReturnType(CreateMethodPredicate(globalConfig, config.PreserveReturnType));
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

		private static void Configure(AsyncGenerator globalConfig, CancellationTokens config, IFluentProjectCancellationTokenConfiguration fluentConfig)
		{
			if (config.Guards.HasValue)
			{
				fluentConfig.Guards(config.Guards.Value);
			}
			if (config.MethodParameter.Any())
			{
				fluentConfig.ParameterGeneration(CreateMethodConversionFunction(globalConfig, config.MethodParameter));
			}
			if (config.RequiresCancellationToken.Any())
			{
				fluentConfig.RequiresCancellationToken(CreateMethodPredicate(globalConfig, config.RequiresCancellationToken));
			}
		}

		private static void Configure(Transformation config, IFluentProjectTransformConfiguration fluentConfig)
		{
			if (config.LocalFunctions.HasValue)
			{
				fluentConfig.LocalFunctions(config.LocalFunctions.Value);
			}
			if (!string.IsNullOrEmpty(config.AsyncFolder))
			{
				fluentConfig.AsyncFolder(config.AsyncFolder);
			}
			if (config.ConfigureAwaitArgument.HasValue)
			{
				fluentConfig.ConfigureAwaitArgument(config.ConfigureAwaitArgument.Value);
			}
			if (config.Disable == true)
			{
				fluentConfig.Disable();
			}
			if (!string.IsNullOrEmpty(config.AsyncLock.FullTypeName))
			{
				fluentConfig.AsyncLock(config.AsyncLock.FullTypeName, config.AsyncLock.MethodName);
			}
		}

		private static Func<IMethodSymbolInfo, Core.MethodCancellationToken> CreateMethodConversionFunction(AsyncGenerator globalConfig, IList<MethodCancellationTokenFilter> filters)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (!string.IsNullOrEmpty(filter.AnyInterfaceRule) && !symbol.ImplementedInterfaces.Any(i => rules[filter.AnyInterfaceRule].Any(o => CanApply(i, o, rules))))
					{
						continue;
					}
					if (CanApply(symbol.Symbol, filter, rules))
					{
						return Convert(filter.Parameter);
					}
				}
				return Core.MethodCancellationToken.Optional;
			};
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

		private static Func<IMethodSymbol, bool?> CreateMethodPredicate(AsyncGenerator globalConfig, IList<MethodRequiresTokenFilter> filters)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return filter.TokenRequired;
					}
				}
				return null;
			};
		}

		private static Predicate<INamedTypeSymbol> CreateTypePredicate(AsyncGenerator globalConfig, IList<TypeScanMissingAsyncMembersFilter> filters)
		{
			var rules = globalConfig.TypeRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return filter.Scan;
					}
				}
				return false;
			};
		}

		private static Predicate<IMethodSymbol> CreateMethodPredicate(AsyncGenerator globalConfig, IList<MethodPreserveReturnTypeFilter> filters)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return filter.Preserve;
					}
				}
				return false;
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

		private static Core.MethodCancellationToken Convert(MethodCancellationToken conversion)
		{
			switch (conversion)
			{
				case MethodCancellationToken.ForwardNone:
					return Core.MethodCancellationToken.ForwardNone;
				case MethodCancellationToken.Required:
					return Core.MethodCancellationToken.Required;
				case MethodCancellationToken.SealedForwardNone:
					return Core.MethodCancellationToken.SealedForwardNone;
				default:
					return Core.MethodCancellationToken.Optional;
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

		private static bool CanApply(ITypeSymbol symbol, TypeFilter filter, IReadOnlyDictionary<string, List<TypeFilter>> rules)
		{
			if (!CanApply(symbol, filter))
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.AnyInterfaceRule) && !symbol.AllInterfaces.Any(i => rules[filter.AnyInterfaceRule].Any(o => CanApply(i, o, rules))))
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.AnyBaseTypeRule) && !GetBaseTypes(symbol).Any(i => rules[filter.AnyBaseTypeRule].Any(o => CanApply(i, o, rules))))
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.Rule) && !rules[filter.Rule].Any(o => CanApply(symbol, o, rules)))
			{
				return false;
			}
			return true;
		}

		internal static IEnumerable<ITypeSymbol> GetBaseTypes(ITypeSymbol type)
		{
			var current = type.BaseType;
			while (current != null)
			{
				yield return current;
				current = current.BaseType;
			}
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
			if (!string.IsNullOrEmpty(filter.ContainingAssemblyName) && filter.ContainingAssemblyName != symbol.ContainingAssembly.Name)
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
