using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFluentProjectTransformConfiguration
	{
		/// <summary>
		/// Disable the transformation process.
		/// </summary>
		IFluentProjectTransformConfiguration Disable();

		/// <summary>
		/// Set the name of the folder where all newly generated files will be stored
		/// </summary>
		IFluentProjectTransformConfiguration AsyncFolder(string folderName);

		/// <summary>
		/// Set the syntax node that will be used as an argument to the method <see cref="Task.ConfigureAwait"/>, if null the invocation is not generated.
		/// Default is null.
		/// </summary>
		/// <param name="node">The node that will be used as an argument to the <see cref="Task.ConfigureAwait"/> method.</param>
		IFluentProjectTransformConfiguration ConfigureAwaitArgument(ExpressionSyntax node);

		/// <summary>
		/// Enable or disable the generation of local functions instead of private methods (eg. method tail split).
		/// Default is false.
		/// </summary>
		IFluentProjectTransformConfiguration LocalFunctions(bool enabled);

		/// <summary>
		/// Set the async locking system of the project. Used only if the project contains methods with <see cref="MethodImplOptions.Synchronized"/> option.
		/// </summary>
		/// <param name="fullTypeName">The full type name of the async lock</param>
		/// <param name="methodName">The method name that triggers the lock</param>
		/// <returns></returns>
		IFluentProjectTransformConfiguration AsyncLock(string fullTypeName, string methodName);

		/// <summary>
		/// Add a callback that is called when the transformation for the project is completed
		/// </summary>
		/// <param name="action">The action to call</param>
		/// <returns></returns>
		IFluentProjectTransformConfiguration AfterTransformation(Action<IProjectTransformationResult> action);


	}
}
