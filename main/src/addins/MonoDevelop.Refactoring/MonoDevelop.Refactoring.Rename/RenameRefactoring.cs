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

namespace MonoDevelop.Refactoring.Rename
{
	public class RenameRefactoring
	{
		public static bool Rename (ISymbol symbol, string newName)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			if (newName == null)
				throw new ArgumentNullException ("newName");
			try {
				var result = new RenameRefactoring ().PerformChanges (symbol, new RenameProperties () { NewName = newName });

				using (var monitor = new ProgressMonitor ()) {
					if (result.Count > 0) {
						RefactoringService.AcceptChanges (monitor, result);
					}
				}
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
				var v = textChanges [i];
				editor.ReplaceText (v.Offset, v.InsertionLength, v.RemovedText);
			}
		}

		public void Rename (ISymbol symbol)
		{

			var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			var ws = TypeSystemService.GetWorkspace (solution);

			var currentSolution = ws.CurrentSolution;
			var newSolution = Renamer.RenameSymbolAsync (currentSolution, symbol, "_" + symbol.Name + "_", ws.Options).Result;
			var projectChanges = currentSolution.GetChanges (newSolution).GetProjectChanges ().ToList ();

			if (projectChanges.Count != 1) {
				MessageService.ShowCustomDialog (new RenameItemDialog (symbol, this));
				return;
			}

			var projectChange = projectChanges [0];
			var changes = projectChange.GetChangedDocuments ().ToList ();
			if (changes.Count != 1 || symbol.Kind == SymbolKind.NamedType) {
				MessageService.ShowCustomDialog (new RenameItemDialog (symbol, this));
				return;
			}
			var doc = IdeApp.Workbench.ActiveDocument;
			var editor = doc.Editor;
			
			var links = new List<TextLink> ();
			var link = new TextLink ("name");

			var cd = changes [0];
			var oldDoc = projectChange.OldProject.GetDocument (cd);
			var newDoc = projectChange.NewProject.GetDocument (cd);
			var oldVersion = editor.Version;
			foreach (var textChange in oldDoc.GetTextChangesAsync (newDoc).Result) {
				var segment = new TextSegment (textChange.Span.Start, textChange.Span.Length);
				if (segment.Offset <= editor.CaretOffset && editor.CaretOffset <= segment.EndOffset) {
					link.Links.Insert (0, segment); 
				} else {
					link.AddLink (segment);
				}
			}
			
			links.Add (link);
			editor.StartTextLinkMode (new TextLinkModeOptions (links, args => {
				if (!args.Success)
					return;

				var version = editor.Version;
				var span = symbol.Locations.First ().SourceSpan;
				var newName = link.CurrentText;
				var textChanges = version.GetChangesTo (oldVersion).ToList ();
				foreach (var v in textChanges) {
					editor.ReplaceText (v.Offset, v.RemovalLength, v.InsertedText);
				}
				var parsedDocument = doc.UpdateParseDocument ();
				if (parsedDocument == null) {
					Rollback (editor, textChanges);
					return;
				}
				var model = parsedDocument.GetAst<SemanticModel> ();
				if (model == null) {
					Rollback (editor, textChanges);
					return;
				}
				var node = model.SyntaxTree.GetRoot ().FindNode (span);
				if (node == null) {
					Rollback (editor, textChanges);
					return;
				}
				var sym = model.GetDeclaredSymbol (node);
				if (sym == null) {
					Rollback (editor, textChanges);
					return;
				}
				if (!Rename (sym, newName))
					Rollback (editor, textChanges);
			}));
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
		
		public List<Change> PerformChanges (ISymbol symbol, RenameProperties properties)
		{
			var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			var ws = TypeSystemService.GetWorkspace (solution);

			var newSolution = Renamer.RenameSymbolAsync (ws.CurrentSolution, symbol, properties.NewName, ws.Options).Result;
			var result = new List<Change> ();

			foreach (var change in ws.CurrentSolution.GetChanges (newSolution).GetProjectChanges ()) {
				foreach (var changedDocument in change.GetChangedDocuments ()) {
					var oldDoc = change.OldProject.GetDocument (changedDocument);
					var newDoc = change.NewProject.GetDocument (changedDocument);

					foreach (var textChange in oldDoc.GetTextChangesAsync (newDoc).Result.OrderByDescending(ts => ts.Span.Start)) {
						var trChange = new TextReplaceChange ();
						trChange.FileName = oldDoc.FilePath;
						trChange.Offset = textChange.Span.Start;
						trChange.RemovedChars = textChange.Span.Length;
						trChange.InsertedText = textChange.NewText;
						trChange.Description = string.Format (GettextCatalog.GetString ("Replace '{0}' with '{1}'"), symbol.Name, properties.NewName);
						result.Add (trChange);
					}
				}
			}

			if (properties.RenameFile && symbol.Kind == SymbolKind.NamedType) {
				int currentPart = 1;
				var alreadyRenamed = new HashSet<string> ();
				foreach (var part in symbol.Locations) {
					var filePath = part.SourceTree.FilePath;
					if (alreadyRenamed.Contains (filePath))
						continue;
					alreadyRenamed.Add (filePath);

					string oldFileName = System.IO.Path.GetFileNameWithoutExtension (filePath);
					string newFileName;
					if (oldFileName.ToUpper () == properties.NewName.ToUpper () || oldFileName.ToUpper ().EndsWith ("." + properties.NewName.ToUpper (), StringComparison.Ordinal))
						continue;
					int idx = oldFileName.IndexOf (symbol.Name, StringComparison.Ordinal);
					if (idx >= 0) {
						newFileName = oldFileName.Substring (0, idx) + properties.NewName + oldFileName.Substring (idx + symbol.Name.Length);
					} else {
						newFileName = currentPart != 1 ? properties.NewName + currentPart : properties.NewName;
						currentPart++;
					}

					int t = 0;
					while (System.IO.File.Exists (GetFullFileName (newFileName, filePath, t))) {
						t++;
					}
					result.Add (new RenameFileChange (filePath, GetFullFileName (newFileName, filePath, t)));
				}
			}

			return result;
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
