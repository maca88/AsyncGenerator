using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Extensions;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using  static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	partial class ProjectTransformer
	{
		private MethodTransformationResult TransformMethod(TypeTransformationMetadata typeMetadata, 
			IMethodAnalyzationResult methodResult)
		{
			var result = new MethodTransformationResult(methodResult);
			if (methodResult.Conversion == MethodConversion.Ignore)
			{
				return result;
			}
			var methodNode = methodResult.Node;
			var methodBodyNode = methodResult.GetBodyNode();
			if (methodBodyNode == null)
			{
				result.TransformedNode = methodNode.ReturnAsTask()
					.WithIdentifier(Identifier(methodNode.Identifier.Value + "Async"));
				return result;
			}
			var startMethodSpan = methodResult.Node.Span.Start;
			methodNode = methodNode.WithAdditionalAnnotations(new SyntaxAnnotation(result.Annotation));
			startMethodSpan -= methodNode.SpanStart;
			// Calculate whitespace method trivias
			var endOfLineTrivia = methodNode.DescendantTrivia().First(o => o.IsKind(SyntaxKind.EndOfLineTrivia));
			var methodLeadWhitespaceTrivia = methodNode.GetFirstToken().LeadingTrivia.First(o => o.IsKind(SyntaxKind.WhitespaceTrivia));
			var methodIndentTrivia = methodLeadWhitespaceTrivia.ToFullString().Substring(typeMetadata.LeadingWhitespaceTrivia.ToFullString().Length);
			var bodyLeadWhitespaceTrivia = Whitespace(methodLeadWhitespaceTrivia.ToFullString() + methodIndentTrivia);
			var metadata = new FunctionTransformationMetadata
			{
				BodyLeadingWhitespaceTrivia = bodyLeadWhitespaceTrivia,
				LeadingWhitespaceTrivia = methodLeadWhitespaceTrivia,
				EndOfLineTrivia = endOfLineTrivia,
				IndentTrivia = Whitespace(methodIndentTrivia)
			};

			// First we need to annotate nodes that will be modified in order to find them later on. 
			// We cannot rely on spans after the first modification as they will change
			var typeReferencesAnnotations = new List<string>();
			foreach (var typeReference in methodResult.TypeReferences)
			{
				var reference = typeReference.ReferenceLocation;
				var startSpan = reference.Location.SourceSpan.Start - startMethodSpan;
				var nameNode = methodNode.GetSimpleName(startSpan, reference.Location.SourceSpan.Length);
				var annotation = Guid.NewGuid().ToString();
				methodNode = methodNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
				typeReferencesAnnotations.Add(annotation);
			}

			var referenceAnnotations = new Dictionary<string, IFunctionReferenceAnalyzationResult>();
			foreach (var referenceResult in methodResult.CrefMethodReferences
				.Union(methodResult.MethodReferences)
				.Where(o => o.GetConversion() == ReferenceConversion.ToAsync))
			{
				var isCref = referenceResult is CrefFunctionReferenceData;
				var reference = referenceResult.ReferenceLocation;
				var startSpan = reference.Location.SourceSpan.Start - startMethodSpan;
				var nameNode = methodNode.GetSimpleName(startSpan, reference.Location.SourceSpan.Length, isCref);
				var annotation = Guid.NewGuid().ToString();
				methodNode = methodNode.ReplaceNode(nameNode, nameNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotation)));
				referenceAnnotations.Add(annotation, referenceResult);

				if (isCref || !methodResult.OmitAsync)
				{
					continue;
				}
				// We need to annotate the reference node (InvocationExpression, IdentifierName) in order to know if we need to wrap the node in a Task.FromResult
				var refNode = referenceResult.ReferenceNode;
				var bodyReference = (IBodyFunctionReferenceAnalyzationResult) referenceResult;
				if (bodyReference.UseAsReturnValue || refNode.IsReturned())
				{
					startSpan = refNode.SpanStart - startMethodSpan;
					var referenceNode = methodNode.DescendantNodes().First(o => o.SpanStart == startSpan && o.Span.Length == refNode.Span.Length);
					methodNode = methodNode.ReplaceNode(referenceNode, referenceNode.WithAdditionalAnnotations(new SyntaxAnnotation(metadata.TaskReturnedAnnotation)));
				}
			}

			// Modify references
			foreach (var refAnnotation in typeReferencesAnnotations)
			{
				var nameNode = methodNode.GetAnnotatedNodes(refAnnotation).OfType<SimpleNameSyntax>().First();
				methodNode = methodNode
							.ReplaceNode(nameNode, nameNode.WithIdentifier(Identifier(nameNode.Identifier.Value + "Async")));
			}
			
			var cancellationTokenParamName = "cancellationToken";
			// TODO: handle variable collision for token
			methodNode = methodNode.AddCancellationTokenParameterIf(cancellationTokenParamName, methodResult.CancellationTokenRequired);

			foreach (var pair in referenceAnnotations)
			{
				var nameNode = methodNode.GetAnnotatedNodes(pair.Key).OfType<SimpleNameSyntax>().First();
				var funReferenceResult = pair.Value;
				var bodyFuncReferenceResult = funReferenceResult as IBodyFunctionReferenceAnalyzationResult;
				// If we have a cref just change the name to the async counterpart (TODO: add the arguments as there may be multiple methods with the same name)
				if (bodyFuncReferenceResult == null)
				{
					var crefNode = (NameMemberCrefSyntax) nameNode.Parent;
					var paramList = new List<CrefParameterSyntax>();
					var asyncSymbol = funReferenceResult.AsyncCounterpartSymbol;
					//TODO: take care of type namespaces
					paramList.AddRange(asyncSymbol.Parameters.Select(o => CrefParameter(IdentifierName(o.Type.Name))));
					// If the async counterpart is internal and a token is required add a token parameter
					if (funReferenceResult.AsyncCounterpartFunction?.GetMethod()?.CancellationTokenRequired == true)
					{
						paramList.Add(CrefParameter(IdentifierName(nameof(CancellationToken))));
					}
					methodNode = methodNode
						.ReplaceNode(crefNode, crefNode
								.ReplaceNode(nameNode, nameNode
									.WithIdentifier(Identifier(funReferenceResult.AsyncCounterpartName))
									.WithTriviaFrom(nameNode))
								.WithParameters(CrefParameterList(SeparatedList(paramList))))
						;
					continue;
				}

				InvocationExpressionSyntax invokeNode = null;
				if (bodyFuncReferenceResult.CancellationTokenRequired || bodyFuncReferenceResult.AwaitInvocation)
				{
					invokeNode = nameNode.Ancestors().OfType<InvocationExpressionSyntax>().First();
				}

				if (!bodyFuncReferenceResult.AwaitInvocation)
				{
					var statement = nameNode.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
					var newNameNode = nameNode
						.WithIdentifier(Identifier(funReferenceResult.AsyncCounterpartName))
						.WithTriviaFrom(nameNode);
					// An arrow method does not have a statement
					if (statement == null)
					{
						if (invokeNode != null)
						{
							methodNode = methodNode.ReplaceNode(invokeNode, invokeNode
								.ReplaceNode(nameNode, newNameNode)
								.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult.CancellationTokenRequired));
						}
						else
						{
							methodNode = methodNode
								.ReplaceNode(nameNode, newNameNode);
						}
					}
					else
					{
						StatementSyntax newStatement;
						if (invokeNode != null)
						{
							newStatement = statement.ReplaceNode(invokeNode, invokeNode
								.ReplaceNode(nameNode, newNameNode)
								.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult.CancellationTokenRequired));
						}
						else
						{
							newStatement = statement.ReplaceNode(nameNode, newNameNode);
						}

						if (bodyFuncReferenceResult.UseAsReturnValue)
						{
							newStatement = newStatement.ToReturnStatement();
						}

						methodNode = methodNode
							.ReplaceNode(statement, newStatement);
					}
				}
				else
				{
					var newNameNode = nameNode
						.WithIdentifier(Identifier(funReferenceResult.AsyncCounterpartName))
						.WithTriviaFrom(nameNode);
					var invokeParent = invokeNode.Parent;
					methodNode = methodNode.ReplaceNode(invokeNode, invokeNode
						.ReplaceNode(nameNode, newNameNode)
						.AddCancellationTokenArgumentIf(cancellationTokenParamName, bodyFuncReferenceResult.CancellationTokenRequired)
						.AddAwait(invokeParent, _configuration.ConfigureAwaitArgument)
					);
				}

			}

			// The method with SplitTail needs to be splitted into two methods
			if (methodResult.SplitTail)
			{
				// Tail method body shall contain all statements after preconditions
				var tailMethodBody = methodNode.Body
						.WithStatements(new SyntaxList<StatementSyntax>()
							.AddRange(methodNode.Body.Statements.Skip(methodResult.Preconditions.Count)));
				// Main method shall contain only preconditions and a call to the tail method
				var bodyStatements = new SyntaxList<StatementSyntax>()
					.AddRange(methodNode.Body.Statements.Take(methodResult.Preconditions.Count));
				ParameterListSyntax tailCallParameterList;
				// TODO: handle name collisions
				var tailIdentifier = Identifier("Internal" + methodNode.Identifier.Value + "Async");
				if (_configuration.LocalFunctions)
				{
					var tailFunction = LocalFunctionStatement(
							methodNode.ReturnType.WrapIntoTask().WithoutLeadingTrivia(),
							tailIdentifier)
						.WithParameterList(ParameterList()
							.WithCloseParenToken(Token(TriviaList(), SyntaxKind.CloseParenToken, TriviaList(metadata.EndOfLineTrivia))))
						.AddAsync()
						.WithLeadingTrivia(metadata.BodyLeadingWhitespaceTrivia)
						.WithBody(tailMethodBody
							.AddWhitespace(metadata.IndentTrivia)
						);
					bodyStatements = bodyStatements.Add(tailFunction);
					// We do not need any parameter for the local function as we already have the parameters from the parent method
					tailCallParameterList = ParameterList();
				}
				else
				{
					var tailMethod = methodNode
						.WithReturnType(methodNode.ReturnType.WithLeadingTrivia()) // Remove lead trivia in case the return type is the first node (eg. void Method())
						.ReturnAsTask()
						.WithIdentifier(tailIdentifier)
						.WithModifiers(TokenList(
							Token(TriviaList(methodNode.GetLeadingTrivia()), SyntaxKind.PrivateKeyword, TriviaList(Space))))
						.AddAsync()
						.WithBody(tailMethodBody);
					result.TailMethodNode = tailMethod;
					// Tail call shall contain the cancellation token parameter
					tailCallParameterList = methodNode.ParameterList;
				}

				var tailCall = ReturnStatement(
					Token(TriviaList(metadata.BodyLeadingWhitespaceTrivia), SyntaxKind.ReturnKeyword, TriviaList(Space)),
					IdentifierName(tailIdentifier).Invoke(tailCallParameterList),
					Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(metadata.EndOfLineTrivia))
				);
				bodyStatements = bodyStatements.Add(tailCall);

				methodNode = methodNode.WithBody(methodNode.Body
					.WithStatements(bodyStatements));
			}
			else if (methodResult.OmitAsync)
			{
				var rewriter = new ReturnTaskMethodRewriter(metadata, methodResult);
				methodNode = (MethodDeclarationSyntax)rewriter.VisitMethodDeclaration(methodNode);
			}
			else
			{
				methodNode = methodNode.AddAsync();
			}
			methodNode = methodNode.ReturnAsTask()
				.WithIdentifier(Identifier(methodNode.Identifier.Value + "Async"));
			result.TransformedNode = methodNode;

			return result;
		}
	}
}
