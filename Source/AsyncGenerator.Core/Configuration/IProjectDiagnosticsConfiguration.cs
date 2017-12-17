using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration
{
	public interface IProjectDiagnosticsConfiguration
	{
		bool Enabled { get; }

		Predicate<Document> CanDiagnoseDocument { get; }

		Predicate<INamedTypeSymbol> CanDiagnoseType { get; }

		Predicate<IMethodSymbol> CanDiagnoseMethod { get;}
	}
}
