using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxTrivia = Microsoft.CodeAnalysis.SyntaxTrivia;

namespace AsyncGenerator.Extensions
{
	public static class SyntaxNodeExtensions
	{
		/// <summary>
		/// Check if the statement is a precondition. A statement will qualify for a precondition only if it is a 
		/// <see cref="IfStatementSyntax"/> and contains a <see cref="ThrowExpressionSyntax"/>
		/// </summary>
		/// <param name="statement">The statement to be checked</param>
		/// <returns></returns>
		public static bool IsPrecondition(this StatementSyntax statement)
		{
			var ifStatement = statement as IfStatementSyntax;
			if (ifStatement?.Statement == null)
			{
				return false;
			}
			// A statement can be a ThrowStatement or a Block that contains a ThrowStatement
			if (!ifStatement.Statement.IsKind(SyntaxKind.Block))
			{
				return ifStatement.Statement.IsKind(SyntaxKind.ThrowStatement);
			}
			var blockStatements = ifStatement.Statement.DescendantNodes().OfType<StatementSyntax>().ToList();
			return blockStatements.Count == 1 && blockStatements[0].IsKind(SyntaxKind.ThrowStatement);
		}

		public static bool IsPartial(this TypeDeclarationSyntax typeDeclaration)
		{
			var interfaceDeclaration = typeDeclaration as InterfaceDeclarationSyntax;
			if (interfaceDeclaration != null)
			{
				return typeDeclaration.Modifiers.Any(o => o.IsKind(SyntaxKind.PartialKeyword));
			}
			var classDeclaration = typeDeclaration as ClassDeclarationSyntax;
			if (classDeclaration != null)
			{
				return typeDeclaration.Modifiers.Any(o => o.IsKind(SyntaxKind.PartialKeyword));
			}
			return false;
		}

		public static TypeDeclarationSyntax WithoutAttributes(this TypeDeclarationSyntax typeDeclaration)
		{
			var interfaceDeclaration = typeDeclaration as InterfaceDeclarationSyntax;
			if (interfaceDeclaration != null)
			{
				return interfaceDeclaration
					.WithAttributeLists(List<AttributeListSyntax>());
			}
			var classDeclaration = typeDeclaration as ClassDeclarationSyntax;
			if (classDeclaration != null)
			{
				return classDeclaration
					.WithAttributeLists(List<AttributeListSyntax>());
			}
			return typeDeclaration;
		}

		public static TypeDeclarationSyntax AddPartial(this TypeDeclarationSyntax typeDeclaration, bool trailingWhitespace = true)
		{
			var interfaceDeclaration = typeDeclaration as InterfaceDeclarationSyntax;
			if (interfaceDeclaration != null && !typeDeclaration.Modifiers.Any(o => o.IsKind(SyntaxKind.PartialKeyword)))
			{
				var token = Token(TriviaList(), SyntaxKind.PartialKeyword, trailingWhitespace ? TriviaList(Space) : TriviaList());
				return interfaceDeclaration.AddModifiers(token);
			}
			var classDeclaration = typeDeclaration as ClassDeclarationSyntax;
			if (classDeclaration != null && !classDeclaration.Modifiers.Any(o => o.IsKind(SyntaxKind.PartialKeyword)))
			{
				var token = Token(TriviaList(), SyntaxKind.PartialKeyword, trailingWhitespace ? TriviaList(Space) : TriviaList());
				return classDeclaration.AddModifiers(token);
			}
			return typeDeclaration;
		}

		public static TypeDeclarationSyntax WithMembers(this TypeDeclarationSyntax typeDeclaration, SyntaxList<MemberDeclarationSyntax> members)
		{
			var interfaceDeclaration = typeDeclaration as InterfaceDeclarationSyntax;
			if (interfaceDeclaration != null)
			{
				return interfaceDeclaration
					.WithMembers(members);
			}
			var classDeclaration = typeDeclaration as ClassDeclarationSyntax;
			if (classDeclaration != null)
			{
				return classDeclaration
					.WithMembers(members);
			}
			return typeDeclaration;
		}

