using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Configuration.Yaml;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.FileConfiguration;
using AsyncGenerator.Core.Logging;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Internal;
using AsyncGenerator.Logging;
using log4net;
using log4net.Config;
using Microsoft.CodeAnalysis.MSBuild;
using NUnit.Framework;

namespace AsyncGenerator.Tests
{
	public abstract class BaseFixture
	{
		private class ReadOnlyProjectAndSolution
		{
			public ReadOnlyProjectAndSolution(Microsoft.CodeAnalysis.Project project, Microsoft.CodeAnalysis.Solution solution)
			{
				Project = project;
				Solution = solution;
			}

			public Microsoft.CodeAnalysis.Project Project { get; }

			public Microsoft.CodeAnalysis.Solution Solution { get; }
		}

		private static readonly Lazy<ReadOnlyProjectAndSolution> ReadOnlyProjectAndSolutionLazy =
			new Lazy<ReadOnlyProjectAndSolution>(SetupAndGetReadOnlyProjectAndSolution, LazyThreadSafetyMode.ExecutionAndPublication);
		private static readonly ILoggerFactory LoggerFactory;
		private static readonly ILogger Logger;

		static BaseFixture()
		{
#if NETCOREAPP
			var configPath = EnvironmentHelper.GetConfigurationFilePath();
			if (!string.IsNullOrEmpty(configPath))
			{
				var logRepository = LogManager.GetRepository(typeof(BaseFixture).Assembly);
				XmlConfigurator.Configure(logRepository, File.OpenRead(configPath));
			}
#endif
#if NET472
			XmlConfigurator.Configure();
#endif
			EnvironmentHelper.Setup();
			LoggerFactory = new Log4NetLoggerFactory();
			Logger = LoggerFactory.GetLogger(nameof(AsyncGenerator));
		}

		protected BaseFixture(string folderPath = null)
		{
			var ns = GetType().Namespace ?? "";
			InputFolderPath = folderPath ?? $"{string.Join("/", ns.Split('.').Skip(2))}/Input";
		}

		public string InputFolderPath { get; }

		public static string GetBaseDirectory()
		{
			return AppDomain.CurrentDomain.BaseDirectory;
		}

		public static string GetExternalProjectDirectory(string name)
		{
			return Path.GetFullPath(Path.Combine(GetBaseDirectory(), "..", "..", "..", "..", "ExternalProjects", name));
		}

		public static string GetTestProjectPath(string name)
		{
			return Path.GetFullPath(Path.Combine(GetBaseDirectory(), "..", "..", "..", "..", "AsyncGenerator.TestProjects", name, $"{name}.csproj"));
		}

		public static string GetTestSolutionPath(string name)
		{
			return Path.GetFullPath(Path.Combine(GetBaseDirectory(), "..", "..", "..", "..", "AsyncGenerator.TestProjects", $"{name}.sln"));
		}

		public async Task ReadonlyTest(Action<IFluentProjectConfiguration> action = null)
		{
			var configuration = Configure(action).ProjectConfigurations.Single();
			var projectData = AsyncCodeGenerator.CreateProjectData(ReadOnlyProjectAndSolutionLazy.Value.Project, configuration);
			await AsyncCodeGenerator.GenerateProject(projectData, LoggerFactory, Logger).ConfigureAwait(false);
		}

		public async Task ReadonlyTest(string fileName, Action<IFluentProjectConfiguration> action = null)
		{
			var configuration = Configure(fileName, action).SolutionConfigurations.First();
			var solutionData = AsyncCodeGenerator.CreateSolutionData(ReadOnlyProjectAndSolutionLazy.Value.Solution, configuration);
			var project = solutionData.GetProjects().Single();
			await AsyncCodeGenerator.GenerateProject(project, LoggerFactory, Logger).ConfigureAwait(false);
		}

