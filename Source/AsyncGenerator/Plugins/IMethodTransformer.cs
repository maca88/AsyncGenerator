using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Transformation;
using AsyncGenerator.Transformation.Internal;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Plugins
{
	public class MethodTransformerResult
	{
		public static readonly MethodTransformerResult Skip = new MethodTransformerResult();

		private MethodTransformerResult() {}

		private MethodTransformerResult(MethodDeclarationSyntax transformedNode)
		{
			TransformedNode = transformedNode ?? throw new ArgumentNullException(nameof(transformedNode));
		}

		public static MethodTransformerResult Update(MethodDeclarationSyntax transformedNode)
		{
			return new MethodTransformerResult(transformedNode);
		}

		public MethodTransformerResult AddField(FieldDeclarationSyntax field)
		{
			if (field == null)
			{
				throw new ArgumentNullException(nameof(field));
			}
			Fields = Fields ?? new List<FieldDeclarationSyntax>(1);
			Fields.Add(field);
			return this;
		}

		public MethodTransformerResult AddMethod(MethodDeclarationSyntax method)
		{
			if (method == null)
			{
				throw new ArgumentNullException(nameof(method));
			}
			Methods = Methods ?? new List<MethodDeclarationSyntax>(1);
			Methods.Add(method);
			return this;
		}

		internal MethodDeclarationSyntax TransformedNode { get; }

		internal List<FieldDeclarationSyntax> Fields { get; set; }

		internal List<MethodDeclarationSyntax> Methods { get; set; }

	}

	public interface IMethodTransformer : IPlugin
	{
		MethodTransformerResult Transform(IMethodTransformationResult methodTransformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata);

	}
}
