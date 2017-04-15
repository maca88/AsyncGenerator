using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator
{
	public class DocumentData : IDocumentAnalyzationResult
	{
		public DocumentData(ProjectData projectData, Document document, CompilationUnitSyntax node, SemanticModel semanticModel)
		{
			ProjectData = projectData;
			Document = document;
			Node = node;
			SemanticModel = semanticModel;
			GlobalNamespaceData = new NamespaceData(this, SemanticModel.Compilation.GlobalNamespace, null);
		}

		public Document Document { get; }

		public string FilePath => Document.FilePath;

		public ProjectData ProjectData { get; }

		public CompilationUnitSyntax Node { get; }

		public SemanticModel SemanticModel { get; }

		public NamespaceData GlobalNamespaceData { get; }

		public ConcurrentDictionary<NamespaceDeclarationSyntax, NamespaceData> NamespaceData { get; } = new ConcurrentDictionary<NamespaceDeclarationSyntax, NamespaceData>();

		/// <summary>
		/// Iterate through all type data from top to bottom
		/// </summary>
		public IEnumerable<TypeData> GetAllTypeDatas(Func<TypeData, bool> predicate = null)
		{
			return NamespaceData.Values
				.SelectMany(o => o.GetSelfAndDescendantsNamespaceData())
				.SelectMany(o => o.TypeData.Values)
				.Union(GlobalNamespaceData.TypeData.Values)
				.SelectMany(o => o.GetSelfAndDescendantsTypeData(predicate));
		}

		public object GetNodeData(SyntaxNode node,
			bool create = false,
			NamespaceData namespaceData = null,
			TypeData typeData = null,
			MethodData methodData = null)
		{
			AnonymousFunctionData functionData = null;
			SyntaxNode endNode;
			if (methodData != null)
			{
				endNode = methodData.Node;
			}
			else if (typeData != null)
			{
				endNode = typeData.Node;
			}
			else if (namespaceData != null)
			{
				endNode = namespaceData.Node;
			}
			else
			{
				endNode = Node;
			}

			foreach (var n in node.AncestorsAndSelf()
				.TakeWhile(o => !ReferenceEquals(o, endNode))
				.Where(
					o =>
						o.IsKind(SyntaxKind.ParenthesizedLambdaExpression) ||
						o.IsKind(SyntaxKind.AnonymousMethodExpression) ||
						o.IsKind(SyntaxKind.SimpleLambdaExpression) ||

						o.IsKind(SyntaxKind.MethodDeclaration) ||

						o.IsKind(SyntaxKind.ClassDeclaration) ||
						o.IsKind(SyntaxKind.InterfaceDeclaration) ||
						o.IsKind(SyntaxKind.StructDeclaration) ||

						o.IsKind(SyntaxKind.NamespaceDeclaration)
						/*
						o is AnonymousFunctionExpressionSyntax || 
						o is MethodDeclarationSyntax || 
						o is TypeDeclarationSyntax ||
						o is NamespaceDeclarationSyntax*/)
				.Reverse())
			{
				switch (n.Kind())
				{
					case SyntaxKind.ParenthesizedLambdaExpression:
					case SyntaxKind.AnonymousMethodExpression:
					case SyntaxKind.SimpleLambdaExpression:
						if (methodData == null)
						{
							throw new InvalidOperationException($"Anonymous function {n} is declared outside a {nameof(TypeDeclarationSyntax)}");
						}
						var symbol = SemanticModel.GetSymbolInfo(n).Symbol as IMethodSymbol;
						functionData = functionData != null 
							? functionData.GetNestedAnonymousFunctionData((AnonymousFunctionExpressionSyntax)n, symbol, create)
							: methodData.GetAnonymousFunctionData((AnonymousFunctionExpressionSyntax) n, symbol, create);
						if (functionData == null)
						{
							return null;
						}
						break;
					case SyntaxKind.MethodDeclaration:
						if (typeData == null)
						{
							throw new InvalidOperationException($"Method {n} is declared outside a {nameof(TypeDeclarationSyntax)}");
						}
						var methodNode = (MethodDeclarationSyntax) n;
						var methodSymbol = SemanticModel.GetDeclaredSymbol(methodNode);
						methodData = typeData.GetMethodData(methodNode, methodSymbol, create);
						if (methodData == null)
						{
							return null;
						}
						break;
					case SyntaxKind.ClassDeclaration:
					case SyntaxKind.InterfaceDeclaration:
					case SyntaxKind.StructDeclaration:
						if (namespaceData == null)
						{
							namespaceData = GlobalNamespaceData;
						}
						var typeNode = (TypeDeclarationSyntax)n;
						var typeSymbol = SemanticModel.GetDeclaredSymbol(typeNode);
						typeData = typeData != null 
							? typeData.GetNestedTypeData(typeNode, typeSymbol, create) 
							: namespaceData.GetTypeData(typeNode, typeSymbol, create);
						if (typeData == null)
						{
							return null;
						}
						break;
					case SyntaxKind.NamespaceDeclaration:
						var namespaceNode = (NamespaceDeclarationSyntax)n;
						var namespaceSymbol = SemanticModel.GetDeclaredSymbol(namespaceNode);
						namespaceData = namespaceData != null 
							? namespaceData.GetNestedNamespaceData(namespaceNode, namespaceSymbol, create)
							: GetNamespaceData(namespaceNode, namespaceSymbol, create);
						if (namespaceData == null)
						{
							return null;
						}
						break;
				}
			}

			switch (node.Kind())
			{
				case SyntaxKind.ParenthesizedLambdaExpression:
				case SyntaxKind.AnonymousMethodExpression:
				case SyntaxKind.SimpleLambdaExpression:
					return functionData;
				case SyntaxKind.MethodDeclaration:
					return methodData;
				case SyntaxKind.ClassDeclaration:
				case SyntaxKind.InterfaceDeclaration:
				case SyntaxKind.StructDeclaration:
					return typeData;
				case SyntaxKind.NamespaceDeclaration:
					return namespaceData;
				default:
					throw new InvalidOperationException($"Invalid node kind {Enum.GetName(typeof(SyntaxKind), node.Kind())}");
			}
		}

		public async Task<FunctionData> GetAnonymousFunctionOrMethodData(IMethodSymbol symbol)
		{
			var syntax = symbol.DeclaringSyntaxReferences.Single(o => o.SyntaxTree.FilePath == FilePath);
			var node = await syntax.GetSyntaxAsync().ConfigureAwait(false);
			if (node.IsKind(SyntaxKind.MethodDeclaration))
			{
				return (MethodData)GetNodeData(node);
			}
			return (AnonymousFunctionData)GetNodeData(node);
		}

		#region AnonymousFunctionData

		public AnonymousFunctionData GetAnonymousFunctionData(AnonymousFunctionExpressionSyntax node)
		{
			return (AnonymousFunctionData)GetNodeData(node);
		}

		public AnonymousFunctionData GetOrCreateAnonymousFunctionData(AnonymousFunctionExpressionSyntax node, MethodData methodData = null)
		{
			return (AnonymousFunctionData)GetNodeData(node, true, methodData: methodData);
		}

		#endregion

		#region MethodData

		public async Task<MethodData> GetMethodData(IMethodSymbol symbol)
		{
			var syntax = symbol.DeclaringSyntaxReferences.Single(o => o.SyntaxTree.FilePath == FilePath);
			var node = (MethodDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);
			return (MethodData)GetNodeData(node);
		}

		public MethodData GetMethodData(MethodDeclarationSyntax node)
		{
			return (MethodData)GetNodeData(node);
		}

		public MethodData GetOrCreateMethodData(MethodDeclarationSyntax node, TypeData typeData = null)
		{
			return (MethodData)GetNodeData(node, true, typeData: typeData);
		}

		#endregion

		#region TypeData

		public TypeData GetOrCreateTypeData(TypeDeclarationSyntax node)
		{
			return (TypeData)GetNodeData(node, true);
		}

		public TypeData GetTypeData(TypeDeclarationSyntax node)
		{
			return (TypeData)GetNodeData(node);
		}

		#endregion

		#region NamespaceData

		public NamespaceData GetOrCreateNamespaceData(NamespaceDeclarationSyntax node)
		{
			return (NamespaceData)GetNodeData(node, true);
		}

		public NamespaceData GetNamespaceData(NamespaceDeclarationSyntax node)
		{
			return (NamespaceData)GetNodeData(node);
		}

		#endregion

		private NamespaceData GetNamespaceData(NamespaceDeclarationSyntax namespaceNode, INamespaceSymbol namespaceSymbol, bool create)
		{
			NamespaceData namespaceData;
			if (NamespaceData.TryGetValue(namespaceNode, out namespaceData))
			{
				return namespaceData;
			}
			return !create ? null : NamespaceData.GetOrAdd(namespaceNode, syntax => new NamespaceData(this, namespaceSymbol, namespaceNode));
		}

		// TODO: DEBUG
		public ISymbol GetEnclosingSymbol(ReferenceLocation reference)
		{
			var enclosingSymbol = SemanticModel.GetEnclosingSymbol(reference.Location.SourceSpan.Start);

			for (var current = enclosingSymbol; current != null; current = current.ContainingSymbol)
			{
				if (current.Kind == SymbolKind.Field)
				{
					return current;
				}

				if (current.Kind == SymbolKind.Property)
				{
					return current;
				}

				if (current.Kind == SymbolKind.Method)
				{
					var method = (IMethodSymbol)current;
					if (method.IsAccessor())
					{
						return method.AssociatedSymbol;
					}
					return method;
				}
			}
			//TODO: reference to a cref
			return null;
		}

		#region IDocumentAnalyzationResult

		private IReadOnlyList<INamespaceAnalyzationResult> _cachedNamespaces;
		IReadOnlyList<INamespaceAnalyzationResult> IDocumentAnalyzationResult.Namespaces => _cachedNamespaces ?? (_cachedNamespaces = NamespaceData.Values.ToImmutableArray());


		INamespaceAnalyzationResult IDocumentAnalyzationResult.GlobalNamespace => GlobalNamespaceData;

		#endregion
	}
}
