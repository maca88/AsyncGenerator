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
			SyntaxKind.ArrowExpressionClause, // arrow expression getter
			// Field
			SyntaxKind.FieldDeclaration,
			SyntaxKind.EventFieldDeclaration,
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
			GlobalNamespace = new NamespaceData(this, SemanticModel.Compilation.GlobalNamespace, null);
		}

		public Document Document { get; }

		public string FilePath => Document.FilePath;

		public ProjectData ProjectData { get; }

		public CompilationUnitSyntax Node { get; }

		public SemanticModel SemanticModel { get; }

		public NamespaceData GlobalNamespace { get; }

		public SyntaxNode GetNode()
		{
			return Node;
		}

		/// <summary>
		/// Can be null
		/// </summary>
		private List<DiagnosticData> Diagnostics { get; set; }

		public IEnumerable<DiagnosticData> GetDiagnostics()
		{
			return Diagnostics ?? Enumerable.Empty<DiagnosticData>();
		}

		public void AddDiagnostic(string description, DiagnosticSeverity severity)
		{
			(Diagnostics ?? (Diagnostics = new List<DiagnosticData>())).Add(new DiagnosticData(description, severity));
		}

		/// <summary>
		/// Iterate through all namespace data from top to bottom
		/// </summary>
		public IEnumerable<NamespaceData> GetAllNamespaceDatas(Func<NamespaceData, bool> predicate = null)
		{
			return GlobalNamespace.GetSelfAndDescendantsNamespaceData(predicate);
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
			BaseFieldData fieldData = null;
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
					case SyntaxKind.ArrowExpressionClause:
						if (propertyData == null)
						{
							continue;
						}
						functionData = propertyData.GetAccessorData;
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
					case SyntaxKind.FieldDeclaration:
					case SyntaxKind.EventFieldDeclaration:
						if (typeData == null)
						{
							throw new InvalidOperationException($"Field {n} is declared outside a {nameof(TypeDeclarationSyntax)}");
						}
						var fieldNode = (BaseFieldDeclarationSyntax)n;
						fieldData = typeData.GetBaseFieldData(fieldNode, SemanticModel, create);
						if (fieldData == null)
						{
							return null;
						}
						break;
					case SyntaxKind.ClassDeclaration:
					case SyntaxKind.InterfaceDeclaration:
					case SyntaxKind.StructDeclaration:
						if (namespaceData == null)
						{
							namespaceData = GlobalNamespace;
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
				case SyntaxKind.FieldDeclaration:
				case SyntaxKind.EventFieldDeclaration:
					return fieldData;
				case SyntaxKind.ArrowExpressionClause: // Arrow expression of a property getter or method
					return functionData ?? baseMethodData;
				default:
					throw new InvalidOperationException($"Invalid node kind {Enum.GetName(typeof(SyntaxKind), node.Kind())}");
			}
		}

		public FunctionData GetFunctionData(ISymbol methodSymbol)
		{
			var syntaxReference = methodSymbol.DeclaringSyntaxReferences.SingleOrDefault();
			return GetFunctionData(syntaxReference);
		}

		public FunctionData GetFunctionData(SyntaxReference syntaxReference)
		{
			if (syntaxReference == null || syntaxReference.SyntaxTree.FilePath != FilePath)
			{
				return null;
			}

			FunctionData closestFunctionData = null;
			foreach (var functionData in GetAllTypeDatas()
				.SelectMany(o => o.MethodsAndAccessors)
				.SelectMany(o => o.GetSelfAndDescendantsFunctions()))
			{
				var node = functionData.GetNode();
				if (node.Span.Equals(syntaxReference.Span))
				{
					return functionData;
				}
				// If the syntaxReference refers to a lambda method e.g. from o in array select AsyncMethod()
				// return the method that contains it
				if (node.Span.Contains(syntaxReference.Span))
				{
					closestFunctionData = functionData;
				}
			}

			return closestFunctionData;
		}

		public PropertyData GetPropertyData(IPropertySymbol propertySymbol)
		{
			var syntaxReference = propertySymbol.DeclaringSyntaxReferences.SingleOrDefault();
			return GetPropertyData(syntaxReference);
		}

		public PropertyData GetPropertyData(SyntaxReference syntaxReference)
		{
			if (syntaxReference == null || syntaxReference.SyntaxTree.FilePath != FilePath)
			{
				return null;
			}
			return GetAllTypeDatas()
				.SelectMany(o => o.Properties.Values)
				.FirstOrDefault(o => o.GetNode().Span.Equals(syntaxReference.Span));
		}

		public FieldVariableDeclaratorData GetFieldVariableDeclaratorData(ISymbol methodSymbol)
		{
			var syntaxReference = methodSymbol.DeclaringSyntaxReferences.SingleOrDefault();
			return GetFieldVariableDeclaratorData(syntaxReference);
		}

		public FieldVariableDeclaratorData GetFieldVariableDeclaratorData(SyntaxReference syntaxReference)
		{
			if (syntaxReference == null || syntaxReference.SyntaxTree.FilePath != FilePath)
			{
				return null;
			}
			return GetAllTypeDatas()
				.SelectMany(o => o.Fields.Values)
				.SelectMany(o => o.Variables)
				.FirstOrDefault(o => o.GetNode().Span.Equals(syntaxReference.Span));
		}

		public AbstractData GetAbstractData(ISymbol symbol)
		{
			var syntaxReference = symbol.DeclaringSyntaxReferences.SingleOrDefault();
			if (syntaxReference == null || syntaxReference.SyntaxTree.FilePath != FilePath)
			{
				return null;
			}
			if (symbol is IMethodSymbol)
			{
				return GetFunctionData(syntaxReference);
			}
			if (symbol is INamedTypeSymbol)
			{
				return GetTypeData(syntaxReference);
			}
			if (symbol is IPropertySymbol)
			{
				return GetPropertyData(syntaxReference);
			}
			if (symbol is IFieldSymbol || symbol is IEventSymbol)
			{
				return GetFieldVariableDeclaratorData(syntaxReference);
			}
			if (symbol is INamespaceSymbol)
			{
				return GetNamespaceData(syntaxReference);
			}
			return null;
		}

		#region AnonymousFunctionData

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
			var syntaxReference = symbol.DeclaringSyntaxReferences.SingleOrDefault();
			return GetMethodData(syntaxReference);
		}

		public MethodData GetMethodData(SyntaxReference syntaxReference)
		{
			if (syntaxReference == null || syntaxReference.SyntaxTree.FilePath != FilePath)
			{
				return null;
			}
			return GetAllTypeDatas()
				.SelectMany(o => o.Methods.Values)
				.FirstOrDefault(o => o.Node.Span.Equals(syntaxReference.Span));
		}

		public MethodOrAccessorData GetMethodOrAccessorData(SyntaxReference syntaxReference)
		{
			if (syntaxReference == null || syntaxReference.SyntaxTree.FilePath != FilePath)
			{
				return null;
			}
			return GetAllTypeDatas()
				.SelectMany(o => o.MethodsAndAccessors)
				.FirstOrDefault(o => o.GetNode().Span.Equals(syntaxReference.Span));
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

		public BaseFieldData GetOrCreateBaseFieldData(BaseFieldDeclarationSyntax node, TypeData typeData = null)
		{
			return (BaseFieldData)GetNodeData(node, true, typeData: typeData);
		}

		#endregion

		#region TypeData

		/// <summary>
		/// Iterate through all type data from top to bottom
		/// </summary>
		public IEnumerable<TypeData> GetAllTypeDatas(Func<TypeData, bool> predicate = null)
		{
			return GetAllNamespaceDatas()
				.SelectMany(o => o.Types.Values)
				.SelectMany(o => o.GetSelfAndDescendantsTypeData(predicate));
		}

		public TypeData GetOrCreateTypeData(TypeDeclarationSyntax node)
		{
			return (TypeData)GetNodeData(node, true);
		}

		public TypeData GetTypeData(TypeDeclarationSyntax node)
		{
			return (TypeData)GetNodeData(node);
		}

		public TypeData GetTypeData(SyntaxReference syntaxReference)
		{
			return GetAllTypeDatas().First(o => o.Node.Span.Equals(syntaxReference.Span));
		}

		#endregion

		#region NamespaceData

		public NamespaceData GetOrCreateNamespaceData(NamespaceDeclarationSyntax node)
		{
			return (NamespaceData)GetNodeData(node, true);
		}

		public NamespaceData GetNamespaceData(SyntaxReference syntaxReference)
		{
			return GetAllNamespaceDatas().FirstOrDefault(o => o.Node.Span.Equals(syntaxReference.Span));
		}

		#endregion

		private NamespaceData GetNamespaceData(NamespaceDeclarationSyntax namespaceNode, bool create)
		{
			NamespaceData namespaceData;
			if (GlobalNamespace.NestedNamespaces.TryGetValue(namespaceNode, out namespaceData))
			{
				return namespaceData;
			}
			var namespaceSymbol = SemanticModel.GetDeclaredSymbol(namespaceNode);
			return !create ? null : GlobalNamespace.NestedNamespaces.GetOrAdd(namespaceNode, syntax => new NamespaceData(this, namespaceSymbol, namespaceNode));
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

		INamespaceAnalyzationResult IDocumentAnalyzationResult.GlobalNamespace => GlobalNamespace;

		IEnumerable<ITypeAnalyzationResult> IDocumentAnalyzationResult.AllTypes => GetAllTypeDatas();

		IEnumerable<INamespaceAnalyzationResult> IDocumentAnalyzationResult.AllNamespaces => GetAllNamespaceDatas();

		#endregion
	}
}
