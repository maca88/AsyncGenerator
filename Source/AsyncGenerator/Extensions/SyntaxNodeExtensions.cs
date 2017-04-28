using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxTrivia = Microsoft.CodeAnalysis.SyntaxTrivia;

namespace AsyncGenerator.Extensions
{
	internal static partial class SyntaxNodeExtensions
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
				UsingDirective(ConstructNameSyntax(fullName))
					.WithUsingKeyword(Token(leadingTrivia, SyntaxKind.UsingKeyword, TriviaList(Space)))
					.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(endOfLineTrivia)))
					);
		}

		internal static bool EndsWithReturnStatement(this BlockSyntax node)
		{
			var lastStatement = node.Statements.LastOrDefault();
			return lastStatement?.IsKind(SyntaxKind.ReturnStatement) == true;
		}

		internal static MethodDeclarationSyntax ReturnAsTask(this MethodDeclarationSyntax methodNode, bool withFullName = false)
		{
			return methodNode.WithReturnType(
				methodNode.ReturnType.WrapIntoTask(withFullName)
			);
		}

		internal static TypeSyntax WrapIntoTask(this TypeSyntax typeNode, bool withFullName = false)
		{
			if (typeNode.ChildTokens().Any(o => o.IsKind(SyntaxKind.VoidKeyword)))
			{
				var taskNode = IdentifierName(nameof(Task)).WithTriviaFrom(typeNode);
				return withFullName
					? QualifiedName(ConstructNameSyntax("System.Threading.Tasks"), taskNode)
					: (TypeSyntax)taskNode;
			}
			var genericTaskNode = GenericName(nameof(Task))
				.WithTriviaFrom(typeNode)
				.AddTypeArgumentListArguments(typeNode.WithoutTrivia());
			return withFullName
				? QualifiedName(ConstructNameSyntax("System.Threading.Tasks"), genericTaskNode)
				: (TypeSyntax) genericTaskNode;
		}

		internal static BlockSyntax AddWhitespace(this BlockSyntax blockNode, SyntaxTrivia whitespace)
		{
			var statements = new SyntaxList<StatementSyntax>();
			foreach (var statement in blockNode.Statements)
			{
				statements = statements.Add(statement.WithLeadingTrivia(statement.GetLeadingTrivia().Add(whitespace)));
			}
			return blockNode
				.WithStatements(statements)
				.WithOpenBraceToken(
					blockNode.OpenBraceToken.WithLeadingTrivia(blockNode.OpenBraceToken.LeadingTrivia.Add(whitespace)))
				.WithCloseBraceToken(
					blockNode.CloseBraceToken.WithLeadingTrivia(blockNode.CloseBraceToken.LeadingTrivia.Add(whitespace)));
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

		internal static SyntaxNode GetFunctionBody(this SyntaxNode node)
		{
			switch (node.Kind())
			{
				case SyntaxKind.SimpleLambdaExpression:
				case SyntaxKind.AnonymousMethodExpression:
				case SyntaxKind.ParenthesizedLambdaExpression:
					return ((AnonymousFunctionExpressionSyntax) node).Body;
				case SyntaxKind.LocalFunctionStatement:
					return ((LocalFunctionStatementSyntax) node).Body ?? (SyntaxNode) ((LocalFunctionStatementSyntax) node).ExpressionBody;
				case SyntaxKind.MethodDeclaration:
					return ((MethodDeclarationSyntax) node).Body ?? (SyntaxNode) ((MethodDeclarationSyntax) node).ExpressionBody;
				default:
					throw new InvalidOperationException($"Node {node} is not a function");
			}
		}

		internal static bool IsFunction(this SyntaxNode node)
		{
			switch (node.Kind())
			{
				case SyntaxKind.SimpleLambdaExpression:
				case SyntaxKind.AnonymousMethodExpression:
				case SyntaxKind.ParenthesizedLambdaExpression:
				case SyntaxKind.LocalFunctionStatement:
				case SyntaxKind.MethodDeclaration:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Check if the node is returned with a <see cref="ReturnStatementSyntax"/>
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		internal static bool IsReturned(this SyntaxNode node)
		{
			if (node.IsKind(SyntaxKind.ReturnStatement))
			{
				return false;
			}
			var statement = node.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
			if (statement == null || !statement.IsKind(SyntaxKind.ReturnStatement))
			{
				return false;
			}
			var currNode = node;
			while (!currNode.IsKind(SyntaxKind.ReturnStatement))
			{
				currNode = currNode.Parent;
				switch (currNode.Kind())
				{
					case SyntaxKind.ConditionalExpression:
						var conditionExpression = (ConditionalExpressionSyntax)currNode;
						if (conditionExpression.Condition.Contains(node))
						{
							return false;
						}
						continue;
					case SyntaxKind.ReturnStatement:
						return true;
					default:
						return false;
				}
			}
			return true;
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

		internal static bool HasUsing(this NamespaceDeclarationSyntax node, string usingFullName)
		{
			foreach (var ancestor in node.AncestorsAndSelf())
			{
				if (ancestor is NamespaceDeclarationSyntax namespaceNode)
				{
					if (namespaceNode.Usings.Any(o => o.Name.ToString() == usingFullName))
					{
						return true;
					}
					continue;
				}
				if (!(ancestor is CompilationUnitSyntax compilationNode))
				{
					continue;
				}
				if (compilationNode.Usings.Any(o => o.Name.ToString() == usingFullName))
				{
					return true;
				}
			}
			return false;
		}

		internal static InvocationExpressionSyntax AddCancellationTokenArgumentIf(this InvocationExpressionSyntax node, string argumentName, bool condition)
		{
			if (!condition)
			{
				return node;
			}
			if (!node.ArgumentList.Arguments.Any())
			{
				return node.AddArgumentListArguments(Argument(IdentifierName(argumentName)));
			}
			// We need to add an extra space after the comma
			var argumentList = SeparatedList<ArgumentSyntax>(
				node.ArgumentList.Arguments.GetWithSeparators()
					.Concat(new SyntaxNodeOrToken[]
					{
						Token(TriviaList(), SyntaxKind.CommaToken, TriviaList(Space)),
						Argument(IdentifierName(argumentName))
					})
			);
			return node.WithArgumentList(node.ArgumentList.WithArguments(argumentList));
		}

		internal static MethodDeclarationSyntax AddCancellationTokenParameterIf(this MethodDeclarationSyntax node, string parameterName, bool condition)
		{
			if (!condition)
			{
				return node;
			}
			var parameter = Parameter(
					Identifier(
						TriviaList(),
						parameterName,
						TriviaList(
							Space)))
				.WithType(
					IdentifierName(
						Identifier(
							TriviaList(),
							nameof(CancellationToken),
							TriviaList(
								Space))))
				.WithDefault(
					EqualsValueClause(
							DefaultExpression(
								IdentifierName(nameof(CancellationToken))))
						.WithEqualsToken(
							Token(
								TriviaList(),
								SyntaxKind.EqualsToken,
								TriviaList(
									Space))));
			if (!node.ParameterList.Parameters.Any())
			{
				return node.AddParameterListParameters(parameter);
			}
			// We need to add an extra space after the comma
			var parameterList = SeparatedList<ParameterSyntax>(
				node.ParameterList.Parameters.GetWithSeparators()
					.Concat(new SyntaxNodeOrToken[]
					{
						Token(TriviaList(), SyntaxKind.CommaToken, TriviaList(Space)),
						parameter
					})
			);
			return node.WithParameterList(node.ParameterList.WithParameters(parameterList));
		}

		internal static InvocationExpressionSyntax AddArgument(this InvocationExpressionSyntax node, string argumentName)
		{
			return node.AddArgumentListArguments(Argument(IdentifierName(argumentName)));
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

		internal static InvocationExpressionSyntax Invoke(this IdentifierNameSyntax identifier, ParameterListSyntax parameterList)
		{
			var callArguments = parameterList.Parameters
				.Select(o => Argument(IdentifierName(o.Identifier)))
				.ToList();
			var argumentList = new List<SyntaxNodeOrToken>();
			var argSeparator = Token(TriviaList(), SyntaxKind.CommaToken, TriviaList(Space));
			for (var i = 0; i < callArguments.Count; i++)
			{
				argumentList.Add(callArguments[i].WithoutTrivia());
				if (i + 1 < callArguments.Count)
				{
					argumentList.Add(argSeparator);
				}
			}
			return InvocationExpression(identifier, ArgumentList(SeparatedList<ArgumentSyntax>(argumentList)));
		}

		internal static NameSyntax ConstructNameSyntax(string name)
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
