using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Core.Extensions.Internal
{
	internal static class SyntaxNodeHelper
	{
		internal static NameSyntax ConstructNameSyntax(string name, SyntaxTrivia? trailingTrivia = null, bool insideCref = false, bool onlyName = false)
		{
			var names = name.Split('.').ToList();
			if (onlyName)
			{
				return GetSimpleName(names.Last(), insideCref: insideCref);
			}

			var trailingTriviaList = names.Count <= 2 && trailingTrivia.HasValue
				? TriviaList(trailingTrivia.Value)
				: TriviaList();
			if (names.Count < 2)
			{
				return GetSimpleName(name, TriviaList(), trailingTriviaList, insideCref);
			}
			var result = QualifiedName(IdentifierName(names[0]), GetSimpleName(names[1], TriviaList(), trailingTriviaList, insideCref));
			for (var i = 2; i < names.Count; i++)
			{
				trailingTriviaList = i + 1 == names.Count && trailingTrivia.HasValue
					? TriviaList(trailingTrivia.Value)
					: TriviaList();
				result = QualifiedName(result, GetSimpleName(names[i], TriviaList(), trailingTriviaList, insideCref));
			}
			return result;
		}

		internal static ReturnStatementSyntax GetReturnTaskCompleted(AsyncReturnType asyncReturnType, bool useQualifiedName, SyntaxTriviaList semicolonTrivia = default)
		{
			var name = useQualifiedName
				? ConstructNameSyntax($"System.Threading.Tasks.{asyncReturnType.ToString()}")
				: IdentifierName(asyncReturnType.ToString());
			var expression = asyncReturnType == AsyncReturnType.Task
				? MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					name,
					IdentifierName("CompletedTask"))
				: (ExpressionSyntax)DefaultExpression(name);

			return ReturnStatement(
				Token(TriviaList(), SyntaxKind.ReturnKeyword, TriviaList(Space)),
				expression,
				Token(TriviaList(), SyntaxKind.SemicolonToken, semicolonTrivia)
			);
		}

		// TODO: Use symbol for type arguments
		private static SimpleNameSyntax GetSimpleName(string name, SyntaxTriviaList? leadingTrivia = null, SyntaxTriviaList? trailingTrivia = null, bool insideCref = false)
		{
			if (!name.Contains("<"))
			{
				return IdentifierName(Identifier(leadingTrivia ?? TriviaList(), name, trailingTrivia ?? TriviaList()));
			}
			var start = name.IndexOf('<');
			var type = name.Substring(0, start);
			var typeArguments = name.Substring(start + 1, name.Length - start - 2).Split(',').Select(o => o.Trim(' '));
			var argList = new List<TypeSyntax>();
			foreach (var argument in typeArguments)
			{
				argList.Add(GetSimpleName(argument));
			}
			var list = argList.Count == 1
				? SingletonSeparatedList(argList[0])
				: SeparatedList(argList);
			var typeArgList = TypeArgumentList(list);
			if (insideCref)
			{
				typeArgList = typeArgList
					.WithLessThanToken(Token(TriviaList(), SyntaxKind.LessThanToken, "{", "{", TriviaList()))
					.WithGreaterThanToken(Token(TriviaList(), SyntaxKind.GreaterThanToken, "}", "}", TriviaList()));
			}
			return GenericName(type)
				.WithTypeArgumentList(typeArgList);
		}
	}
}
