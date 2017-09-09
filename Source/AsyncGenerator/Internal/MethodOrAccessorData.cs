using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Internal
{
	internal abstract class MethodOrAccessorData : BaseMethodData, IMethodOrAccessorAnalyzationResult, IMethodSymbolInfo
	{
		protected MethodOrAccessorData(TypeData typeData, IMethodSymbol symbol, SyntaxNode node) : base(typeData, symbol, node)
		{
			InterfaceMethod = Symbol.ContainingType.TypeKind == TypeKind.Interface;
		}

		public bool InterfaceMethod { get; }

		/// <summary>
		/// Async counterparts of external or internal related (overriden/interface) methods
		/// </summary>
		public ConcurrentSet<IMethodSymbol> RelatedAsyncMethods { get; } = new ConcurrentSet<IMethodSymbol>();

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

		public bool HasAsyncCounterpart => AsyncCounterpartWithTokenSymbol != null || AsyncCounterpartSymbol != null;

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

		private IReadOnlyList<IMethodOrAccessorAnalyzationResult> _cachedRelatedMethods;
		IReadOnlyList<IMethodOrAccessorAnalyzationResult> IMethodOrAccessorAnalyzationResult.RelatedMethods =>
			_cachedRelatedMethods ?? (_cachedRelatedMethods = RelatedMethods.ToImmutableArray());

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

		public override void Ignore(string reason, bool explicitlyIgnored = false)
		{
			CancellationTokenRequired = false;
			base.Ignore(reason, explicitlyIgnored);
		}

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

		public IEnumerable<FunctionData> GetAllInvokedBy()
		{
			var result = new HashSet<FunctionData>();
			var deps = new HashSet<FunctionData>();
			var depsQueue = new Queue<FunctionData>(InvokedBy);
			while (depsQueue.Count > 0)
			{
				var dependency = depsQueue.Dequeue();
				if (deps.Contains(dependency))
				{
					continue;
				}
				deps.Add(dependency);
				if (dependency is MethodOrAccessorData methodOrAccessorData)
				{
					foreach (var subDependency in methodOrAccessorData.InvokedBy)
					{
						if (!deps.Contains(subDependency))
						{
							depsQueue.Enqueue(subDependency);
						}
					}
				}
				//yield return dependency;
				result.Add(dependency);
			}
			return result;
		}

	}
}
