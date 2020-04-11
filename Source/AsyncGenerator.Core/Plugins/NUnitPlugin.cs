using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Extensions.Internal;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public class NUnitPlugin :
		IAsyncCounterpartsFinder,
		ITypeConversionProvider,
		IMethodConversionProvider,
		IAlwaysAwaitMethodProvider,
		IPreserveMethodReturnTypeProvider,
		IMethodRequiresCancellationTokenProvider,
		IMethodExceptionHandler,
		ISearchForMethodReferencesProvider
	{
		private const string IgnoreAttribute = "NUnit.Framework.IgnoreAttribute";
		private static readonly HashSet<string> TypeTestAttributes = new HashSet<string>
		{
			"NUnit.Framework.TestFixtureAttribute",
			"NUnit.Framework.TestFixtureSourceAttribute"
		};
		private static readonly HashSet<string> MethodTestAttributes = new HashSet<string>
		{
			"NUnit.Framework.TestAttribute",
			"NUnit.Framework.TheoryAttribute",
			"NUnit.Framework.TestCaseAttribute",
			"NUnit.Framework.TestCaseSourceAttribute"
		};
		private static readonly HashSet<string> SetupAttributes = new HashSet<string>
		{
			"NUnit.Framework.OneTimeSetUpAttribute",
			"NUnit.Framework.OneTimeTearDownAttribute",
			"NUnit.Framework.SetUpAttribute",
			"NUnit.Framework.TearDownAttribute"
		};


		private readonly bool _createNewTypes;
		private Dictionary<IMethodSymbol, IMethodSymbol> _thatAsyncCounterparts;

		public NUnitPlugin(bool createNewTypes)
		{
			_createNewTypes = createNewTypes;
		}

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

		public bool? CatchMethodBody(IMethodSymbol methodSymbol, IMethodSymbol argumentOfMethodSymbol)
		{
			if (argumentOfMethodSymbol != null && argumentOfMethodSymbol.ContainingAssembly.Name == "nunit.framework")
			{
				return false;
			}
			return null;
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
					var pair = _thatAsyncCounterparts.FirstOrDefault(o => o.Key.EqualTo(syncMethodSymbol));
					if (pair.Value != null)
					{
						yield return pair.Value;
					}
					break;
			}
		}

		public TypeConversion? GetConversion(INamedTypeSymbol typeSymbol)
		{
			var currentType = typeSymbol;
			TypeConversion? result = null;
			while (currentType != null)
			{
				foreach (var attribute in currentType.GetAttributes())
				{
					var fullName = attribute.AttributeClass.ToString();
					if (IgnoreAttribute == fullName)
					{
						return TypeConversion.Ignore;
					}

					if (_createNewTypes && TypeTestAttributes.Contains(fullName))
					{
						result = TypeConversion.NewType;
					}
				}

				currentType = currentType.BaseType;
			}

			return result;
		}

		public MethodConversion? GetConversion(IMethodSymbol methodSymbol)
		{
			MethodConversion? result = null;
			foreach (var attribute in methodSymbol.GetAttributes())
			{
				var fullName = attribute.AttributeClass.ToString();
				if (methodSymbol.OverriddenMethod == null && IgnoreAttribute == fullName)
				{
					return MethodConversion.Ignore;
				}

				if (MethodTestAttributes.Contains(fullName))
				{
					result = MethodConversion.Smart;
				}
				else if (SetupAttributes.Contains(fullName))
				{
					result = _createNewTypes ? MethodConversion.Copy : MethodConversion.Ignore;
				}
			}

			return result;
		}

		public bool? AlwaysAwait(IMethodSymbol methodSymbol)
		{
			return methodSymbol.GetAttributes()
				.Any(o => MethodTestAttributes.Contains(o.AttributeClass.ToString()))
				? true
				: (bool?) null;
		}

		public bool? PreserveReturnType(IMethodSymbol methodSymbol)
		{
			return methodSymbol.GetAttributes()
				.Any(o => MethodTestAttributes.Contains(o.AttributeClass.ToString()))
				? true
				: (bool?) null;
		}

		public bool? RequiresCancellationToken(IMethodSymbol methodSymbol)
		{
			return methodSymbol.GetAttributes()
				.Any(o => MethodTestAttributes.Contains(o.AttributeClass.ToString()))
				? false
				: (bool?) null;
		}

		public bool? SearchForMethodReferences(IMethodSymbol methodSymbol)
		{
			return methodSymbol.GetAttributes()
				.Any(o => MethodTestAttributes.Contains(o.AttributeClass.ToString()))
				? false
				: (bool?) null;
		}

		private static bool MatchParameters(IMethodSymbol thatMethod, IMethodSymbol asyncThatMethod)
		{
			if (thatMethod.Parameters.Length != asyncThatMethod.Parameters.Length)
			{
				return false;
			}
			for (var i = 1; i < thatMethod.Parameters.Length; i++)
			{
				if (!thatMethod.Parameters[i].Type.EqualTo(asyncThatMethod.Parameters[i].Type))
				{
					return false;
				}
			}
			return true;
		}
	}
}
