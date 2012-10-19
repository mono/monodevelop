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
using System.Linq;
using Mono.TextEditor.PopupWindow;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.ProgressMonitoring;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.Gui;


namespace MonoDevelop.Refactoring.Rename
{
	public class RenameRefactoring : RefactoringOperation
	{
		public override string AccelKey {
			get {
				var key = IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.EditCommands.Rename).AccelKey;
				return key == null ? null : key.Replace ("dead_circumflex", "^");
			}
		}
		
		public RenameRefactoring ()
		{
			Name = "Rename";
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			if (options.SelectedItem is IVariable || options.SelectedItem is IParameter)
				return true;
			if (options.SelectedItem is ITypeDefinition)
				return !string.IsNullOrEmpty (((ITypeDefinition)options.SelectedItem).Region.FileName);
			if (options.SelectedItem is IType && ((IType)options.SelectedItem).Kind == TypeKind.TypeParameter)
				return !string.IsNullOrEmpty (((ITypeParameter)options.SelectedItem).Region.FileName);

			if (options.SelectedItem is IMember) {
				var cls = ((IMember)options.SelectedItem).DeclaringTypeDefinition;
				return cls != null;
			}
			return false;
		}

		public static void Rename (IEntity entity, string newName)
		{
			if (newName == null) {
				var options = new RefactoringOptions () {
					SelectedItem = entity
				};
				new RenameRefactoring ().Run (options);
				return;
			}
			using (var monitor = new NullProgressMonitor ()) {
				var col = ReferenceFinder.FindReferences (entity, true, monitor);
				
				List<Change> result = new List<Change> ();
				foreach (var memberRef in col) {
					var change = new TextReplaceChange ();
					change.FileName = memberRef.FileName;
					change.Offset = memberRef.Offset;
					change.RemovedChars = memberRef.Length;
					change.InsertedText = newName;
					change.Description = string.Format (GettextCatalog.GetString ("Replace '{0}' with '{1}'"), memberRef.GetName (), newName);
					result.Add (change);
				}
				if (result.Count > 0) {
					RefactoringService.AcceptChanges (monitor, result);
				}
			}
		}

		public static void RenameVariable (IVariable variable, string newName)
		{
			using (var monitor = new NullProgressMonitor ()) {
				var col = ReferenceFinder.FindReferences (variable, true, monitor);
				
				List<Change> result = new List<Change> ();
				foreach (var memberRef in col) {
					var change = new TextReplaceChange ();
					change.FileName = memberRef.FileName;
					change.Offset = memberRef.Offset;
					change.RemovedChars = memberRef.Length;
					change.InsertedText = newName;
					change.Description = string.Format (GettextCatalog.GetString ("Replace '{0}' with '{1}'"), memberRef.GetName (), newName);
					result.Add (change);
				}
				if (result.Count > 0) {
					RefactoringService.AcceptChanges (monitor, result);
				}
			}
		}

		public static void RenameTypeParameter (ITypeParameter typeParameter, string newName)
		{
			if (newName == null) {
				var options = new RefactoringOptions () {
					SelectedItem = typeParameter
				};
				new RenameRefactoring ().Run (options);
				return;
			}

			using (var monitor = new NullProgressMonitor ()) {
				var col = ReferenceFinder.FindReferences (typeParameter, true, monitor);
				
				List<Change> result = new List<Change> ();
				foreach (var memberRef in col) {
					var change = new TextReplaceChange ();
					change.FileName = memberRef.FileName;
					change.Offset = memberRef.Offset;
					change.RemovedChars = memberRef.Length;
					change.InsertedText = newName;
					change.Description = string.Format (GettextCatalog.GetString ("Replace '{0}' with '{1}'"), memberRef.GetName (), newName);
					result.Add (change);
				}
				if (result.Count > 0) {
					RefactoringService.AcceptChanges (monitor, result);
				}
			}
		}

