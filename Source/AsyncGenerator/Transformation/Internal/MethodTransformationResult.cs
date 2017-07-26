using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Configuration;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	internal class RootFunctionTransformationResult : FunctionTransformationResult
	{
		public RootFunctionTransformationResult(IFunctionAnalyzationResult result) : base(result)
		{
		}

		public List<FunctionTransformationResult> DescendantTransformedFunctions { get; } = new List<FunctionTransformationResult>();

		public IEnumerable<FunctionTransformationResult> GetSelfAndDescendantTransformedFunctions()
		{
			yield return this;
			foreach (var transformFunc in DescendantTransformedFunctions)
			{
				yield return transformFunc;
			}
		}
	}

	internal class FunctionTransformationResult : TransformationResult
	{
		public FunctionTransformationResult(IFunctionAnalyzationResult result) : base(result.GetNode())
		{
			AnalyzationResult = result;
		}

		public IFunctionAnalyzationResult AnalyzationResult { get; }

		public List<FunctionReferenceTransformationResult> TransformedFunctionReferences { get; } = new List<FunctionReferenceTransformationResult>();

		public List<TransformationResult> TransformedNodes { get; } = new List<TransformationResult>();
	}

	internal class LockTransformationResult : TransformationResult, ILockTransformationResult
	{
		public LockTransformationResult(ILockAnalyzationResult result) : base(result.Node)
		{
			AnalyzationResult = result;
		}

		public ILockAnalyzationResult AnalyzationResult { get; }
	}

	internal class PropertyTransformationResult : TransformationResult<PropertyDeclarationSyntax>
	{
		public PropertyTransformationResult(IPropertyAnalyzationResult result) : base(result.Node)
		{
			AnalyzationResult = result;
		}

		public List<AccessorTransformationResult> TransformedAccessors { get; } = new List<AccessorTransformationResult>();

		public SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia BodyLeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia EndOfLineTrivia { get; set; }

		public SyntaxTrivia IndentTrivia { get; set; }

		public IPropertyAnalyzationResult AnalyzationResult { get; }

		public List<MethodDeclarationSyntax> Methods { get; private set; }

		public List<FieldDeclarationSyntax> Fields { get; set; }

		public void AddMethod(MethodDeclarationSyntax node)
		{
			Methods = Methods ?? new List<MethodDeclarationSyntax>();
			Methods.Add(node);
		}

		public void AddMethods(IEnumerable<MethodDeclarationSyntax> nodes)
		{
			Methods = Methods ?? new List<MethodDeclarationSyntax>();
			Methods.AddRange(nodes);
		}

		public override IEnumerable<SyntaxNode> GetTransformedNodes()
		{
			if (Fields != null)
			{
				foreach (var field in Fields)
				{
					yield return field;
				}
			}

			if (Transformed != null)
			{
				yield return Transformed;
			}

			foreach (var accessor in TransformedAccessors.Where(o => o.Transformed != null))
			{
				yield return accessor.Transformed;
			}

			if (Methods == null)
			{
				yield break;
			}
			foreach (var method in Methods)
			{
				yield return method;
			}
		}
	}

	internal class AccessorTransformationResult : TransformationResult<SyntaxNode, MethodDeclarationSyntax>, IAccessorTransformationResult
	{
		public AccessorTransformationResult(IAccessorAnalyzationResult result) : base(result.Node)
		{
			AnalyzationResult = result;
		}

		public IAccessorAnalyzationResult AnalyzationResult { get; }

		public List<RootFunctionTransformationResult> TransformedFunctions { get; } = new List<RootFunctionTransformationResult>();

		public List<FunctionReferenceTransformationResult> TransformedFunctionReferences { get; } = new List<FunctionReferenceTransformationResult>();

		public List<LockTransformationResult> TransformedLocks { get; } = new List<LockTransformationResult>();

		public SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia BodyLeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia EndOfLineTrivia { get; set; }

		public SyntaxTrivia IndentTrivia { get; set; }

		#region IMethodOrAccessorTransformationResult

		private IReadOnlyList<IFunctionReferenceTransformationResult> _cachedTransformedFunctionReferences;
		IReadOnlyList<IFunctionReferenceTransformationResult> IMethodOrAccessorTransformationResult.TransformedFunctionReferences =>
			_cachedTransformedFunctionReferences ?? (_cachedTransformedFunctionReferences = TransformedFunctionReferences.Where(o => o.Transformed != null).ToImmutableList());

		private IReadOnlyList<ILockTransformationResult> _cachedTransformedLocks;
		IReadOnlyList<ILockTransformationResult> IMethodOrAccessorTransformationResult.TransformedLocks =>
			_cachedTransformedLocks ?? (_cachedTransformedLocks = TransformedLocks.ToImmutableList());

		IMethodOrAccessorAnalyzationResult IMethodOrAccessorTransformationResult.AnalyzationResult => AnalyzationResult;

		#endregion

		IMemberAnalyzationResult IMemberTransformationResult.GetAnalyzationResult() => AnalyzationResult;
	}

	internal class MethodTransformationResult : TransformationResult<MethodDeclarationSyntax>, IMethodTransformationResult
	{
		public MethodTransformationResult(IMethodAnalyzationResult result) : base(result.Node)
		{
			AnalyzationResult = result;
		}

		public IMethodAnalyzationResult AnalyzationResult { get; }

		public List<FieldDeclarationSyntax> Fields { get; set; }

		public List<MethodDeclarationSyntax> Methods { get; private set; }

		public List<FunctionReferenceTransformationResult> TransformedFunctionReferences { get; } = new List<FunctionReferenceTransformationResult>();

		public List<RootFunctionTransformationResult> TransformedFunctions { get; } = new List<RootFunctionTransformationResult>();

		public List<LockTransformationResult> TransformedLocks { get; } = new List<LockTransformationResult>();

		public SyntaxTrivia LeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia BodyLeadingWhitespaceTrivia { get; set; }

		public SyntaxTrivia EndOfLineTrivia { get; set; }

		public SyntaxTrivia IndentTrivia { get; set; }

		#region IMethodOrAccessorTransformationResult

		private IReadOnlyList<IFunctionReferenceTransformationResult> _cachedTransformedFunctionReferences;
		IReadOnlyList<IFunctionReferenceTransformationResult> IMethodOrAccessorTransformationResult.TransformedFunctionReferences =>
			_cachedTransformedFunctionReferences ?? (_cachedTransformedFunctionReferences = TransformedFunctionReferences.Where(o => o.Transformed != null).ToImmutableList());

		private IReadOnlyList<ILockTransformationResult> _cachedTransformedLocks;
		IReadOnlyList<ILockTransformationResult> IMethodOrAccessorTransformationResult.TransformedLocks =>
			_cachedTransformedLocks ?? (_cachedTransformedLocks = TransformedLocks.ToImmutableList());

		IMethodOrAccessorAnalyzationResult IMethodOrAccessorTransformationResult.AnalyzationResult => AnalyzationResult;

		#endregion


		public void AddMethod(MethodDeclarationSyntax node)
		{
			Methods = Methods ?? new List<MethodDeclarationSyntax>();
			Methods.Add(node);
		}

		public void AddMethods(IEnumerable<MethodDeclarationSyntax> nodes)
		{
			Methods = Methods ?? new List<MethodDeclarationSyntax>();
			Methods.AddRange(nodes);
		}

		public override IEnumerable<SyntaxNode> GetTransformedNodes()
		{
			if (Fields != null)
			{
				foreach (var field in Fields)
				{
					yield return field;
				}
			}

			if (Transformed != null)
			{
				yield return Transformed;
			}
			if (Methods == null)
			{
				yield break;
			}
			foreach (var method in Methods)
			{
				yield return method;
			}
		}

		public IMemberAnalyzationResult GetAnalyzationResult()
		{
			return AnalyzationResult;
		}

		IMemberAnalyzationResult IMemberTransformationResult.GetAnalyzationResult() => AnalyzationResult;
	}
}
