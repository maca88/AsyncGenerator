using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Extensions;
using AsyncGenerator.Internal;
using AsyncGenerator.Transformation;
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
					// Initialize plugins
					foreach (var registeredPlugin in projectData.Configuration.RegisteredPlugins)
					{
						await registeredPlugin.Initialize(projectData.Project).ConfigureAwait(false);
					}
					// Analyze project
					var analyzeConfig = projectData.Configuration.AnalyzeConfiguration;
					var analyzationResult = await AnalyzeProject(projectData).ConfigureAwait(false);
					foreach (var action in analyzeConfig.Callbacks.AfterAnalyzation)
					{
						action(analyzationResult);
					}

					// Transform documents
					var transformConfig = projectData.Configuration.TransformConfiguration;
					//TransformProject(analyzationResult, transformConfig);
				}
			}

			//conf.SolutionConfigurations.First().ProjectConfigurations.First().TransformConfiguration.
		}

		protected virtual Task<IProjectAnalyzationResult> AnalyzeProject(ProjectData projectData)
		{
			var analyzer = new ProjectAnalyzer(projectData);
			return analyzer.Analyze();
		}

		protected virtual void TransformProject(IProjectAnalyzationResult analyzationResult, ProjectTransformConfiguration configuration)
		{
			var transformer = new ProjectTransformer(configuration);
			transformer.Transform(analyzationResult);
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
	}
}
