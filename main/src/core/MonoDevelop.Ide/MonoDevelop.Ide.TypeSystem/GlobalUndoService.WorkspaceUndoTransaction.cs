//
// GlobalUndoServiceFactory.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory.MonoCSharp;
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using System.Linq;
using System.IO;

namespace MonoDevelop.Ide.TypeSystem
{
	partial class GlobalUndoService
	{
		internal abstract class Change
		{
			public abstract void UndoChange ();
			public virtual bool CheckUndoChange ()
			{
				return true;
			}

			public abstract void RedoChange ();
			public virtual bool CheckRedoChange ()
			{
				return true;
			}
		}

		internal class RenameChange : Change
		{
			string oldName, newName;

			public RenameChange (string oldName, string newName)
			{
				this.oldName = oldName ?? throw new ArgumentNullException (nameof (oldName));
				this.newName = newName ?? throw new ArgumentNullException (nameof (newName));
			}

			public override bool CheckUndoChange ()
			{
				if (!File.Exists (newName)) {
					MessageService.ShowError (GettextCatalog.GetString ("Can't find file '{0}'. Undo aborted.", newName));
					return false;
				}
				return true;
			}

			public override void UndoChange ()
			{
				FileService.RenameFile (newName, oldName);
				IdeApp.ProjectOperations.CurrentSelectedSolution?.SaveAsync (new ProgressMonitor ());
			}

			public override bool CheckRedoChange ()
			{
				if (!File.Exists (oldName)) {
					MessageService.ShowError (GettextCatalog.GetString ("Can't find file '{0}'. Redo aborted.", oldName));
					return false;
				}
				return true;
			}

			public override void RedoChange ()
			{
				FileService.RenameFile (oldName, newName);
				IdeApp.ProjectOperations.CurrentSelectedSolution?.SaveAsync (new ProgressMonitor ());
			}
		}

		internal class TextReplaceChange : Change
		{
			readonly WorkspaceUndoTransaction transaction;
			readonly string fileName;

			public string FileName {
				get {
					return fileName;
				}
			}

			readonly int offset;

			public int Offset {
				get {
					return offset;
				}
			}

			readonly string insertedText;

			public string InsertedText {
				get {
					return insertedText;
				}
			}

			readonly int removedChars;

			public int RemovedChars {
				get {
					return removedChars;
				}
			}

			public TextReplaceChange (IMonoDevelopUndoTransaction transaction, string fileName, int offset, int removedChars, string insertedText)
			{
				this.transaction = (WorkspaceUndoTransaction)transaction;
				this.fileName = fileName;
				this.offset = offset;
				this.insertedText = insertedText;
				this.removedChars = removedChars;
			}

			public override void UndoChange ()
			{
				// handled via text reset.
			}

			public override void RedoChange ()
			{
				var data = transaction.GetTextEditorData (fileName);
				data.ReplaceText (offset, removedChars, insertedText);
			}
		}

		class WorkspaceUndoTransaction : IMonoDevelopUndoTransaction
		{
			readonly Workspace workspace;
			readonly GlobalUndoService service;
			readonly Solution undoSolution;
			Solution redoSolution;
			string description;

			public string Description => description;
			List<Change> changes = new List<Change> ();
			Dictionary<string, ITextSource> originalDocuments = new Dictionary<string, ITextSource> ();

			// indicate whether undo transaction is currently active
			bool transactionAlive;

			public WorkspaceUndoTransaction (Workspace workspace, GlobalUndoService globalUndoService, string description)
			{
				Runtime.AssertMainThread ();
				this.workspace = workspace;
				this.service = globalUndoService;
				this.undoSolution = workspace?.CurrentSolution;
				this.description = description;
				foreach (var doc in IdeApp.Workbench.Documents) {
					doc?.Editor.StartGlobalUndoTransaction ();
				}
			}

			public void AddDocument (DocumentId id)
			{
				// nothing

				Runtime.AssertMainThread ();
			}