		public async Task YamlReadonlyTest(string yamlConfig, Action<IFluentProjectConfiguration> action = null)
		{
			var configuration = ConfigureByYaml(yamlConfig, null, action).ProjectConfigurations.Single();
			var projectData = AsyncCodeGenerator.CreateProjectData(ReadOnlyProjectAndSolutionLazy.Value.Project, configuration);
			await AsyncCodeGenerator.GenerateProject(projectData, LoggerFactory, Logger).ConfigureAwait(false);
		}

		public async Task YamlReadonlyTest(string fileName, string yamlConfig, Action<IFluentProjectConfiguration> action = null)
		{
			var configuration = ConfigureByYaml(yamlConfig, fileName, action).ProjectConfigurations.Single();
			var projectData = AsyncCodeGenerator.CreateProjectData(ReadOnlyProjectAndSolutionLazy.Value.Project, configuration);
			await AsyncCodeGenerator.GenerateProject(projectData, LoggerFactory, Logger).ConfigureAwait(false);
		}

		public async Task XmlReadonlyTest(string xmlConfig, Action<IFluentProjectConfiguration> action = null)
		{
			var configuration = ConfigureByXml(xmlConfig, null, action).ProjectConfigurations.Single();
			var projectData = AsyncCodeGenerator.CreateProjectData(ReadOnlyProjectAndSolutionLazy.Value.Project, configuration);
			await AsyncCodeGenerator.GenerateProject(projectData, LoggerFactory, Logger).ConfigureAwait(false);
		}

		public async Task XmlReadonlyTest(string fileName, string xmlConfig, Action<IFluentProjectConfiguration> action = null)
		{
			var configuration = ConfigureByXml(xmlConfig, fileName, action).ProjectConfigurations.Single();
			var projectData = AsyncCodeGenerator.CreateProjectData(ReadOnlyProjectAndSolutionLazy.Value.Project, configuration);
			await AsyncCodeGenerator.GenerateProject(projectData, LoggerFactory, Logger).ConfigureAwait(false);
		}

		public virtual AsyncCodeConfiguration Configure(Action<IFluentProjectConfiguration> action = null)
		{
			var filePath = Path.GetFullPath(Path.Combine(GetBaseDirectory(), "..", "..", "..", "AsyncGenerator.Tests.csproj"));
			
			return AsyncCodeConfiguration.Create()
				.ConfigureProject(filePath, p =>
				{
					p.ConfigureAnalyzation(a => a
						.DocumentSelection(o => string.Join("/", o.Folders) == InputFolderPath)
					);
					action?.Invoke(p);
				})
				;
		}

		public AsyncCodeConfiguration ConfigureByYaml(string yamlConfig, string fileName = null, Action<IFluentProjectConfiguration> action = null)
		{
			return AsyncCodeConfiguration.Create()
				.ConfigureFromStream(GenerateStreamFromString(yamlConfig), new TestProjectYamlFileConfigurator(InputFolderPath, fileName, action));
		}

		public AsyncCodeConfiguration ConfigureByXml(string xmlConfig, string fileName = null, Action<IFluentProjectConfiguration> action = null)
		{
			return AsyncCodeConfiguration.Create()
				.ConfigureFromStream(GenerateStreamFromString(xmlConfig), new TestProjectXmlFileConfigurator(InputFolderPath, fileName, action));
		}

		public AsyncCodeConfiguration Configure(string fileName, Action<IFluentProjectConfiguration> action = null)
		{
			var slnFilePath = Path.GetFullPath(Path.Combine(GetBaseDirectory(), "..", "..", "..", "..", "AsyncGenerator.sln"));
			return AsyncCodeConfiguration.Create()
				.ConfigureSolution(slnFilePath, c => c
					.ConfigureProject("AsyncGenerator.Tests", p =>
					{
						p.ConfigureAnalyzation(a => a
							.DocumentSelection(o => string.Join("/", o.Folders) == InputFolderPath && o.Name == fileName + ".cs")
						);
						action?.Invoke(p);
					})

				);
		}

