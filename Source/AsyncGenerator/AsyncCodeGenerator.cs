using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AsyncGenerator.Analyzation.Internal;
using AsyncGenerator.Configuration;
using AsyncGenerator.Configuration.Internal;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Logging;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Internal;
using AsyncGenerator.Plugins.Internal;
using AsyncGenerator.Transformation.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace AsyncGenerator
{
	public static class AsyncCodeGenerator
	{
#if !NETCOREAPP
		// In .NET only one project or solution can be opened simultaneously
		private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
		private const int LockTimeout = 30 * 1000;
#endif

		static AsyncCodeGenerator() { }

		public static async Task GenerateAsync(AsyncCodeConfiguration configuration, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			var logger = configuration.LoggerFactoryInstance.GetLogger(nameof(AsyncCodeGenerator));

			logger.Info("Generating async code started");

			foreach (var config in configuration.SolutionConfigurations)
			{
				var workspace = CreateWorkspace(config.TargetFramework);
				logger.Info($"Opening solution '{config.Path}' started");
				var solution = await OpenSolution(workspace, config.Path,
					config.SuppressDiagnosticFailuresPrediactes, logger, cancellationToken).ConfigureAwait(false);
				logger.Info($"Opening solution '{config.Path}' completed");

				logger.Info("Configuring solution prior analyzation started");
				var solutionData = CreateSolutionData(solution, config);
				logger.Info("Configuring solution prior analyzation completed");

				foreach (var projectData in solutionData.GetProjects())
				{
					await GenerateProject(projectData, configuration.LoggerFactoryInstance, logger, cancellationToken).ConfigureAwait(false);
				}
				if (config.ApplyChanges)
				{
					await ApplyChanges(workspace, solutionData.Solution, logger, cancellationToken).ConfigureAwait(false);
				}
				workspace.Dispose();
			}

			foreach (var config in configuration.ProjectConfigurations)
			{
				var workspace = CreateWorkspace(config.TargetFramework);
				logger.Info($"Opening project '{config.Path}' started");
				var project = await OpenProject(workspace, config.Path, 
					config.SuppressDiagnosticFailuresPrediactes, logger, cancellationToken).ConfigureAwait(false);
				logger.Info($"Opening project '{config.Path}' completed");

				logger.Info("Configuring project prior analyzation started");
				var projectData = CreateProjectData(project, config);
				logger.Info("Configuring project prior analyzation completed");

				await GenerateProject(projectData, configuration.LoggerFactoryInstance, logger, cancellationToken).ConfigureAwait(false);

				if (config.ApplyChanges)
				{
					await ApplyChanges(workspace, projectData.Project.Solution, logger, cancellationToken).ConfigureAwait(false);
				}
				workspace.Dispose();
			}

			logger.Info("Generating async code completed");
		}

		internal static MSBuildWorkspace CreateWorkspace(string targetFramework)
		{
			var props = new Dictionary<string, string>
			{
				["CheckForSystemRuntimeDependency"] = "true" // needed in order that project references are loaded
			};
			if (!string.IsNullOrEmpty(targetFramework))
			{
				props["TargetFramework"] = targetFramework;
			}
			return MSBuildWorkspace.Create(props);
		}

		internal static async Task GenerateProject(ProjectData projectData, ILoggerFactory loggerFactory, ILogger logger,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			var analyzeConfig = projectData.Configuration.AnalyzeConfiguration;

			// Register internal plugins
			RegisterInternalPlugins(projectData.Configuration);

			// Register async extension methods finders
			foreach (var pair in analyzeConfig.AsyncExtensionMethods.ProjectFiles)
			{
				foreach (var fileName in pair.Value)
				{
					RegisterPlugin(projectData.Configuration, new AsyncExtensionMethodsFinder(pair.Key, fileName));
				}
			}

			// Setup parsing
			SetupParsing(projectData);

			// Compile project for the first time
			logger.Info($"Compiling project '{projectData.Project.Name}' started");
			projectData.Compilation = await projectData.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
			logger.Info($"Compiling project '{projectData.Project.Name}' completed");

			// Initialize plugins
			logger.Info($"Initializing registered plugins for project '{projectData.Project.Name}' started");
			foreach (var registeredPlugin in projectData.Configuration.RegisteredPlugins)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
				await registeredPlugin.Initialize(projectData.Project, projectData.Configuration, projectData.Compilation).ConfigureAwait(false);
			}
			logger.Info($"Initializing registered plugins for project '{projectData.Project.Name}' completed");

			// Analyze project
			logger.Info($"Analyzing project '{projectData.Project.Name}' started");
			var analyzationResult = await AnalyzeProject(projectData, loggerFactory, cancellationToken).ConfigureAwait(false);
			foreach (var action in analyzeConfig.AfterAnalyzation)
			{
				action(analyzationResult);
			}
			logger.Info($"Analyzing project '{projectData.Project.Name}' completed");

			// Transform documents
			var transformConfig = projectData.Configuration.TransformConfiguration;
			if (transformConfig.Enabled)
			{
				logger.Info($"Transforming project '{projectData.Project.Name}' started");
				var transformResult = TransformProject(analyzationResult, transformConfig, loggerFactory);
				foreach (var action in transformConfig.AfterTransformation)
				{
					action(transformResult);
				}
				projectData.Project = transformResult.Project; // updates also the solution
				logger.Info($"Transforming project '{projectData.Project.Name}' completed");
			}

			// Compile transformed project
			var compileConfig = projectData.Configuration.CompileConfiguration;
			if (compileConfig != null)
			{
				logger.Info($"Compiling project '{projectData.Project.Name}' started");
				var compilation = await projectData.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
				var emit = compilation.Emit(compileConfig.OutputPath, compileConfig.SymbolsPath, compileConfig.XmlDocumentationPath);
				if (!emit.Success)
				{
					var messages = string.Join(
						Environment.NewLine,
						emit.Diagnostics.Where(o => o.Severity == DiagnosticSeverity.Error).Select(o => o.GetMessage()));
					throw new InvalidOperationException(
						$"Generation for Project {projectData.Project.Name} failed to generate a valid code. Errors:{Environment.NewLine}{messages}");
				}
				logger.Info($"Compiling project '{projectData.Project.Name}' completed");
			}
		}

		internal static async Task ApplyChanges(Workspace workspace, Solution solution, ILogger logger, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			var changes = solution.GetChanges(workspace.CurrentSolution);
			var newSolution = workspace.CurrentSolution;

			if (solution.FilePath != null)
			{
				logger.Info($"Applying solution '{solution.FilePath}' changes started");
			}

			// Apply changes manually as the AddDocument and RemoveDocument methods do not play well with the new csproj format
			// Problems with AddDocument and RemoveDocument methods:
			// - When an imported document is removed in a new csproj, TryApplyChanges will throw because the file was imported by a glob
			// - When a document is added in a new csproj, the file will be explicitly added in the csproj even if there is a glob that could import it
			foreach (var projectChanges in changes.GetProjectChanges())
			{
				logger.Info($"Applying project '{projectChanges.NewProject.FilePath}' changes started");

				var xml = new XmlDocument();
				xml.Load(projectChanges.NewProject.FilePath);
				var isNewCsproj = xml.DocumentElement?.GetAttribute("Sdk") == "Microsoft.NET.Sdk";

				var addedDocuments = projectChanges
					.GetAddedDocuments()
					.Select(o => projectChanges.NewProject.GetDocument(o))
					.ToDictionary(o => o.FilePath);
				var removedDocuments = projectChanges
					.GetRemovedDocuments()
					.Select(o => projectChanges.OldProject.GetDocument(o))
					.ToDictionary(o => o.FilePath);

				// Add new documents or replace the document text if it was already there
				foreach (var addedDocumentPair in addedDocuments)
				{
					var addedDocument = addedDocumentPair.Value;
					if (removedDocuments.ContainsKey(addedDocumentPair.Key))
					{
						var removedDocument = removedDocuments[addedDocumentPair.Key];
						newSolution = newSolution.GetDocument(removedDocument.Id)
							.WithText(await addedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false))
							.Project.Solution;
						continue;
					}

					var sourceText = await addedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
					// For new csproj format we don't want to explicitly add the document as they are imported by default
					if (isNewCsproj)
					{
						var dirPath = Path.GetDirectoryName(addedDocument.FilePath);
						Directory.CreateDirectory(dirPath); // Create all directories if not exist
						using (var writer = new StreamWriter(addedDocument.FilePath, false, Encoding.UTF8))
						{
							sourceText.Write(writer, cancellationToken);
						}
					}
					else
					{
						var newProject = newSolution.GetProject(projectChanges.ProjectId);
						newSolution = newProject.AddDocument(
								addedDocument.Name,
								sourceText,
								addedDocument.Folders,
								addedDocument.FilePath)
							.Project.Solution;
					}
				}

				// Remove documents that are not generated anymore
				foreach (var removedDocumentPair in removedDocuments.Where(o => !addedDocuments.ContainsKey(o.Key)))
				{
					var removedDocument = removedDocumentPair.Value;
					// For new csproj format we cannot remove a document as they are imported by globs (RemoveDocument throws an exception for new csproj format)
					if (!isNewCsproj)
					{
						newSolution = newSolution.RemoveDocument(removedDocument.Id);
					}
					File.Delete(removedDocument.FilePath);
				}

				// Update changed documents
				foreach (var documentId in projectChanges.GetChangedDocuments())
				{
					var newDocument = projectChanges.NewProject.GetDocument(documentId);
					newSolution = newSolution.GetDocument(documentId)
						.WithText(await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false))
						.Project.Solution;
				}

				logger.Info($"Applying project '{projectChanges.NewProject.FilePath}' changes completed");
			}

			workspace.TryApplyChanges(newSolution);

			if (solution.FilePath != null)
			{
				logger.Info($"Applying solution '{solution.FilePath}' changes completed");
			}
		}

		private static void SetupParsing(ProjectData projectData)
		{
			var parseConfig = projectData.Configuration.ParseConfiguration;
			if (!parseConfig.IsSet)
			{
				return; // Do not modify the project if parsing was not configured
			}

			var parseOptions = (CSharpParseOptions)projectData.Project.ParseOptions;
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

		private static Task<IProjectAnalyzationResult> AnalyzeProject(ProjectData projectData, ILoggerFactory loggerFactory,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			var analyzer = new ProjectAnalyzer(projectData, loggerFactory);
			return analyzer.Analyze(cancellationToken);
		}

		private static IProjectTransformationResult TransformProject(IProjectAnalyzationResult analyzationResult, ProjectTransformConfiguration configuration,
			ILoggerFactory loggerFactory)
		{
			var transformer = new ProjectTransformer(configuration, loggerFactory);
			return transformer.Transform(analyzationResult);
		}

		internal static ProjectData CreateProjectData(Project project, ProjectConfiguration configuration, SolutionData solutionData = null)
		{
			foreach (var configurator in configuration.RegisteredConfigurators)
			{
				configurator.Configure(project, configuration);
			}

			var projectData = solutionData == null
				? new ProjectData(project, configuration)
				: new ProjectData(solutionData, project.Id, configuration);
			RemoveGeneratedDocuments(projectData);
			return projectData;
		}

		internal static async Task<Project> OpenProject(MSBuildWorkspace workspace, string filePath,
			ImmutableArray<Predicate<string>> supressFailuresPredicates,
			ILogger logger,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}

#if !NETCOREAPP
			if (!await _lock.WaitAsync(LockTimeout, cancellationToken).ConfigureAwait(false))
			{
				throw new InvalidOperationException($"Project {filePath} cannot be opened beacause a build is already in progress.");
			}
#endif
			var project = await workspace.OpenProjectAsync(filePath, null, cancellationToken).ConfigureAwait(false);
#if !NETCOREAPP
			_lock.Release();
#endif
			CheckForErrors(workspace, "project", supressFailuresPredicates, logger);
			return project;
		}

		internal static async Task<Solution> OpenSolution(MSBuildWorkspace workspace, string filePath,
			ImmutableArray<Predicate<string>> supressFailuresPredicates,
			ILogger logger,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}

