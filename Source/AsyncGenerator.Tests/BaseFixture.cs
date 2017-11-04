using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Transformation;
using log4net.Config;
using Microsoft.VisualStudio.Setup.Configuration;
using NUnit.Framework;

namespace AsyncGenerator.Tests
{
	public abstract class BaseFixture
	{
		private static readonly Lazy<Microsoft.CodeAnalysis.Project> ReadonlyProject =
			new Lazy<Microsoft.CodeAnalysis.Project>(OpenProject, LazyThreadSafetyMode.ExecutionAndPublication);
		private static readonly Lazy<Microsoft.CodeAnalysis.Solution> ReadonlySolution =
			new Lazy<Microsoft.CodeAnalysis.Solution>(OpenSolution, LazyThreadSafetyMode.ExecutionAndPublication);

		static BaseFixture()
		{
			ConfigureMSBuild();
		}

		protected BaseFixture(string folderPath = null)
		{
			XmlConfigurator.Configure();
			var ns = GetType().Namespace ?? "";
			InputFolderPath = folderPath ?? $"{string.Join("/", ns.Split('.').Skip(2))}/Input";
		}

		public string InputFolderPath { get; }

		public static string GetBaseDirectory()
		{
			return AppDomain.CurrentDomain.BaseDirectory;
		}

		public async Task ReadonlyTest(Action<IFluentProjectConfiguration> action = null)
		{
			var configuration = Configure(action).ProjectConfigurations.Single();
			var projectData = AsyncCodeGenerator.CreateProjectData(ReadonlyProject.Value, configuration);
			await AsyncCodeGenerator.GenerateProject(projectData).ConfigureAwait(false);
		}

		public async Task ReadonlyTest(string fileName, Action<IFluentProjectConfiguration> action = null)
		{
			var configuration = Configure(fileName, action).SolutionConfigurations.First();
			var solutionData = AsyncCodeGenerator.CreateSolutionData(ReadonlySolution.Value, configuration);
			var project = solutionData.GetProjects().Single();
			await AsyncCodeGenerator.GenerateProject(project).ConfigureAwait(false);
		}

		public AsyncCodeConfiguration Configure(Action<IFluentProjectConfiguration> action = null)
		{
			var filePath = Path.GetFullPath(Path.Combine(GetBaseDirectory(), "..", "..", "AsyncGenerator.Tests.csproj"));
			
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

		public AsyncCodeConfiguration Configure(string fileName, Action<IFluentProjectConfiguration> action = null)
		{
			var slnFilePath = Path.GetFullPath(Path.Combine(GetBaseDirectory(), "..", "..", "..", "AsyncGenerator.sln"));
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

		// Copied from here: https://github.com/T4MVC/R4MVC/commit/ae2fd5d8f3ab60708419d37c8a42d237d86d3061#diff-89dd7d1659695edb3702bfe879b34b09R61
		// in order to fix the issue https://github.com/Microsoft/msbuild/issues/2369 -> https://github.com/Microsoft/msbuild/issues/2030
		private static void ConfigureMSBuild()
		{
			if (Type.GetType("Mono.Runtime") != null)
				return;
			
			var query = new SetupConfiguration();
			var query2 = (ISetupConfiguration2)query;

			try
			{
				if (query2.GetInstanceForCurrentProcess() is ISetupInstance2 instance)
				{
					Environment.SetEnvironmentVariable("VSINSTALLDIR", instance.GetInstallationPath());
					Environment.SetEnvironmentVariable("VisualStudioVersion", @"15.0");
					return;
				}
			}
			catch { }

			var instances = new ISetupInstance[1];
			var e = query2.EnumAllInstances();
			int fetched;
			do
			{
				e.Next(1, instances, out fetched);
				if (fetched > 0)
				{
					var instance = instances[0] as ISetupInstance2;
					if (instance.GetInstallationVersion().StartsWith("15."))
					{
						Environment.SetEnvironmentVariable("VSINSTALLDIR", instance.GetInstallationPath());
						Environment.SetEnvironmentVariable("VisualStudioVersion", @"15.0");
						return;
					}
				}
			}
			while (fetched > 0);
		}

		private static Microsoft.CodeAnalysis.Project OpenProject()
		{
			var filePath = Path.GetFullPath(Path.Combine(GetBaseDirectory(), "..", "..", "AsyncGenerator.Tests.csproj"));
			var workspace = AsyncCodeGenerator.CreateWorkspace();
			return AsyncCodeGenerator.OpenProject(workspace, filePath, ImmutableArray<Predicate<string>>.Empty).GetAwaiter().GetResult();
		}

		private static Microsoft.CodeAnalysis.Solution OpenSolution()
		{
			var filePath = Path.GetFullPath(Path.Combine(GetBaseDirectory(), "..", "..", "..", "AsyncGenerator.sln"));
			var workspace = AsyncCodeGenerator.CreateWorkspace();
			return AsyncCodeGenerator.OpenSolution(workspace, filePath, ImmutableArray<Predicate<string>>.Empty).GetAwaiter().GetResult();
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
