using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;

namespace AsyncGenerator.Tests
{
	public abstract class BaseTest
	{
		protected BaseTest(string folderPath)
		{
			FolderPath = folderPath;
		}

		public string FolderPath { get; }

		public IAsyncCodeConfiguration Configure(string fileName, Action<IProjectConfiguration> action = null)
		{
			var slnFilePath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\AsyncGenerator.sln");
			return AsyncCodeConfiguration.Create()
				.ConfigureSolution(slnFilePath, c =>
					c.ConfigureProject("AsyncGenerator.Tests", p =>
					{
						p.ConfigureAnalyzation(a => a
							.DocumentSelectionPredicate(o => string.Join("/", o.Folders) == FolderPath && o.Name == fileName + ".cs")
							//.ScanMethodBody(true)
							);
						action?.Invoke(p);
					})

				);
		}

		public string GetMethodName<T>(Expression<Func<T, object>> expression)
		{
			return GetMethodName((LambdaExpression)expression);
		}

		public string GetMethodName<T>(Expression<Func<T, Action>> expression)
		{
			return GetMethodName((LambdaExpression)expression);
		}

		public string GetMethodName<T>(Expression<Func<T, Delegate>> expression)
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
			if (unaryExpression != null)
			{
				methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
				var constantExpression = methodCallExpression.Object as ConstantExpression;
				if (constantExpression == null)
				{
					return methodCallExpression.Method.Name;
				}
				var methodInfo = (MethodInfo)constantExpression.Value;
				return methodInfo.Name;
			}
			throw new NotSupportedException();
		}

	}
}
