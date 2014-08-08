//
// TextEditorViewContent.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// The TextEditor object needs to be available through IBaseViewContent.GetContent therefore we need to insert a 
	/// decorator in between.
	/// </summary>
	class TextEditorViewContent : IViewContent, ICommandRouter
	{
		readonly TextEditor textEditor;
		readonly ITextEditorImpl textEditorImpl;

		MonoDevelop.Projects.Policies.PolicyContainer policyContainer;

		public TextEditorViewContent (TextEditor textEditor, ITextEditorImpl textEditorImpl)
		{
			if (textEditor == null)
				throw new ArgumentNullException ("textEditor");
			if (textEditorImpl == null)
				throw new ArgumentNullException ("textEditorImpl");
			this.textEditor = textEditor;
			this.textEditorImpl = textEditorImpl;
			this.textEditor.MimeTypeChanged += UpdateTextEditorOptions;
			this.textEditor.TextChanged += HandleTextChanged;
			DefaultSourceEditorOptions.Instance.Changed += UpdateTextEditorOptions;
			this.textEditorImpl.DirtyChanged += HandleDirtyChanged;
		}

		void HandleDirtyChanged (object sender, EventArgs e)
		{
			InformAutoSave ();
		}

		void HandleTextChanged (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			InformAutoSave ();
		}

		void UpdateTextEditorOptions (object sender, EventArgs e)
		{
			UpdateStyleParent (Project, textEditor.MimeType);
		}

		uint autoSaveTimer = 0;

		void InformAutoSave ()
		{
			RemoveAutoSaveTimer ();
			autoSaveTimer = GLib.Timeout.Add (500, delegate {
				AutoSave.InformAutoSaveThread (textEditor.CreateSnapshot (), textEditor.FileName, textEditorImpl.IsDirty);
				autoSaveTimer = 0;
				return false;
			});
		}


		void RemoveAutoSaveTimer ()
		{
			if (autoSaveTimer == 0)
				return;
			GLib.Source.Remove (autoSaveTimer);
			autoSaveTimer = 0;
		}

		void RemovePolicyChangeHandler ()
		{
			if (policyContainer != null)
				policyContainer.PolicyChanged -= HandlePolicyChanged;
		}

		void UpdateStyleParent (Project styleParent, string mimeType)
		{
			RemovePolicyChangeHandler ();

			if (string.IsNullOrEmpty (mimeType))
				mimeType = "text/plain";

			var mimeTypes = DesktopService.GetMimeTypeInheritanceChain (mimeType);

			if (styleParent != null)
				policyContainer = styleParent.Policies;
			else
				policyContainer = MonoDevelop.Projects.Policies.PolicyService.DefaultPolicies;
			var currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);

			policyContainer.PolicyChanged += HandlePolicyChanged;
			textEditor.Options = DefaultSourceEditorOptions.Instance.WithTextStyle (currentPolicy);
		}

		void HandlePolicyChanged (object sender, MonoDevelop.Projects.Policies.PolicyChangedEventArgs args)
		{
			var mimeTypes = DesktopService.GetMimeTypeInheritanceChain (textEditor.MimeType);
			var currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);
			textEditor.Options = DefaultSourceEditorOptions.Instance.WithTextStyle (currentPolicy);
		}

		#region IViewContent implementation

		event EventHandler IViewContent.ContentNameChanged {
			add {
				textEditorImpl.ContentNameChanged += value;
			}
			remove {
				textEditorImpl.ContentNameChanged -= value;
			}
		}

		event EventHandler IViewContent.ContentChanged {
			add {
				textEditorImpl.ContentChanged += value;
			}
			remove {
				textEditorImpl.ContentChanged -= value;
			}
		}

		event EventHandler IViewContent.DirtyChanged {
			add {
				textEditorImpl.DirtyChanged += value;
			}
			remove {
				textEditorImpl.DirtyChanged -= value;
			}
		}

		event EventHandler IViewContent.BeforeSave {
			add {
				textEditorImpl.BeforeSave += value;
			}
			remove {
				textEditorImpl.BeforeSave -= value;
			}
		}

		void IViewContent.Load (FileOpenInformation fileOpenInformation)
		{
			textEditorImpl.Load (fileOpenInformation);
		}
		
		void IViewContent.Load (string fileName)
		{
			textEditorImpl.Load (new FileOpenInformation (fileName));
		}

		void IViewContent.LoadNew (System.IO.Stream content, string mimeType)
		{
			textEditorImpl.LoadNew (content, mimeType);
		}

		void IViewContent.Save (FileSaveInformation fileSaveInformation)
		{
			if (!string.IsNullOrEmpty (fileSaveInformation.FileName))
				AutoSave.RemoveAutoSaveFile (fileSaveInformation.FileName);
			textEditorImpl.Save (fileSaveInformation);
		}

		void IViewContent.Save (string fileName)
		{
			if (!string.IsNullOrEmpty (fileName))
				AutoSave.RemoveAutoSaveFile (fileName);
			textEditorImpl.Save (new FileSaveInformation (fileName));
		}

		void IViewContent.Save ()
		{
			if (!string.IsNullOrEmpty (textEditorImpl.ContentName))
				AutoSave.RemoveAutoSaveFile (textEditorImpl.ContentName);
			textEditorImpl.Save ();
		}

		void IViewContent.DiscardChanges ()
		{
			textEditorImpl.DiscardChanges ();
		}

		public Project Project {
			get {
				return textEditorImpl.Project;
			}
			set {
				textEditorImpl.Project = value;
				UpdateTextEditorOptions (null, null);
			}
		}

		string IViewContent.PathRelativeToProject {
			get {
				return textEditorImpl.PathRelativeToProject;
			}
		}

		string IViewContent.ContentName {
			get {
				return textEditorImpl.ContentName;
			}
			set {
				textEditorImpl.ContentName = value;
			}
		}

		string IViewContent.UntitledName {
			get {
				return textEditorImpl.UntitledName;
			}
			set {
				textEditorImpl.UntitledName = value;
			}
		}

		string IViewContent.StockIconId {
			get {
				return textEditorImpl.StockIconId;
			}
		}

		bool IViewContent.IsUntitled {
			get {
				return textEditorImpl.IsUntitled;
			}
		}

		bool IViewContent.IsViewOnly {
			get {
				return textEditorImpl.IsViewOnly;
			}
		}

		bool IViewContent.IsFile {
			get {
				return textEditorImpl.IsFile;
			}
		}

		bool IViewContent.IsDirty {
			get {
				return textEditorImpl.IsDirty;
			}
			set {
				textEditorImpl.IsDirty = value;
			}
		}

		bool IViewContent.IsReadOnly {
			get {
				return textEditorImpl.IsReadOnly;
			}
		}

		#endregion

		#region IBaseViewContent implementation

		object IBaseViewContent.GetContent (Type type)
		{
			if (type.IsAssignableFrom (typeof(TextEditor)))
				return textEditor;
			var ext = textEditorImpl.EditorExtension;
			while (ext != null) {
				if (type.IsInstanceOfType (ext))
					return ext;
				ext = ext.Next;
			}
			return textEditorImpl.GetContent (type);
		}

		bool IBaseViewContent.CanReuseView (string fileName)
		{
			return textEditorImpl.CanReuseView (fileName);
		}

		void IBaseViewContent.RedrawContent ()
		{
			textEditorImpl.RedrawContent ();
		}

		IWorkbenchWindow IBaseViewContent.WorkbenchWindow {
			get {
				return textEditorImpl.WorkbenchWindow;
			}
			set {
				textEditorImpl.WorkbenchWindow = value;
			}
		}

		Gtk.Widget IBaseViewContent.Control {
			get {
				return textEditor;
			}
		}

		string IBaseViewContent.TabPageLabel {
			get {
				return textEditorImpl.TabPageLabel;
			}
		}

		#endregion

		#region IDisposable implementation

		void IDisposable.Dispose ()
		{
			DefaultSourceEditorOptions.Instance.Changed -= UpdateTextEditorOptions;
			RemovePolicyChangeHandler ();
			RemoveAutoSaveTimer ();
			textEditorImpl.Dispose ();
		}

		#endregion

		#region ICommandRouter implementation

		object ICommandRouter.GetNextCommandTarget ()
		{
			return textEditorImpl;
		}

		#endregion
	
		#region Commands
		void ToggleCodeCommentWithBlockComments ()
		{
			var blockStarts = TextEditorFactory.GetSyntaxProperties (textEditor.MimeType, "BlockCommentStart");
			var blockEnds = TextEditorFactory.GetSyntaxProperties (textEditor.MimeType, "BlockCommentEnd");
			if (blockStarts == null || blockEnds == null || blockStarts.Length == 0 || blockEnds.Length == 0)
				return;

			string blockStart = blockStarts[0];
			string blockEnd = blockEnds[0];

			using (var undo = textEditor.OpenUndoGroup ()) {
				IDocumentLine startLine;
				IDocumentLine endLine;

				if (textEditor.IsSomethingSelected) {
					startLine = textEditor.GetLineByOffset (textEditor.SelectionRange.Offset);
					endLine = textEditor.GetLineByOffset (textEditor.SelectionRange.EndOffset);
				} else {
					startLine = endLine = textEditor.GetLine (textEditor.CaretLine);
				}
				string startLineText = textEditor.GetTextAt (startLine.Offset, startLine.Length);
				string endLineText = textEditor.GetTextAt (endLine.Offset, endLine.Length);
				if (startLineText.StartsWith (blockStart, StringComparison.Ordinal) && endLineText.EndsWith (blockEnd, StringComparison.Ordinal)) {
					textEditor.RemoveText (endLine.Offset + endLine.Length - blockEnd.Length, blockEnd.Length);
					textEditor.RemoveText (startLine.Offset, blockStart.Length);
					if (textEditor.IsSomethingSelected) {
						textEditor.SelectionAnchorOffset -= blockEnd.Length;
					}
				} else {
					textEditor.InsertText (endLine.Offset + endLine.Length, blockEnd);
					textEditor.InsertText (startLine.Offset, blockStart);
					if (textEditor.IsSomethingSelected) {
						textEditor.SelectionAnchorOffset += blockEnd.Length;
					}
				}
			}
		}

		bool TryGetLineCommentTag (out string commentTag)
		{
			var lineComments = TextEditorFactory.GetSyntaxProperties (textEditor.MimeType, "LineComment");
			if (lineComments == null || lineComments.Length == 0) {
				commentTag = null;
				return false;
			}
			commentTag = lineComments [0];
			return true;
		}

		[CommandUpdateHandler (EditCommands.AddCodeComment)]
		[CommandUpdateHandler (EditCommands.RemoveCodeComment)]
		[CommandUpdateHandler (EditCommands.ToggleCodeComment)]
		void OnUpdateToggleComment (CommandInfo info)
		{
			var lineComments = TextEditorFactory.GetSyntaxProperties (textEditor.MimeType, "LineComment");
			if (lineComments != null && lineComments.Length > 0) {
				info.Visible = true;
				return;
			}
			var blockStarts = TextEditorFactory.GetSyntaxProperties (textEditor.MimeType, "BlockCommentStart");
			var blockEnds = TextEditorFactory.GetSyntaxProperties (textEditor.MimeType, "BlockCommentEnd");
			info.Visible = blockStarts != null && blockStarts.Length > 0 && blockEnds != null && blockEnds.Length > 0;
		}

		[CommandHandler (EditCommands.ToggleCodeComment)]
		void ToggleCodeComment ()
		{
			string commentTag;
			if (!TryGetLineCommentTag (out commentTag))
				return;
			bool comment = false;
			foreach (var line in GetSelectedLines (textEditor)) {
				if (line.GetIndentation (textEditor).Length == line.Length)
					continue;
				string text = textEditor.GetTextAt (line);
				string trimmedText = text.TrimStart ();
				if (!trimmedText.StartsWith (commentTag, StringComparison.Ordinal)) {
					comment = true;
					break;
				}
			}

			if (comment) {
				AddCodeComment ();
			} else {
				RemoveCodeComment ();
			}
		}

		static IEnumerable<IDocumentLine> GetSelectedLines (TextEditor textEditor)
		{
			var selection = textEditor.SelectionRange;
			var line = textEditor.GetLineByOffset (selection.EndOffset);
			do {
				yield return line;
				line = line.PreviousLine;
			} while (line.EndOffset > selection.Offset);
		}

		[CommandHandler (EditCommands.AddCodeComment)]
		void AddCodeComment ()
		{
			string commentTag;
			if (!TryGetLineCommentTag (out commentTag))
				return;

			using (var undo = textEditor.OpenUndoGroup ()) {
				var wasSelected = textEditor.IsSomethingSelected;
				var lead = textEditor.SelectionLeadOffset;
				var anchor = textEditor.SelectionAnchorOffset;
				int lines = 0;
				foreach (var line in GetSelectedLines (textEditor)) {
					lines++;
					//					if (line.GetIndentation (TextEditor.Document).Length == line.EditableLength)
					//						continue;
					textEditor.InsertText (line.Offset, commentTag);
				}
				if (wasSelected) {
					if (anchor < lead) {
						textEditor.SelectionAnchorOffset = anchor + commentTag.Length;
						textEditor.SelectionLeadOffset = lead + commentTag.Length * lines;
					} else {
						textEditor.SelectionAnchorOffset = anchor + commentTag.Length * lines;
						textEditor.SelectionLeadOffset = lead + commentTag.Length;
					}
				}
			}
		}

		[CommandHandler (EditCommands.RemoveCodeComment)]
		void RemoveCodeComment ()
		{
			string commentTag;
			if (!TryGetLineCommentTag (out commentTag))
				return;
			
			using (var undo = textEditor.OpenUndoGroup ()) {
				var wasSelected = textEditor.IsSomethingSelected;
				var lead = textEditor.SelectionLeadOffset;
				var anchor = textEditor.SelectionAnchorOffset;
				int lines = 0;
				
				int first = -1;
				int last;
				foreach (var line in GetSelectedLines (textEditor)) {
					string text = textEditor.GetTextAt (line);
					string trimmedText = text.TrimStart ();
					int length = 0;
					if (trimmedText.StartsWith (commentTag, StringComparison.Ordinal)) {
						textEditor.RemoveText (line.Offset + (text.Length - trimmedText.Length), commentTag.Length);
						length = commentTag.Length;
						lines++;
					}
					
					last = length;
					if (first < 0)
						first = last;
				}
				
				if (wasSelected) {
					if (anchor < lead) {
						textEditor.SelectionAnchorOffset = anchor - commentTag.Length;
						textEditor.SelectionLeadOffset = lead - commentTag.Length * lines;
					} else {
						textEditor.SelectionAnchorOffset = anchor - commentTag.Length * lines;
						textEditor.SelectionLeadOffset = lead - commentTag.Length;
					}
				}
			}
		}



		[CommandHandler (EditCommands.InsertGuid)]
		void InsertGuid ()
		{
			textEditor.InsertAtCaret (Guid.NewGuid ().ToString ());
		}
		#endregion
	
	}
}