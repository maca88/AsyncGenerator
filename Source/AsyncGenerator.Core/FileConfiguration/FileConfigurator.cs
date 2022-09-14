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
			if (!string.IsNullOrEmpty(solution.TargetFramework))
			{
				solutionConfiguration.TargetFramework(solution.TargetFramework);
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
			if (!string.IsNullOrEmpty(project.TargetFramework))
			{
				projectConfiguration.TargetFramework(project.TargetFramework);
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
				.GroupBy(o => o.GetName().Name) // there may be multiple versions of the same assembly
				.ToDictionary(o => o.Key, o => o.First());

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

				IPlugin pluginInstance;
				if (plugin.Parameters.Count > 0)
				{
					var arguments = plugin.Parameters.ToDictionary(o => o.Name, o => o.Value);
					var constructor = type.GetConstructors()
						.Where(o => o.GetParameters().Length == plugin.Parameters.Count)
						.FirstOrDefault(o => o.GetParameters().All(p => arguments.ContainsKey(p.Name)));
					if (constructor == null)
					{
						throw new InvalidOperationException(
							$"Type {plugin.Type} from assembly {plugin.AssemblyName} does not contain a constructor with the provided parameter names.");
					}

					var argumentValues = new object[arguments.Count];
					var parameters = constructor.GetParameters();
					for (var i = 0; i < parameters.Length; i++)
					{
						var parameter = parameters[i];
						argumentValues[i] = Convert.ChangeType(arguments[parameter.Name], parameter.ParameterType);
					}

					pluginInstance = constructor.Invoke(argumentValues) as IPlugin;
				}
				else
				{
					pluginInstance = Activator.CreateInstance(type) as IPlugin;
					
				}

				if (pluginInstance == null)
				{
					throw new InvalidOperationException($"Type {plugin.Type} from assembly {plugin.AssemblyName} does not implement IPlugin interface");
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
			if (config.SearchAsyncCounterpartsInInheritedTypes.HasValue)
			{
				fluentConfig.SearchAsyncCounterpartsInInheritedTypes(config.SearchAsyncCounterpartsInInheritedTypes.Value);
			}
			if (config.ScanForMissingAsyncMembers.Any())
			{
				fluentConfig.ScanForMissingAsyncMembers(CreateTypePredicate(configuration, config.ScanForMissingAsyncMembers));
			}
			if (config.CancellationTokens.IsEnabled)
			{
				fluentConfig.CancellationTokens(o => Configure(configuration, config.CancellationTokens, o));
			}
			fluentConfig.AsyncExtensionMethods(o => Configure(config.AsyncExtensionMethods, o));
			fluentConfig.Diagnostics(o => Configure(configuration, config.Diagnostics, o));
			fluentConfig.ExceptionHandling(o => Configure(configuration, config.ExceptionHandling, o));

			if (config.IgnoreDocuments.Any())
			{
				fluentConfig.DocumentSelection(CreateDocumentPredicate(config.IgnoreDocuments, false));
			}
			if (config.MethodConversion.Any())
			{
				foreach (var group in config.MethodConversion.GroupBy(o => o.ExecutionPhase))
				{
					fluentConfig.MethodConversion(CreateMethodConversionFunction(configuration, group.ToList()), group.Key);
				}
			}
			if (config.PreserveReturnType.Any())
			{
				foreach (var group in config.PreserveReturnType.GroupBy(o => o.ExecutionPhase))
				{
					fluentConfig.PreserveReturnType(CreateMethodNullablePredicate(configuration, group.ToList(), true), group.Key);
				}
			}
			if (config.AsyncReturnType.Any())
			{
				foreach (var group in config.AsyncReturnType.GroupBy(o => o.ExecutionPhase))
				{
					fluentConfig.AsyncReturnType(CreateAsyncReturnTypeFunction(configuration, group.ToList()));
				}
			}
			if (config.AlwaysAwait.Any())
			{
				foreach (var group in config.AlwaysAwait.GroupBy(o => o.ExecutionPhase))
				{
					fluentConfig.AlwaysAwait(CreateMethodNullablePredicate(configuration, group.ToList(), true), group.Key);
				}
			}
			if (config.IgnoreSearchForAsyncCounterparts.Any())
			{
				fluentConfig.SearchForAsyncCounterparts(CreateMethodPredicate(configuration, config.IgnoreSearchForAsyncCounterparts, false));
			}
			if (config.IgnoreAsyncCounterparts.Any())
			{
				fluentConfig.IgnoreAsyncCounterparts(CreateMethodPredicate(configuration, config.IgnoreAsyncCounterparts, true));
			}
			if (config.IgnoreSearchForMethodReferences.Any())
			{
				foreach (var group in config.IgnoreSearchForMethodReferences.GroupBy(o => o.ExecutionPhase))
				{
					fluentConfig.SearchForMethodReferences(CreateMethodNullablePredicate(configuration, group.ToList(), false), group.Key);
				}
			}
			if (config.TypeConversion.Any())
			{
				foreach (var group in config.TypeConversion.GroupBy(o => o.ExecutionPhase))
				{
					fluentConfig.TypeConversion(CreateTypeConversionFunction(configuration, group.ToList()), group.Key);
				}
			}
		}

		private static void Configure(AsyncExtensionMethods config, IFluentProjectAsyncExtensionMethodsConfiguration fluentConfig)
		{
			foreach (var projectFile in config.ProjectFiles)
			{
				fluentConfig.ProjectFile(projectFile.ProjectName, projectFile.FileName);
			}

			foreach (var assemblyType in config.AssemblyTypes)
			{
				fluentConfig.ExternalType(assemblyType.AssemblyName, assemblyType.FullTypeName);
			}
		}

		private static void Configure(AsyncGenerator configuration, Diagnostics config, IFluentProjectDiagnosticsConfiguration fluentConfig)
		{
			if (config.Disable == true)
			{
				fluentConfig.Disable();
			}
			if (config.DiagnoseDocument.Any())
			{
				fluentConfig.DiagnoseDocument(CreateDocumentPredicateFunction(config.DiagnoseDocument, true));
			}
			if (config.DiagnoseType.Any())
			{
				fluentConfig.DiagnoseType(CreateTypePredicateFunction(configuration, config.DiagnoseType, true));
			}
			if (config.DiagnoseMethod.Any())
			{
				fluentConfig.DiagnoseMethod(CreateMethodPredicateFunction(configuration, config.DiagnoseMethod, true));
			}
		}

		private static void Configure(AsyncGenerator configuration, ExceptionHandling config, IFluentProjectExceptionHandlingConfiguration fluentConfig)
		{
			if (config.CatchPropertyGetterCalls.Any())
			{
				fluentConfig.CatchPropertyGetterCalls(CreateMethodPredicateFunction(configuration, config.CatchPropertyGetterCalls, false));
			}
			if (config.CatchMethodBody.Any())
			{
				fluentConfig.CatchMethodBody(CreateMethodNullablePredicate(configuration, config.CatchMethodBody));
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
				foreach (ExecutionPhase value in Enum.GetValues(typeof(ExecutionPhase)))
				{
					var withoutTokenGroup = config.WithoutCancellationToken.GroupBy(o => o.ExecutionPhase).FirstOrDefault(o => o.Key == value);
					var requiresTokenGroup = config.RequiresCancellationToken.GroupBy(o => o.ExecutionPhase).FirstOrDefault(o => o.Key == value);
					if (withoutTokenGroup != null || requiresTokenGroup != null)
					{
						fluentConfig.RequiresCancellationToken(
							CreateMethodNullablePredicate(configuration,
								withoutTokenGroup?.ToList() ?? new List<MethodFilter>(),
								requiresTokenGroup?.ToList() ?? new List<MethodFilter>()
							), value);
					}
				}

				fluentConfig.RequiresCancellationToken(CreateMethodNullablePredicate(configuration, 
					config.WithoutCancellationToken, config.RequiresCancellationToken));
			}
		}

		private static void Configure(AsyncGenerator configuration, Transformation config, IFluentProjectTransformConfiguration fluentConfig)
		{
			if (config.MethodGeneration.Any())
			{
				fluentConfig.MethodGeneration(CreateMethodGenerationFunction(configuration, config.MethodGeneration));
			}
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
			fluentConfig.DocumentationComments(o => Configure(configuration, config.DocumentationComments, o));
			fluentConfig.PreprocessorDirectives(o => Configure(configuration, config.PreprocessorDirectives, o));
		}

		private static void Configure(AsyncGenerator configuration, PreprocessorDirectives config, IFluentProjectPreprocessorDirectivesConfiguration fluentConfig)
		{
			if (config.AddForMethod.Any())
			{
				fluentConfig.AddForMethod(CreateMethodPreprocessorDirectiveFunction(configuration, config.AddForMethod));
			}
		}

		private static void Configure(AsyncGenerator configuration, DocumentationComments config, IFluentProjectDocumentationCommentConfiguration fluentConfig)
		{
			if (config.AddOrReplacePartialTypeComments.Any())
			{
				fluentConfig.AddOrReplacePartialTypeComments(CreateTypeContentFunction(configuration, config.AddOrReplacePartialTypeComments));
			}
			if (config.RemovePartialTypeComments.Any())
			{
				fluentConfig.RemovePartialTypeComments(CreateTypePredicate(configuration, config.RemovePartialTypeComments));
			}

			if (config.AddOrReplaceNewTypeComments.Any())
			{
				fluentConfig.AddOrReplaceNewTypeComments(CreateTypeContentFunction(configuration, config.AddOrReplaceNewTypeComments));
			}
			if (config.RemoveNewTypeComments.Any())
			{
				fluentConfig.RemoveNewTypeComments(CreateTypePredicate(configuration, config.RemoveNewTypeComments));
			}

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

		private static Func<INamedTypeSymbol, string> CreateTypeContentFunction(AsyncGenerator globalConfig, List<TypeContentFilter> filters)
		{
			var rules = globalConfig.TypeRules.ToDictionary(o => o.Name, o => o.Filters);
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

		private static Func<IMethodSymbol, Core.PreprocessorDirectives> CreateMethodPreprocessorDirectiveFunction(AsyncGenerator globalConfig, List<MethodPreprocessorDirectiveFilter> filters)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return new Core.PreprocessorDirectives(filter.StartDirective, filter.EndDirective);
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

		private static Predicate<INamedTypeSymbol> CreateTypePredicateFunction(AsyncGenerator globalConfig, IList<TypePredicateFilter> filters, bool defaultResult)
		{
			var rules = globalConfig.TypeRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return filter.Result;
					}
				}
				return defaultResult;
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

		private static Func<IMethodSymbol, bool?> CreateMethodNullablePredicate(AsyncGenerator globalConfig, IList<MethodPredicateFilter> filters)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return filter.Result;
					}
				}
				return null;
			};
		}

		private static Func<IMethodSymbol, bool?> CreateMethodNullablePredicate(AsyncGenerator globalConfig, IList<MethodFilter> filters, bool validValue)
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

		private static Func<IMethodSymbol, MethodGeneration> CreateMethodGenerationFunction(AsyncGenerator globalConfig, IList<MethodGenerationFilter> filters)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return filter.Generation;
					}
				}
				return MethodGeneration.Generate; // Default value
			};
		}

		private static Func<IMethodSymbol, AsyncReturnType?> CreateAsyncReturnTypeFunction(AsyncGenerator globalConfig, IList<AsyncReturnTypeFilter> filters)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return filter.ReturnType;
					}
				}
				return null; // Default value
			};
		}

		private static Predicate<IMethodSymbol> CreateMethodPredicateFunction(AsyncGenerator globalConfig, IList<MethodPredicateFilter> filters, bool defaultResult)
		{
			var rules = globalConfig.MethodRules.ToDictionary(o => o.Name, o => o.Filters);
			return symbol =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(symbol, filter, rules))
					{
						return filter.Result;
					}
				}
				return defaultResult; // Default value
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

		private static Predicate<Document> CreateDocumentPredicateFunction(IList<DocumentPredicateFilter> filters, bool defaultResult)
		{
			return document =>
			{
				foreach (var filter in filters)
				{
					if (CanApply(document, filter))
					{
						return filter.Result;
					}
				}
				return defaultResult;
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
			if (filter.ReturnsVoid.HasValue && filter.ReturnsVoid.Value != symbol.ReturnsVoid)
			{
				return false;
			}
			if (!string.IsNullOrEmpty(filter.Rule))
			{
				if (!rules.TryGetValue(filter.Rule, out var rule))
				{
					throw new InvalidOperationException($"Method rule {filter.Rule} do not exist");
				}
				if (!rule.Any(o => CanApply(symbol, o, rules)))
				{
					return false;
				}
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
			if (!string.IsNullOrEmpty(filter.Rule))
			{
				if (!rules.TryGetValue(filter.Rule, out var rule))
				{
					throw new InvalidOperationException($"Type rule {filter.Rule} do not exist");
				}
				if (!rule.Any(o => CanApply(symbol, o, rules)))
				{
					return false;
				}
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
			if (filter.HasDocumentationComment.HasValue && filter.HasDocumentationComment.Value == string.IsNullOrEmpty(symbol.GetDocumentationCommentXml()))
			{
				return false;
			}
			if (filter.IsVirtual.HasValue && filter.IsVirtual.Value == symbol.IsVirtual)
			{
				return false;
			}
			if (filter.IsAbstract.HasValue && filter.IsAbstract.Value == symbol.IsAbstract)
			{
				return false;
			}
			return true;
		}

		private static bool CanApply(Document document, DocumentFilter filter)
		{
			if (filter.All)
			{
				return true;
			}
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
