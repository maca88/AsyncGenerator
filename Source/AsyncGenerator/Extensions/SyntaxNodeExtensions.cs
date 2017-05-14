﻿using System;
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
			return typeDeclaration.WithLeadingTrivia(typeDeclaration.GetLeadingTrivia()); // Modify the node for consistency so that the Parent node will be always null
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

		internal static bool IsReturnStatementRequired(this BlockSyntax node)
		{
			var lastStatement = node.Statements.LastOrDefault();
			if (lastStatement == null)
			{
				return true;
			}
			if (lastStatement.IsKind(SyntaxKind.ReturnStatement))
			{
				return false;
			}
			// We need to check if the body has an if else statement and both have a return statement
			if (lastStatement is IfStatementSyntax ifStatement)
			{
				return IsReturnStatementRequired(ifStatement);
			}
			return true;
		}

		internal static bool IsReturnStatementRequired(this IfStatementSyntax ifStatement)
		{
			if (ifStatement.Else?.Statement == null || ifStatement.Statement == null)
			{
				return true;
			}
			var isReturnRequired = ifStatement.Statement.IsKind(SyntaxKind.ReturnKeyword) ||
			                       (
				                       ifStatement.Statement.IsKind(SyntaxKind.Block) &&
				                       IsReturnStatementRequired((BlockSyntax)ifStatement.Statement)
			                       );
			var elseStatement = ifStatement.Else.Statement;
			var elseIsReturnRequired = elseStatement.IsKind(SyntaxKind.ReturnKeyword) ||
			                           (
				                           elseStatement.IsKind(SyntaxKind.Block) &&
				                           IsReturnStatementRequired((BlockSyntax)elseStatement)
			                           ) ||
			                           (
				                           elseStatement.IsKind(SyntaxKind.IfStatement) &&
				                           IsReturnStatementRequired((IfStatementSyntax)elseStatement)
			                           );
			return isReturnRequired || elseIsReturnRequired;
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

		internal static SyntaxTrivia GetLeadingWhitespace(this SyntaxNode node)
		{
			return node.GetLeadingTrivia().FirstOrDefault(o => o.IsKind(SyntaxKind.WhitespaceTrivia));
		}

		internal static SyntaxTrivia GetEndOfLine(this SyntaxNode node)
		{
			return node.DescendantTrivia().First(o => o.IsKind(SyntaxKind.EndOfLineTrivia));
		}

		internal static SyntaxTrivia GetIndent(this SyntaxNode node, SyntaxTrivia? leadingWhitespaceTrivia = null, SyntaxTrivia? parentLeadingWhitespace = null)
		{
			var nodeIndent = (leadingWhitespaceTrivia ?? node.GetLeadingWhitespace()).ToFullString();
			var parentIndent = parentLeadingWhitespace.HasValue 
				? parentLeadingWhitespace.Value.ToFullString()
				: node.Parent?.GetLeadingWhitespace().ToFullString();
			if (parentIndent == null)
			{
				return default(SyntaxTrivia);
			}

			if (parentIndent.Length > nodeIndent.Length)
			{
				// The parent node is using a different indent as the node. Probably the parent is using spaces and child tabs
				return default(SyntaxTrivia);
			}

			return Whitespace(nodeIndent.Substring(parentIndent.Length));
		}

		internal static SyntaxNode PrependCloseBraceLeadingTrivia(this SyntaxNode node, SyntaxTriviaList leadingTrivia)
		{
			if (node is TypeDeclarationSyntax typeNode)
			{
				return typeNode
					.ReplaceToken(typeNode.CloseBraceToken,
						typeNode.CloseBraceToken.WithLeadingTrivia(leadingTrivia.AddRange(typeNode.CloseBraceToken.LeadingTrivia)));
			}
			if (node is NamespaceDeclarationSyntax nsNode)
			{
				return nsNode
					.ReplaceToken(nsNode.CloseBraceToken,
						nsNode.CloseBraceToken.WithLeadingTrivia(leadingTrivia.AddRange(nsNode.CloseBraceToken.LeadingTrivia)));
			}
			throw new InvalidOperationException($"Unable to prepend CloseBraceToken trivia to node {node}");
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

		// TODO: remove
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

		internal static TypeDeclarationSyntax ReplaceWithMembers(this TypeDeclarationSyntax node, 
			MemberDeclarationSyntax member, MemberDeclarationSyntax newMember,
			IEnumerable<FieldDeclarationSyntax> extraFields, IEnumerable<MethodDeclarationSyntax> extraMethods)
		{
			if (newMember == null)
			{
				throw new ArgumentNullException(nameof(newMember));
			}
			if (extraMethods != null)
			{
				// Append all additional members after the original one
				var index = node.Members.IndexOf(member);
				node = node.ReplaceNode(member, newMember);
				var currIndex = index + 1;
				foreach (var extraMethod in extraMethods)
				{
					node = node.WithMembers(node.Members.Insert(currIndex, extraMethod));
					currIndex++;
				}
			}
			else
			{
				node = node.ReplaceNode(member, newMember);
			}
			if (extraFields != null)
			{
				foreach (var extraField in extraFields)
				{
					node = node.WithMembers(node.Members.Insert(0, extraField));
				}
			}
			return node;
		}

		internal static NamespaceDeclarationSyntax ReplaceWithMembers(this NamespaceDeclarationSyntax node,
			MemberDeclarationSyntax member, IReadOnlyList<MemberDeclarationSyntax> newMembers)
		{
			if (newMembers.Count == 1)
			{
				node = node.ReplaceNode(member, newMembers[0]);
			}
			else
			{
				// Append all additional members after the original one
				var index = node.Members.IndexOf(member);
				node = node.ReplaceNode(member, newMembers[0]);
				var currIndex = index + 1;
				foreach (var newMember in newMembers.Skip(1))
				{
					node = node.WithMembers(node.Members.Insert(currIndex, newMember));
					currIndex++;
				}
			}
			return node;
		}

		// TODO: take the original directive whitespace
		internal static TypeDeclarationSyntax RemoveMembersKeepDirectives(this TypeDeclarationSyntax node, 
			Predicate<MemberDeclarationSyntax> predicate, SyntaxTrivia directiveLeadingWhitespace)
		{
			var annotations = new List<string>();
			foreach (var memberSpan in node.Members.Where(o => predicate(o)).Select(o => o.Span))
			{
				var annotation = Guid.NewGuid().ToString();
				annotations.Add(annotation);
				var member = node.Members.First(o => o.Span == memberSpan);
				node = node.ReplaceNode(member, member.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
			}
			foreach (var annotation in annotations)
			{
				node = RemoveNodeKeepDirectives(node, annotation, directiveLeadingWhitespace);
			}
			return node;
		}

		// TODO: take the original directive whitespace
		internal static NamespaceDeclarationSyntax RemoveMembersKeepDirectives(this NamespaceDeclarationSyntax node,
			Predicate<MemberDeclarationSyntax> predicate, SyntaxTrivia directiveLeadingWhitespace)
		{
			var annotations = new List<string>();
			foreach (var memberSpan in node.Members.Where(o => predicate(o)).Select(o => o.Span))
			{
				var annotation = Guid.NewGuid().ToString();
				annotations.Add(annotation);
				var member = node.Members.First(o => o.Span == memberSpan);
				node = node.ReplaceNode(member, member.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
			}
			foreach (var annotation in annotations)
			{
				node = RemoveNodeKeepDirectives(node, annotation, directiveLeadingWhitespace);
			}
			return node;
		}

		// TODO: take the original directive whitespace
		internal static T RemoveNodeKeepDirectives<T>(this T node, string annotation, SyntaxTrivia directiveLeadingWhitespace)
			where T : SyntaxNode
		{
			var toRemoveNode = node.GetAnnotatedNodes(annotation).First();
			// We need to add a whitespace trivia to keept directives as they will not have any leading whitespace
			var directiveAnnotations = new List<string>();
			var directiveSpans = toRemoveNode.GetDirectives().Select(o => o.Span);
			foreach (var directiveSpan in directiveSpans)
			{
				var directiveNode = toRemoveNode.GetDirectives().First(o => o.Span == directiveSpan).GetStructure();
				var directiveAnnotation = Guid.NewGuid().ToString();
				directiveAnnotations.Add(directiveAnnotation);
				node = node.ReplaceNode(directiveNode, directiveNode.WithAdditionalAnnotations(new SyntaxAnnotation(directiveAnnotation)));
				toRemoveNode = node.GetAnnotatedNodes(annotation).First();
			}
			node = node.RemoveNode(toRemoveNode, SyntaxRemoveOptions.KeepUnbalancedDirectives);
			if (node == null)
			{
				return null; // TODO: we need to preserve or remove directives!
			}
			foreach (var directiveAnnotation in directiveAnnotations)
			{
				var directiveNode = node.GetAnnotatedNodes(directiveAnnotation).FirstOrDefault();
				if (directiveNode == null)
				{
					continue; // The directive was removed
				}
				node = node.ReplaceNode(directiveNode,
					directiveNode.WithLeadingTrivia(directiveLeadingWhitespace));
			}
			return node;
		}

		private static IEnumerable<SyntaxTrivia> GetDirectives(this SyntaxNode node)
		{
			if (node is TypeDeclarationSyntax typeNode)
			{
				return typeNode.GetLeadingTrivia().Where(o => o.IsDirective)
					.Union(typeNode.CloseBraceToken.LeadingTrivia.Where(o => o.IsDirective));
			}
			if (node is NamespaceDeclarationSyntax nsNode)
			{
				return nsNode.GetLeadingTrivia().Where(o => o.IsDirective)
					.Union(nsNode.CloseBraceToken.LeadingTrivia.Where(o => o.IsDirective));
			}
			return node.GetLeadingTrivia().Where(o => o.IsDirective);
		}

		/// <summary>
		/// Normalize body by using the NormalizeWhitespace method using the original method that belongs to a <see cref="CompilationUnitSyntax"/>
		/// </summary>
		/// <param name="originalNode"></param>
		/// <param name="body"></param>
		/// <param name="indentTrivia"></param>
		/// <param name="endOfLineTrivia"></param>
		/// <returns></returns>
		internal static BlockSyntax NormalizeMethodBody(this MethodDeclarationSyntax originalNode, BlockSyntax body, SyntaxTrivia indentTrivia, SyntaxTrivia endOfLineTrivia)
		{
			var annotation = Guid.NewGuid().ToString();
			var docNode = originalNode.Ancestors().OfType<CompilationUnitSyntax>().First();
			docNode = docNode
				.ReplaceNode(originalNode, originalNode
					.WithBody(body)
					.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)))
				.NormalizeWhitespace(indentTrivia.ToFullString(), endOfLineTrivia.ToString());
			return docNode.GetAnnotatedNodes(annotation).OfType<MethodDeclarationSyntax>().First().Body;
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

		internal static InvocationExpressionSyntax ForwardCall(this MethodDeclarationSyntax methodNode, IMethodSymbol symbol, string identifier, params ArgumentSyntax[] additionalArgs)
		{
			var name = methodNode.TypeParameterList != null
				? GenericName(identifier)
					.WithTypeArgumentList(
						TypeArgumentList(
							SeparatedList<TypeSyntax>(
								methodNode.TypeParameterList.Parameters.Select(o => IdentifierName(o.Identifier.ValueText))
							)))
				: (SimpleNameSyntax)IdentifierName(identifier);
			MemberAccessExpressionSyntax accessExpression = null;
			if (symbol.MethodKind == MethodKind.ExplicitInterfaceImplementation)
			{
				// Explicit implementations needs an explicit cast (ie. ((Type)this).SyncMethod() )
				accessExpression = MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					ParenthesizedExpression(
						CastExpression(
							IdentifierName(symbol.ExplicitInterfaceImplementations.Single().ContainingType.Name),
							ThisExpression())),
					name);
			}
			var comma = Token(TriviaList(), SyntaxKind.CommaToken, TriviaList(Space));
			var arguments = methodNode.ParameterList.Parameters
				.Select(o => Argument(IdentifierName(o.Identifier.Text)))
				.Union(additionalArgs)
				.SelectMany((o, i) => i == 0
					? new SyntaxNodeOrToken[] {o}
					: new SyntaxNodeOrToken[] {comma, o});

			return InvocationExpression(accessExpression ?? (ExpressionSyntax)name)
				.WithArgumentList(
					ArgumentList(
						SeparatedList<ArgumentSyntax>(arguments)));
		}

		internal static MethodDeclarationSyntax AddCancellationTokenParameter(this MethodDeclarationSyntax node, 
			string parameterName, 
			bool defaultParameter,
			SyntaxTrivia leadingWhitespace,
			SyntaxTrivia endOfLine)
		{
			var totalParameters = node.ParameterList.Parameters.Count;
			var parameter = Parameter(
					Identifier(
						TriviaList(),
						parameterName,
						defaultParameter ? TriviaList(Space) : TriviaList()))
				.WithType(
					IdentifierName(
						Identifier(
							TriviaList(),
							nameof(CancellationToken),
							TriviaList(Space))));
			if (defaultParameter)
			{
				parameter = parameter
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
			}

			var comma = Token(TriviaList(), SyntaxKind.CommaToken, TriviaList(Space));
			var parameters = node.ParameterList.Parameters
				.Union(new []{ parameter })
				.SelectMany((o, i) => i == 0
					? new SyntaxNodeOrToken[] { o }
					: new SyntaxNodeOrToken[] { comma, o });
			node = node.WithParameterList(node.ParameterList.WithParameters(SeparatedList<ParameterSyntax>(parameters)));
			
			var commentTrivia = node.GetLeadingTrivia().FirstOrDefault(o => o.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
			var commentNode = (DocumentationCommentTriviaSyntax)commentTrivia.GetStructure();
			if (commentNode == null)
			{
				return node;
			}
			
			var lastParam = commentNode.Content.OfType<XmlElementSyntax>()
				.LastOrDefault(o => o.StartTag.Name.ToString() == "param");
			var eol = endOfLine.ToFullString();
			var leadingSpace = leadingWhitespace.ToFullString();
			int index;
			if (lastParam != null)
			{
				index = commentNode.Content.IndexOf(lastParam);
			}
			else
			{
				// If the method have at least one parameter and non of them has a param tag then we do not want to add a param tag in order to avoid warnings
				if (totalParameters > 0)
				{
					return node;
				}
				// If there is no param tags we need to insert after the summary tag
				var summaryNode = commentNode.Content.OfType<XmlElementSyntax>()
					.FirstOrDefault(o => o.StartTag.Name.ToString() == "summary");
				if (summaryNode == null)
				{
					// Can be a include or inheritdoc tag
					return node;
				}
				index = commentNode.Content.IndexOf(summaryNode);
			}
			var xmlText = XmlText()
				.WithTextTokens(
					TokenList(
						XmlTextNewLine(
							TriviaList(),
							eol,
							eol,
							TriviaList()), XmlTextLiteral(
							TriviaList(
								DocumentationCommentExterior($"{leadingSpace}///")),
							" ",
							" ",
							TriviaList())));
			var newCommentNode = commentNode
				.WithContent(
					commentNode.Content
						.InsertRange(index + 1, new XmlNodeSyntax[]
						{
							xmlText,
							CreateParameter(parameterName, "A cancellation token that can be used to cancel the work")
						})
				);
			node = node.ReplaceNode(commentNode, newCommentNode);
			

			return node;
		}

		private static XmlElementSyntax CreateParameter(string identifierName, string description)
		{
			var identifier = Identifier(identifierName);

			var attribute = XmlNameAttribute(
				XmlName(Identifier(TriviaList(Space), "name", TriviaList())),
				Token(SyntaxKind.DoubleQuoteToken),
				IdentifierName(identifier),
				Token(SyntaxKind.DoubleQuoteToken));

			var startTag = XmlElementStartTag(XmlName("param"))
				.WithAttributes(new SyntaxList<XmlAttributeSyntax>().Add(attribute));

			var endTag = XmlElementEndTag(XmlName("param"));

			var content = SingletonList<XmlNodeSyntax>(
				XmlText()
					.WithTextTokens(
						TokenList(
							XmlTextLiteral(
								TriviaList(),
								description,
								description,
								TriviaList()))));
			return XmlElement(startTag, content, endTag);
		}

		internal static InvocationExpressionSyntax AddArgument(this InvocationExpressionSyntax node, string argumentName)
		{
			return node.AddArgumentListArguments(Argument(IdentifierName(argumentName)));
		}

		internal static SyntaxTriviaList AddAutoGeneratedTrivia(SyntaxTrivia endOfLineTrivia)
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

			return TriviaList(triviaList);
		}

		internal static TypeDeclarationSyntax WithXmlContentTrivia(this TypeDeclarationSyntax node, SyntaxTrivia endOfLineTrivia, SyntaxTrivia leadingSyntaxTrivia)
		{
			var endOfLine = endOfLineTrivia.ToFullString();
			var leadingSpace = leadingSyntaxTrivia.ToFullString();
			var comment = DocumentationCommentTrivia(
				SyntaxKind.SingleLineDocumentationCommentTrivia,
				List(
					new XmlNodeSyntax[]
					{
						XmlText()
							.WithTextTokens(
								TokenList(XmlTextLiteral(TriviaList(DocumentationCommentExterior("///")), " ", " ", TriviaList()))),
						XmlExampleElement(
								SingletonList<XmlNodeSyntax>(
									XmlText()
										.WithTextTokens(
											TokenList(
												XmlTextNewLine(TriviaList(), endOfLine, endOfLine, TriviaList()),
												XmlTextLiteral(
													TriviaList(DocumentationCommentExterior($"{leadingSpace}///")),
													" Contains generated async methods",
													" Contains generated async methods",
													TriviaList()),
												XmlTextNewLine(
													TriviaList(),
													endOfLine,
													endOfLine,
													TriviaList()),
												XmlTextLiteral(
													TriviaList(DocumentationCommentExterior($"{leadingSpace}///")),
													" ",
													" ",
													TriviaList())))))
							.WithStartTag(XmlElementStartTag(XmlName(Identifier("content"))))
							.WithEndTag(XmlElementEndTag(XmlName(Identifier("content")))),
						XmlText()
							.WithTextTokens(TokenList(XmlTextNewLine(TriviaList(), endOfLine, endOfLine, TriviaList())))
					}));

			// We have to preserve directives, so we need to modify the existing leading trivia
			var leadingTrivia = node.GetLeadingTrivia();
			var trivias = leadingTrivia
				.Where(o => o.IsKind(SyntaxKind.SingleLineCommentTrivia) || // double slash
				            o.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)) // triple slash
				.ToList();
			if (trivias.Any())
			{
				// We have to replace the last comment with ours and remove other if they exist
				var indexes = trivias.Select(o => leadingTrivia.IndexOf(o)).OrderByDescending(o => o).ToList();
				leadingTrivia = leadingTrivia.Replace(leadingTrivia.ElementAt(indexes[0]), Trivia(comment));
				foreach (var index in indexes.Skip(1))
				{
					leadingTrivia = leadingTrivia.RemoveAt(index);
				}
			}
			else
			{
				leadingTrivia = leadingTrivia.AddRange(TriviaList(
					Trivia(comment),
					leadingSyntaxTrivia
				));
			}
			return node.WithLeadingTrivia(leadingTrivia);
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
