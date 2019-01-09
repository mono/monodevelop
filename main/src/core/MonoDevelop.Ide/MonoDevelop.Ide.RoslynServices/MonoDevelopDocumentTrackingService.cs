//
// MonoDevelopDocumentTrackingService.cs
//
// Author:
//       Marius <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.RoslynServices
{
	[ExportWorkspaceServiceFactory (typeof (IDocumentTrackingService), ServiceLayer.Host), Shared]
	sealed class MonoDevelopDocumentTrackingServiceFactory : IWorkspaceServiceFactory
	{
		IDocumentTrackingService service;

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			return service ?? (service = new MonoDevelopDocumentTrackingService ());
		}
	}

	sealed class MonoDevelopDocumentTrackingService : IDocumentTrackingService, IDisposable
	{
		Gui.Document activeDocument = null;
		public MonoDevelopDocumentTrackingService ()
		{
			IdeApp.Initialized += (o, args) => {
				activeDocument = IdeApp.Workbench.ActiveDocument;
				IdeApp.Workbench.ActiveDocumentChanged += OnActiveDocumentChanged;

				IdeApp.Workbench.DocumentOpened += OnDocumentOpened;
				IdeApp.Workbench.DocumentClosed += OnDocumentClosed;
			};
		}

		void OnDocumentOpened (object sender, Gui.DocumentEventArgs e)
		{
			// TODO: Figure out how to detect this is a non-roslyn document.
			//e.Document.Editor.TextView.TextBuffer.PostChanged += OnNonRoslynBufferChanged;
		}

		void OnDocumentClosed (object sender, Gui.DocumentEventArgs e)
		{
			// TODO: Figure out how to detect this is a non-roslyn document.
			//e.Document.Editor.TextView.TextBuffer.PostChanged -= OnNonRoslynBufferChanged;
		}

		void OnActiveDocumentChanged (object sender, Gui.DocumentEventArgs e)
		{
			activeDocument = e.Document;
			ActiveDocumentChanged?.Invoke (this, GetActiveDocument ());
		}

		public event EventHandler<DocumentId> ActiveDocumentChanged;

		/// <summary>
		/// Get the <see cref="DocumentId"/> of the active document. May be called from any thread.
		/// May return null if there is no active document or the active document is not part of this
		/// workspace.
		/// </summary>
		/// <returns>The ID of the active document (if any)</returns>
		public DocumentId GetActiveDocument ()
		{
			return activeDocument?.AnalysisDocument?.Id;
		}

		/// <summary>
		/// Get a read only collection of the <see cref="DocumentId"/>s of all the visible documents in the workspace.
		/// </summary>
		public ImmutableArray<DocumentId> GetVisibleDocuments ()
		{
			var docs = IdeApp.Workbench?.Documents;
			if (docs == null)
				return ImmutableArray<DocumentId>.Empty;

			var ids = ArrayBuilder<DocumentId>.GetInstance (docs.Count);
			foreach (var doc in docs)
				if (doc.AnalysisDocument != null)
					ids.Add (doc.AnalysisDocument.Id);

			return ids.ToImmutableAndFree ();
		}

		public event EventHandler<EventArgs> NonRoslynBufferTextChanged;

		void OnNonRoslynBufferChanged (object sender, EventArgs e)
		{
			NonRoslynBufferTextChanged?.Invoke (sender, e);
		}

		public void Dispose ()
		{
			IdeApp.Workbench.ActiveDocumentChanged -= OnActiveDocumentChanged;
			IdeApp.Workbench.DocumentOpened -= OnDocumentOpened;
			IdeApp.Workbench.DocumentClosed -= OnDocumentClosed;
		}
	}
}