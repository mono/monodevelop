// 
// Rename.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using System.Text;
using MonoDevelop.Ide;
using System.Linq;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.ProgressMonitoring;
using MonoDevelop.Core.ProgressMonitoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.FindSymbols;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using Microsoft.CodeAnalysis.Rename;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace MonoDevelop.Refactoring.Rename
{
	public class RenameRefactoring
	{
		public static async Task<bool> Rename (ISymbol symbol, string newName)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			if (newName == null)
				throw new ArgumentNullException ("newName");
			try {
				await new RenameRefactoring ().PerformChangesAsync (symbol, new RenameProperties () { NewName = newName });
				return true;
			} catch (AggregateException ae) {
				foreach (var inner in ae.Flatten ().InnerExceptions)
					LoggingService.LogError ("Exception while rename.", inner);
				return false;
			} catch (Exception e) {
				LoggingService.LogError ("Exception while rename.", e);
				return false;
			}
		}

		static void Rollback (TextEditor editor, List<MonoDevelop.Core.Text.TextChangeEventArgs> textChanges)
		{
			for (int i = textChanges.Count - 1; i >= 0; i--) {
				for (int j = 0; j < textChanges [i].TextChanges.Count; ++j) {
					var v = textChanges [i].TextChanges [j];
					editor.ReplaceText (v.Offset, v.InsertionLength, v.RemovedText);
				}
			}
		}

		public async Task Rename (ISymbol symbol)
		{
			var ws = IdeApp.Workbench.ActiveDocument.RoslynWorkspace;

			var currentSolution = ws.CurrentSolution;
			var cts = new CancellationTokenSource ();
			var newSolution = await MessageService.ExecuteTaskAndShowWaitDialog (Task.Run (() => Renamer.RenameSymbolAsync (currentSolution, symbol, "_" + symbol.Name + "_", ws.Options, cts.Token)), GettextCatalog.GetString ("Looking for all references"), cts);
			var projectChanges = currentSolution.GetChanges (newSolution).GetProjectChanges ().ToList ();
			var changedDocuments = new List<string> ();
			foreach (var change in projectChanges) {
				foreach (var changedDoc in change.GetChangedDocuments ()) {
					changedDocuments.Add (ws.CurrentSolution.GetDocument (changedDoc).FilePath);
				}
			}

			if (changedDocuments.Count > 1) {
				using (var dlg = new RenameItemDialog (symbol, this)) {
					dlg.ChangedDocuments = changedDocuments;
					MessageService.ShowCustomDialog (dlg);
				}
				return;
			}

			var projectChange = projectChanges [0];
			var changes = projectChange.GetChangedDocuments ().ToList ();
			if (changes.Count != 1 || symbol.Kind == SymbolKind.NamedType) {
				using (var dlg = new RenameItemDialog (symbol, this))
					MessageService.ShowCustomDialog (dlg);
				return;
			}
			var doc = IdeApp.Workbench.ActiveDocument;
			var editor = doc.Editor;
			var oldVersion = editor.Version;

			var links = new List<TextLink> ();
			var link = new TextLink ("name");

			var documents = ImmutableHashSet.Create (doc.AnalysisDocument);

			foreach (var loc in symbol.Locations) {
				if (loc.IsInSource && FilePath.PathComparer.Equals (loc.SourceTree.FilePath, doc.FileName)) {
					link.AddLink (new TextSegment (loc.SourceSpan.Start, loc.SourceSpan.Length));
				}
			}

			foreach (var mref in await SymbolFinder.FindReferencesAsync (symbol, doc.AnalysisDocument.Project.Solution, documents, default (CancellationToken))) {
				foreach (var loc in mref.Locations) {
					TextSpan span = loc.Location.SourceSpan;
					var root = loc.Location.SourceTree.GetRoot ();
					var node = root.FindNode (loc.Location.SourceSpan);
					var trivia = root.FindTrivia (loc.Location.SourceSpan.Start);
					if (!trivia.IsKind (SyntaxKind.SingleLineDocumentationCommentTrivia)) {
						span = node.Span;
					}
					if (span.Start != loc.Location.SourceSpan.Start) {
						span = loc.Location.SourceSpan;
					}
					var segment = new TextSegment (span.Start, span.Length);
					if (segment.Offset <= editor.CaretOffset && editor.CaretOffset <= segment.EndOffset) {
						link.Links.Insert (0, segment);
					} else {
						link.AddLink (segment);
					}
				}
			}
			links.Add (link);

			editor.StartTextLinkMode (new TextLinkModeOptions (links, (arg) => {
				//If user cancel renaming revert changes
				if (!arg.Success) {
					var textChanges = editor.Version.GetChangesTo (oldVersion).ToList ();
					for (int i = textChanges.Count - 1; i >= 0; i--) {
						var change = textChanges [i];
						var rollbackChanges = new List<Microsoft.CodeAnalysis.Text.TextChange> ();
						for (int j = 0; j < change.TextChanges.Count; j++) {
							var textChange = change.TextChanges [j];
							rollbackChanges.Add (new Microsoft.CodeAnalysis.Text.TextChange (new TextSpan (textChange.Offset, textChange.InsertionLength), textChange.RemovedText.Text));
						}
						editor.ApplyTextChanges (rollbackChanges);
					}
				}
			}) { TextLinkPurpose = TextLinkPurpose.Rename });
		}

		public class RenameProperties
		{
			public string NewName {
				get;
				set;
			}

			public bool RenameFile {
				get;
				set;
			}

			public bool IncludeOverloads {
				get;
				set;
			}
		}

		public async Task<List<Change>> PerformChangesAsync (ISymbol symbol, RenameProperties properties)
		{
			var ws = IdeApp.Workbench.ActiveDocument.RoslynWorkspace;

			var newSolution = await Renamer.RenameSymbolAsync (ws.CurrentSolution, symbol, properties.NewName, ws.Options);
			var changes = new List<Change> ();
			var documents = new List<Microsoft.CodeAnalysis.Document> ();
			foreach (var projectChange in newSolution.GetChanges (ws.CurrentSolution).GetProjectChanges ()) {
				documents.AddRange (projectChange.GetChangedDocuments ().Select(d => newSolution.GetDocument (d)));
			}
			FilterDuplicateLinkedDocs ((MonoDevelopWorkspace)ws, newSolution, documents);
			foreach (var newDoc in documents) {
				foreach (var textChange in await newDoc.GetTextChangesAsync (ws.CurrentSolution.GetDocument (newDoc.Id))) {
					changes.Add (new TextReplaceChange () {
						FileName = newDoc.FilePath,
						Offset = textChange.Span.Start,
						RemovedChars = textChange.Span.Length,
						InsertedText = textChange.NewText
					});
				}
			}

			if (properties.RenameFile && symbol is INamedTypeSymbol) {
				var type = (INamedTypeSymbol)symbol;
				int currentPart = 1;
				var alreadyRenamed = new HashSet<string> ();
				foreach (var part in type.Locations) {
					if (!part.IsInSource)
						continue;
					var fileName = part?.SourceTree?.FilePath;
					if (fileName == null || alreadyRenamed.Contains (fileName))
						continue;
					alreadyRenamed.Add (fileName);

					string oldFileName = System.IO.Path.GetFileNameWithoutExtension (fileName);
					string newFileName;
					var newName = properties.NewName;
					if (string.IsNullOrEmpty (oldFileName) || string.IsNullOrEmpty (newName))
						continue;
					if (IsCompatibleForRenaming (oldFileName, newName))
						continue;
					int idx = oldFileName.IndexOf (type.Name, StringComparison.Ordinal);
					if (idx >= 0) {
						newFileName = oldFileName.Substring (0, idx) + newName + oldFileName.Substring (idx + type.Name.Length);
					} else {
						newFileName = currentPart != 1 ? newName + currentPart : newName;
						currentPart++;
					}

					int t = 0;
					while (System.IO.File.Exists (GetFullFileName (newFileName, fileName, t))) {
						t++;
					}
					changes.Add (new RenameFileChange (fileName, GetFullFileName (newFileName, fileName, t)));
				}
			}
			return changes;
		}

		internal static bool IsCompatibleForRenaming (string oldName, string newName)
		{
			return string.Equals (oldName, newName, StringComparison.OrdinalIgnoreCase) || oldName.EndsWith ("." + newName, StringComparison.OrdinalIgnoreCase);
		}

		static void FilterDuplicateLinkedDocs (MonoDevelopWorkspace ws, Solution newSolution, List<Microsoft.CodeAnalysis.Document> documents)
		{
			foreach (var newDoc in documents) {
				bool didRemove = false;

				if (documents.Count > 1) {
					// Filter out projections from other type systems otherwise changes would be reflected twice to the original
					// file.
					foreach (var projectionEntry in ws.ProjectionList) {
						if (!projectionEntry.Projections.Any (p => p.Document.FileName.Equals (newDoc.FilePath)))
							continue;
						if (!documents.Any (d => d != newDoc && d.Project == newDoc.Project)) {
							documents.Remove (newDoc);
							didRemove = true;
							goto skip;
						}
					}
				}
				foreach (var linkId in newDoc.GetLinkedDocumentIds ()) {
					var link = documents.FirstOrDefault (d => d.Id == linkId);
					if (link != null) {
						documents.Remove (link);
						didRemove = true;
					}
				}
				skip:
				if (didRemove) {
					FilterDuplicateLinkedDocs (ws, newSolution, documents);
					return;
				}
			}
		}

		static string GetFullFileName (string fileName, string oldFullFileName, int tryCount)
		{
			var name = new StringBuilder (fileName);
			if (tryCount > 0) {
				name.Append ("_");
				name.Append (tryCount.ToString ());
			}
			if (System.IO.Path.HasExtension (oldFullFileName))
				name.Append (System.IO.Path.GetExtension (oldFullFileName));

			return System.IO.Path.Combine (System.IO.Path.GetDirectoryName (oldFullFileName), name.ToString ());
		}
	}
}
