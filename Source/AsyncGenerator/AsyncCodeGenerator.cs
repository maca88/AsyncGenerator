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
		public async Task GenerateAsync(AsyncCodeConfiguration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}
			foreach (var config in configuration.SolutionConfigurations)
			{
				var solutionData = await CreateSolutionData(config).ConfigureAwait(false);
				foreach (var projectData in solutionData.ProjectData.Values)
				{
					// Register internal plugins
					RegisterInternalPlugins(projectData.Configuration);
					
					// Initialize plugins
					foreach (var registeredPlugin in projectData.Configuration.RegisteredPlugins)
					{
						await registeredPlugin.Initialize(projectData.Project, projectData.Configuration).ConfigureAwait(false);
					}
					// Analyze project
					var analyzeConfig = projectData.Configuration.AnalyzeConfiguration;
					var analyzationResult = await AnalyzeProject(projectData).ConfigureAwait(false);
					foreach (var action in analyzeConfig.AfterAnalyzation)
					{
						action(analyzationResult);
					}

					// Transform documents
					var transformConfig = projectData.Configuration.TransformConfiguration;
					var transformResult = TransformProject(analyzationResult, transformConfig);
					foreach (var action in transformConfig.AfterTransformation)
					{
						action(transformResult);
					}
					projectData.Project = transformResult.Project; // updates also the solution

					// Compile
					var compileConfig = projectData.Configuration.CompileConfiguration;
					if (compileConfig != null)
					{
						var compilation = await projectData.Project.GetCompilationAsync();
						var emit = compilation.Emit(compileConfig.OutputPath, compileConfig.SymbolsPath, compileConfig.XmlDocumentationPath);
						if (!emit.Success)
						{
							var messages = string.Join(
								Environment.NewLine,
								emit.Diagnostics.Where(o => o.Severity == DiagnosticSeverity.Error).Select(o => o.GetMessage()));
							throw new InvalidOperationException(
								$"Generation for Project {transformResult.Project.Name} failed to generate a valid code. Errors:{Environment.NewLine}{messages}");
						}
					}
				}
				if (config.ApplyChanges)
				{
					solutionData.Workspace.TryApplyChanges(solutionData.Solution);
				}
			}
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

			var projectConfigs = configuration.ProjectConfigurations.ToDictionary(o => o.Name);
			foreach (var project in solution.Projects.Where(o => projectConfigs.ContainsKey(o.Name)))
			{
				var config = projectConfigs[project.Name];
				var projectData = new ProjectData(solutionData, project.Id, config);
				RemoveGeneratedDocuments(projectData);
				solutionData.ProjectData.AddOrUpdate(project.Id, projectData, (id, data) => projectData);
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
			configuration.RegisterPlugin(new IncludeFilePathTransformer());

			// Method transformers
			configuration.RegisterPlugin(new AsyncLockMethodTransformer());
			configuration.RegisterPlugin(new CancellationTokenMethodTransformer());
			configuration.RegisterPlugin(new SplitTailMethodTransformer());
		}
	}
}
