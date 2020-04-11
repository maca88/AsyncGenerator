using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Extensions.Internal;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// Adds a <see cref="ObsoleteAttribute"/> to a missing async method when its base async method as it.
	/// </summary>
	internal class ObsoleteMissingMethodTransformer : IMethodOrAccessorTransformer
	{
		private readonly TriviaRemover _directiveRemover;

		public ObsoleteMissingMethodTransformer()
		{
			_directiveRemover = new TriviaRemover(trivia => trivia.IsDirective);
		}

		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			return Task.CompletedTask;
		}

		public MethodTransformerResult Transform(IMethodOrAccessorTransformationResult transformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			var methodResult = transformResult.AnalyzationResult;
			if (!methodResult.Missing || !methodResult.Conversion.HasFlag(MethodConversion.ToAsync) || methodResult.Symbol.IsObsolete())
			{
				return MethodTransformerResult.Skip;
			}
			var methodNode = transformResult.Transformed;

			var baseMethod = methodResult.RelatedMethods
				.Where(o =>
					(methodResult.BaseOverriddenMethod != null && o.Symbol.EqualTo(methodResult.BaseOverriddenMethod)) ||
					methodResult.ImplementedInterfaces.Any(i => o.Symbol.EqualTo(i)))
				.FirstOrDefault(o => o.AsyncCounterpartSymbol?.IsObsolete() == true);
			if (baseMethod == null)
			{
				return MethodTransformerResult.Skip;
			}

			namespaceMetadata.AddUsing("System");
			AttributeListSyntax obsoleteAttribute = null;
			var syntaxReference = baseMethod.AsyncCounterpartSymbol.DeclaringSyntaxReferences.SingleOrDefault();
			SyntaxTrivia? documentationTrivia = null;
			if (syntaxReference != null)
			{
				var baseMethodNode = syntaxReference.GetSyntax() as MethodDeclarationSyntax;
				obsoleteAttribute = baseMethodNode?.AttributeLists.FirstOrDefault(o => o.Attributes.Count == 1 && o.Attributes.First().Name.ToString() == "Obsolete");
				obsoleteAttribute = (AttributeListSyntax)_directiveRemover.VisitAttributeList(obsoleteAttribute);
				documentationTrivia = obsoleteAttribute.GetLeadingTrivia()
					.Select(o => o.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ? o : (SyntaxTrivia?) null)
					.FirstOrDefault(o => o.HasValue);
			}

			if (obsoleteAttribute == null)
			{
				obsoleteAttribute = AttributeList(SingletonSeparatedList(Attribute(IdentifierName("Obsolete"))))
					.WithOpenBracketToken(Token(TriviaList(transformResult.LeadingWhitespaceTrivia), SyntaxKind.OpenBracketToken, TriviaList()))
					.WithCloseBracketToken(Token(TriviaList(), SyntaxKind.CloseBracketToken, TriviaList(transformResult.EndOfLineTrivia)));
			}

			var inheritDocTrivia = Trivia(GetInheritdoc(transformResult.EndOfLineTrivia.ToFullString()));
			if (documentationTrivia.HasValue)
			{
				obsoleteAttribute = obsoleteAttribute.WithLeadingTrivia(obsoleteAttribute.GetLeadingTrivia()
					.Replace(documentationTrivia.Value, inheritDocTrivia));
			}
			else
			{
				// Append <inheritdoc />
				var leadingTrivia = obsoleteAttribute.GetLeadingTrivia();
				var trivias = new List<SyntaxTrivia>();
				if (leadingTrivia.Count == 0 || !leadingTrivia.Last().IsKind(SyntaxKind.WhitespaceTrivia))
				{
					trivias.Add(transformResult.LeadingWhitespaceTrivia);
				}

				trivias.Add(inheritDocTrivia);
				trivias.Add(transformResult.LeadingWhitespaceTrivia);
				obsoleteAttribute = obsoleteAttribute.WithLeadingTrivia(leadingTrivia.AddRange(trivias));
			}

			methodNode = methodNode
				.WithLeadingTrivia(TriviaList(transformResult.LeadingWhitespaceTrivia))
				.WithAttributeLists(methodNode.AttributeLists.Add(obsoleteAttribute));
				
			return MethodTransformerResult.Update(methodNode);
		}

		private DocumentationCommentTriviaSyntax GetInheritdoc(string eol)
		{
			return DocumentationCommentTrivia(
				SyntaxKind.SingleLineDocumentationCommentTrivia,
				List(new XmlNodeSyntax[]
					{
						XmlText()
							.WithTextTokens(TokenList(XmlTextLiteral(TriviaList(DocumentationCommentExterior("///")), " ", " ", TriviaList()))),
						XmlEmptyElement(XmlName(Identifier("inheritdoc")))
							.WithSlashGreaterThanToken(Token(TriviaList(Space), SyntaxKind.SlashGreaterThanToken, TriviaList())),
						XmlText()
							.WithTextTokens(TokenList(XmlTextNewLine(TriviaList(), eol, eol, TriviaList())))
					}
				)
			);
		}
	}
}
