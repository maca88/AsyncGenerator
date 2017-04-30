using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Transformation;
using NUnit.Framework;

namespace AsyncGenerator.Tests
{
	public abstract class BaseFixture<T>
	{
		protected BaseFixture(string folderPath = null)
		{
			var ns = GetType().Namespace ?? "";
			InputFolderPath = folderPath ?? $"{string.Join("/", ns.Split('.').Skip(2))}/Input";
		}

		public string InputFolderPath { get; }

		public AsyncCodeConfiguration Configure(Action<IProjectConfiguration> action = null)
		{
			// BaseDirectory ends with a backslash when running with Visual Studio (Test Explorer), but when running with 
			// Reshaper (Unit Test Session) there is no backslash at the end
			var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			if (!baseDirectory.EndsWith(@"\"))
			{
				baseDirectory += @"\";
			}
			var slnFilePath = Path.GetFullPath(baseDirectory + @"..\..\..\AsyncGenerator.sln");
			return AsyncCodeConfiguration.Create()
				.ConfigureSolution(slnFilePath, c => c
					.ConfigureProject("AsyncGenerator.Tests", p =>
					{
						p.ConfigureAnalyzation(a => a
							.DocumentSelection(o => string.Join("/", o.Folders) == InputFolderPath)
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

		private string GetMethodName(LambdaExpression expression)
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

	}
}