		internal static NamespaceDeclarationSyntax AddUsing(this NamespaceDeclarationSyntax namespaceDeclaration, string fullName, 
			SyntaxTriviaList leadingTrivia, SyntaxTrivia endOfLineTrivia)
		{
			return namespaceDeclaration.AddUsings(
				UsingDirective(ConstructNameSyntax("System.Threading.Tasks"))
					.WithUsingKeyword(Token(leadingTrivia, SyntaxKind.UsingKeyword, TriviaList(Space)))
					.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(endOfLineTrivia)))
					);
		}

		internal static MethodDeclarationSyntax ReturnAsTask(this MethodDeclarationSyntax methodNode, IMethodSymbol methodSymbol, bool withFullName = false)
		{
			if (methodSymbol.ReturnsVoid)
			{
				var taskNode = IdentifierName("Task").WithTriviaFrom(methodNode.ReturnType);
				return methodNode
					.WithReturnType(
						withFullName
							? QualifiedName(ConstructNameSyntax("System.Threading.Tasks"), taskNode)
							: (TypeSyntax)taskNode);
			}
			var genericTaskNode = GenericName("Task")
				.WithTriviaFrom(methodNode.ReturnType)
				.AddTypeArgumentListArguments(methodNode.ReturnType.WithoutTrivia());
			return methodNode
				.WithReturnType(
					withFullName
						? QualifiedName(ConstructNameSyntax("System.Threading.Tasks"), genericTaskNode)
						: (TypeSyntax)genericTaskNode);
		}

		public static SyntaxNode GetBody(this MethodDeclarationSyntax methodNode)
		{
			return methodNode.Body ?? (SyntaxNode) methodNode.ExpressionBody;
		}

		internal static ReturnStatementSyntax ToReturnStatement(this StatementSyntax statement)
		{
			if (statement.IsKind(SyntaxKind.ReturnStatement))
			{
				return (ReturnStatementSyntax) statement;
			}
			var expressionStatement = statement as ExpressionStatementSyntax;
			if (expressionStatement != null)
			{
				return ReturnStatement(
					Token(expressionStatement.GetLeadingTrivia(), SyntaxKind.ReturnKeyword, TriviaList(Space)),
					expressionStatement.Expression.WithoutLeadingTrivia(),
					expressionStatement.SemicolonToken);
			}
			throw new InvalidOperationException($"Cannot convert statement {statement} to ReturnStatementSyntax");
		}

		internal static SimpleNameSyntax GetSimpleName(this SyntaxNode node, int spanStart, int spanLength, bool descendIntoTrivia = false)
		{
			return node
				.DescendantNodes(descendIntoTrivia: descendIntoTrivia)
				.OfType<SimpleNameSyntax>()
				.First(
					o =>
					{
						if (!o.IsKind(SyntaxKind.GenericName))
						{
							return o.Span.Start == spanStart && o.Span.Length == spanLength;
						}
						var token = o.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken));
						return token.Span.Start == spanStart && token.Span.Length == spanLength;
					});
		}

		internal static SimpleNameSyntax GetSimpleName(this SyntaxNode node, TextSpan sourceSpan, bool descendIntoTrivia = false)
		{
			return node
				.DescendantNodes(descendIntoTrivia: descendIntoTrivia)
				.OfType<SimpleNameSyntax>()
				.First(
					o =>
					{
						if (o.IsKind(SyntaxKind.GenericName))
						{
							return o.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).Span == sourceSpan;
						}
						return o.Span == sourceSpan;
					});
		}

		internal static SyntaxToken AddAutoGeneratedTrivia(this SyntaxToken token, SyntaxTrivia endOfLineTrivia)
		{
			var triviaList = new []
			{
				Comment("//------------------------------------------------------------------------------"),
				endOfLineTrivia,
				Comment("// <auto-generated>"),
				endOfLineTrivia,
				Comment("//     This code was generated by AsyncGenerator."),
				endOfLineTrivia,
				Comment("//"),
				endOfLineTrivia,
				Comment("//     Changes to this file may cause incorrect behavior and will be lost if"),
				endOfLineTrivia,
				Comment("//     the code is regenerated."),
				endOfLineTrivia,
				Comment("// </auto-generated>"),
				endOfLineTrivia,
				Comment("//------------------------------------------------------------------------------"),
				endOfLineTrivia,
				endOfLineTrivia,
				endOfLineTrivia
			};

			return token.WithLeadingTrivia(
				token.HasLeadingTrivia 
					? triviaList.Union(token.LeadingTrivia)
					: triviaList);
		}

		private static NameSyntax ConstructNameSyntax(string name)
		{
			var names = name.Split('.').ToList();
			if (names.Count < 2)
			{
				return IdentifierName(name);
			}
			var result = QualifiedName(IdentifierName(names[0]), IdentifierName(names[1]));
			for (var i = 2; i < names.Count; i++)
			{
				result = QualifiedName(result, IdentifierName(names[i]));
			}
			return result;
		}

	}
}
