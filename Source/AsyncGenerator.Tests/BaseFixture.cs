using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Internal;
using AsyncGenerator.Transformation;
using log4net;
using log4net.Config;
#if NET461
using Microsoft.Build.MSBuildLocator;
#endif
using NUnit.Framework;

namespace AsyncGenerator.Tests
{
	public abstract class BaseFixture
	{
		protected BaseFixture(string folderPath = null)
		{
			EnvironmentHelper.Setup();
#if NETCORE2
			var logRepository = LogManager.GetRepository(typeof(BaseFixture).Assembly);
			XmlConfigurator.Configure(logRepository, File.OpenRead(EnvironmentHelper.GetConfigurationFilePath()));
#endif
#if NET461
			XmlConfigurator.Configure();
#endif
			var ns = GetType().Namespace ?? "";
			InputFolderPath = folderPath ?? $"{string.Join("/", ns.Split('.').Skip(2))}/Input";
		}

		public string InputFolderPath { get; }

		public string GetBaseDirectory()
		{
			// BaseDirectory ends with a backslash when running with Visual Studio (Test Explorer), but when running with 
			// Reshaper (Unit Test Session) there is no backslash at the end
			var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			if (!baseDirectory.EndsWith(@"\"))
			{
				baseDirectory += @"\";
			}
			return baseDirectory.Replace(".NETCore", "");
		}

		public AsyncCodeConfiguration Configure(Action<IFluentProjectConfiguration> action = null)
		{
#if NETCORE2
			var filePath = Path.GetFullPath(GetBaseDirectory() + @"..\..\..\..\AsyncGenerator.Tests\AsyncGenerator.Tests.csproj");
#else
			var filePath = Path.GetFullPath(GetBaseDirectory() + @"..\..\..\AsyncGenerator.Tests.csproj");
#endif
			return AsyncCodeConfiguration.Create()
				.ConfigureProject(filePath, p =>
				{
					p.ConfigureAnalyzation(a => a
						.DocumentSelection(o => string.Join("/", o.Folders) == InputFolderPath)
					);
					p.ConfigureTransformation(t => t // TODO: Remove
						.DocumentationComments(c => c
							.AddOrReplacePartialTypeComments(symbol =>
								$"/// <content>{Environment.NewLine}/// Contains generated async methods{Environment.NewLine}/// </content>"
							)
						)
					);
					action?.Invoke(p);
				})
				;
		}

		public AsyncCodeConfiguration Configure(string fileName, Action<IFluentProjectConfiguration> action = null)
		{
			var slnFilePath = Path.GetFullPath(GetBaseDirectory() + @"..\..\..\AsyncGenerator.sln");
			return AsyncCodeConfiguration.Create()
				.ConfigureSolution(slnFilePath, c => c
					.ConfigureProject("AsyncGenerator.Tests", p =>
					{
						p.ConfigureAnalyzation(a => a
							.DocumentSelection(o => string.Join("/", o.Folders) == InputFolderPath && o.Name == fileName + ".cs")
						);
						p.ConfigureTransformation(t => t // TODO: Remove
							.DocumentationComments(dc => dc
								.AddOrReplacePartialTypeComments(symbol =>
									$"/// <content>{Environment.NewLine}/// Contains generated async methods{Environment.NewLine}/// </content>"
								)
							)
						);
						action?.Invoke(p);
					})

				);
		}

		public string GetOutputFile(string fileName)
		{
			var asm = Assembly.GetExecutingAssembly();
#if NETCORE2
			var resource = $"{GetType().Namespace.Replace("AsyncGenerator.Tests", "AsyncGenerator.Tests.NETCore")}.Output.{fileName}.txt";
#else
			var resource = $"{GetType().Namespace}.Output.{fileName}.txt";
#endif
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
			if (expression.Body is MethodCallExpression methodCallExpression)
			{
				return methodCallExpression.Method.Name;
			}
			if (!(expression.Body is UnaryExpression unaryExpression))
			{
				throw new NotSupportedException();
			}
			methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
			if (!(methodCallExpression.Object is ConstantExpression constantExpression))
			{
				return methodCallExpression.Method.Name;
			}
			var methodInfo = (MethodInfo)constantExpression.Value;
			return methodInfo.Name;
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