		public string GetOutputFile(string fileName)
		{
			var asm = Assembly.GetExecutingAssembly();
			var resource = $"{GetType().Namespace}.Output.{fileName}.txt";
			using (var stream = asm.GetManifestResourceStream(resource))
			{
				if (stream == null) return string.Empty;
				var reader = new StreamReader(stream);
				return reader.ReadToEnd();
			}
		}

		public void AssertValidAnnotations(IProjectTransformationResult projectResult)
		{
			foreach (var documentResult in projectResult.Documents)
			{
				var rootNode = documentResult.Transformed;
				Assert.AreEqual(1, rootNode.GetAnnotatedNodes(documentResult.Annotation).ToList().Count);
				foreach (var namespaceResult in documentResult.TransformedNamespaces)
				{
					Assert.AreEqual(1, rootNode.GetAnnotatedNodes(namespaceResult.Annotation).ToList().Count);
					foreach (var typeResult in namespaceResult.TransformedTypes)
					{
						Assert.AreEqual(1, rootNode.GetAnnotatedNodes(typeResult.Annotation).ToList().Count);
						foreach (var methodResult in typeResult.TransformedMethods)
						{
							Assert.AreEqual(1, rootNode.GetAnnotatedNodes(methodResult.Annotation).ToList().Count);
						}
					}
				}
				foreach (var typeResult in documentResult.TransformedTypes)
				{
					Assert.AreEqual(1, rootNode.GetAnnotatedNodes(typeResult.Annotation).ToList().Count);
				}
			}
		}

		public void CheckMethodsConversion(IEnumerable<IMethodAnalyzationResult> methodAnalyzationResults)
		{
			foreach (var method in methodAnalyzationResults)
			{
				Assert.AreNotEqual(MethodConversion.Smart, method.Conversion);
				Assert.AreNotEqual(MethodConversion.Unknown, method.Conversion);
			}
		}

		public string GetMethodName(Expression<Action> expression)
		{
			return GetMethodName((LambdaExpression)expression);
		}

		public string GetMethodName<TType>(Expression<Action<TType>> expression)
		{
			return GetMethodName((LambdaExpression)expression);
		}

		public string GetMethodName<TType>(Expression<Func<TType, object>> expression)
		{
			return GetMethodName((LambdaExpression)expression);
		}

		public string GetMethodName<TType>(Expression<Func<TType, Action>> expression)
		{
			return GetMethodName((LambdaExpression)expression);
		}

		public string GetMethodName<TType>(Expression<Func<TType, Delegate>> expression)
		{
			return GetMethodName((LambdaExpression)expression);
		}

		protected string GetMethodName(LambdaExpression expression)
		{
			var methodCallExpression = expression.Body as MethodCallExpression;
			if (methodCallExpression != null)
			{
				return methodCallExpression.Method.Name;
			}
			var unaryExpression = expression.Body as UnaryExpression;
			if (unaryExpression == null)
			{
				throw new NotSupportedException();
			}
			methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
			var constantExpression = methodCallExpression.Object as ConstantExpression;
			if (constantExpression == null)
			{
				return methodCallExpression.Method.Name;
			}
			var methodInfo = (MethodInfo)constantExpression.Value;
			return methodInfo.Name;
		}

		private static Microsoft.CodeAnalysis.Project OpenProject()
		{
			var filePath = Path.GetFullPath(Path.Combine(GetBaseDirectory(), "..", "..", "..", "AsyncGenerator.Tests.csproj"));
			var workspace = AsyncCodeGenerator.CreateWorkspace(null);
			return AsyncCodeGenerator.OpenProject(workspace, filePath, ImmutableArray<Predicate<string>>.Empty, Logger).GetAwaiter().GetResult();
		}

		private static Microsoft.CodeAnalysis.Solution OpenSolution()
		{
			var filePath = Path.GetFullPath(Path.Combine(GetBaseDirectory(), "..", "..", "..", "..", "AsyncGenerator.sln"));
			var workspace = AsyncCodeGenerator.CreateWorkspace(null);
			return AsyncCodeGenerator.OpenSolution(workspace, filePath, ImmutableArray<Predicate<string>>.Empty, Logger).GetAwaiter().GetResult();
		}

