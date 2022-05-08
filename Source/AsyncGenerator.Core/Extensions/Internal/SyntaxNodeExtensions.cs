using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static AsyncGenerator.Core.Extensions.Internal.SyntaxNodeHelper;

namespace AsyncGenerator.Core.Extensions.Internal
{
	internal static partial class SyntaxNodeExtensions
	{
		internal static TypeSyntax WrapIntoTask(this TypeSyntax typeNode, bool withFullName = false)
		{
			return WrapInto(typeNode, nameof(Task), withFullName);
		}

		internal static TypeSyntax WrapIntoValueTask(this TypeSyntax typeNode, bool withFullName = false)
		{
			return WrapInto(typeNode, nameof(ValueTask), withFullName);
		}

		private static TypeSyntax WrapInto(this TypeSyntax typeNode, string typeName, bool withFullName = false)
		{
			if (typeNode.ChildTokens().Any(o => o.IsKind(SyntaxKind.VoidKeyword)))
			{
				var taskNode = IdentifierName(typeName).WithTriviaFrom(typeNode);
				return withFullName
					? QualifiedName(ConstructNameSyntax("System.Threading.Tasks"), taskNode)
					: (TypeSyntax)taskNode;
			}
			var genericTaskNode = GenericName(typeName)
				.WithTriviaFrom(typeNode)
				.AddTypeArgumentListArguments(typeNode.WithoutTrivia());
			return withFullName
				? QualifiedName(ConstructNameSyntax("System.Threading.Tasks"), genericTaskNode)
				: (TypeSyntax)genericTaskNode;
		}
	}
}
