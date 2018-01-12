using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;

namespace MonoDevelop.Ide.Composition
{
	[Export(typeof(IPreviewFactoryService))]
	class PreviewFactoryService : IPreviewFactoryService
	{
		public Task<object> CreateAddedDocumentPreviewViewAsync (Document document, CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		public Task<object> CreateAddedDocumentPreviewViewAsync (Document document, double zoomLevel, CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		public Task<object> CreateChangedDocumentPreviewViewAsync (Document oldDocument, Document newDocument, CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		public Task<object> CreateChangedDocumentPreviewViewAsync (Document oldDocument, Document newDocument, double zoomLevel, CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		public Task<object> CreateRemovedDocumentPreviewViewAsync (Document document, CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		public Task<object> CreateRemovedDocumentPreviewViewAsync (Document document, double zoomLevel, CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		public SolutionPreviewResult GetSolutionPreviews (Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		public SolutionPreviewResult GetSolutionPreviews (Solution oldSolution, Solution newSolution, double zoomLevel, CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}
	}
}
