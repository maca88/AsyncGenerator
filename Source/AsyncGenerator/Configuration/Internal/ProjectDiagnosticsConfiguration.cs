using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectDiagnosticsConfiguration : IFluentProjectDiagnosticsConfiguration, IProjectDiagnosticsConfiguration
	{
		public bool Enabled { get; private set; } = true;

		public Predicate<Document> CanDiagnoseDocument { get; private set; } = document => true;

		public Predicate<INamedTypeSymbol> CanDiagnoseType { get; private set; } = symbol => true;

		public Predicate<IMethodSymbol> CanDiagnoseMethod { get; private set; } = symbol => true;

		void IFluentProjectDiagnosticsConfiguration.Disable()
		{
			Enabled = false;
		}

		IFluentProjectDiagnosticsConfiguration IFluentProjectDiagnosticsConfiguration.DiagnoseDocument(Predicate<Document> predicate)
		{
			CanDiagnoseDocument = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectDiagnosticsConfiguration IFluentProjectDiagnosticsConfiguration.DiagnoseType(Predicate<INamedTypeSymbol> predicate)
		{
			CanDiagnoseType = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		IFluentProjectDiagnosticsConfiguration IFluentProjectDiagnosticsConfiguration.DiagnoseMethod(Predicate<IMethodSymbol> predicate)
		{
			CanDiagnoseMethod = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}
	}
}
