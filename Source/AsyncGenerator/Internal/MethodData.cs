using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class AccessorData : MethodOrAccessorData, IAccessorAnalyzationResult
	{
		public AccessorData(PropertyData propertyData, IMethodSymbol symbol, SyntaxNode node) : base(propertyData?.TypeData, symbol, node)
		{
			PropertyData = propertyData ?? throw new ArgumentNullException(nameof(propertyData));
			Node = node ?? throw new ArgumentNullException(nameof(node));
		}

		public PropertyData PropertyData { get; }

		public SyntaxNode Node { get; } // Can be an AccessorDeclarationSyntax or ArrowExpressionClauseSyntax

		public override TypeData TypeData => PropertyData.TypeData;

		public override MethodOrAccessorData GetMethodOrAccessorData() => this;
	}


	internal class PropertyData : AbstractData, IPropertyAnalyzationResult
	{
		public PropertyData(TypeData typeData, IPropertySymbol symbol, PropertyDeclarationSyntax node)
		{
			TypeData = typeData ?? throw new ArgumentNullException(nameof(typeData));
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
			Node = node ?? throw new ArgumentNullException(nameof(node));
			if (Symbol.GetMethod != null)
			{
				GetAccessorData = new AccessorData(this, Symbol.GetMethod, Node.AccessorList != null 
					? (SyntaxNode)Node.AccessorList.Accessors.First(o => o.Keyword.IsKind(SyntaxKind.GetKeyword))
					: Node.ExpressionBody);
			}
			if (Symbol.SetMethod != null)
			{
				SetAccessorData = new AccessorData(this, Symbol.SetMethod, Node.AccessorList.Accessors.First(o => o.Keyword.IsKind(SyntaxKind.SetKeyword)));
			}
		}

		public IPropertySymbol Symbol { get; }

		public PropertyConversion Conversion { get; set; }

		public TypeData TypeData { get; }

		public PropertyDeclarationSyntax Node { get; }

		public AccessorData GetAccessorData { get; }

		public AccessorData SetAccessorData { get; }

		public IEnumerable<AccessorData> GetAccessors()
		{
			if (GetAccessorData != null)
			{
				yield return GetAccessorData;
			}
			if (SetAccessorData != null)
			{
				yield return SetAccessorData;
			}
		}

		#region IPropertyAnalyzationResult

		IEnumerable<IAccessorAnalyzationResult> IPropertyAnalyzationResult.GetAccessors() => GetAccessors();

		#endregion

		public override SyntaxNode GetNode()
		{
			return Node;
		}

		public override void Ignore(string reason, bool explicitlyIgnored = false)
		{
			IgnoredReason = reason;
			ExplicitlyIgnored = explicitlyIgnored;
			GetAccessorData?.Ignore("Cascade ignored.");
			SetAccessorData?.Ignore("Cascade ignored.");
		}
	}

	internal class BaseMethodData : FunctionData
	{
		private readonly SyntaxNode _node;
		private readonly SyntaxNode _bodyNode;

		public BaseMethodData(TypeData typeData, IMethodSymbol symbol, SyntaxNode node) : base(symbol)
		{
			TypeData = typeData;
			_node = node;
			// Find and set body node
			if (_node is BaseMethodDeclarationSyntax baseMethodNode)
			{
				_bodyNode = baseMethodNode.Body ?? (SyntaxNode)baseMethodNode.ExpressionBody;
			}
			else if (_node is AccessorDeclarationSyntax accessorNode) // Property getter/setter
			{
				_bodyNode = accessorNode.Body ?? (SyntaxNode)accessorNode.ExpressionBody;
			}
			else if (_node is ArrowExpressionClauseSyntax arrowNode) // Property arrow getter
			{
				_bodyNode = arrowNode;
			}
			else
			{
				throw new InvalidOperationException($"Invalid base method node {node}");
			}
		}

		/// <summary>
		/// Implementation/derived/base/interface methods
		/// </summary>
		public ConcurrentSet<MethodOrAccessorData> RelatedMethods { get; } = new ConcurrentSet<MethodOrAccessorData>();

		public override TypeData TypeData { get; }

		public override SyntaxNode GetNode()
		{
			return _node;
		}

		public override SyntaxNode GetBodyNode()
		{
			return _bodyNode;
		}

		public override MethodOrAccessorData GetMethodOrAccessorData() => null;

		public override BaseMethodData GetBaseMethodData() => this;
	}

	internal abstract class MethodOrAccessorData : BaseMethodData, IMethodOrAccessorAnalyzationResult, IMethodSymbolInfo
	{
		protected MethodOrAccessorData(TypeData typeData, IMethodSymbol symbol, SyntaxNode node) : base(typeData, symbol, node)
		{
			InterfaceMethod = Symbol.ContainingType.TypeKind == TypeKind.Interface;
		}

		public bool InterfaceMethod { get; }

		public ConcurrentSet<IMethodSymbol> ExternalAsyncMethods { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Interface members within project that the method implements
		/// </summary>
		public ConcurrentSet<IMethodSymbol> ImplementedInterfaces { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// External Base/derivered or interface/implementation methods
		/// </summary>
		public ConcurrentSet<IMethodSymbol> ExternalRelatedMethods { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Methods within project that the method overrides
		/// </summary>
		public ConcurrentSet<IMethodSymbol> OverridenMethods { get; } = new ConcurrentSet<IMethodSymbol>();

		/// <summary>
		/// Method datas that invokes this method
		/// </summary>
		public ConcurrentSet<FunctionData> InvokedBy { get; } = new ConcurrentSet<FunctionData>();

		/// <summary>
		/// Related and invoked by methods
		/// </summary>
		public IEnumerable<FunctionData> Dependencies => InvokedBy.Union(RelatedMethods);

		/// <summary>
		/// The base method that is overriden
		/// </summary>
		public IMethodSymbol BaseOverriddenMethod { get; set; }

		/// <summary>
		/// Reference to the async counterpart
		/// </summary>
		public IMethodSymbol AsyncCounterpartSymbol { get; set; }

		/// <summary>
		/// Reference to the async counterpart that has a <see cref="System.Threading.CancellationToken"/>
		/// </summary>
		public IMethodSymbol AsyncCounterpartWithTokenSymbol { get; set; }

		public bool CancellationTokenRequired { get; set; }

		#region Analyzation step

		public bool MustRunSynchronized { get; set; }

		#endregion

		#region Scanning step

		internal bool Scanned { get; set; }

		public bool Missing { get; set; }

		#endregion

		#region Post-Analyzation step

		public bool ForwardCall { get; set; }

		public MethodCancellationToken? MethodCancellationToken { get; set; }

		public bool AddCancellationTokenGuards { get; set; }

		#endregion

		#region IMethodOrAccessorAnalyzationResult

		private IReadOnlyList<IFunctionAnalyzationResult> _cachedInvokedBy;
		IReadOnlyList<IFunctionAnalyzationResult> IMethodOrAccessorAnalyzationResult.InvokedBy => _cachedInvokedBy ?? (_cachedInvokedBy = InvokedBy.ToImmutableArray());

		private IReadOnlyList<IFunctionReferenceAnalyzationResult> _cachedMethodCrefReferences;
		IReadOnlyList<IFunctionReferenceAnalyzationResult> IMethodOrAccessorAnalyzationResult.CrefMethodReferences =>
			_cachedMethodCrefReferences ?? (_cachedMethodCrefReferences = CrefMethodReferences.ToImmutableArray());

		#endregion

		#region IMethodSymbolInfo

		private IReadOnlyList<IMethodSymbol> _cachedImplementedInterfaces;
		IReadOnlyList<IMethodSymbol> IMethodSymbolInfo.ImplementedInterfaces =>
			_cachedImplementedInterfaces ?? (_cachedImplementedInterfaces = ImplementedInterfaces.ToImmutableArray());

		private IReadOnlyList<IMethodSymbol> _cachedOverridenMethods;
		IReadOnlyList<IMethodSymbol> IMethodSymbolInfo.OverridenMethods =>
			_cachedOverridenMethods ?? (_cachedOverridenMethods = OverridenMethods.ToImmutableArray());

		#endregion

		public override MethodOrAccessorData GetMethodOrAccessorData() => this;

		public IEnumerable<MethodOrAccessorData> GetAllRelatedMethods()
		{
			var result = new HashSet<MethodOrAccessorData>();
			var deps = new HashSet<MethodOrAccessorData>();
			var depsQueue = new Queue<MethodOrAccessorData>(RelatedMethods);
			while (depsQueue.Count > 0)
			{
				var dependency = depsQueue.Dequeue();
				if (deps.Contains(dependency))
				{
					continue;
				}
				deps.Add(dependency);
				foreach (var subDependency in dependency.RelatedMethods)
				{
					if (!deps.Contains(subDependency))
					{
						depsQueue.Enqueue(subDependency);
					}
				}
				//yield return dependency;
				result.Add(dependency);
			}
			return result;
		}

	}

	internal class MethodData : MethodOrAccessorData, IMethodAnalyzationResult
	{
		public MethodData(TypeData typeData, IMethodSymbol symbol, MethodDeclarationSyntax node) : base(typeData, symbol, node)
		{
			Node = node;
		}

		public MethodDeclarationSyntax Node { get; }

	}
}
