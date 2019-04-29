using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Internal
{
	internal class BaseFieldData : AbstractData, IFieldAnalyzationResult
	{
		public BaseFieldData(TypeData typeData, BaseFieldDeclarationSyntax node, SemanticModel semanticModel)
		{
			TypeData = typeData;
			Node = node;
			Symbol = semanticModel.GetSymbolInfo(node.Declaration.Type).Symbol ?? throw new InvalidOperationException($"symbol for field type {node.Declaration.Type} was not found");
			var variables = new List<FieldVariableDeclaratorData>();
			foreach (var variable in node.Declaration.Variables)
			{
				var symbol = semanticModel.GetDeclaredSymbol(variable);
				variables.Add(new FieldVariableDeclaratorData(this, symbol, variable));
			}

			Variables = variables.AsReadOnly();
		}

		public IReadOnlyList<FieldVariableDeclaratorData> Variables { get; }

		/// <summary>
		/// Represent the field type symbol
		/// </summary>
		public ISymbol Symbol { get; }

		public TypeData TypeData { get; }

		public BaseFieldDeclarationSyntax Node { get; }

		#region IFieldAnalyzationResult

		IReadOnlyList<IFieldVariableDeclaratorResult> IFieldAnalyzationResult.Variables => Variables;

		private IReadOnlyList<ITypeReferenceAnalyzationResult> _cachedTypeReferences;
		IReadOnlyList<ITypeReferenceAnalyzationResult> IFieldAnalyzationResult.TypeReferences => 
			_cachedTypeReferences ?? (_cachedTypeReferences = References.OfType<TypeDataReference>().ToImmutableArray());

		#endregion

		public override SyntaxNode GetNode() => Node;

		public override ISymbol GetSymbol() => Symbol;

		public FieldVariableDeclaratorData GetVariableDeclaratorData(VariableDeclaratorSyntax node, SemanticModel semanticModel)
		{
			return Variables.FirstOrDefault(o => o.Node == node);
		}

		protected override void Ignore()
		{
			base.Ignore();
			foreach (var variable in Variables)
			{
				variable.Ignore(IgnoreReason.Cascade, ExplicitlyIgnored);
			}
		}
	}
}
