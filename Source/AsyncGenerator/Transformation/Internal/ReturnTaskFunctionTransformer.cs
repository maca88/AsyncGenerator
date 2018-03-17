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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	internal class ReturnTaskFunctionTransformer : IMethodOrAccessorTransformer, IFunctionTransformer
	{
		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			return Task.CompletedTask;
		}

		public SyntaxNode Transform(IFunctionTransformationResult transformResult, ITypeTransformationMetadata typeMetadata,
			INamespaceTransformationMetadata namespaceMetadata)
		{
			var methodResult = transformResult.AnalyzationResult;
			if (!methodResult.Conversion.HasFlag(MethodConversion.ToAsync))
			{
				return null;
			}
			var functionNode = transformResult.Transformed;
			if (functionNode.GetFunctionBody() == null)
			{
				return Update(functionNode, methodResult, namespaceMetadata);
			}
			if (methodResult.SplitTail || methodResult.PreserveReturnType || !methodResult.OmitAsync)
			{
				if (!methodResult.OmitAsync)
				{
					functionNode = functionNode.AddAsync();
				}
				return Update(functionNode, methodResult, namespaceMetadata);
			}
			var rewriter = new ReturnTaskFunctionRewriter(transformResult, namespaceMetadata);
			functionNode = rewriter.Visit(functionNode);
			return Update(functionNode, methodResult, namespaceMetadata);
		}

		public MethodTransformerResult Transform(IMethodOrAccessorTransformationResult transformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			var methodResult = transformResult.AnalyzationResult;
			if (!methodResult.Conversion.HasFlag(MethodConversion.ToAsync))
			{
				return MethodTransformerResult.Skip;
			}
			var methodNode = transformResult.Transformed;
			if (methodNode.GetFunctionBody() == null)
			{
				return Update(methodNode, methodResult, namespaceMetadata);
			}
			if (methodResult.SplitTail || methodResult.PreserveReturnType || !methodResult.OmitAsync)
			{
				if (!methodResult.OmitAsync)
				{
					methodNode = methodNode.AddAsync();
				}
				return Update(methodNode, methodResult, namespaceMetadata);
			}
			var rewriter = new ReturnTaskFunctionRewriter(transformResult, namespaceMetadata);
			methodNode = (MethodDeclarationSyntax)rewriter.VisitMethodDeclaration(methodNode);
			return Update(methodNode, methodResult, namespaceMetadata);
		}

		private MethodTransformerResult Update(MethodDeclarationSyntax methodNode,
			IMethodOrAccessorAnalyzationResult methodResult, INamespaceTransformationMetadata namespaceMetadata)
		{
			methodNode = methodNode.WithIdentifier(Identifier(methodResult.AsyncCounterpartName));
			if (!methodResult.PreserveReturnType && methodResult.Symbol.MethodKind != MethodKind.PropertySet)
			{
				methodNode = methodNode.ReturnAsTask(namespaceMetadata.TaskConflict);
			}
			return MethodTransformerResult.Update(methodNode);
		}

		private LocalFunctionStatementSyntax Update(LocalFunctionStatementSyntax functionNode,
			IFunctionAnalyzationResult analyzeResult, INamespaceTransformationMetadata namespaceMetadata)
		{
			functionNode = functionNode.WithIdentifier(Identifier(analyzeResult.AsyncCounterpartName));
			if (!analyzeResult.PreserveReturnType && analyzeResult.Symbol.MethodKind != MethodKind.PropertySet)
			{
				functionNode = functionNode.ReturnAsTask(namespaceMetadata.TaskConflict);
			}
			return functionNode;
		}

		private SyntaxNode Update(SyntaxNode functionNode,
			IFunctionAnalyzationResult methodResult, INamespaceTransformationMetadata namespaceMetadata)
		{
			if (functionNode is LocalFunctionStatementSyntax localFunction)
			{
				return Update(localFunction, methodResult, namespaceMetadata);
			}
			return functionNode;
		}
	}
}
