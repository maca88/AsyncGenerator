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
using NUnit.Framework;

namespace AsyncGenerator.Tests
{
	public abstract class BaseTest<T>
	{
		protected BaseTest(string folderPath = null)
		{
			var ns = GetType().Namespace ?? "";
			InputFolderPath = folderPath ?? $"{string.Join("/", ns.Split('.').Skip(2))}/Input";
		}

		public string InputFolderPath { get; }

		public AsyncCodeConfiguration Configure(Action<IProjectConfiguration> action = null)
		{
			var slnFilePath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\AsyncGenerator.sln");
			return AsyncCodeConfiguration.Create()
				.ConfigureSolution(slnFilePath, c => c
					.ConfigureProject("AsyncGenerator.Tests", p =>
					{
						p.ConfigureAnalyzation(a => a
							.DocumentSelection(o => string.Join("/", o.Folders) == InputFolderPath)
							//.ScanMethodBody(true)
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
