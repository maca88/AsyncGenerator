using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Internal
{
	internal class IgnoreReason
	{
		public static IgnoreReason Cascade = new IgnoreReason("Cascade ignored", DiagnosticSeverity.Hidden);
		public static IgnoreReason PartialContainingType = new IgnoreReason("The containing type is partial", DiagnosticSeverity.Hidden);
		public static IgnoreReason AlreadyAsync = new IgnoreReason("Already async", DiagnosticSeverity.Hidden);
		public static IgnoreReason AllRelatedMethodsIgnored = new IgnoreReason("All abstract/virtual related methods are ignored", DiagnosticSeverity.Hidden);
		public static IgnoreReason NeverUsedAndNoAsyncInvocations = new IgnoreReason("Never used and has no async invocations", DiagnosticSeverity.Hidden);
		public static IgnoreReason NoAsyncInvocations = new IgnoreReason("Has no async invocations", DiagnosticSeverity.Hidden);
		public static IgnoreReason NeverUsed = new IgnoreReason("Never used", DiagnosticSeverity.Hidden);
		public static IgnoreReason NeverUsedAsAsync = new IgnoreReason("Never used as async", DiagnosticSeverity.Hidden);
		public static IgnoreReason NoAsyncMembers = new IgnoreReason("No async members", DiagnosticSeverity.Hidden);
		public static IgnoreReason AsyncCounterpartExists = new IgnoreReason("An async counterpart already exists", DiagnosticSeverity.Hidden);
		public static IgnoreReason CallObsoleteMethod = new IgnoreReason("Calling an obsolete method", DiagnosticSeverity.Hidden);

		public static IgnoreReason InvokedMethodNoAsyncCounterpart = new IgnoreReason("The invoked method does not have an async counterpart", DiagnosticSeverity.Hidden);
		public static IgnoreReason NoAsyncCounterparts = new IgnoreReason("No async counterparts", DiagnosticSeverity.Hidden);
		public static IgnoreReason MethodIsCopied = new IgnoreReason("Method is copied", DiagnosticSeverity.Hidden);
		public static IgnoreReason OutParameters = new IgnoreReason("Has out parameters", DiagnosticSeverity.Hidden);

		// By option
		public static IgnoreReason TypeConversion = new IgnoreReason("Ignored by TypeConversion option", DiagnosticSeverity.Hidden);
		public static IgnoreReason PropertyConversion = new IgnoreReason("Ignored by PropertyConversion option", DiagnosticSeverity.Hidden);
		public static IgnoreReason MethodConversion = new IgnoreReason("Ignored by MethodConversion option", DiagnosticSeverity.Hidden);


		public static IgnoreReason OverridesExternalMethodWithoutAsync(IMethodSymbol method) => new IgnoreReason($"Overrides an external method {method} that has not an async counterpart", DiagnosticSeverity.Hidden);
		public static IgnoreReason ExplicitImplementsExternalMethodWithoutAsync(IMethodSymbol method) => new IgnoreReason($"Explicity implements an external interface {method} that has not an async counterpart", DiagnosticSeverity.Hidden);
		public static IgnoreReason ImplementsExternalMethodWithoutAsync(IMethodSymbol method) => new IgnoreReason($"Implements an external interface {method} that has not an async counterpart", DiagnosticSeverity.Hidden);


		// Not supported
		public static IgnoreReason NotSupported(string reason) => new IgnoreReason(reason, DiagnosticSeverity.Hidden);

		/// <summary>
		/// Should be used only for very specific reasons
		/// </summary>
		public static IgnoreReason Custom(string reason, DiagnosticSeverity diagnosticSeverity) => new IgnoreReason(reason, DiagnosticSeverity.Hidden);

		private IgnoreReason(string reason, DiagnosticSeverity diagnosticSeverity)
		{
			Reason = reason;
			DiagnosticSeverity = diagnosticSeverity;
		}

		public string Reason { get; }

		public DiagnosticSeverity DiagnosticSeverity { get; }

		public IgnoreReason WithSeverity(DiagnosticSeverity diagnosticSeverity)
		{
			return new IgnoreReason(Reason, diagnosticSeverity);
		}
	}
}
