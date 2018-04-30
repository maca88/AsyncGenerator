using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// Prepends a <see cref="OperationCanceledException"/> catch block on try statements inside an async function that has a
	/// cancellation token
	/// </summary>
	internal class OperationCanceledExceptionFunctionTransformer : IMethodOrAccessorTransformer, IFunctionTransformer, IFunctionReferenceTransformer
	{
		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			return Task.CompletedTask;
		}

		public SyntaxNode TransformFunctionReference(SyntaxNode node, IFunctionAnalyzationResult funcResult,
			IFunctionReferenceAnalyzationResult funcReferenceResult, INamespaceTransformationMetadata namespaceMetadata)
		{
			if (funcReferenceResult is IBodyFunctionReferenceAnalyzationResult bodyFunctionReference && 
			    (bodyFunctionReference.PassCancellationToken || funcResult.GetMethodOrAccessor().AddCancellationTokenGuards))
			{
				// Mark the invocation node in order to add the OperationCanceledException catch block only if there is at least one async invocation
				// with a cancellation token passed as an argument or there will be a cancellation token guard added
				return node.WithAdditionalAnnotations(new SyntaxAnnotation(Annotations.AsyncCallWithTokenOrGuard));
			}
			return null;
		}

		public SyntaxNode Transform(IFunctionTransformationResult transformResult, ITypeTransformationMetadata typeMetadata,
			INamespaceTransformationMetadata namespaceMetadata)
		{
			var methodResult = transformResult.AnalyzationResult;
			var functionNode = transformResult.Transformed;
			if (
				!methodResult.Conversion.HasFlag(MethodConversion.ToAsync) ||
				methodResult.OmitAsync ||
				!methodResult.GetMethodOrAccessor().CancellationTokenRequired ||
				functionNode.GetFunctionBody() == null)
			{
				return null;
			}

			var rewriter = new OperationCanceledExceptionFunctionRewriter(transformResult.EndOfLineTrivia, namespaceMetadata);
			return rewriter.Visit(functionNode);
		}

		public MethodTransformerResult Transform(IMethodOrAccessorTransformationResult transformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			var methodResult = transformResult.AnalyzationResult;
			var methodNode = transformResult.Transformed;
			if (
				!methodResult.Conversion.HasFlag(MethodConversion.ToAsync) ||
				methodResult.OmitAsync ||
				!methodResult.CancellationTokenRequired ||
				methodNode.GetFunctionBody() == null)
			{
				return MethodTransformerResult.Skip;
			}

			var rewriter = new OperationCanceledExceptionFunctionRewriter(transformResult.EndOfLineTrivia, namespaceMetadata);
			methodNode = (MethodDeclarationSyntax)rewriter.VisitMethodDeclaration(methodNode);
			return MethodTransformerResult.Update(methodNode);
		}
	}
}
