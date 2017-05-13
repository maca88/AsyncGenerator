using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Transformation.Internal
{
	internal class ProjectTransformationResult : IProjectTransformationResult
	{
		public ProjectTransformationResult(Project project)
		{
			Project = project;
		}

		public Project Project { get; set; }

		public ConcurrentSet<DocumentTransformationResult> Documents { get; } = new ConcurrentSet<DocumentTransformationResult>();

		#region IProjectTransformationResult

		private IReadOnlyList<IDocumentTransformationResult> _cachedDocuments;
		IReadOnlyList<IDocumentTransformationResult> IProjectTransformationResult.Documents => 
			_cachedDocuments ?? (_cachedDocuments = Documents.Values.Where(o => o.Transformed != null).ToImmutableArray());

		#endregion

	}
}
