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
	public abstract class FileConfigurator : IFileConfigurator
	{
		public abstract AsyncGenerator Parse(string content);

		public virtual void Configure(AsyncGenerator configuration, Solution solution, IFluentSolutionConfiguration solutionConfiguration, Assembly assembly)
		{
			if (solution.ApplyChanges.HasValue)
			{
				solutionConfiguration.ApplyChanges(solution.ApplyChanges.Value);
			}
			if (solution.ConcurrentRun.HasValue)
			{
				solutionConfiguration.ConcurrentRun(solution.ConcurrentRun.Value);
			}

			foreach (var item in solution.SuppressDiagnosticFailures)
			{
				solutionConfiguration.SuppressDiagnosticFailures(item.Pattern);
			}

			// Configure projects
			foreach (var projectConfig in solution.Projects)
			{
				solutionConfiguration.ConfigureProject(projectConfig.Name, o => Configure(configuration, projectConfig, o, assembly));
			}
		}

		public virtual void Configure(AsyncGenerator configuration, Project project, IFluentProjectConfiguration projectConfiguration, Assembly assembly)
		{
			if (project.ApplyChanges.HasValue)
			{
				projectConfiguration.ApplyChanges(project.ApplyChanges.Value);
			}
			if (project.ConcurrentRun.HasValue)
			{
				projectConfiguration.ConcurrentRun(project.ConcurrentRun.Value);
			}

			foreach (var item in project.SuppressDiagnosticFailures)
			{
				projectConfiguration.SuppressDiagnosticFailures(item.Pattern);
			}

			projectConfiguration.ConfigureAnalyzation(o => Configure(configuration, project.Analyzation, o));
			projectConfiguration.ConfigureTransformation(o => Configure(configuration, project.Transformation, o));

			if (!project.RegisterPlugin.Any())
			{
				return;
			}
			var assemblies = AppDomain.CurrentDomain.GetAssemblies()
				.Where(o => !o.IsDynamic)
				.Distinct()
				.ToDictionary(o => o.GetName().Name);

			foreach (var plugin in project.RegisterPlugin)
			{
				if (!string.IsNullOrEmpty(plugin.AssemblyName) && !assemblies.ContainsKey(plugin.AssemblyName))
				{
					assemblies[plugin.AssemblyName] = Assembly.Load(plugin.AssemblyName);
				}
				if (string.IsNullOrEmpty(plugin.AssemblyName) && assembly == null)
				{
					throw new InvalidOperationException($"Assembly name must be provided for type {plugin.Type}.");
				}

				var type = string.IsNullOrEmpty(plugin.AssemblyName)
					? assembly.GetExportedTypes().FirstOrDefault(o => o.Name == plugin.Type)
					: assemblies[plugin.AssemblyName].GetExportedTypes().FirstOrDefault(o => o.FullName == plugin.Type);
				if (type == null)
				{
					throw new InvalidOperationException($"Type {plugin.Type} was not found inside assembly {plugin.AssemblyName}. Hint: Make sure that the type is public.");
				}
				var pluginInstance = Activator.CreateInstance(type) as IPlugin;
				if (pluginInstance == null)
				{
					throw new InvalidOperationException($"Type {plugin.Type} from assembly {plugin.AssemblyName} does not implement IPlugin interaface");
				}
				projectConfiguration.RegisterPlugin(pluginInstance);
			}
		}

		private static void Configure(AsyncGenerator configuration, Analyzation config, IFluentProjectAnalyzeConfiguration fluentConfig)
		{
			if (config.CallForwarding.HasValue)
			{
				fluentConfig.CallForwarding(config.CallForwarding.Value);
			}
			if (config.PropertyConversion.HasValue)
			{
				fluentConfig.PropertyConversion(config.PropertyConversion.Value);
			}
			if (config.ScanMethodBody.HasValue)
			{
				fluentConfig.ScanMethodBody(config.ScanMethodBody.Value);
			}
			if (config.ScanForMissingAsyncMembers.Any())
			{
				fluentConfig.ScanForMissingAsyncMembers(CreateTypePredicate(configuration, config.ScanForMissingAsyncMembers));
			}
			fluentConfig.CancellationTokens(o => Configure(configuration, config.CancellationTokens, o));
			fluentConfig.AsyncExtensionMethods(o => Configure(config.AsyncExtensionMethods, o));

			if (config.IgnoreDocuments.Any())
			{
				fluentConfig.DocumentSelection(CreateDocumentPredicate(config.IgnoreDocuments, false));
			}
			if (config.MethodConversion.Any())
			{
				fluentConfig.MethodConversion(CreateMethodConversionFunction(configuration, config.MethodConversion));
			}
			if (config.PreserveReturnType.Any())
			{
				fluentConfig.PreserveReturnType(CreateMethodPredicate(configuration, config.PreserveReturnType, true));
			}
			if (config.IgnoreSearchForAsyncCounterparts.Any())
			{
				fluentConfig.SearchForAsyncCounterparts(CreateMethodPredicate(configuration, config.IgnoreSearchForAsyncCounterparts, false));
			}
			if (config.TypeConversion.Any())
			{
				fluentConfig.TypeConversion(CreateTypeConversionFunction(configuration, config.TypeConversion));
			}
		}

		private static void Configure(AsyncExtensionMethods config, IFluentProjectAsyncExtensionMethodsConfiguration fluentConfig)
		{
			foreach (var projectFile in config.ProjectFiles)
			{
				fluentConfig.ProjectFile(projectFile.ProjectName, projectFile.FileName);
			}
		}

		private static void Configure(AsyncGenerator configuration, CancellationTokens config, IFluentProjectCancellationTokenConfiguration fluentConfig)
		{
			if (config.Guards.HasValue)
			{
				fluentConfig.Guards(config.Guards.Value);
			}
			if (config.MethodParameter.Any())
			{
				fluentConfig.ParameterGeneration(CreateParameterGenerationFunction(configuration, config.MethodParameter));
			}
			if (config.WithoutCancellationToken.Any() || config.RequiresCancellationToken.Any())
			{
				fluentConfig.RequiresCancellationToken(CreateMethodNullablePredicate(configuration, 
					config.WithoutCancellationToken, config.RequiresCancellationToken));
			}
		}

		private static void Configure(AsyncGenerator configuration, Transformation config, IFluentProjectTransformConfiguration fluentConfig)
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
			if (!string.IsNullOrEmpty(config.AsyncLock.Type))
			{
				fluentConfig.AsyncLock(config.AsyncLock.Type, config.AsyncLock.MethodName);
			}
			fluentConfig.DocumentationComments(o => Configure(configuration, config. DocumentationComments, o));
		}

		private static void Configure(AsyncGenerator configuration, DocumentationComments config, IFluentProjectDocumentationCommentConfiguration fluentConfig)
		{
			if (config.AddOrReplaceMethodRemarks.Any())
			{
				fluentConfig.AddOrReplaceMethodRemarks(CreateMethodContentFunction(configuration, config.AddOrReplaceMethodRemarks));
			}
			if (config.RemoveMethodRemarks.Any())
			{
				fluentConfig.RemoveMethodRemarks(CreateMethodPredicate(configuration, config.RemoveMethodRemarks, true));
			}

			if (config.AddOrReplaceMethodSummary.Any())
			{
				fluentConfig.AddOrReplaceMethodSummary(CreateMethodContentFunction(configuration, config.AddOrReplaceMethodSummary));
			}
			if (config.RemoveMethodSummary.Any())
			{
				fluentConfig.RemoveMethodSummary(CreateMethodPredicate(configuration, config.RemoveMethodSummary, true));
			}
		}

		private static Func<IMethodSymbol, string> CreateMethodContentFunction(AsyncGenerator globalConfig, List<MethodContentFilter> filters)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return filter.Content;
					}
				}
				return null;
			};
		}

		private static Func<IMethodSymbolInfo, MethodCancellationToken> CreateParameterGenerationFunction(AsyncGenerator globalConfig, IList<MethodCancellationTokenFilter> filters)
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
						return filter.Parameter;
					}
				}
				return MethodCancellationToken.Optional;
			};
		}

		private static Func<INamedTypeSymbol, TypeConversion> CreateTypeConversionFunction(AsyncGenerator globalConfig, IList<TypeConversionFilter> filters)
		{
			var rules = globalConfig.TypeRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return filter.Conversion;
					}
				}
				return TypeConversion.Unknown;
			};
		}

		private static Func<IMethodSymbol, bool?> CreateMethodNullablePredicate(AsyncGenerator globalConfig, 
			IList<MethodFilter> falseFilters, IList<MethodFilter> trueFilters)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in falseFilters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return false;
					}
				}
				foreach (var filter in trueFilters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return true;
					}
				}
				return null;
			};
		}

		private static Predicate<INamedTypeSymbol> CreateTypePredicate(AsyncGenerator globalConfig, IList<TypeFilter> filters)
		{
			var rules = globalConfig.TypeRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return true;
					}
				}
				return false;
			};
		}

		private static Predicate<IMethodSymbol> CreateMethodPredicate(AsyncGenerator globalConfig, IList<MethodFilter> filters, bool validValue)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return validValue;
					}
				}
				return !validValue;
			};
		}

		private static Func<IMethodSymbol, MethodConversion> CreateMethodConversionFunction(AsyncGenerator globalConfig, IList<MethodConversionFilter> filters)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return filter.Conversion;
					}
				}
				return MethodConversion.Unknown; // Default value
			};
		}

		private static Predicate<Document> CreateDocumentPredicate(IList<DocumentFilter> filters, bool validValue)
		{
			return document =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(document, filter))
					{
						return validValue;
					}
				}
				return !validValue;
			};
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

		private static bool CanApply(IMethodSymbol symbol, MethodFilter filter, Dictionary<string, List<MethodFilter>> rules)
		{
			if (filter.All)
			{
				return true;
			}
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
			if (filter.All)
			{
				return true;
			}
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

		private static bool CanApply(Document document, DocumentFilter filter)
		{
			if (!string.IsNullOrEmpty(filter.Name) && filter.Name != document.Name)
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.FilePath) && filter.FilePath != document.FilePath)
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.FilePathEndsWith) && !document.FilePath.EndsWith(filter.FilePathEndsWith))
			{
				return false;
			}
			return true;
		}
	}
}