#if !NETCOREAPP
			if (!await _lock.WaitAsync(LockTimeout, cancellationToken).ConfigureAwait(false))
			{
				throw new InvalidOperationException($"Solution {filePath} cannot be opened beacause a build is already in progress.");
			}
#endif
			var solution = await workspace.OpenSolutionAsync(filePath, null, cancellationToken).ConfigureAwait(false);
#if !NETCOREAPP
			_lock.Release();
#endif
			CheckForErrors(workspace, "solution", supressFailuresPredicates, logger);
			return solution;
		}

		internal static SolutionData CreateSolutionData(Solution solution, SolutionConfiguration configuration,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			var solutionData = new SolutionData(solution, configuration);

			var projects = solution.Projects.ToDictionary(o => o.Name);
			foreach (var config in configuration.ProjectConfigurations)
			{
				if (!projects.ContainsKey(config.Name))
				{
					throw new InvalidOperationException($"Project '{config.Name}' does not exist in solution '{solution.FilePath}'");
				}
				var project = projects[config.Name];
				var projectData = CreateProjectData(project, config, solutionData);
				solutionData.ProjectData.Add(project.Id, projectData);
			}
			return solutionData;
		}

		internal static void CheckForErrors(MSBuildWorkspace workspace, string itemType, ImmutableArray<Predicate<string>> supressFailuresPredicates, ILogger logger)
		{
			// Throw if any failure
			var failures = workspace.Diagnostics
				.Where(o => o.Kind == WorkspaceDiagnosticKind.Failure)
				.Select(o => o.Message)
				.Where(o => !supressFailuresPredicates.Any(p => p(o)))
				.ToList();
			if (failures.Any())
			{
				var message =
					$"One or more errors occurred while opening the {itemType}:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}{Environment.NewLine}" +
					"Hint: For suppressing irrelevant errors use SuppressDiagnosticFailures option.";
				throw new InvalidOperationException(message);
			}
			var warnings = workspace.Diagnostics
				.Where(o => o.Kind == WorkspaceDiagnosticKind.Warning)
				.Select(o => o.Message)
				.ToList();
			if (warnings.Any())
			{
				logger.Warn(
					$"One or more warnings occurred while opening the {itemType}:{Environment.NewLine}{string.Join(Environment.NewLine, warnings)}{Environment.NewLine}");
			}
		}

		private static void RemoveGeneratedDocuments(ProjectData projectData)
		{
			var project = projectData.Project;
			var asyncFolder = projectData.Configuration.TransformConfiguration.AsyncFolder;
			if (string.IsNullOrEmpty(asyncFolder))
			{
				return;
			}
			var asyncProjectFolder = Path.Combine(projectData.DirectoryPath, asyncFolder) + Path.DirectorySeparatorChar;
			// Remove all generated documents
			var toRemove = project.Documents.Where(o => o.FilePath.StartsWith(asyncProjectFolder)).Select(doc => doc.Id).ToList();
			foreach (var docId in toRemove)
			{
				project = project.RemoveDocument(docId);
			}
			projectData.Project = project;
		}

		private static void RegisterInternalPlugins(IFluentProjectConfiguration configuration)
		{
			configuration.RegisterPlugin(new DefaultAsyncCounterpartsFinder());
			configuration.RegisterPlugin(new ThreadSleepAsyncCounterpartFinder());
			configuration.RegisterPlugin(new ParallelForForEachTransformer());
			configuration.RegisterPlugin(new DefaultPreconditionChecker());

			// Document transformers
			configuration.RegisterPlugin(new RestoreNullableTransformer());

			// Type transformers
			configuration.RegisterPlugin(new DocumentCommentTypeTransformer());
			configuration.RegisterPlugin(new DisabledTextTypeTransformer());

			// Method transformers
			configuration.RegisterPlugin(new YieldMethodTransformer());
			configuration.RegisterPlugin(new OperationCanceledExceptionFunctionTransformer());
			configuration.RegisterPlugin(new ReturnTaskFunctionTransformer());
			configuration.RegisterPlugin(new InModifierRemovalTransformer());
			configuration.RegisterPlugin(new ObsoleteMissingMethodTransformer());

			configuration.RegisterPlugin(new AsyncLockMethodTransformer());
			configuration.RegisterPlugin(new CancellationTokenMethodTransformer());
			configuration.RegisterPlugin(new SplitTailMethodTransformer());
			configuration.RegisterPlugin(new DocumentationCommentMethodTransformer());
		}

		private static void RegisterPlugin(IFluentProjectConfiguration configuration, IPlugin plugin)
		{
			configuration.RegisterPlugin(plugin);
		}
	}
}
