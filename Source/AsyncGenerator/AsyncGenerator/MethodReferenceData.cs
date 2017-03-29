using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator
{
	public class MethodReferenceData
	{
		public MethodReferenceData(MethodData methodData, ReferenceLocation reference, SimpleNameSyntax referenceNode,
			IMethodSymbol referenceSymbol, MethodData referenceMethodData)
		{
			MethodData = methodData;
			ReferenceLocation = reference;
			ReferenceNode = referenceNode;
			ReferenceSymbol = referenceSymbol;
			ReferenceMethodData = referenceMethodData;
		}

		public MethodData MethodData { get; }

		public MethodData ReferenceMethodData { get; }

		public SimpleNameSyntax ReferenceNode { get; }

		public ReferenceLocation ReferenceLocation { get; }

		public IMethodSymbol ReferenceSymbol { get; }

		public bool CanBeAsync { get; set; }

		public bool CanBeAwaited { get; internal set; } = true;

		public bool PassedAsArgument { get; internal set; }

		public bool MakeAnonymousFunctionAsync { get; set; }

		public bool UsedAsReturnValue { get; internal set; }

		public override int GetHashCode()
		{
			return ReferenceLocation.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != GetType())
			{
				return false;
			}
			return ReferenceLocation.Equals(((MethodReferenceData)obj).ReferenceLocation);
		}
	}
}