		public override string GetMenuDescription (RefactoringOptions options)
		{
			return IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.EditCommands.Rename).Text;
		}
		
		public override void Run (RefactoringOptions options)
		{
			if (options.SelectedItem is IVariable) {
				var field = options.SelectedItem as IField;
				if (field != null && field.Accessibility != Accessibility.Private) {
					MessageService.ShowCustomDialog (new RenameItemDialog (options, this));
					return;
				}

				var col = ReferenceFinder.FindReferences (options.SelectedItem, true);
				if (col == null)
					return;
				var data = options.Document != null ? options.GetTextEditorData () : IdeApp.Workbench.ActiveDocument.Editor;
				var editor = data.Parent;
				if (editor == null) {
					MessageService.ShowCustomDialog (new RenameItemDialog (options, this));
					return;
				}
				
				var links = new List<TextLink> ();
				var link = new TextLink ("name");
				int baseOffset = Int32.MaxValue;
				foreach (var r in col) {
					baseOffset = Math.Min (baseOffset, r.Offset);
				}
				foreach (MemberReference r in col) {
					var segment = new TextSegment (r.Offset - baseOffset, r.Length);
					if (segment.Offset <= data.Caret.Offset - baseOffset && data.Caret.Offset - baseOffset <= segment.EndOffset) {
						link.Links.Insert (0, segment); 
					} else {
						link.AddLink (segment);
					}
				}
				
				links.Add (link);
				if (editor.CurrentMode is TextLinkEditMode)
					((TextLinkEditMode)editor.CurrentMode).ExitTextLinkMode ();
				TextLinkEditMode tle = new TextLinkEditMode (editor, baseOffset, links);
				tle.SetCaretPosition = false;
				tle.SelectPrimaryLink = true;
				if (tle.ShouldStartTextLinkMode) {
					var helpWindow = new TableLayoutModeHelpWindow ();
					helpWindow.TransientFor = IdeApp.Workbench.RootWindow;
					helpWindow.TitleText = options.SelectedItem is IVariable ? GettextCatalog.GetString ("<b>Local Variable -- Renaming</b>") : GettextCatalog.GetString ("<b>Parameter -- Renaming</b>");
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
				MessageService.ShowCustomDialog (new RenameItemDialog (options, this));
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
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			RenameProperties properties = (RenameProperties)prop;
			List<Change> result = new List<Change> ();
			IEnumerable<MemberReference> col = null;
			using (var monitor = new MessageDialogProgressMonitor (true, false, false, true)) {
				col = ReferenceFinder.FindReferences (options.SelectedItem, true, monitor);
				if (col == null)
					return result;
					
				if (properties.RenameFile && options.SelectedItem is IType) {
					var cls = ((IType)options.SelectedItem).GetDefinition ();
					int currentPart = 1;
					HashSet<string> alreadyRenamed = new HashSet<string> ();
					foreach (var part in cls.Parts) {
						if (alreadyRenamed.Contains (part.Region.FileName))
							continue;
						alreadyRenamed.Add (part.Region.FileName);
							
						string oldFileName = System.IO.Path.GetFileNameWithoutExtension (part.Region.FileName);
						string newFileName;
						if (oldFileName.ToUpper () == properties.NewName.ToUpper () || oldFileName.ToUpper ().EndsWith ("." + properties.NewName.ToUpper ()))
							continue;
						int idx = oldFileName.IndexOf (cls.Name);
						if (idx >= 0) {
							newFileName = oldFileName.Substring (0, idx) + properties.NewName + oldFileName.Substring (idx + cls.Name.Length);
						} else {
							newFileName = currentPart != 1 ? properties.NewName + currentPart : properties.NewName;
							currentPart++;
						}
							
						int t = 0;
						while (System.IO.File.Exists (GetFullFileName (newFileName, part.Region.FileName, t))) {
							t++;
						}
						result.Add (new RenameFileChange (part.Region.FileName, GetFullFileName (newFileName, part.Region.FileName, t)));
					}
				}
				
				foreach (var memberRef in col) {
					TextReplaceChange change = new TextReplaceChange ();
					change.FileName = memberRef.FileName;
					change.Offset = memberRef.Offset;
					change.RemovedChars = memberRef.Length;
					change.InsertedText = properties.NewName;
					change.Description = string.Format (GettextCatalog.GetString ("Replace '{0}' with '{1}'"), memberRef.GetName (), properties.NewName);
					result.Add (change);
				}
			}
			return result;
		}
		
		static string GetFullFileName (string fileName, string oldFullFileName, int tryCount)
		{
			StringBuilder name = new StringBuilder (fileName);
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
