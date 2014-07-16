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
using Mono.TextEditor;
using System.Text;
using MonoDevelop.Ide;
using Mono.TextEditor.PopupWindow;
using MonoDevelop.Ide.ProgressMonitoring;
using MonoDevelop.Core.ProgressMonitoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.FindSymbols;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Refactoring.Rename
{
	public class RenameRefactoring
	{
		public static void Rename (ISymbol symbol, string newName)
		{
			var locations = new List<Tuple<string, TextSpan>> ();
			foreach (var loc in symbol.Locations) {
				locations.Add (Tuple.Create (loc.SourceTree.FilePath, loc.SourceSpan));
			}
					
			foreach (var mref in SymbolFinder.FindReferencesAsync (symbol, RoslynTypeSystemService.Workspace.CurrentSolution).Result) {
				foreach (var loc in mref.Locations) {
					locations.Add (Tuple.Create (loc.Document.FilePath, loc.Location.SourceSpan));
				}
			}
			
			using (var monitor = new NullProgressMonitor ()) {
				var result = new List<Change> ();
				foreach (var memberRef in locations) {
					var change = new TextReplaceChange ();
					change.FileName = memberRef.Item1;
					change.Offset = memberRef.Item2.Start;
					change.RemovedChars = memberRef.Item2.Length;
					change.InsertedText = newName;
					change.Description = string.Format (GettextCatalog.GetString ("Replace '{0}' with '{1}'"), symbol.Name, newName);
					result.Add (change);
				}
				if (result.Count > 0) {
					RefactoringService.AcceptChanges (monitor, result);
				}
			}
		}

		public void Rename (ISymbol symbol)
		{
			var locations = new List<Tuple<string, TextSpan>> ();
			var fileNames = new HashSet<string> ();
			foreach (var loc in symbol.Locations) {
				locations.Add (Tuple.Create (loc.SourceTree.FilePath, loc.SourceSpan));
				fileNames.Add (loc.SourceTree.FilePath);
			}
					
			foreach (var mref in SymbolFinder.FindReferencesAsync (symbol, RoslynTypeSystemService.Workspace.CurrentSolution).Result) {
				foreach (var loc in mref.Locations) {
					locations.Add (Tuple.Create (loc.Document.FilePath, loc.Location.SourceSpan));
					fileNames.Add (loc.Document.FilePath);
				}
			}
			
			if (fileNames.Count == 1) {
				var data = IdeApp.Workbench.ActiveDocument.Editor;
				var editor = data.Parent;
				if (editor == null)
					return;
				
				var links = new List<TextLink> ();
				var link = new TextLink ("name");
				int baseOffset = Int32.MaxValue;
				foreach (var r in locations) {
					baseOffset = Math.Min (baseOffset, r.Item2.Start);
				}
				foreach (var r in locations) {
					var segment = new TextSegment (r.Item2.Start - baseOffset, r.Item2.Length);
					if (segment.Offset <= data.Caret.Offset - baseOffset && data.Caret.Offset - baseOffset <= segment.EndOffset) {
						link.Links.Insert (0, segment); 
					} else {
						link.AddLink (segment);
					}
				}
				
				links.Add (link);
				var textLinkEditMode = editor.CurrentMode as TextLinkEditMode;
				if (textLinkEditMode != null)
					textLinkEditMode.ExitTextLinkMode ();
				var tle = new TextLinkEditMode (editor, baseOffset, links);
				tle.SetCaretPosition = false;
				tle.SelectPrimaryLink = true;
				if (tle.ShouldStartTextLinkMode) {
					var helpWindow = new TableLayoutModeHelpWindow ();
					helpWindow.TitleText = GettextCatalog.GetString ("<b>Renaming</b>"); //options.SelectedItem is IVariable ? GettextCatalog.GetString ("<b>Local Variable -- Renaming</b>") : GettextCatalog.GetString ("<b>Parameter -- Renaming</b>");
					helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Key</b>"), GettextCatalog.GetString ("<b>Behavior</b>")));
					helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Return</b>"), GettextCatalog.GetString ("<b>Accept</b> this refactoring.")));
					helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Esc</b>"), GettextCatalog.GetString ("<b>Cancel</b> this refactoring.")));
					tle.HelpWindow = helpWindow;
					tle.Cancel += delegate {
						if (tle.HasChangedText)
							editor.Document.Undo ();
					};
					tle.OldMode = data.CurrentMode;
					tle.StartMode ();
					data.CurrentMode = tle;
				}
			} else {
				MessageService.ShowCustomDialog (new RenameItemDialog (symbol, locations, this));
			}
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
		
		public List<Change> PerformChanges (ISymbol symbol, List<Tuple<string, TextSpan>> locations, RenameProperties properties)
		{
			var result = new List<Change> ();
			using (var monitor = new MessageDialogProgressMonitor (true, false, false, true)) {
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
							newFileName = oldFileName.Substring (0, idx) + properties.NewName + oldFileName.Substring (idx + filePath.Length);
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
				
				foreach (var memberRef in locations) {
					var change = new TextReplaceChange ();
					change.FileName = memberRef.Item1;
					change.Offset = memberRef.Item2.Start;
					change.RemovedChars = memberRef.Item2.Length;
					change.InsertedText = properties.NewName;
					change.Description = string.Format (GettextCatalog.GetString ("Replace '{0}' with '{1}'"), symbol.Name, properties.NewName);
					result.Add (change);
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
