using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Transformation.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SyntaxTrivia = Microsoft.CodeAnalysis.SyntaxTrivia;

namespace AsyncGenerator.Extensions.Internal
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
					.WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>());
			}
			var classDeclaration = typeDeclaration as ClassDeclarationSyntax;
			if (classDeclaration != null)
			{
				return classDeclaration
					.WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>());
			}
			return typeDeclaration;
		}

		public static TypeDeclarationSyntax AddPartial(this TypeDeclarationSyntax typeDeclaration, bool trailingWhitespace = true)
		{
			var interfaceDeclaration = typeDeclaration as InterfaceDeclarationSyntax;
			if (interfaceDeclaration != null && !typeDeclaration.Modifiers.Any(o => o.IsKind(SyntaxKind.PartialKeyword)))
			{
				var token = SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.PartialKeyword, trailingWhitespace ? SyntaxFactory.TriviaList(SyntaxFactory.Space) : SyntaxFactory.TriviaList());
				return interfaceDeclaration.AddModifiers(token);
			}
			var classDeclaration = typeDeclaration as ClassDeclarationSyntax;
			if (classDeclaration != null && !classDeclaration.Modifiers.Any(o => o.IsKind(SyntaxKind.PartialKeyword)))
			{
				var token = SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.PartialKeyword, trailingWhitespace ? SyntaxFactory.TriviaList(SyntaxFactory.Space) : SyntaxFactory.TriviaList());
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
				SyntaxFactory.UsingDirective(ConstructNameSyntax(fullName))
					.WithUsingKeyword(SyntaxFactory.Token(leadingTrivia, SyntaxKind.UsingKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)))
					.WithSemicolonToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.SemicolonToken, SyntaxFactory.TriviaList(endOfLineTrivia)))
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

			var isReturnRequired = !ifStatement.Statement.IsKind(SyntaxKind.ReturnStatement);
			if (isReturnRequired && ifStatement.Statement.IsKind(SyntaxKind.Block))
			{
				isReturnRequired = IsReturnStatementRequired((BlockSyntax) ifStatement.Statement);
			}
			if (isReturnRequired)
			{
				return true;
			}

			var elseStatement = ifStatement.Else.Statement;
			var elseIsReturnRequired = !elseStatement.IsKind(SyntaxKind.ReturnStatement);
			if (elseIsReturnRequired && elseStatement.IsKind(SyntaxKind.Block))
			{
				elseIsReturnRequired = IsReturnStatementRequired((BlockSyntax)elseStatement);
			}
			else if (elseIsReturnRequired && elseStatement.IsKind(SyntaxKind.IfStatement))
			{
				elseIsReturnRequired = IsReturnStatementRequired((IfStatementSyntax)elseStatement);
			}
			return elseIsReturnRequired;
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
				var taskNode = SyntaxFactory.IdentifierName(nameof(Task)).WithTriviaFrom(typeNode);
				return withFullName
					? SyntaxFactory.QualifiedName(ConstructNameSyntax("System.Threading.Tasks"), taskNode)
					: (TypeSyntax)taskNode;
			}
			var genericTaskNode = SyntaxFactory.GenericName(nameof(Task))
				.WithTriviaFrom(typeNode)
				.AddTypeArgumentListArguments(typeNode.WithoutTrivia());
			return withFullName
				? SyntaxFactory.QualifiedName(ConstructNameSyntax("System.Threading.Tasks"), genericTaskNode)
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

			return SyntaxFactory.Whitespace(nodeIndent.Substring(parentIndent.Length));
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
				return SyntaxFactory.ReturnStatement(
					SyntaxFactory.Token(expressionStatement.GetLeadingTrivia(), SyntaxKind.ReturnKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)),
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

		internal static T AppendIndent<T>(this T node, string indent) where T : SyntaxNode
		{
			var indentRewriter = new IndentRewriter(indent);
			return (T)indentRewriter.Visit(node);
		}

		internal static InvocationExpressionSyntax AddCancellationTokenArgumentIf(this InvocationExpressionSyntax node, string argumentName, IBodyFunctionReferenceAnalyzationResult functionReference)
		{
			if (!functionReference.CancellationTokenRequired)
			{
				return node;
			}

			ExpressionSyntax argExpression = argumentName != null 
				? SyntaxFactory.IdentifierName(argumentName) 
				: ConstructNameSyntax("CancellationToken.None");

			var invokedMethod = functionReference.ReferenceSymbol;
			// We have to add a name colon when one of the previous argument has it or if the method invoked has a default parameter that is not passed
			var colonRequired = node.ArgumentList.Arguments.Any(o => o.NameColon != null) || (node.ArgumentList.Arguments.Count < invokedMethod.Parameters.Length);
			var argument = SyntaxFactory.Argument(argExpression);
			if (colonRequired)
			{
				argument = argument.WithNameColon(SyntaxFactory.NameColon("cancellationToken")); // TODO: dynamic
			}

			if (!node.ArgumentList.Arguments.Any())
			{
				return node.AddArgumentListArguments(argument);
			}
			
			// We need to add an extra space after the comma
			var argumentList = SyntaxFactory.SeparatedList<ArgumentSyntax>(
				node.ArgumentList.Arguments.GetWithSeparators()
					.Concat(new SyntaxNodeOrToken[]
					{
						SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.CommaToken, SyntaxFactory.TriviaList(SyntaxFactory.Space)),
						argument
					})
			);
			return node.WithArgumentList(node.ArgumentList.WithArguments(argumentList));
		}

		internal static InvocationExpressionSyntax ForwardCall(this MethodDeclarationSyntax methodNode, IMethodSymbol symbol, string identifier, params ArgumentSyntax[] additionalArgs)
		{
			var name = methodNode.TypeParameterList != null
				? SyntaxFactory.GenericName(identifier)
					.WithTypeArgumentList(
						SyntaxFactory.TypeArgumentList(
							SyntaxFactory.SeparatedList<TypeSyntax>(
								methodNode.TypeParameterList.Parameters.Select(o => SyntaxFactory.IdentifierName(o.Identifier.ValueText))
							)))
				: (SimpleNameSyntax)SyntaxFactory.IdentifierName(identifier);
			MemberAccessExpressionSyntax accessExpression = null;
			if (symbol.MethodKind == MethodKind.ExplicitInterfaceImplementation)
			{
				// Explicit implementations needs an explicit cast (ie. ((Type)this).SyncMethod() )
				accessExpression = SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					SyntaxFactory.ParenthesizedExpression(
						SyntaxFactory.CastExpression(
							SyntaxFactory.IdentifierName(symbol.ExplicitInterfaceImplementations.Single().ContainingType.Name),
							SyntaxFactory.ThisExpression())),
					name);
			}
			var comma = SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.CommaToken, SyntaxFactory.TriviaList(SyntaxFactory.Space));
			var arguments = methodNode.ParameterList.Parameters
				.Select(o => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(o.Identifier.Text)))
				.Union(additionalArgs)
				.SelectMany((o, i) => i == 0
					? new SyntaxNodeOrToken[] {o}
					: new SyntaxNodeOrToken[] {comma, o});

			return SyntaxFactory.InvocationExpression(accessExpression ?? (ExpressionSyntax)name)
				.WithArgumentList(
					SyntaxFactory.ArgumentList(
						SyntaxFactory.SeparatedList<ArgumentSyntax>(arguments)));
		}

		private static ReturnStatementSyntax GetReturnTaskCompleted(bool useQualifiedName)
		{
			return SyntaxFactory.ReturnStatement(
				SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.ReturnKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)),
				SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					useQualifiedName
						? ConstructNameSyntax("System.Threading.Tasks.Task")
						: SyntaxFactory.IdentifierName(nameof(Task)),
					SyntaxFactory.IdentifierName("CompletedTask")),
				SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.SemicolonToken, SyntaxFactory.TriviaList())
			);
		}

		internal static ParenthesizedLambdaExpressionSyntax WrapInsideFunction(this ExpressionSyntax expression, IMethodSymbol delegateSymbol,
			bool returnTypeMismatch, bool taskConflict, Func<InvocationExpressionSyntax, InvocationExpressionSyntax> invocationModifierFunc)
		{
			var comma = SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.CommaToken, SyntaxFactory.TriviaList(SyntaxFactory.Space));
			var parameters = delegateSymbol.Parameters
				.Select(o => SyntaxFactory.Parameter(SyntaxFactory.Identifier(o.Name)))
				.SelectMany((o, i) => i == 0
					? new SyntaxNodeOrToken[] { o }
					: new SyntaxNodeOrToken[] { comma, o });
			var arguments = delegateSymbol.Parameters
				.Select(o => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(o.Name)))
				.SelectMany((o, i) => i == 0
					? new SyntaxNodeOrToken[] { o }
					: new SyntaxNodeOrToken[] { comma, o });
			var invocation = SyntaxFactory.InvocationExpression(expression)
				.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(arguments)));
			invocation = invocationModifierFunc(invocation);
			CSharpSyntaxNode body = invocation;
			if (returnTypeMismatch)
			{
				// TODO: non void return type
				body = SyntaxFactory.Block()
					.WithStatements(new SyntaxList<StatementSyntax>().AddRange(new StatementSyntax[]
					{
						SyntaxFactory.ExpressionStatement(invocation),
						GetReturnTaskCompleted(taskConflict)
					}));
			}

			var lambda = SyntaxFactory.ParenthesizedLambdaExpression(body)
				.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(parameters))
					.WithCloseParenToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.CloseParenToken, SyntaxFactory.TriviaList(SyntaxFactory.Space))))
				.WithArrowToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.EqualsGreaterThanToken, SyntaxFactory.TriviaList(SyntaxFactory.Space)));

			return lambda;
		}

		internal static MethodDeclarationSyntax AddCancellationTokenParameter(this MethodDeclarationSyntax node, 
			string parameterName, 
			bool defaultParameter,
			SyntaxTrivia leadingWhitespace,
			SyntaxTrivia endOfLine)
		{
			var totalParameters = node.ParameterList.Parameters.Count;
			var parameter = SyntaxFactory.Parameter(
					SyntaxFactory.Identifier(
						SyntaxFactory.TriviaList(),
						parameterName,
						defaultParameter ? SyntaxFactory.TriviaList(SyntaxFactory.Space) : SyntaxFactory.TriviaList()))
				.WithType(
					SyntaxFactory.IdentifierName(
						SyntaxFactory.Identifier(
							SyntaxFactory.TriviaList(),
							nameof(CancellationToken),
							SyntaxFactory.TriviaList(SyntaxFactory.Space))));
			if (defaultParameter)
			{
				parameter = parameter
					.WithDefault(
						SyntaxFactory.EqualsValueClause(
								SyntaxFactory.DefaultExpression(
									SyntaxFactory.IdentifierName(nameof(CancellationToken))))
							.WithEqualsToken(
								SyntaxFactory.Token(
									SyntaxFactory.TriviaList(),
									SyntaxKind.EqualsToken,
									SyntaxFactory.TriviaList(
										SyntaxFactory.Space))));
			}

			var comma = SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.CommaToken, SyntaxFactory.TriviaList(SyntaxFactory.Space));
			var parameters = node.ParameterList.Parameters
				.Union(new []{ parameter })
				.SelectMany((o, i) => i == 0
					? new SyntaxNodeOrToken[] { o }
					: new SyntaxNodeOrToken[] { comma, o });
			node = node.WithParameterList(node.ParameterList.WithParameters(SyntaxFactory.SeparatedList<ParameterSyntax>(parameters)));
			
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
			var xmlText = SyntaxFactory.XmlText()
				.WithTextTokens(
					SyntaxFactory.TokenList(
						SyntaxFactory.XmlTextNewLine(
							SyntaxFactory.TriviaList(),
							eol,
							eol,
							SyntaxFactory.TriviaList()), SyntaxFactory.XmlTextLiteral(
							SyntaxFactory.TriviaList(
								SyntaxFactory.DocumentationCommentExterior($"{leadingSpace}///")),
							" ",
							" ",
							SyntaxFactory.TriviaList())));
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
			var identifier = SyntaxFactory.Identifier(identifierName);

			var attribute = SyntaxFactory.XmlNameAttribute(
				SyntaxFactory.XmlName(SyntaxFactory.Identifier(SyntaxFactory.TriviaList(SyntaxFactory.Space), "name", SyntaxFactory.TriviaList())),
				SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
				SyntaxFactory.IdentifierName(identifier),
				SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));

			var startTag = SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName("param"))
				.WithAttributes(new SyntaxList<XmlAttributeSyntax>().Add(attribute));

			var endTag = SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName("param"));

			var content = SyntaxFactory.SingletonList<XmlNodeSyntax>(
				SyntaxFactory.XmlText()
					.WithTextTokens(
						SyntaxFactory.TokenList(
							SyntaxFactory.XmlTextLiteral(
								SyntaxFactory.TriviaList(),
								description,
								description,
								SyntaxFactory.TriviaList()))));
			return SyntaxFactory.XmlElement(startTag, content, endTag);
		}

		internal static InvocationExpressionSyntax AddArgument(this InvocationExpressionSyntax node, string argumentName)
		{
			return node.AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(argumentName)));
		}

		internal static SyntaxTriviaList AddAutoGeneratedTrivia(SyntaxTrivia endOfLineTrivia)
		{
			var triviaList = new []
			{
				SyntaxFactory.Comment("//------------------------------------------------------------------------------"),
				endOfLineTrivia,
				SyntaxFactory.Comment("// <auto-generated>"),
				endOfLineTrivia,
				SyntaxFactory.Comment("//     This code was generated by AsyncGenerator."),
				endOfLineTrivia,
				SyntaxFactory.Comment("//"),
				endOfLineTrivia,
				SyntaxFactory.Comment("//     Changes to this file may cause incorrect behavior and will be lost if"),
				endOfLineTrivia,
				SyntaxFactory.Comment("//     the code is regenerated."),
				endOfLineTrivia,
				SyntaxFactory.Comment("// </auto-generated>"),
				endOfLineTrivia,
				SyntaxFactory.Comment("//------------------------------------------------------------------------------"),
				endOfLineTrivia,
				endOfLineTrivia,
				endOfLineTrivia
			};

			return SyntaxFactory.TriviaList(triviaList);
		}

		internal static TypeDeclarationSyntax WithXmlContentTrivia(this TypeDeclarationSyntax node, SyntaxTrivia endOfLineTrivia, SyntaxTrivia leadingSyntaxTrivia)
		{
			var endOfLine = endOfLineTrivia.ToFullString();
			var leadingSpace = leadingSyntaxTrivia.ToFullString();
			var comment = SyntaxFactory.DocumentationCommentTrivia(
				SyntaxKind.SingleLineDocumentationCommentTrivia,
				SyntaxFactory.List(
					new XmlNodeSyntax[]
					{
						SyntaxFactory.XmlText()
							.WithTextTokens(
								SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral(SyntaxFactory.TriviaList(SyntaxFactory.DocumentationCommentExterior("///")), " ", " ", SyntaxFactory.TriviaList()))),
						SyntaxFactory.XmlExampleElement(
								SyntaxFactory.SingletonList<XmlNodeSyntax>(
									SyntaxFactory.XmlText()
										.WithTextTokens(
											SyntaxFactory.TokenList(
												SyntaxFactory.XmlTextNewLine(SyntaxFactory.TriviaList(), endOfLine, endOfLine, SyntaxFactory.TriviaList()),
												SyntaxFactory.XmlTextLiteral(
													SyntaxFactory.TriviaList(SyntaxFactory.DocumentationCommentExterior($"{leadingSpace}///")),
													" Contains generated async methods",
													" Contains generated async methods",
													SyntaxFactory.TriviaList()),
												SyntaxFactory.XmlTextNewLine(
													SyntaxFactory.TriviaList(),
													endOfLine,
													endOfLine,
													SyntaxFactory.TriviaList()),
												SyntaxFactory.XmlTextLiteral(
													SyntaxFactory.TriviaList(SyntaxFactory.DocumentationCommentExterior($"{leadingSpace}///")),
													" ",
													" ",
													SyntaxFactory.TriviaList())))))
							.WithStartTag(SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName(SyntaxFactory.Identifier("content"))))
							.WithEndTag(SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName(SyntaxFactory.Identifier("content")))),
						SyntaxFactory.XmlText()
							.WithTextTokens(SyntaxFactory.TokenList(SyntaxFactory.XmlTextNewLine(SyntaxFactory.TriviaList(), endOfLine, endOfLine, SyntaxFactory.TriviaList())))
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
				leadingTrivia = leadingTrivia.Replace(leadingTrivia.ElementAt(indexes[0]), SyntaxFactory.Trivia(comment));
				foreach (var index in indexes.Skip(1))
				{
					leadingTrivia = leadingTrivia.RemoveAt(index);
				}
			}
			else
			{
				leadingTrivia = leadingTrivia.AddRange(SyntaxFactory.TriviaList(
					SyntaxFactory.Trivia(comment),
					leadingSyntaxTrivia
				));
			}
			return node.WithLeadingTrivia(leadingTrivia);
		}

		internal static InvocationExpressionSyntax Invoke(this IdentifierNameSyntax identifier, ParameterListSyntax parameterList)
		{
			var callArguments = parameterList.Parameters
				.Select(o => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(o.Identifier)))
				.ToList();
			var argumentList = new List<SyntaxNodeOrToken>();
			var argSeparator = SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.CommaToken, SyntaxFactory.TriviaList(SyntaxFactory.Space));
			for (var i = 0; i < callArguments.Count; i++)
			{
				argumentList.Add(callArguments[i].WithoutTrivia());
				if (i + 1 < callArguments.Count)
				{
					argumentList.Add(argSeparator);
				}
			}
			return SyntaxFactory.InvocationExpression(identifier, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(argumentList)));
		}

		internal static NameSyntax ConstructNameSyntax(string name, SyntaxTrivia? trailingTrivia = null, bool insideCref = false, bool onlyName = false)
		{
			var names = name.Split('.').ToList();
			if (onlyName)
			{
				return GetSimpleName(names.Last(), insideCref: insideCref);
			}

			var trailingTriviaList = names.Count <= 2 && trailingTrivia.HasValue
				? SyntaxFactory.TriviaList(trailingTrivia.Value)
				: SyntaxFactory.TriviaList();
			if (names.Count < 2)
			{
				return GetSimpleName(name, SyntaxFactory.TriviaList(), trailingTriviaList, insideCref);
			}
			var result = SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName(names[0]), GetSimpleName(names[1], SyntaxFactory.TriviaList(), trailingTriviaList, insideCref));
			for (var i = 2; i < names.Count; i++)
			{
				trailingTriviaList = i + 1 == names.Count && trailingTrivia.HasValue
					? SyntaxFactory.TriviaList(trailingTrivia.Value)
					: SyntaxFactory.TriviaList();
				result = SyntaxFactory.QualifiedName(result, GetSimpleName(names[i], SyntaxFactory.TriviaList(), trailingTriviaList, insideCref));
			}
			return result;
		}

		// TODO: Use symbol for type arguments
		private static SimpleNameSyntax GetSimpleName(string name, SyntaxTriviaList? leadingTrivia = null, SyntaxTriviaList? trailingTrivia = null, bool insideCref = false)
		{
			if (!name.Contains("<"))
			{
				return SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(leadingTrivia ?? SyntaxFactory.TriviaList(), name, trailingTrivia ?? SyntaxFactory.TriviaList()));
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
				? SyntaxFactory.SingletonSeparatedList(argList[0])
				: SyntaxFactory.SeparatedList(argList);
			var typeArgList = SyntaxFactory.TypeArgumentList(list);
			if (insideCref)
			{
				typeArgList = typeArgList
					.WithLessThanToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.LessThanToken, "{", "{", SyntaxFactory.TriviaList()))
					.WithGreaterThanToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.GreaterThanToken, "}", "}", SyntaxFactory.TriviaList()));
			}
			return SyntaxFactory.GenericName(type)
				.WithTypeArgumentList(typeArgList);
		}

	}
}
