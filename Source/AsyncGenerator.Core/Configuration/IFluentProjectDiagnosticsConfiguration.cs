using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Configuration
{
	public interface IFluentProjectDiagnosticsConfiguration
	{
		void Disable();

		IFluentProjectDiagnosticsConfiguration DiagnoseDocument(Predicate<Document> predicate);

		IFluentProjectDiagnosticsConfiguration DiagnoseType(Predicate<INamedTypeSymbol> predicate);

		IFluentProjectDiagnosticsConfiguration DiagnoseMethod(Predicate<IMethodSymbol> predicate);
	}
}
