using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Transformation.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SyntaxTrivia = Microsoft.CodeAnalysis.SyntaxTrivia;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

		internal static bool IsReturnStatementRequired(this SyntaxNode node)
		{
			var body = node.GetFunctionBody();
			if (body is BlockSyntax block)
			{
				return block.IsReturnStatementRequired();
			}
			if (body is ArrowExpressionClauseSyntax arrow)
			{
				return arrow.Expression.IsReturnStatementRequired();
			}
			if (body is ExpressionSyntax expressionNode)
			{
				return expressionNode.IsReturnStatementRequired();
			}
			throw new InvalidOperationException("Unable to detect if the return statement is required for body: " + body);
		}

		internal static bool IsReturnStatementRequired(this ExpressionSyntax expressionNode)
		{
			if (expressionNode is AssignmentExpressionSyntax)
			{
				return true;
			}
			return false;
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
			return node.GetLeadingTrivia().LastOrDefault(o => o.IsKind(SyntaxKind.WhitespaceTrivia));
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

		internal static TypeSyntax GetReturnType(this SyntaxNode node)
		{
			if (node is MethodDeclarationSyntax methodNode)
			{
				return methodNode.ReturnType;
			}
			if (node is AccessorDeclarationSyntax accessorNode)
			{
				var propertyNode = accessorNode.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
				if (propertyNode != null)
				{
					return propertyNode.Type;
				}
			}
			throw new InvalidOperationException($"Unable to get return type for node {node}");
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

		internal static bool IsInsideCref(this SimpleNameSyntax node)
		{
			return node.Parent.IsKind(SyntaxKind.NameMemberCref);
		}

		internal static bool IsInsideTypeOf(this SimpleNameSyntax node)
		{
			return node.Parent.IsKind(SyntaxKind.TypeOfExpression);
		}

		internal static bool IsInsideNameOf(this SimpleNameSyntax node)
		{
			var parent = node.Parent;
			return parent.IsKind(SyntaxKind.Argument) &&
			       parent.Parent.Parent is InvocationExpressionSyntax invocation &&
			       invocation.Expression.ToString() == "nameof";
		}

		internal static bool IsAssigned(this SimpleNameSyntax identifier)
		{
			var statement = identifier.Ancestors()
				.TakeWhile(o => !(o is StatementSyntax) && !(o is ArrowExpressionClauseSyntax))
				.OfType<AssignmentExpressionSyntax>()
				.FirstOrDefault();
			// Check if the assignement belongs to the current identifier
			return statement != null && statement.DescendantTokens().Any(o => o.SpanStart == identifier.Span.End + 1 && o.IsKind(SyntaxKind.EqualsToken));
		}
		/// <summary>
		/// Get the whole expression for the accessor
		/// eg: Property => Property
		/// eg: Property.Count => Property
		/// eg: StaticClass.Property => StaticClass.Property
		/// eg: StaticClass.Property.Count => StaticClass.Property
		/// eg: new Class().Property.Method() => new Class().Property
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		internal static ExpressionSyntax GetAccessorExpression(this SimpleNameSyntax identifier)
		{
			if (identifier.Parent is AssignmentExpressionSyntax assignmentExpression)
			{
				return assignmentExpression;
			}

			if (identifier.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Span.End == identifier.Span.End)
			{
				return memberAccess;
			}
			return identifier;
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
				if (!node.IsKind(SyntaxKind.ThrowExpression) &&
					!node.IsKind(SyntaxKind.SimpleAssignmentExpression) &&
					node.Parent.IsKind(SyntaxKind.ArrowExpressionClause))
				{
					return true;
				}
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
			return (SimpleNameSyntax)node.FindToken(spanStart, descendIntoTrivia).Parent;
		}

		internal static SimpleNameSyntax GetSimpleName(this SyntaxNode node, TextSpan sourceSpan, bool descendIntoTrivia = false)
		{
			return node.FindNode(sourceSpan, descendIntoTrivia, true) as SimpleNameSyntax;
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

		internal static TypeDeclarationSyntax AppendMembers(this TypeDeclarationSyntax node,
			MemberDeclarationSyntax member, IEnumerable<FieldDeclarationSyntax> extraFields = null, IEnumerable<MethodDeclarationSyntax> extraMethods = null)
		{
			if (extraMethods != null)
			{
				// Append all additional members after the original one
				var index = node.Members.IndexOf(member);
				var currIndex = index + 1;
				foreach (var extraMethod in extraMethods)
				{
					node = node.WithMembers(node.Members.Insert(currIndex, extraMethod));
					currIndex++;
				}
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

		internal static T SubtractIndent<T>(this T node, string indent) where T : SyntaxNode
		{
			var indentRewriter = new IndentRewriter(indent, true);
			return (T)indentRewriter.Visit(node);
		}

		internal static InvocationExpressionSyntax AddCancellationTokenArgumentIf(this InvocationExpressionSyntax node, string argumentName, IBodyFunctionReferenceAnalyzationResult functionReference)
		{
			if (!functionReference.PassCancellationToken)
			{
				return node;
			}

			ExpressionSyntax argExpression = argumentName != null 
				? IdentifierName(argumentName) 
				: ConstructNameSyntax("CancellationToken.None");

			var invokedMethod = functionReference.ReferenceSymbol;
			// We have to add a name colon when one of the previous argument has it or if the method invoked has a default parameter that is not passed
			var colonRequired = node.ArgumentList.Arguments.Any(o => o.NameColon != null) || (node.ArgumentList.Arguments.Count < invokedMethod.Parameters.Length);
			var argument = Argument(argExpression);
			if (colonRequired)
			{
				argument = argument.WithNameColon(NameColon("cancellationToken")); // TODO: dynamic
			}

			if (!node.ArgumentList.Arguments.Any())
			{
				return node.AddArgumentListArguments(argument);
			}
			
			// We need to add an extra space after the comma
			var argumentList = SeparatedList<ArgumentSyntax>(
				node.ArgumentList.Arguments.GetWithSeparators()
					.Concat(new SyntaxNodeOrToken[]
					{
						Token(TriviaList(), SyntaxKind.CommaToken, TriviaList(Space)),
						argument
					})
			);
			return node.WithArgumentList(node.ArgumentList.WithArguments(argumentList));
		}

		internal static InvocationExpressionSyntax AddAssignedValueAsArgument(this InvocationExpressionSyntax node, ExpressionSyntax expressionNode)
		{
			if (expressionNode is AssignmentExpressionSyntax assignmentExpression)
			{
				var argument = Argument(assignmentExpression.Right);
				if (!node.ArgumentList.Arguments.Any())
				{
					return node.AddArgumentListArguments(argument);
				}

				// We need to add an extra space after the comma
				var argumentList = SeparatedList<ArgumentSyntax>(
					node.ArgumentList.Arguments.GetWithSeparators()
						.Concat(new SyntaxNodeOrToken[]
						{
							Token(TriviaList(), SyntaxKind.CommaToken, TriviaList(Space)),
							argument
						})
				);
				return node.WithArgumentList(node.ArgumentList.WithArguments(argumentList));
			}
			return node;
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

		internal static MethodDeclarationSyntax ConvertExpressionBodyToBlock(this MethodDeclarationSyntax methodNode,
			IMethodOrAccessorTransformationResult transformResult)
		{
			if (methodNode.ExpressionBody != null)
			{
				return methodNode
					.WithBody(ConvertToBlock(methodNode.ExpressionBody.Expression, transformResult))
					.WithParameterList(
						methodNode.ParameterList.WithCloseParenToken(
							methodNode.ParameterList.CloseParenToken.WithTrailingTrivia(transformResult.EndOfLineTrivia)))
					.WithExpressionBody(null)
					.WithSemicolonToken(Token(SyntaxKind.None));
			}
			return methodNode;
		}

		internal static BlockSyntax ConvertToBlock(this ExpressionSyntax expressionNode, IMethodOrAccessorTransformationResult transformResult)
		{
			var analyzeResult = transformResult.AnalyzationResult;
			var statement = analyzeResult.Symbol.ReturnsVoid
				? (StatementSyntax)ExpressionStatement(
					expressionNode.WithLeadingTrivia(transformResult.BodyLeadingWhitespaceTrivia),
					Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(transformResult.EndOfLineTrivia))
				)
				: ReturnStatement(
					Token(TriviaList(transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.ReturnKeyword, TriviaList(Space)),
					expressionNode,
					Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(transformResult.EndOfLineTrivia))
				);
			return Block(
				Token(TriviaList(transformResult.LeadingWhitespaceTrivia), SyntaxKind.OpenBraceToken, TriviaList(transformResult.EndOfLineTrivia)),
				SingletonList(statement),
				Token(TriviaList(transformResult.LeadingWhitespaceTrivia), SyntaxKind.CloseBraceToken, TriviaList(transformResult.EndOfLineTrivia))
			);
		}

		private static ReturnStatementSyntax GetReturnTaskCompleted(bool useQualifiedName)
		{
			return ReturnStatement(
				Token(TriviaList(), SyntaxKind.ReturnKeyword, TriviaList(Space)),
				MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					useQualifiedName
						? ConstructNameSyntax("System.Threading.Tasks.Task")
						: IdentifierName(nameof(Task)),
					IdentifierName("CompletedTask")),
				Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList())
			);
		}

		internal static ParenthesizedLambdaExpressionSyntax WrapInsideFunction(this ExpressionSyntax expression, IMethodSymbol delegateSymbol,
			bool returnTypeMismatch, bool taskConflict, Func<InvocationExpressionSyntax, InvocationExpressionSyntax> invocationModifierFunc)
		{
			var comma = Token(TriviaList(), SyntaxKind.CommaToken, TriviaList(Space));
			var parameters = delegateSymbol.Parameters
				.Select(o => Parameter(Identifier(o.Name)))
				.SelectMany((o, i) => i == 0
					? new SyntaxNodeOrToken[] { o }
					: new SyntaxNodeOrToken[] { comma, o });
			var arguments = delegateSymbol.Parameters
				.Select(o => Argument(IdentifierName(o.Name)))
				.SelectMany((o, i) => i == 0
					? new SyntaxNodeOrToken[] { o }
					: new SyntaxNodeOrToken[] { comma, o });
			var invocation = InvocationExpression(expression)
				.WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(arguments)));
			invocation = invocationModifierFunc(invocation);
			CSharpSyntaxNode body = invocation;
			if (returnTypeMismatch)
			{
				// TODO: non void return type
				body = Block()
					.WithStatements(new SyntaxList<StatementSyntax>().AddRange(new StatementSyntax[]
					{
						ExpressionStatement(invocation),
						GetReturnTaskCompleted(taskConflict)
					}));
			}

			var lambda = ParenthesizedLambdaExpression(body)
				.WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(parameters))
					.WithCloseParenToken(Token(TriviaList(), SyntaxKind.CloseParenToken, TriviaList(Space))))
				.WithArrowToken(Token(TriviaList(), SyntaxKind.EqualsGreaterThanToken, TriviaList(Space)));

			return lambda;
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

			var parameters = node.ParameterList.Parameters.GetWithSeparators()
				.Union(node.ParameterList.Parameters.Count > 0 
					? new SyntaxNodeOrToken[]
					{
						Token(TriviaList(), SyntaxKind.CommaToken, TriviaList(Space)),
						parameter
					}
					: new SyntaxNodeOrToken[] { parameter });
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


		internal static TypeDeclarationSyntax RemoveCommentTrivias(this TypeDeclarationSyntax node)
		{
			var leadingTrivia = node.GetLeadingTrivia();
			var triviaIndexes = GetCommentTriviaIndexes(leadingTrivia);
			foreach (var index in triviaIndexes.OrderByDescending(o => o))
			{
				leadingTrivia = leadingTrivia.RemoveAt(index);
			}
			return node.WithLeadingTrivia(leadingTrivia);
		}

		internal static IEnumerable<SyntaxTrivia> CreateCommentTrivias(string comment, SyntaxTrivia leadingSyntaxTrivia,
			SyntaxTrivia endOfLineTrivia)
		{
			var endOfLine = endOfLineTrivia.ToFullString();
			var leadingSpace = leadingSyntaxTrivia.ToFullString();
			var lines = comment
				.Replace("\r\n", "\r")
				.Replace("\n\r", "\r")
				.Split('\r', '\n')
				.Select(o => o.Trim())
				.Where(o => !string.IsNullOrEmpty(o))
				.ToList();

			for (var i = 0; i < lines.Count; i++)
			{
				lines[i] = leadingSpace + lines[i];
			}
			comment = lines.Aggregate("", (current, line) => current + line + endOfLine);

			return CSharpSyntaxTree.ParseText(comment)
				.GetRoot()
				.DescendantTrivia()
				;
		}

		internal static T WithCommentTrivias<T>(this T node, string comment, SyntaxTrivia leadingSyntaxTrivia, SyntaxTrivia endOfLineTrivia)
			where T : SyntaxNode
		{
			var commentTrivias = CreateCommentTrivias(comment, leadingSyntaxTrivia, endOfLineTrivia);
			return WithCommentTrivias(node, commentTrivias);
		}

		internal static T WithCommentTrivias<T>(this T node, IEnumerable<SyntaxTrivia> commentTrivias)
			where T : SyntaxNode
		{
			// We have to preserve directives, so we need to modify the existing leading trivia
			var leadingTrivia = node.GetLeadingTrivia();
			var triviaIndexes = GetCommentTriviaIndexes(leadingTrivia);
			if (triviaIndexes.Any())
			{
				// We have to replace the last comment with ours and remove others
				var indexes = triviaIndexes
					.OrderByDescending(o => o)
					.ToList();
				foreach (var commentTrivia in commentTrivias.Reverse())
				{
					leadingTrivia = leadingTrivia.Insert(indexes[0] + 1, commentTrivia);
				}
				foreach (var index in indexes)
				{
					leadingTrivia = leadingTrivia.RemoveAt(index);
				}
			}
			else
			{
				int? lastWhitespaceIndex = null;
				// We need to insert before the last whitespace trivia if exists otherwise on the last position
				for (var i = leadingTrivia.Count - 1; i >= 0; i--)
				{
					if (leadingTrivia[i].IsKind(SyntaxKind.WhitespaceTrivia))
					{
						lastWhitespaceIndex = i;
						break;
					}
				}
				leadingTrivia = lastWhitespaceIndex.HasValue 
					? leadingTrivia.InsertRange(lastWhitespaceIndex.Value, commentTrivias)
					: leadingTrivia.AddRange(commentTrivias);
				
			}
			return node.WithLeadingTrivia(leadingTrivia);
		}

		private static List<int> GetCommentTriviaIndexes(SyntaxTriviaList triviaList)
		{
			int? lastWhitespaceTrivia = null;
			var lastKind = SyntaxKind.None;
			var triviaIndexes = new List<int>();
			for (var i = 0; i < triviaList.Count; i++)
			{
				var item = triviaList[i];
				// Comment will have and end of line at the end that we have to remove
				if (item.IsKind(SyntaxKind.EndOfLineTrivia) && lastKind == SyntaxKind.SingleLineCommentTrivia)
				{
					triviaIndexes.Add(i);
				}
				lastKind = item.Kind();
				if (item.IsKind(SyntaxKind.WhitespaceTrivia))
				{
					lastWhitespaceTrivia = i;
					continue;
				}
				if (!item.IsKind(SyntaxKind.SingleLineCommentTrivia) && // double slash
					!item.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)) // triple slash
				{
					continue;
				}
				if (lastWhitespaceTrivia.HasValue)
				{
					triviaIndexes.Add(lastWhitespaceTrivia.Value);
					lastWhitespaceTrivia = null;
				}
				triviaIndexes.Add(i);
			}
			return triviaIndexes;
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
