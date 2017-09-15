using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Transformation.Internal
{
	internal class DocumentCommentTypeTransformer : ITypeTransformer
	{
		//Dict<LeadingWhitespace, Dict<EndOfLine, Dict<Commnet, List<Trivia>>>
		private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, List<SyntaxTrivia>>>> _cachedComments = 
			new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, List<SyntaxTrivia>>>>();
		private IProjectDocumentationCommentConfiguration _configuration;
		private bool _isEnabledForPartialTypes;
		private bool _isEnabledForNewTypes;

		public Task Initialize(Project project, IProjectConfiguration configuration)
		{
			_configuration = configuration.TransformConfiguration.DocumentationComments;
			_isEnabledForPartialTypes =
				_configuration.AddOrReplacePartialTypeComments != null ||
				_configuration.RemovePartialTypeComments != null;
			_isEnabledForNewTypes =
				_configuration.AddOrReplaceNewTypeComments != null ||
				_configuration.RemoveNewTypeComments != null
				;
			_cachedComments.Clear();
			return Task.CompletedTask;
		}

		public TypeDeclarationSyntax Transform(TypeDeclarationSyntax transformedNode, ITypeTransformationResult transformationResult,
			INamespaceTransformationMetadata namespaceMetadata, bool missingMembers)
		{
			if (!_isEnabledForNewTypes && !_isEnabledForPartialTypes)
			{
				return null;
			}
			var symbol = transformationResult.AnalyzationResult.Symbol;
			var conversion = transformationResult.AnalyzationResult.Conversion;
			if (_isEnabledForPartialTypes && (conversion == TypeConversion.Partial || missingMembers))
			{
				var comment = _configuration.AddOrReplacePartialTypeComments?.Invoke(symbol);
				if (!string.IsNullOrEmpty(comment))
				{
					var commentTrivias = GetOrCreateCommentTrivias(transformationResult, comment);
					return transformedNode.WithCommentTrivias(commentTrivias);
				}
				if (_configuration.RemovePartialTypeComments?.Invoke(symbol) == true)
				{
					return transformedNode.RemoveCommentTrivias();
				}
			}

			if (_isEnabledForNewTypes && (conversion == TypeConversion.Copy || conversion == TypeConversion.NewType))
			{
				var comment = _configuration.AddOrReplaceNewTypeComments?.Invoke(symbol);
				if (!string.IsNullOrEmpty(comment))
				{
					var commentTrivias = GetOrCreateCommentTrivias(transformationResult, comment);
					return transformedNode.WithCommentTrivias(commentTrivias);
				}
				if (_configuration.RemoveNewTypeComments?.Invoke(symbol) == true)
				{
					return transformedNode.RemoveCommentTrivias();
				}
			}
			return null;
		}

		private IEnumerable<SyntaxTrivia> GetOrCreateCommentTrivias(ITypeTransformationResult transformationResult, string comment)
		{
			return _cachedComments
				.GetOrAdd(transformationResult.LeadingWhitespaceTrivia.ToFullString(), s => new ConcurrentDictionary<string, ConcurrentDictionary<string, List<SyntaxTrivia>>>())
				.GetOrAdd(transformationResult.EndOfLineTrivia.ToFullString(), s => new ConcurrentDictionary<string, List<SyntaxTrivia>>())
				.GetOrAdd(comment, s => CreateCommentTrivias(transformationResult, s));
		}

		private static List<SyntaxTrivia> CreateCommentTrivias(ITypeTransformationResult transformationResult, string comment)
		{
			return Extensions.Internal.SyntaxNodeExtensions.CreateCommentTrivias(
					comment,
					transformationResult.LeadingWhitespaceTrivia,
					transformationResult.EndOfLineTrivia)
				.ToList();
		}
	}
}
