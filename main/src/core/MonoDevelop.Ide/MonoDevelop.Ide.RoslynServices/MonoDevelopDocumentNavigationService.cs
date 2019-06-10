using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.RoslynServices
{
	[ExportWorkspaceServiceFactory (typeof (IDocumentNavigationService), ServiceLayer.Host), Shared]
	internal sealed class VisualStudioDocumentNavigationServiceFactory : IWorkspaceServiceFactory
	{
		private readonly IDocumentNavigationService _singleton;

		[ImportingConstructor]
		[Obsolete (MefConstruction.ImportingConstructorMessage, error: true)]
		private VisualStudioDocumentNavigationServiceFactory ()
		{
			_singleton = new MonoDevelopDocumentNavigationService ();
		}

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			return _singleton;
		}
	}

	class MonoDevelopDocumentNavigationService : IDocumentNavigationService
	{
		public bool CanNavigateToSpan (Workspace workspace, DocumentId documentId, TextSpan textSpan)
		{
			// Navigation should not change the context of linked files and Shared Projects.
			documentId = workspace.GetDocumentIdInCurrentContext (documentId);
			var document = workspace.CurrentSolution.GetDocument (documentId);

			if (!IsSecondaryBuffer (workspace, document)) {
				return true;
			}

			var text = document.GetTextSynchronously (CancellationToken.None);

			var boundedTextSpan = GetSpanWithinDocumentBounds (textSpan, text.Length);
			if (boundedTextSpan != textSpan) {
				throw new ArgumentOutOfRangeException ();
			}

			return CanMapFromSecondaryBufferToPrimaryBuffer (workspace, document, textSpan);
		}

		public bool CanNavigateToLineAndOffset (Workspace workspace, DocumentId documentId, int lineNumber, int offset)
		{
			// Navigation should not change the context of linked files and Shared Projects.
			documentId = workspace.GetDocumentIdInCurrentContext (documentId);
			var document = workspace.CurrentSolution.GetDocument (documentId);

			if (!IsSecondaryBuffer (workspace, document)) {
				return true;
			}

			var text = document.GetTextSynchronously (CancellationToken.None);
			var textSpan = new TextSpan (text.Lines [lineNumber].Start + offset, 0);

			return CanMapFromSecondaryBufferToPrimaryBuffer (workspace, document, textSpan);
		}

		public bool CanNavigateToPosition (Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0)
		{
			// Navigation should not change the context of linked files and Shared Projects.
			documentId = workspace.GetDocumentIdInCurrentContext (documentId);
			var document = workspace.CurrentSolution.GetDocument (documentId);

			if (!IsSecondaryBuffer (workspace, document)) {
				return true;
			}

			var text = document.GetTextSynchronously (CancellationToken.None);

			var boundedPosition = GetPositionWithinDocumentBounds (position, text.Length);
			if (boundedPosition != position) {
				throw new ArgumentOutOfRangeException ();
			}

			var textSpan = new TextSpan (position+ virtualSpace,0);

			return CanMapFromSecondaryBufferToPrimaryBuffer (workspace, document, textSpan);
		}

		public bool TryNavigateToSpan (Workspace workspace, DocumentId documentId, TextSpan textSpan, OptionSet options)
		{
			// Navigation should not change the context of linked files and Shared Projects.
			documentId = workspace.GetDocumentIdInCurrentContext (documentId);

			Runtime.AssertMainThread ();

			var document = OpenDocument (workspace, documentId, options);
			if (document == null) {
				return false;
			}

			var text = document.GetTextSynchronously (CancellationToken.None);
			var textBuffer = text.Container.GetTextBuffer ();

			var boundedTextSpan = GetSpanWithinDocumentBounds (textSpan, text.Length);
			if (boundedTextSpan != textSpan) {
				throw new ArgumentOutOfRangeException ();
			}

			if (IsSecondaryBuffer (workspace, document) &&
				!TryMapSpanFromSecondaryBufferToPrimaryBuffer (textSpan, workspace, document, out textSpan)) {
				return false;
			}

			return NavigateTo (document, textSpan);
		}

		public bool TryNavigateToLineAndOffset (Workspace workspace, DocumentId documentId, int lineNumber, int offset, OptionSet options)
		{
			// Navigation should not change the context of linked files and Shared Projects.
			documentId = workspace.GetDocumentIdInCurrentContext (documentId);

			Runtime.AssertMainThread ();

			var document = OpenDocument (workspace, documentId, options);
			if (document == null) {
				return false;
			}

			var textSpan = new TextSpan (offset, 0);

			if (IsSecondaryBuffer (workspace, document) &&
				!TryMapSpanFromSecondaryBufferToPrimaryBuffer (textSpan, workspace, document, out textSpan)) {
				return false;
			}

			return NavigateTo (document, textSpan);
		}

		public bool TryNavigateToPosition (Workspace workspace, DocumentId documentId, int position, int virtualSpace, OptionSet options)
		{
			// Navigation should not change the context of linked files and Shared Projects.
			documentId = workspace.GetDocumentIdInCurrentContext (documentId);

			Runtime.AssertMainThread ();

			var document = OpenDocument (workspace, documentId, options);
			if (document == null) {
				return false;
			}

			var textSpan = new TextSpan (position + virtualSpace, 0);

			if (IsSecondaryBuffer (workspace, document) &&
				!TryMapSpanFromSecondaryBufferToPrimaryBuffer (textSpan, workspace, document, out textSpan)) {
				return false;
			}

			return NavigateTo (document, textSpan);
		}

		/// <summary>
		/// It is unclear why, but we are sometimes asked to navigate to a position that is not
		/// inside the bounds of the associated <see cref="Document"/>. This method returns a
		/// position that is guaranteed to be inside the <see cref="Document"/> bounds. If the
		/// returned position is different from the given position, then the worst observable
		/// behavior is either no navigation or navigation to the end of the document. See the
		/// following bugs for more details:
		///     https://devdiv.visualstudio.com/DevDiv/_workitems?id=112211
		///     https://devdiv.visualstudio.com/DevDiv/_workitems?id=136895
		///     https://devdiv.visualstudio.com/DevDiv/_workitems?id=224318
		///     https://devdiv.visualstudio.com/DevDiv/_workitems?id=235409
		/// </summary>
		private static int GetPositionWithinDocumentBounds (int position, int documentLength)
		{
			return Math.Min (documentLength, Math.Max (position, 0));
		}

		/// <summary>
		/// It is unclear why, but we are sometimes asked to navigate to a <see cref="TextSpan"/>
		/// that is not inside the bounds of the associated <see cref="Document"/>. This method
		/// returns a span that is guaranteed to be inside the <see cref="Document"/> bounds. If
		/// the returned span is different from the given span, then the worst observable behavior
		/// is either no navigation or navigation to the end of the document.
		/// See https://github.com/dotnet/roslyn/issues/7660 for more details.
		/// </summary>
		private static TextSpan GetSpanWithinDocumentBounds (TextSpan span, int documentLength)
		{
			return TextSpan.FromBounds (GetPositionWithinDocumentBounds (span.Start, documentLength), GetPositionWithinDocumentBounds (span.End, documentLength));
		}

		private static Document OpenDocument (Workspace workspace, DocumentId documentId, OptionSet options)
		{
			options = options ?? workspace.Options;

			// Always open the document again, even if the document is already open in the 
			// workspace. If a document is already open in a preview tab and it is opened again 
			// in a permanent tab, this allows the document to transition to the new state.
			if (workspace.CanOpenDocuments) {
				if (options.GetOption (NavigationOptions.PreferProvisionalTab)) {
					// If we're just opening the provisional tab, then do not "activate" the document
					// (i.e. don't give it focus).  This way if a user is just arrowing through a set 
					// of FindAllReferences results, they don't have their cursor placed into the document.
					//TODO: MAC we don't support this kind of opening
					workspace.OpenDocument (documentId, true);
				} else {
					workspace.OpenDocument (documentId, true);
				}
			}

			if (!workspace.IsDocumentOpen (documentId)) {
				return null;
			}

			return workspace.CurrentSolution.GetDocument (documentId);
		}

		private bool NavigateTo (Document document, TextSpan span)
		{
			var proj = ((MonoDevelopWorkspace)document.Project.Solution.Workspace).GetMonoProject (document.Project);
			var task = IdeApp.Workbench.OpenDocument (new Gui.FileOpenInformation (document.FilePath, proj) {
				Offset = span.Start
			});
			return true;
		}

		private bool IsSecondaryBuffer (Workspace workspace, Document document)
		{
			var containedDocument = MonoDevelopHostDocumentRegistration.FromDocument (document);
			if (containedDocument == null) {
				return false;
			}

			return true;
		}

		public static bool TryMapSpanFromSecondaryBufferToPrimaryBuffer (TextSpan spanInSecondaryBuffer, Microsoft.CodeAnalysis.Workspace workspace, Document document, out TextSpan spanInPrimaryBuffer)
		{
			spanInPrimaryBuffer = default;

			var containedDocument = MonoDevelopHostDocumentRegistration.FromDocument (document);
			if (containedDocument == null) {
				return false;
			}
			throw new NotImplementedException ();
			//var bufferCoordinator = containedDocument.BufferCoordinator;

			//var primary = new VsTextSpan [1];
			//var hresult = bufferCoordinator.MapSecondaryToPrimarySpan (spanInSecondaryBuffer, primary);

			//spanInPrimaryBuffer = primary [0];

			//return ErrorHandler.Succeeded (hresult);
		}

		private bool CanMapFromSecondaryBufferToPrimaryBuffer (Workspace workspace, Document document, TextSpan spanInSecondaryBuffer)
		{
			return TryMapSpanFromSecondaryBufferToPrimaryBuffer (spanInSecondaryBuffer, workspace, document, out var spanInPrimaryBuffer);
		}
	}
}
