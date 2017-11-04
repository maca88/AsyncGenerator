using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public class NUnitAsyncCounterpartsFinder : IAsyncCounterpartsFinder
	{
		private Dictionary<IMethodSymbol, IMethodSymbol> _thatAsyncCounterparts;

		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			var assertSymbol =
				compilation.References
					.Select(compilation.GetAssemblyOrModuleSymbol)
					.OfType<IAssemblySymbol>()
					.Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName("NUnit.Framework.Assert"))
					.FirstOrDefault(o => o != null);
			if (assertSymbol == null)
			{
				throw new InvalidOperationException("Unable to find NUnit.Framework.Assert type");
			}
			// We should get:
			// void That<TActual>(ActualValueDelegate<TActual> del, IResolveConstraint expr)
			// void That<TActual>(ActualValueDelegate<TActual> del, IResolveConstraint expr, string message, params object[] args)
			// void That<TActual>(ActualValueDelegate<TActual> del, IResolveConstraint expr, Func<string> getExceptionMessage)
			// void That(TestDelegate code, IResolveConstraint constraint)
			// void That(TestDelegate code, IResolveConstraint constraint, string message, params object[] args)
			// void That(TestDelegate code, IResolveConstraint constraint, Func<string> getExceptionMessage)
			var thatMethods = assertSymbol.GetMembers("That").OfType<IMethodSymbol>()
				.Where(o => new[] {"ActualValueDelegate", "TestDelegate"}.Contains(o.Parameters[0].Type.Name))
				.ToList();
			var asyncThatMethods = thatMethods.Where(o => o.IsGenericMethod).ToList();

			// With NUnit version 3.8, each sync method has its own async counterpart, but we are using
			// FirstOrDefault just in case if there will be a new sync overload without an async counterpart in the future
			_thatAsyncCounterparts = thatMethods.Where(o => !o.IsGenericMethod)
				.ToDictionary(o => o, o => asyncThatMethods.FirstOrDefault(a => MatchParameters(o, a)));
				
			return Task.CompletedTask;
		}

		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol syncMethodSymbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options)
		{
			if (syncMethodSymbol.Name != "That" ||
				syncMethodSymbol.ContainingType.Name != "Assert" ||
				syncMethodSymbol.ContainingType.ContainingNamespace.ToString() != "NUnit.Framework")
			{
				yield break;
			}
			var firstParamType = syncMethodSymbol.Parameters.First().Type;
			switch (firstParamType.Name)
			{
				case "ActualValueDelegate":
					yield return syncMethodSymbol;
					yield break;
				case "TestDelegate":
					var pair = _thatAsyncCounterparts.FirstOrDefault(o => o.Key.Equals(syncMethodSymbol));
					if (pair.Value != null)
					{
						yield return pair.Value;
					}
					break;
			}
		}
		
		private bool MatchParameters(IMethodSymbol thatMethod, IMethodSymbol asyncThatMethod)
		{
			if (thatMethod.Parameters.Length != asyncThatMethod.Parameters.Length)
			{
				return false;
			}
			for (var i = 1; i < thatMethod.Parameters.Length; i++)
			{
				if (!thatMethod.Parameters[i].Type.Equals(asyncThatMethod.Parameters[i].Type))
				{
					return false;
				}
			}
			return true;
		}
	}
}
