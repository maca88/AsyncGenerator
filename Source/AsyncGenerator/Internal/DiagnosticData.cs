using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Internal
{
	internal class DiagnosticData
	{
		public DiagnosticData(string description, DiagnosticSeverity diagnosticSeverity)
		{
			Description = description;
			DiagnosticSeverity = diagnosticSeverity;
		}

		public string Description { get; }

		public DiagnosticSeverity DiagnosticSeverity { get; }
	}
}
