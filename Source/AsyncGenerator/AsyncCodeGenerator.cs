using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Analyzation.Internal;
using AsyncGenerator.Configuration;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Internal;
using AsyncGenerator.Transformation;
using AsyncGenerator.Transformation.Internal;
using log4net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

namespace AsyncGenerator
{
	public class AsyncCodeGenerator
	{
		private static readonly ILog Logger = LogManager.GetLogger(typeof(AsyncCodeGenerator));

		public async Task GenerateAsync(AsyncCodeConfiguration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}
			Logger.Info("Generating async code started");

			foreach (var config in configuration.SolutionConfigurations)
			{
				Logger.Info($"Configuring solution '{config.Path}' prior analyzation started");
				var solutionData = await CreateSolutionData(config).ConfigureAwait(false);
				Logger.Info($"Configuring solution '{config.Path}' prior analyzation completed");

				foreach (var projectData in solutionData.GetProjects())
				{
					// Register internal plugins
					RegisterInternalPlugins(projectData.Configuration);

					// Initialize plugins
					Logger.Info($"Initializing registered plugins for project '{projectData.Project.Name}' started");
					foreach (var registeredPlugin in projectData.Configuration.RegisteredPlugins)
					{
						await registeredPlugin.Initialize(projectData.Project, projectData.Configuration).ConfigureAwait(false);
					}
					Logger.Info($"Initializing registered plugins for project '{projectData.Project.Name}' completed");

					// Setup parsing
					SetupParsing(projectData);

					// Analyze project
					Logger.Info($"Analyzing project '{projectData.Project.Name}' started");
					var analyzeConfig = projectData.Configuration.AnalyzeConfiguration;
					var analyzationResult = await AnalyzeProject(projectData).ConfigureAwait(false);
					foreach (var action in analyzeConfig.AfterAnalyzation)
					{
						action(analyzationResult);
					}
					Logger.Info($"Analyzing project '{projectData.Project.Name}' completed");

					// Transform documents
					var transformConfig = projectData.Configuration.TransformConfiguration;
					if (transformConfig.Enabled)
					{
						Logger.Info($"Transforming project '{projectData.Project.Name}' started");
						var transformResult = TransformProject(analyzationResult, transformConfig);
						foreach (var action in transformConfig.AfterTransformation)
						{
							action(transformResult);
						}
						projectData.Project = transformResult.Project; // updates also the solution
						Logger.Info($"Transforming project '{projectData.Project.Name}' completed");
					}
					
					// Compile
					var compileConfig = projectData.Configuration.CompileConfiguration;
					if (compileConfig != null)
					{
						Logger.Info($"Compiling project '{projectData.Project.Name}' started");
						var compilation = await projectData.Project.GetCompilationAsync();
						var emit = compilation.Emit(compileConfig.OutputPath, compileConfig.SymbolsPath, compileConfig.XmlDocumentationPath);
						if (!emit.Success)
						{
							var messages = string.Join(
								Environment.NewLine,
								emit.Diagnostics.Where(o => o.Severity == DiagnosticSeverity.Error).Select(o => o.GetMessage()));
							throw new InvalidOperationException(
								$"Generation for Project {projectData.Project.Name} failed to generate a valid code. Errors:{Environment.NewLine}{messages}");
						}
						Logger.Info($"Compiling project '{projectData.Project.Name}' completed");
					}
				}
				if (config.ApplyChanges)
				{
					Logger.Info($"Applying solution '{config.Path}' changes started");
					solutionData.Workspace.TryApplyChanges(solutionData.Solution);
					Logger.Info($"Applying solution '{config.Path}' changes completed");
				}
			}

			Logger.Info("Generating async code completed");
		}

		private void SetupParsing(ProjectData projectData)
		{
			var parseOptions = (CSharpParseOptions)projectData.Project.ParseOptions;
			var parseConfig = projectData.Configuration.ParseConfiguration;
			var currentProcessorSymbolNames = parseOptions.PreprocessorSymbolNames.ToList();
			foreach (var name in parseConfig.RemovePreprocessorSymbolNames)
			{
				if (!currentProcessorSymbolNames.Remove(name))
				{
					throw new InvalidOperationException($"Unable to remove a preprocessor symbol with the name {name} as it does not exist");
				}
			}
			foreach (var name in parseConfig.AddPreprocessorSymbolNames)
			{
				currentProcessorSymbolNames.Add(name);
			}
			var newParseOptions = new CSharpParseOptions(
				parseConfig.LanguageVersion ?? parseOptions.SpecifiedLanguageVersion,
				parseOptions.DocumentationMode,
				parseOptions.Kind,
				currentProcessorSymbolNames);
			projectData.Project = projectData.Project.WithParseOptions(newParseOptions);
		}

		private Task<IProjectAnalyzationResult> AnalyzeProject(ProjectData projectData)
		{
			var analyzer = new ProjectAnalyzer(projectData);
			return analyzer.Analyze();
		}

		private IProjectTransformationResult TransformProject(IProjectAnalyzationResult analyzationResult, ProjectTransformConfiguration configuration)
		{
			var transformer = new ProjectTransformer(configuration);
			return transformer.Transform(analyzationResult);
		}

		private async Task<SolutionData> CreateSolutionData(SolutionConfiguration configuration)
		{
			var props = new Dictionary<string, string>
			{
				["CheckForSystemRuntimeDependency"] = "true" // needed in order that project references are loaded
			};
			var workspace = MSBuildWorkspace.Create(props);
			var solution = await workspace.OpenSolutionAsync(configuration.Path).ConfigureAwait(false);
			var solutionData = new SolutionData(solution, workspace, configuration);

			var projects = solution.Projects.ToDictionary(o => o.Name);
			foreach (var config in configuration.ProjectConfigurations)
			{
				if (!projects.ContainsKey(config.Name))
				{
					throw new InvalidOperationException($"Project '{config.Name}' does not exist in solution '{solution.FilePath}'");
				}
				var project = projects[config.Name];
				var projectData = new ProjectData(solutionData, project.Id, config);
				RemoveGeneratedDocuments(projectData);
				solutionData.ProjectData.Add(project.Id, projectData);
			}
			return solutionData;
		}

		private void RemoveGeneratedDocuments(ProjectData projectData)
		{
			var project = projectData.Project;
			var asyncFolder = projectData.Configuration.TransformConfiguration.AsyncFolder;
			if (string.IsNullOrEmpty(asyncFolder))
			{
				return;
			}
			var asyncProjectFolder = Path.Combine(projectData.DirectoryPath, asyncFolder) + @"\";
			// Remove all generated documents
			var toRemove = project.Documents.Where(o => o.FilePath.StartsWith(asyncProjectFolder)).Select(doc => doc.Id).ToList();
			foreach (var docId in toRemove)
			{
				project = project.RemoveDocument(docId);
			}
			projectData.Project = project;
		}

		private void RegisterInternalPlugins(IFluentProjectConfiguration configuration)
		{
			configuration.RegisterPlugin(new DefaultAsyncCounterpartsFinder());
			configuration.RegisterPlugin(new DefaultPreconditionChecker());

			// Document transformers
			configuration.RegisterPlugin(new IncludeFilePathTransformer()); // TODO: remove - make it optional

			// Method transformers
			configuration.RegisterPlugin(new AsyncLockMethodTransformer());
			configuration.RegisterPlugin(new CancellationTokenMethodTransformer());
			configuration.RegisterPlugin(new SplitTailMethodTransformer());
			configuration.RegisterPlugin(new DocumentationCommentMethodTransformer());
		}
	}
}
