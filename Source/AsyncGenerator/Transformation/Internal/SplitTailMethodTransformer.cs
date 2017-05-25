using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Configuration;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// A method transformer that splits method into two when SplitTail is set to true.
	/// This transformer must run after the <see cref="CancellationTokenMethodTransformer"/>
	/// </summary>
	internal class SplitTailMethodTransformer : IMethodTransformer
	{
		private IProjectTransformConfiguration _configuration;
		private IProjectAnalyzeConfiguration _analyzeConfiguration;

		public Task Initialize(Project project, IProjectConfiguration configuration)
		{
			_configuration = configuration.TransformConfiguration;
			_analyzeConfiguration = configuration.AnalyzeConfiguration;
			return Task.CompletedTask;
		}

		/// <summary>
		/// The method with SplitTail needs to be splitted into two methods
		/// </summary>
		public MethodTransformerResult Transform(IMethodTransformationResult transformResult, 
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			var methodResult = transformResult.AnalyzationResult;
			if (!methodResult.SplitTail)
			{
				return MethodTransformerResult.Skip;
			}

			var methodNode = transformResult.Transformed;
			// Tail method body shall contain all statements after preconditions
			var skipStatements = methodResult.Preconditions.Count;
			// If a cancellation guard was generated we need to skip also that
			if (_analyzeConfiguration.UseCancellationTokens && _analyzeConfiguration.CancellationTokens.Guards && methodResult.CancellationTokenRequired)
			{
				skipStatements++; 
			}
			var tailMethodBody = methodNode.Body
				.WithStatements(new SyntaxList<StatementSyntax>()
					.AddRange(methodNode.Body.Statements.Skip(skipStatements)));
			// Main method shall contain only preconditions and a call to the tail method
			var bodyStatements = new SyntaxList<StatementSyntax>()
				.AddRange(methodNode.Body.Statements.Take(skipStatements));
			ParameterListSyntax tailCallParameterList;
			// TODO: handle name collisions
			var tailIdentifier = Identifier("Internal" + methodNode.Identifier.Value); // The transformed method has already the Async postfix
			MethodDeclarationSyntax tailMethod = null;
			if (_configuration.LocalFunctions)
			{
				var tailFunction = LocalFunctionStatement(
						methodNode.ReturnType.WithoutLeadingTrivia(),
						tailIdentifier)
					.WithParameterList(ParameterList()
						.WithCloseParenToken(Token(TriviaList(), SyntaxKind.CloseParenToken, TriviaList(transformResult.EndOfLineTrivia))))
					.AddAsync()
					.WithLeadingTrivia(transformResult.BodyLeadingWhitespaceTrivia)
					.WithBody(tailMethodBody
					);
				tailFunction = methodResult.Node.NormalizeMethodBody(Block(SingletonList(tailFunction)), transformResult.IndentTrivia, transformResult.EndOfLineTrivia)
					.Statements
					.OfType<LocalFunctionStatementSyntax>()
					.First();
				bodyStatements = bodyStatements.Add(tailFunction);
				// We do not need any parameter for the local function as we already have the parameters from the parent method
				tailCallParameterList = ParameterList();
			}
			else
			{
				var tailMethodModifiers = TokenList(
					Token(TriviaList(transformResult.EndOfLineTrivia, transformResult.LeadingWhitespaceTrivia), SyntaxKind.PrivateKeyword, TriviaList(Space)));
				if (methodNode.Modifiers.Any(o => o.IsKind(SyntaxKind.StaticKeyword)))
				{
					tailMethodModifiers = tailMethodModifiers.Add(Token(TriviaList(), SyntaxKind.StaticKeyword, TriviaList(Space)));
				}
				tailMethod = methodNode
					.WithReturnType(methodNode.ReturnType.WithLeadingTrivia()) // Remove lead trivia in case the return type is the first node (eg. void Method())
					.WithIdentifier(tailIdentifier)
					.WithModifiers(tailMethodModifiers)
					.AddAsync()
					.WithBody(tailMethodBody);
				// Tail call shall contain the cancellation token parameter
				tailCallParameterList = methodNode.ParameterList;
			}

			var tailCall = ReturnStatement(
				Token(TriviaList(transformResult.BodyLeadingWhitespaceTrivia), SyntaxKind.ReturnKeyword, TriviaList(Space)),
				IdentifierName(tailIdentifier).Invoke(tailCallParameterList),
				Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(transformResult.EndOfLineTrivia))
			);
			bodyStatements = bodyStatements.Add(tailCall);

			methodNode = methodNode.WithBody(methodNode.Body
				.WithStatements(bodyStatements));
			
			var result = MethodTransformerResult.Update(methodNode);
			if (tailMethod != null)
			{
				result.AddMethod(tailMethod);
			}
			return result;
		}
	}
}
