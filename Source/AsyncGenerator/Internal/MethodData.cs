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
	internal class MethodData : MethodOrAccessorData, IMethodAnalyzationResult
	{
		public MethodData(TypeData typeData, IMethodSymbol symbol, MethodDeclarationSyntax node) : base(typeData, symbol, node)
		{
			Node = node;
		}

		public MethodDeclarationSyntax Node { get; }

	}
}