			public void Commit ()
			{
				Runtime.AssertMainThread ();
				redoSolution = workspace?.CurrentSolution;
				undoStack.Push (this);
				CloseTransaction ();

				foreach (var doc in IdeApp.Workbench.Documents)
					doc?.Editor.EndGlobalUndoTransaction (true);
			}

			public void UndoOperation ()
			{
				try {
					// undo file rename changes
					foreach (var change in changes)
						change.UndoChange ();
				} catch (OperationCanceledException) {
					CloseOpenDocuments ();
					return;
				}

				// reset documents
				foreach (var doc in originalDocuments) {
					var data = GetTextEditorData (doc.Key);
					if (doc.Value?.Version != null && data.Version.BelongsToSameDocumentAs (doc.Value.Version)) {
						foreach (var change in doc.Value.Version.GetChangesTo (data.Version)) {
							foreach (var c in change.TextChanges) {
								data.ReplaceText (c.Offset, c.InsertionLength, c.RemovedText);
							}
						}
					} else {
						data.Text = doc.Value.Text;
					}
					if (data is TextEditor editor)
						editor.ResetQuickDiff ();
				}
				CloseOpenDocuments ();
			}

			internal bool CheckUndoOperation ()
			{
				foreach (var change in changes)
					if (!change.CheckUndoChange ())
						return false;
				return true;
			}

			internal bool CheckRedoOperation ()
			{
				foreach (var change in changes)
					if (!change.CheckRedoChange ())
						return false;
				return true;
			}

			public void RedoOperation ()
			{
				try {
					foreach (var doc in originalDocuments) {
						var data = GetTextEditorData (doc.Key);
						data.Text = doc.Value.Text;
						if (data is TextEditor editor)
							editor.ResetQuickDiff ();
					}
					foreach (var change in changes)
						change.RedoChange ();
				} catch (OperationCanceledException) {
					return;
				} finally {
					CloseOpenDocuments ();
				}
			}

			public void CommitChange (Change change)
			{
				if (change is TextReplaceChange trc) {
					if (!originalDocuments.ContainsKey (trc.FileName)) {
						originalDocuments [trc.FileName] = GetTextEditorData (trc.FileName).CreateSnapshot ();
					}
				}
				changes.Add (change);
			}

			public void Dispose ()
			{
				CloseTransaction ();
				foreach (var doc in IdeApp.Workbench.Documents)
					doc?.Editor.EndGlobalUndoTransaction (false);
			}

			private void CloseTransaction ()
			{
				Runtime.AssertMainThread ();

				// once either commit or disposed is called, don't do finalizer check
				GC.SuppressFinalize (this);

				if (transactionAlive) {
					service.ActiveTransactions--;
					transactionAlive = false;
				}
			}

			Dictionary<string, ITextDocument> documents = new Dictionary<string, ITextDocument> ();

			void CloseOpenDocuments ()
			{
				foreach (var doc in documents) {
					doc.Value.Save ();
				}
				documents.Clear ();
			}

			public ITextDocument GetTextEditorData (FilePath filePath)
			{
				if (documents.TryGetValue (filePath, out var result))
					return result;
				result = TextFileProvider.Instance.GetTextEditorData (filePath, out var isOpen);
				if (!isOpen)
					documents [filePath] = result;
				return result;
			}

			internal bool ShowNagScreenForUndo ()
			{
				foreach (var doc in originalDocuments) {
					var data = GetTextEditorData (doc.Key);
					int delta = 0;
					foreach (var change in changes) {
						if (!(change is TextReplaceChange trc))
							continue;
						if (trc.FileName != data.FileName)
							continue;
						if (trc.InsertedText != data.GetTextAt (trc.Offset, trc.InsertedText.Length))
							return true;
						delta = trc.RemovedChars - trc.InsertedText.Length;
					}
					if (data.Length + delta != doc.Value.Length)
						return true;
				}
				return false;
			}

			internal bool ShowNagScreenForRedo ()
			{
				foreach (var doc in originalDocuments) {
					if (GetTextEditorData (doc.Key).Text != doc.Value.Text)
						return true;
				}
				return false;
			}
		}
	}
}
