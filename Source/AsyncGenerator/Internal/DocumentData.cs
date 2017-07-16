using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Internal
{
	internal class DocumentData : IDocumentAnalyzationResult
	{
		private readonly SyntaxKind[] _validDataKinds = {
			// Function
			SyntaxKind.ParenthesizedLambdaExpression,
			SyntaxKind.AnonymousMethodExpression,
			SyntaxKind.SimpleLambdaExpression,
			SyntaxKind.LocalFunctionStatement,
			// Method
			SyntaxKind.MethodDeclaration,
			SyntaxKind.ConstructorDeclaration,
			SyntaxKind.DestructorDeclaration,
			SyntaxKind.OperatorDeclaration,
			SyntaxKind.ConversionOperatorDeclaration,
			// Property
			SyntaxKind.PropertyDeclaration,
			SyntaxKind.GetAccessorDeclaration,
			SyntaxKind.SetAccessorDeclaration,
			//SyntaxKind.FieldDeclaration,
			//SyntaxKind.DelegateDeclaration,
			// Type
			SyntaxKind.ClassDeclaration,
			SyntaxKind.InterfaceDeclaration,
			SyntaxKind.StructDeclaration,
			// Namespace
			SyntaxKind.NamespaceDeclaration
		};

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

		public ConcurrentDictionary<NamespaceDeclarationSyntax, NamespaceData> Namespaces { get; } = new ConcurrentDictionary<NamespaceDeclarationSyntax, NamespaceData>();

		public SyntaxNode GetNode()
		{
			return Node;
		} 

		/// <summary>
		/// Iterate through all type data from top to bottom
		/// </summary>
		public IEnumerable<TypeData> GetAllTypeDatas(Func<TypeData, bool> predicate = null)
		{
			return GetAllNamespaceDatas()
				.SelectMany(o => o.Types.Values)
				.SelectMany(o => o.GetSelfAndDescendantsTypeData(predicate));
		}

		/// <summary>
		/// Iterate through all namespace data from top to bottom
		/// </summary>
		public IEnumerable<NamespaceData> GetAllNamespaceDatas(Func<NamespaceData, bool> predicate = null)
		{
			return new[] {GlobalNamespaceData}
				.Union(Namespaces.Values
					.SelectMany(o => o.GetSelfAndDescendantsNamespaceData(predicate)));
		}

		public AbstractData GetNearestNodeData(SyntaxNode node, bool isCref = false)
		{
			var currentNode = node;
			if (isCref)
			{
				currentNode = Node.DescendantNodes()
					.OfType<MemberDeclarationSyntax>()
					.OrderByDescending(o => o.SpanStart)
					.First(o => o.FullSpan.Contains(node.FullSpan));
			}
			
			while (currentNode != null)
			{
				if (_validDataKinds.Contains(currentNode.Kind()))
				{
					return GetNodeData(currentNode);
				}
				currentNode = currentNode.Parent;
			}
			return null;
		}

		public AbstractData GetNodeData(SyntaxNode node,
			bool create = false,
			NamespaceData namespaceData = null,
			TypeData typeData = null,
			BaseMethodData baseMethodData = null)
		{
			FunctionData functionData = null;
			PropertyData propertyData = null;
			SyntaxNode endNode;
			if (baseMethodData != null)
			{
				endNode = baseMethodData.GetNode();
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
					o => _validDataKinds.Contains(o.Kind()))
				.Reverse())
			{
				switch (n.Kind())
				{
					case SyntaxKind.ParenthesizedLambdaExpression:
					case SyntaxKind.AnonymousMethodExpression:
					case SyntaxKind.SimpleLambdaExpression:
					case SyntaxKind.LocalFunctionStatement:
						if (baseMethodData == null)
						{
							// ParenthesizedLambda, AnonymousMethod and SimpleLambda can be also defined inside a type
							if (!n.IsKind(SyntaxKind.LocalFunctionStatement))
							{
								if (typeData != null)
								{
									return null; // TODO: A type can have one or many FuncionData so we need to register them
								}
							}
							throw new InvalidOperationException($"Anonymous function {n} is declared outside a {nameof(TypeDeclarationSyntax)}");
						}
						functionData = functionData != null 
							? functionData.GetChildFunction(n, SemanticModel, create)
							: baseMethodData.GetChildFunction(n, SemanticModel, create);
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
						baseMethodData = typeData.GetMethodData(methodNode, SemanticModel, create);
						if (baseMethodData == null)
						{
							return null;
						}
						break;
					case SyntaxKind.DestructorDeclaration:
					case SyntaxKind.ConstructorDeclaration:
					case SyntaxKind.OperatorDeclaration:
					case SyntaxKind.ConversionOperatorDeclaration:
						if (typeData == null)
						{
							throw new InvalidOperationException($"Method {n} is declared outside a {nameof(TypeDeclarationSyntax)}");
						}
						var baseMethodNode = (BaseMethodDeclarationSyntax)n;
						baseMethodData = typeData.GetSpecialMethodData(baseMethodNode, SemanticModel, create);
						if (baseMethodData == null)
						{
							return null;
						}
						break;
					case SyntaxKind.GetAccessorDeclaration:
						if (propertyData == null)
						{
							throw new InvalidOperationException($"Get accessor property {n} is declared outside a {nameof(PropertyDeclarationSyntax)}");
						}
						functionData = propertyData.GetAccessorData;
						if (functionData == null)
						{
							return null;
						}
						break;
					case SyntaxKind.SetAccessorDeclaration:
						if (propertyData == null)
						{
							throw new InvalidOperationException($"Set accessor property {n} is declared outside a {nameof(PropertyDeclarationSyntax)}");
						}
						functionData = propertyData.SetAccessorData;
						if (functionData == null)
						{
							return null;
						}
						break;
					case SyntaxKind.PropertyDeclaration:
						if (typeData == null)
						{
							throw new InvalidOperationException($"Property {n} is declared outside a {nameof(TypeDeclarationSyntax)}");
						}
						var propertyNode = (PropertyDeclarationSyntax)n;
						propertyData = typeData.GetPropertyData(propertyNode, SemanticModel, create);
						if (propertyData == null)
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
						typeData = typeData != null 
							? typeData.GetNestedTypeData(typeNode, SemanticModel, create) 
							: namespaceData.GetTypeData(typeNode, SemanticModel, create);
						if (typeData == null)
						{
							return null;
						}
						break;
					case SyntaxKind.NamespaceDeclaration:
						var namespaceNode = (NamespaceDeclarationSyntax)n;
						namespaceData = namespaceData != null 
							? namespaceData.GetNestedNamespaceData(namespaceNode, SemanticModel, create)
							: GetNamespaceData(namespaceNode, create);
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
				case SyntaxKind.LocalFunctionStatement:
				case SyntaxKind.GetAccessorDeclaration: // Property getter
				case SyntaxKind.SetAccessorDeclaration: // Property setter
					return functionData;
				case SyntaxKind.MethodDeclaration:
				case SyntaxKind.DestructorDeclaration:
				case SyntaxKind.ConstructorDeclaration:
				case SyntaxKind.OperatorDeclaration:
				case SyntaxKind.ConversionOperatorDeclaration:
					return baseMethodData;
				case SyntaxKind.ClassDeclaration:
				case SyntaxKind.InterfaceDeclaration:
				case SyntaxKind.StructDeclaration:
					return typeData;
				case SyntaxKind.NamespaceDeclaration:
					return namespaceData;
				case SyntaxKind.PropertyDeclaration:
					return propertyData;
				default:
					throw new InvalidOperationException($"Invalid node kind {Enum.GetName(typeof(SyntaxKind), node.Kind())}");
			}
		}
		/*
		public async Task<FunctionData> GetFunctionData(IMethodSymbol symbol)
		{
			var syntax = symbol.DeclaringSyntaxReferences.Single(o => o.SyntaxTree.FilePath == FilePath);
			var node = await syntax.GetSyntaxAsync().ConfigureAwait(false);
			return (FunctionData)GetNodeData(node);
		}
		*/
		public FunctionData GetFunctionData(ISymbol methodSymbol)
		{
			var syntaxReference = methodSymbol.DeclaringSyntaxReferences.SingleOrDefault();
			if (syntaxReference == null || syntaxReference.SyntaxTree.FilePath != FilePath)
			{
				return null;
			}
			return GetAllTypeDatas()
				.SelectMany(o => o.MethodsAndAccessors)
				.SelectMany(o => o.GetSelfAndDescendantsFunctions())
				.FirstOrDefault(o => o.GetNode().Span.Equals(syntaxReference.Span));
		}

		#region AnonymousFunctionData

		public AnonymousFunctionData GetAnonymousFunctionData(AnonymousFunctionExpressionSyntax node)
		{
			return (AnonymousFunctionData)GetNodeData(node);
		}

		public AnonymousFunctionData GetOrCreateAnonymousFunctionData(AnonymousFunctionExpressionSyntax node, BaseMethodData methodData = null)
		{
			return (AnonymousFunctionData)GetNodeData(node, true, baseMethodData: methodData);
		}

		public LocalFunctionData GetOrCreateLocalFunctionData(LocalFunctionStatementSyntax node, BaseMethodData methodData = null)
		{
			return (LocalFunctionData)GetNodeData(node, true, baseMethodData: methodData);
		}

		#endregion

		#region MethodData

		public MethodData GetMethodData(IMethodSymbol symbol)
		{
			return (MethodData)GetFunctionData(symbol);
		}
		/*
		public async Task<MethodData> GetMethodData(IMethodSymbol symbol)
		{
			var syntax = symbol.DeclaringSyntaxReferences.Single(o => o.SyntaxTree.FilePath == FilePath);
			var node = (MethodDeclarationSyntax)await syntax.GetSyntaxAsync().ConfigureAwait(false);
			return (MethodData)GetNodeData(node);
		}*/

		public MethodData GetMethodData(MethodDeclarationSyntax node)
		{
			return (MethodData)GetNodeData(node);
		}

		public MethodOrAccessorData GetMethodOrAccessorData(SyntaxNode node)
		{
			return (MethodOrAccessorData)GetNodeData(node);
		}

		public MethodData GetOrCreateMethodData(MethodDeclarationSyntax node, TypeData typeData = null)
		{
			return (MethodData)GetNodeData(node, true, typeData: typeData);
		}

		public BaseMethodData GetOrCreateBaseMethodData(BaseMethodDeclarationSyntax node, TypeData typeData = null)
		{
			return (BaseMethodData)GetNodeData(node, true, typeData: typeData);
		}

		public PropertyData GetOrCreatePropertyData(PropertyDeclarationSyntax node, TypeData typeData = null)
		{
			return (PropertyData)GetNodeData(node, true, typeData: typeData);
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

		private NamespaceData GetNamespaceData(NamespaceDeclarationSyntax namespaceNode, bool create)
		{
			NamespaceData namespaceData;
			if (Namespaces.TryGetValue(namespaceNode, out namespaceData))
			{
				return namespaceData;
			}
			var namespaceSymbol = SemanticModel.GetDeclaredSymbol(namespaceNode);
			return !create ? null : Namespaces.GetOrAdd(namespaceNode, syntax => new NamespaceData(this, namespaceSymbol, namespaceNode));
		}

		public ISymbol GetEnclosingSymbol(ReferenceLocation reference)
		{
			var enclosingSymbol = SemanticModel.GetEnclosingSymbol(reference.Location.SourceSpan.Start);
			for (var current = enclosingSymbol; current != null; current = current.ContainingSymbol)
			{
				switch (current.Kind)
				{
					case SymbolKind.Field:
					case SymbolKind.Property:
					case SymbolKind.Method:
					case SymbolKind.NamedType:
						return current;
				}
			}
			//TODO: reference to a cref
			return null;
		}

		#region IDocumentAnalyzationResult

		private IReadOnlyList<INamespaceAnalyzationResult> _cachedNamespaces;
		IReadOnlyList<INamespaceAnalyzationResult> IDocumentAnalyzationResult.Namespaces => _cachedNamespaces ?? (_cachedNamespaces = Namespaces.Values.ToImmutableArray());


		INamespaceAnalyzationResult IDocumentAnalyzationResult.GlobalNamespace => GlobalNamespaceData;

		IEnumerable<ITypeAnalyzationResult> IDocumentAnalyzationResult.GetAllTypes() => GetAllTypeDatas();

		#endregion
	}
}