		private static ReadOnlyProjectAndSolution SetupAndGetReadOnlyProjectAndSolution()
		{
			var project = OpenProject();
			var solution = OpenSolution();
			return new ReadOnlyProjectAndSolution(project, solution);
		}

		private static Stream GenerateStreamFromString(string value)
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
		}

		private class TestProjectYamlFileConfigurator : TestProjectFileConfigurator
		{
			public TestProjectYamlFileConfigurator(string inputFolderPath, string fileName, Action<IFluentProjectConfiguration> configureProjectAction) 
				: base(new YamlFileConfigurator(), inputFolderPath, fileName, configureProjectAction)
			{
			}
		}

		private class TestProjectXmlFileConfigurator : TestProjectFileConfigurator
		{
			public TestProjectXmlFileConfigurator(string inputFolderPath, string fileName, Action<IFluentProjectConfiguration> configureProjectAction)
				: base(new XmlFileConfigurator(), inputFolderPath, fileName, configureProjectAction)
			{
			}
		}

		private abstract class TestProjectFileConfigurator : IFileConfigurator
		{
			private readonly string _inputFolderPath;
			private readonly string _fileName;
			private readonly Action<IFluentProjectConfiguration> _configureProjectAction;
			private readonly IFileConfigurator _realConfigurator;

			protected TestProjectFileConfigurator(IFileConfigurator realConfigurator, string inputFolderPath, string fileName, Action<IFluentProjectConfiguration> configureProjectAction)
			{
				_fileName = fileName;
				_inputFolderPath = inputFolderPath;
				_configureProjectAction = configureProjectAction;
				_realConfigurator = realConfigurator;
			}

			public virtual Core.FileConfiguration.AsyncGenerator Parse(string content)
			{
				var config = _realConfigurator.Parse(content);
				foreach (var testProject in config.Projects.Where(o => o.FilePath == "AsyncGenerator.Tests.csproj"))
				{
					testProject.FilePath = Path.GetFullPath(Path.Combine(GetBaseDirectory(), "..", "..", "..", "AsyncGenerator.Tests.csproj"));
				}
				return config;
			}

			public void Configure(Core.FileConfiguration.AsyncGenerator configuration, Solution solution, IFluentSolutionConfiguration solutionConfiguration,
				Assembly assembly)
			{
				_realConfigurator.Configure(configuration, solution, solutionConfiguration, assembly);
			}

			public virtual void Configure(Core.FileConfiguration.AsyncGenerator configuration, Project project, IFluentProjectConfiguration projectConfiguration,
				Assembly assembly)
			{
				_realConfigurator.Configure(configuration, project, projectConfiguration, assembly);

				if (!string.IsNullOrEmpty(_fileName))
				{
					projectConfiguration.ConfigureAnalyzation(a => a
						.DocumentSelection(o => string.Join("/", o.Folders) == _inputFolderPath && o.Name == _fileName + ".cs")
					);
				}
				else
				{
					projectConfiguration.ConfigureAnalyzation(a => a
						.DocumentSelection(o => string.Join("/", o.Folders) == _inputFolderPath)
					);
				}
				_configureProjectAction?.Invoke(projectConfiguration);
			}
		}
	}

	public abstract class BaseFixture<T> : BaseFixture
	{
		protected BaseFixture(string folderPath = null) : base(folderPath)
		{
		}

		public string GetMethodName(Expression<Action<T>> expression)
		{
			return GetMethodName((LambdaExpression)expression);
		}

		public string GetMethodName(Expression<Func<T, object>> expression)
		{
			return GetMethodName((LambdaExpression)expression);
		}

		public string GetMethodName(Expression<Func<T, Action>> expression)
		{
			return GetMethodName((LambdaExpression)expression);
		}

		public string GetMethodName(Expression<Func<T, Delegate>> expression)
		{
			return GetMethodName((LambdaExpression)expression);
		}
	}
}
