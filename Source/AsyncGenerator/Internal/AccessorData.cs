using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AsyncGenerator.Internal
{
	internal class AccessorData : MethodOrAccessorData, IAccessorAnalyzationResult
	{
		public AccessorData(PropertyData propertyData, IMethodSymbol symbol, SyntaxNode node) : base(propertyData?.TypeData, symbol, node)
		{
			PropertyData = propertyData ?? throw new ArgumentNullException(nameof(propertyData));
			Node = node ?? throw new ArgumentNullException(nameof(node));
		}

		public PropertyData PropertyData { get; }

		public SyntaxNode Node { get; } // Can be an AccessorDeclarationSyntax or ArrowExpressionClauseSyntax

		public override TypeData TypeData => PropertyData.TypeData;

		public override bool IsPrivate => !InterfaceMethod && Symbol.ExplicitInterfaceImplementations.Length == 0 &&
		                                  (!PropertyData.Node.Modifiers.Any() ||
		                                   PropertyData.Node.Modifiers.Any(SyntaxKind.PrivateKeyword));

		public override MethodOrAccessorData GetMethodOrAccessorData() => this;
	}
}
