// SourceEditorView.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Text;

using Gtk;
using Gnome;

using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Search;

namespace MonoDevelop.SourceEditor
{	
	public class SourceEditorView : AbstractViewContent, IPositionable, IExtensibleTextEditor, IBookmarkBuffer, IClipboardHandler, ICompletionWidget, IDocumentInformation, ICodeStyleOperations, ISplittable, IFoldable, IPrintable
	{
		SourceEditorWidget widget;
		bool isDisposed = false;
		static object fileSaveLock = new object ();
		FileSystemWatcher fileSystemWatcher;
		
		public Mono.TextEditor.Document Document {
			get {
				return widget.TextEditor.Document;
			}
		}
		
		public Mono.TextEditor.TextEditorData TextEditorData {
			get {
				return widget.TextEditor.TextEditorData;
			}
		}
		
		public override Gtk.Widget Control {
			get {
				return widget;
			}
		}
		
		public SourceEditorView()
		{
			widget = new SourceEditorWidget (this);
//			widget.TextEditor.Buffer.TextReplaced += delegate (object sender, ReplaceEventArgs args) {
//				int startIndex = args.Offset;
//				int endIndex   = startIndex + Math.Max (args.Count, args.Value != null ? args.Value.Length : 0);
//				if (TextChanged != null)
//					TextChanged (this, new TextChangedEventArgs (startIndex, endIndex));
//			};
			
			widget.TextEditor.Buffer.TextReplaced += delegate {
				this.IsDirty = true;
			};
			
			widget.TextEditor.Caret.PositionChanged += delegate {
				FireCompletionContextChanged ();
			};
			
//			GLib.Timeout.Add (1000, delegate {
//				if (!widget.IsSplitted)
//					widget.Split (true);
//				return true;
//			});
			
			fileSystemWatcher = new FileSystemWatcher ();
			fileSystemWatcher.Created += (FileSystemEventHandler)MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new FileSystemEventHandler (OnFileChanged));	
			fileSystemWatcher.Changed += (FileSystemEventHandler)MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new FileSystemEventHandler (OnFileChanged));
			widget.ShowAll ();
		}
		
		public override void Save (string fileName)
		{
			Save (fileName, null);
		}
		
		public void Save (string fileName, string encoding)
		{
			fileName = ConvertFileNameToVFS (fileName);

			if (warnOverwrite) {
				if (fileName == ContentName) {
					if (!MonoDevelop.Core.Gui.Services.MessageService.AskQuestion (string.Format (GettextCatalog.GetString ("This file {0} has been changed outside of MonoDevelop. Are you sure you want to overwrite the file?"), fileName),"MonoDevelop"))
						return;
				}
				warnOverwrite = false;
				widget.RemoveReloadBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			
			lock (fileSaveLock) {
				File.WriteAllText (fileName, Document.Text);
//				lastSaveTime = File.GetLastWriteTime (fileName);
			}
//			if (encoding != null)
//				se.Buffer.SourceEncoding = encoding;
//			TextFileService.FireCommitCountChanges (this);
			ContentName = fileName;
			Document.MimeType = IdeApp.Services.PlatformService.GetMimeTypeForUri (fileName);
//			InitializeFormatter ();
			this.IsDirty = false;
		}
		
		public override void Load (string fileName)
		{
			Load (fileName, null);
		}
		

		static string ConvertFileNameToVFS (string fileName)
		{
			string result = fileName;
			result = result.Replace ("%", "%25");
			result = result.Replace ("#", "%23");
			result = result.Replace ("?", "%3F");
			return result;
		}
		bool warnOverwrite = false;
		public void Load (string fileName, string encoding)
		{
			fileName = ConvertFileNameToVFS (fileName);
			if (warnOverwrite) {
				warnOverwrite = false;
				widget.RemoveReloadBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			
			Document.Text = File.ReadAllText (fileName);
			Document.MimeType = IdeApp.Services.PlatformService.GetMimeTypeForUri (fileName);
			ContentName = fileName;
			lastSaveTime = File.GetLastWriteTime (ContentName);
//			InitializeFormatter ();
//			
//			if (Services.DebuggingService != null) {
//				foreach (IBreakpoint b in Services.DebuggingService.GetBreakpointsAtFile (fileName))
//					se.View.ShowBreakpointAt (b.Line - 1);
//					
//				UpdateExecutionLocation ();
//			}
//			
			widget.LoadClassCombo ();
			this.IsDirty = false;
		}
		
		public override void Dispose()
		{
			this.isDisposed = true;
		}
		
		public IParserContext GetParserContext ()
		{
			IParserDatabase pdb = IdeApp.ProjectOperations.ParserDatabase;
			
			Project project = Project;
			if (project != null) 
				return pdb.GetProjectParserContext (project);
			
			return pdb.GetFileParserContext (IsUntitled ? UntitledName : ContentName);
		}
		
		public MonoDevelop.Projects.Ambience.Ambience GetAmbience ()
		{
			Project project = Project;
			if (project != null)
				return project.Ambience;
			string file = this.IsUntitled ? this.UntitledName : this.ContentName;
			return MonoDevelop.Projects.Services.Ambience.GetAmbienceForFile (file);
		}
		
		DateTime lastSaveTime;
		void OnFileChanged (object sender, FileSystemEventArgs args)
		{
			lock (fileSaveLock) {
				if (lastSaveTime == File.GetLastWriteTime (ContentName))
					return;
			}
			
			if (args.ChangeType == WatcherChangeTypes.Changed || args.ChangeType == WatcherChangeTypes.Created) {
				widget.ShowFileChangedWarning ();
			}
		}
		
#region IExtensibleTextEditor
		ITextEditorExtension IExtensibleTextEditor.AttachExtension (ITextEditorExtension extension)
		{
			this.widget.TextEditor.Extension = extension;
			return this.widget;
		}
		
//		protected override void OnMoveCursor (MovementStep step, int count, bool extend_selection)
//		{
//			base.OnMoveCursor (step, count, extend_selection);
//			if (extension != null)
//				extension.CursorPositionChanged ();
//		}
		
//		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
//		{
//			if (extension != null)
//				return extension.KeyPress (evnt.Key, evnt.State);
//			return this.KeyPress (evnt.Key, evnt.State); 
//		}		
#endregion
		
#region IEditableTextBuffer
		public IClipboardHandler ClipboardHandler {
			get {
				return this;
			}
		}
		
		public bool EnableUndo {
			get {
				return this.Document.CanUndo;
			}
		}
		
		public void Undo()
		{
			this.Document.Undo ();
		}
		
		public bool EnableRedo {
			get {
				return this.Document.CanRedo;
			}
		}
		
		public void Redo()
		{
			this.Document.Redo ();
		}
		
		public void BeginAtomicUndo ()
		{
			this.Document.BeginAtomicUndo ();
		}
		public void EndAtomicUndo ()
		{
			this.Document.EndAtomicUndo ();
		}
			
		public string SelectedText { 
			get {
				return this.widget.TextEditor.TextEditorData.IsSomethingSelected ? this.widget.TextEditor.Document.Buffer.GetTextAt (this.widget.TextEditor.TextEditorData.SelectionRange) : "";
			}
			set {
				this.widget.TextEditor.TextEditorData.DeleteSelectedText ();
				this.widget.TextEditor.Document.Buffer.Insert (this.widget.TextEditor.Caret.Offset,
				                                               new StringBuilder (value));
				this.widget.TextEditor.TextEditorData.SelectionRange = new Segment (this.widget.TextEditor.Caret.Offset, value.Length);
				this.widget.TextEditor.Caret.Offset += value.Length; 
			}
		}
		
		public event TextChangedEventHandler TextChanged;
#endregion

#region ITextBuffer
		public int CursorPosition { 
			get {
				return this.widget.TextEditor.Caret.Offset;
			}
			set {
				this.widget.TextEditor.Caret.Offset = value;
			}
		}

		public int SelectionStartPosition { 
			get {
				if (!widget.TextEditor.TextEditorData.IsSomethingSelected)
					return this.widget.TextEditor.Caret.Offset;
				return this.widget.TextEditor.TextEditorData.SelectionRange.Offset;
			}
		}
		public int SelectionEndPosition { 
			get {
				if (!widget.TextEditor.TextEditorData.IsSomethingSelected)
					return this.widget.TextEditor.Caret.Offset;
				return this.widget.TextEditor.TextEditorData.SelectionRange.EndOffset;
			}
		}
		
		public void Select (int startPosition, int endPosition)
		{
			this.widget.TextEditor.TextEditorData.SelectionRange = new Segment (startPosition, endPosition - startPosition);
			this.widget.TextEditor.ScrollToCaret ();
		}
		
		public void ShowPosition (int position)
		{
			// TODO
		}
#endregion
		
#region ITextFile
		public string Name {
			get { 
				return this.ContentName; 
			} 
		}

		public string Text {
			get {
				return this.widget.TextEditor.Buffer.Text;
			}
			set {
				this.widget.TextEditor.Buffer.Text = value;
			}
		}
		public int Length { 
			get {
				return this.widget.TextEditor.Buffer.Length;
			}
		}

		public bool WarnOverwrite {
			get {
				return warnOverwrite;
			}
			set {
				warnOverwrite = value;
			}
		}

		public string GetText (int startPosition, int endPosition)
		{
			if (startPosition < 0 || endPosition < 0 || startPosition > endPosition)
				return "";
			return this.widget.TextEditor.Buffer.GetTextAt (startPosition, endPosition - startPosition);
		}
		
		public char GetCharAt (int position)
		{
			return this.widget.TextEditor.Buffer.GetCharAt (position);
		}
		
		public int GetPositionFromLineColumn (int line, int column)
		{
			return this.widget.TextEditor.Document.LocationToOffset (new DocumentLocation (line - 1, column - 1));
		}
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			DocumentLocation location = this.widget.TextEditor.Document.OffsetToLocation (position);
			line   = location.Line + 1;
			column = location.Column + 1;
		}
#endregion

#region IEditableTextFile
		public void InsertText (int position, string text)
		{
			this.widget.TextEditor.Document.Buffer.Insert (position, new StringBuilder (text));
			if (text != null && this.widget.TextEditor.Caret.Offset >= position) 
				this.widget.TextEditor.Caret.Offset += text.Length;
		}
		public void DeleteText (int position, int length)
		{
			this.widget.TextEditor.Document.Buffer.Remove (position, length);
			if (this.widget.TextEditor.Caret.Offset >= position) 
				this.widget.TextEditor.Caret.Offset -= length;
		}
#endregion 
		
#region IPositionable
		public void JumpTo (int line, int column)
		{
			widget.TextEditor.Caret.Location = new DocumentLocation (line - 1, column - 1);
			
			GLib.Timeout.Add (20,  delegate {
				if (this.isDisposed)
					return false;
				widget.TextEditor.GrabFocus ();
				widget.TextEditor.ScrollToCaret ();
				return false;
			});
		}
#endregion
		
#region IBookmarkBuffer
		LineSegment GetLine (int position)
		{
			DocumentLocation location = widget.TextEditor.TextEditorData.Document.OffsetToLocation (position);
			return widget.TextEditor.TextEditorData.Document.GetLine (location.Line);
		}
				
		public void SetBookmarked (int position, bool mark)
		{
			LineSegment line = GetLine (position);
			if (line != null && line.IsBookmarked != mark) {
				int lineNumber = widget.TextEditor.Document.Splitter.GetLineNumberForOffset (line.Offset);
				line.IsBookmarked = mark;
				widget.TextEditor.Document.RequestUpdate (new LineUpdate (lineNumber));
				widget.TextEditor.Document.CommitDocumentUpdate ();
			}
		}
		
		public bool IsBookmarked (int position)
		{
			LineSegment line = GetLine (position);
			return line != null ? line.IsBookmarked : false;
		}
		
		public void PrevBookmark ()
		{
			new GotoPrevBookmark ().Run (widget.TextEditor.TextEditorData);
		}
		public void NextBookmark ()
		{
			new GotoNextBookmark ().Run (widget.TextEditor.TextEditorData);
		}
		public void ClearBookmarks ()
		{
			new ClearAllBookmarks ().Run (widget.TextEditor.TextEditorData);
		}
#endregion
		
#region IClipboardHandler
		public bool EnableCut {
			get {
				return this.widget.TextEditor.TextEditorData.IsSomethingSelected;
			}
		}
		public bool EnableCopy {
			get {
				return EnableCut;
			}
		}
		public bool EnablePaste {
			get {
				return true;
			}
		}
		public bool EnableDelete {
			get {
				return true;
			}
		}
		public bool EnableSelectAll {
			get {
				return true;
			}
		}
		
		public void Cut (object sender, EventArgs args)
		{
			new CutAction ().Run (widget.TextEditor.TextEditorData);
		}
		public void Copy (object sender, EventArgs args)
		{
			new CopyAction ().Run (widget.TextEditor.TextEditorData);
		}
		public void Paste (object sender, EventArgs args)
		{
			new PasteAction ().Run (widget.TextEditor.TextEditorData);
		}
		public void Delete (object sender, EventArgs args)
		{
			new DeleteAction ().Run (widget.TextEditor.TextEditorData);
		}
		public void SelectAll (object sender, EventArgs args)
		{
			new SelectionSelectAll ().Run (widget.TextEditor.TextEditorData);
		}
#endregion

#region ICompletionWidget		
		public int TextLength {
			get {
				return this.widget.TextEditor.Document.Buffer.Length;
			}
		}
		public int SelectedLength { 
			get {
				return this.widget.TextEditor.TextEditorData.IsSomethingSelected ? this.widget.TextEditor.TextEditorData.SelectionRange.Length : 0;
			}
		}
//		public string GetText (int startOffset, int endOffset)
//		{
//			return this.widget.TextEditor.Document.Buffer.GetTextAt (startOffset, endOffset - startOffset);
//		}
		public char GetChar (int offset)
		{
			return this.widget.TextEditor.Document.Buffer.GetCharAt (offset);
		}
		
		public Gtk.Style GtkStyle { 
			get {
				return widget.Style.Copy ();
			}
		}

		public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset) 
		{
			CodeCompletionContext result = new CodeCompletionContext ();
			result.TriggerOffset = this.widget.TextEditor.Caret.Offset;
			result.TriggerLine   = this.widget.TextEditor.Caret.Line;
			result.TriggerLineOffset = this.widget.TextEditor.Caret.Column;
			Gdk.Point p = this.widget.TextEditor.DocumentToVisualLocation (this.widget.TextEditor.Caret.Location);
			int tx, ty;
			widget.ParentWindow.GetOrigin (out tx, out ty);
			tx += widget.TextEditor.Allocation.X;
			ty += widget.TextEditor.Allocation.Y;
			result.TriggerXCoord = tx + p.X + this.widget.TextEditor.XOffset - (int)this.widget.TextEditor.TextEditorData.HAdjustment.Value;
			result.TriggerYCoord = ty + p.Y - (int)this.widget.TextEditor.TextEditorData.VAdjustment.Value + this.widget.TextEditor.LineHeight;
			result.TriggerTextHeight = this.widget.TextEditor.LineHeight;
			return result;
		}
 
		public string GetCompletionText (ICodeCompletionContext ctx)
		{
			if (ctx == null)
				return null;
			int min = Math.Min (ctx.TriggerOffset, this.widget.TextEditor.Caret.Offset);
			int max = Math.Max (ctx.TriggerOffset, this.widget.TextEditor.Caret.Offset);
			return widget.TextEditor.Buffer.GetTextAt (min, max - min);
		}
		
		public void SetCompletionText (ICodeCompletionContext ctx, string partial_word, string complete_word)
		{
			this.widget.TextEditor.TextEditorData.DeleteSelectedText ();
			
			int idx = complete_word.IndexOf ('|'); // | in the completion text now marks the caret position
			if (idx >= 0) {
				complete_word = complete_word.Remove (idx, 1);
			} else {
				idx = complete_word.Length;
			}
			
			this.widget.TextEditor.Buffer.Replace (ctx.TriggerOffset, String.IsNullOrEmpty (partial_word) ? 0 : partial_word.Length, new StringBuilder (complete_word));
			this.widget.TextEditor.Caret.Offset = ctx.TriggerOffset + idx;
		}
		
		void FireCompletionContextChanged ()
		{
			if (CompletionContextChanged != null)
				CompletionContextChanged (this, EventArgs.Empty);
		}
		
		public event EventHandler CompletionContextChanged;
#endregion
		
#region IDocumentInformation
		string IDocumentInformation.FileName {
			get { 
				return this.IsUntitled ? UntitledName : ContentName; 
			}
		}
		
		public ITextIterator GetTextIterator ()
		{
			return new DocumentTextIterator (this, this.widget.TextEditor.TextEditorData.Caret.Offset);
		}
		
		public string GetLineTextAtOffset (int offset)
		{
			LineSegment line = this.widget.TextEditor.Document.Splitter.GetByOffset (offset);
			if (line == null)
				return null;
			return this.widget.TextEditor.Document.Buffer.GetTextAt (line);
		}
		
		class DocumentTextIterator : ITextIterator
		{
			SourceEditorView view;
			int initialOffset, offset;
			
			public DocumentTextIterator (SourceEditorView view, int offset)
			{
				this.view = view;
				this.initialOffset = this.offset = offset;
			}
			
			public char Current {
				get {
					return view.Document.Buffer.GetCharAt (offset);
				}
			}
			public int Position {
				get {
					return offset;
				}
				set {
					offset = value;
				}
			}
			public int Line { 
				get {
					return view.Document.Splitter.GetLineNumberForOffset (offset);
				}
			}
			public int Column {
				get {
					return offset - view.Document.Splitter.GetByOffset (offset).Offset;
				}
			}
			public int DocumentOffset { 
				get {
					return offset;
				}
			}
			
			public char GetCharRelative (int offset)
			{
				return view.Document.Buffer.GetCharAt (this.offset + offset);
			}
			
			public bool MoveAhead (int numChars)
			{
				bool result = offset + numChars < view.Document.Buffer.Length && offset + numChars >= 0;
				offset = Math.Max (Math.Min (offset + numChars, view.Document.Buffer.Length - 1), 0);
				return result;
			}
			public void MoveToEnd ()
			{
				offset = view.Document.Buffer.Length - 1;
			}
			public string ReadToEnd ()
			{
				return view.Document.Buffer.GetTextAt (offset, view.Document.Buffer.Length - offset);
			}
					
			public void Reset()
			{
				offset = this.initialOffset;
			}
			public void Replace (int length, string pattern)
			{
				view.Document.Buffer.Replace (offset, length, new StringBuilder (pattern));
			}
			public void Close ()
			{
				// nothing
			}
		
			public IDocumentInformation DocumentInformation { 
				get {
					return this.view;
				}
			}
			
			public bool SupportsSearch (SearchOptions options, bool reverse)
			{
				return false;
			}
			public bool SearchNext (string text, SearchOptions options, bool reverse)
			{
				return false;
			}
		}
#endregion

#region ICodeStyleOperations
		public void ToggleCodeComment ()
		{
			bool comment = false;
			string commentTag = Services.Languages.GetBindingPerFileName (this.ContentName).CommentTag ?? "//";
			foreach (LineSegment line in this.TextEditorData.SelectedLines) {
				string text = Document.Buffer.GetTextAt (line);
				string trimmedText = text.TrimStart ();
				int length = 0;
				if (!trimmedText.StartsWith (commentTag)) {
					comment = true;
					break;
				}
			}
			if (comment) {
				CommentCode ();
			} else {
				UncommentCode ();
			}
		}
		
		public void CommentCode ()
		{
			int startLineNr = this.TextEditorData.IsSomethingSelected ? this.TextEditorData.Document.Splitter.GetLineNumberForOffset (this.TextEditorData.SelectionRange.Offset) : this.TextEditorData.Caret.Line;
			int endLineNr   = this.TextEditorData.IsSomethingSelected ? this.TextEditorData.Document.Splitter.GetLineNumberForOffset (this.TextEditorData.SelectionRange.EndOffset) : this.TextEditorData.Caret.Line;
			if (endLineNr < 0)
				endLineNr = this.TextEditorData.Document.Splitter.LineCount;
			
			StringBuilder commentTag = new StringBuilder(Services.Languages.GetBindingPerFileName (this.ContentName).CommentTag ?? "//");
			Document.BeginAtomicUndo ();
			foreach (LineSegment line in this.TextEditorData.SelectedLines) {
				this.Document.Buffer.Insert (line.Offset, commentTag);
			}
			if (this.TextEditorData.IsSomethingSelected)
				this.TextEditorData.SelectionStart.Column += commentTag.Length;
			if (!this.TextEditorData.IsSomethingSelected || this.TextEditorData.SelectionEnd.Column != 0) {
				if (this.TextEditorData.IsSomethingSelected)
					this.TextEditorData.SelectionEnd.Column += commentTag.Length;
				this.TextEditorData.Caret.PreserveSelection = true;
				this.TextEditorData.Caret.Column += commentTag.Length;
				this.TextEditorData.Caret.PreserveSelection = false;
			}
			Document.EndAtomicUndo ();
			this.TextEditorData.Document.RequestUpdate (new MultipleLineUpdate (startLineNr, endLineNr));
			this.TextEditorData.Document.CommitDocumentUpdate ();
		}
		
		public void UncommentCode ()
		{
			int startLineNr = this.TextEditorData.IsSomethingSelected ? this.TextEditorData.Document.Splitter.GetLineNumberForOffset (this.TextEditorData.SelectionRange.Offset) : this.TextEditorData.Caret.Line;
			int endLineNr   = this.TextEditorData.IsSomethingSelected ? this.TextEditorData.Document.Splitter.GetLineNumberForOffset (this.TextEditorData.SelectionRange.EndOffset) : this.TextEditorData.Caret.Line;
			if (endLineNr < 0)
				endLineNr = this.TextEditorData.Document.Splitter.LineCount;
			string commentTag = Services.Languages.GetBindingPerFileName (this.ContentName).CommentTag ?? "//";
			Document.BeginAtomicUndo ();
			int first = -1;
			int last  = 0;
			foreach (LineSegment line in this.TextEditorData.SelectedLines) {
				string text = Document.Buffer.GetTextAt (line);
				string trimmedText = text.TrimStart ();
				int length = 0;
				if (trimmedText.StartsWith (commentTag)) {
					this.Document.Buffer.Remove (line.Offset + (text.Length - trimmedText.Length), commentTag.Length);
					length = commentTag.Length;
				}
				last = length;
				if (first < 0)
					first = last;
			}
			if (this.TextEditorData.IsSomethingSelected)
				this.TextEditorData.SelectionStart.Column = System.Math.Max (0, this.TextEditorData.SelectionStart.Column - first);
			if (!this.TextEditorData.IsSomethingSelected || this.TextEditorData.SelectionEnd.Column != 0) {
				if (this.TextEditorData.IsSomethingSelected)
					this.TextEditorData.SelectionEnd.Column = System.Math.Max (0, this.TextEditorData.SelectionEnd.Column - last);
				this.TextEditorData.Caret.PreserveSelection = true;
				this.TextEditorData.Caret.Column = System.Math.Max (0, this.TextEditorData.Caret.Column - last);
				this.TextEditorData.Caret.PreserveSelection = false;
			}
			Document.EndAtomicUndo ();
			this.TextEditorData.Document.RequestUpdate (new MultipleLineUpdate (startLineNr, endLineNr));
			this.TextEditorData.Document.CommitDocumentUpdate ();
		}
		
		public void IndentSelection ()
		{
			InsertTab.IndentSelection (this.TextEditorData);
		}
		
		public void UnIndentSelection ()
		{
			RemoveTab.RemoveIndentSelection (this.TextEditorData);
		}
#endregion

#region ISplittable
		public bool EnableSplitHorizontally {
			get {
				return !EnableUnsplit;
			}
		}
		public bool EnableSplitVertically {
			get {
				return !EnableUnsplit;
			}
		}
		public bool EnableUnsplit {
			get {
				return widget.IsSplitted;
			}
		}
		
		public void SplitHorizontally ()
		{
			widget.Split (false);
		}
		
		public void SplitVertically ()
		{
			widget.Split (true);
		}
		
		public void Unsplit ()
		{
			widget.Unsplit ();
		}
		
#endregion
		
		#region IFoldable
		void ToggleFoldings (ICollection<FoldSegment> segments)
		{
			bool doFold = true;
			foreach (FoldSegment segment in segments) {
				if (segment.IsFolded) {
					doFold = false;
					break;
				}
			}
			foreach (FoldSegment segment in segments) {
				segment.IsFolded = doFold;
			}
			Document.RequestUpdate (new UpdateAll ());
			Document.CommitDocumentUpdate ();
		}
		
		public void ToggleAllFoldings ()
		{
			ToggleFoldings (Document.FoldSegments);
		}
		
		public void FoldDefinitions ()
		{
			foreach (FoldSegment segment in Document.FoldSegments) {
				segment.IsFolded = segment.FoldingType == FoldingType.TypeMember;
			}
			Document.RequestUpdate (new UpdateAll ());
			Document.CommitDocumentUpdate ();
		}
		
		public void ToggleFolding ()
		{
			ToggleFoldings (Document.GetStartFoldings (Document.GetLine (TextEditorData.Caret.Line)));
		}
		#endregion
		
		#region IPrintable
		PrintDialog    printDialog;
		Gnome.PrintJob printJob;
		
		public void PrintDocument ()
		{
			if (printDialog != null) 
				return;
			CreatePrintJob ();
			
			printDialog = new PrintDialog (printJob, GettextCatalog.GetString ("Print Source Code"));
			printDialog.SkipTaskbarHint = true;
			printDialog.Modal = true;
//			printDialog.IconName = "gtk-print";
			printDialog.SetPosition (WindowPosition.CenterOnParent);
			printDialog.Gravity = Gdk.Gravity.Center;
			printDialog.TypeHint = Gdk.WindowTypeHint.Dialog;
			printDialog.TransientFor = IdeApp.Workbench.RootWindow;
			printDialog.KeepAbove = false;
			printDialog.Response += OnPrintDialogResponse;
			printDialog.Close += delegate {
				printDialog = null;
			};
			printDialog.Run ();
		}
		
		public void PrintPreviewDocument ()
		{
			CreatePrintJob ();
			PrintJobPreview preview = new PrintJobPreview (printJob, GettextCatalog.GetString ("Print Preview - Source Code"));
			preview.Modal = true;
			preview.SetPosition (WindowPosition.CenterOnParent);
			preview.Gravity = Gdk.Gravity.Center;
			preview.TransientFor = printDialog != null ? printDialog : IdeApp.Workbench.RootWindow;
//			preview.IconName = "gtk-print-preview";
			preview.ShowAll ();
		}
		
		void OnPrintDialogResponse (object sender, Gtk.ResponseArgs args)
		{
			switch ((int)args.ResponseId) {
			case (int)PrintButtons.Print:
				int result = printJob.Print ();
				if (result != 0)
					IdeApp.Services.MessageService.ShowError (GettextCatalog.GetString ("Print operation failed."));
				goto default;
			case (int)PrintButtons.Preview:
				PrintPreviewDocument ();
				break;
			default:
				printDialog.HideAll ();
				printDialog.Destroy ();
				break;
			}
		}
		
		const int marginTop    = 50;
		const int marginBottom = 50;
		const int marginLeft   = 30;
		const int marginRight  = 30;
		
		int yPos = 0;
		int xPos = 0;
		int page = 0;
		int totalPages = 0;
		
		double pageWidth, pageHeight;
		
		void PrintHeader (Gnome.PrintContext gpc, Gnome.PrintConfig config)
		{
			gpc.SetRgbColor (0, 0, 0);
			string header = GettextCatalog.GetString ("File:") +  " " + StrMiddleTruncate (IdeApp.Workbench.ActiveDocument.FileName, 60);
			yPos = marginTop;
			gpc.MoveTo (xPos, pageHeight - yPos);
			gpc.Show (header);
			xPos = marginLeft;
			gpc.RectFilled (marginLeft, pageHeight - (marginTop + 5), pageWidth - marginRight - marginLeft, 2);
			yPos += widget.TextEditor.LineHeight;
		}
		
		void PrintFooter (Gnome.PrintContext gpc, Gnome.PrintConfig config)
		{
			gpc.SetRgbColor (0, 0, 0);
			gpc.MoveTo (xPos, marginBottom);
			gpc.Show ("MonoDevelop");
			gpc.MoveTo (xPos + 200, marginBottom);
			string footer = GettextCatalog.GetString ("Page") + " " + page + "/" + (totalPages + 1);
			gpc.Show (footer);
			gpc.RectFilled (marginLeft, marginBottom - 3 + widget.TextEditor.LineHeight, pageWidth - marginRight - marginLeft, 2);
		}
		
		void MyPrint (Gnome.PrintContext gpc, Gnome.PrintConfig config)
		{
			config.GetPageSize (out pageWidth, out pageHeight);
			int linesPerPage = (int)((pageHeight - marginBottom - marginTop - 10) / widget.TextEditor.LineHeight);
			linesPerPage -= 2;
			totalPages = Document.Splitter.LineCount / linesPerPage;
			System.Console.WriteLine("height:" + pageHeight + " -- lines:" + linesPerPage);
			gpc.BeginPage ("page " + page++);
			xPos = marginLeft;
			PrintHeader (gpc, config);
			Gnome.Font font       =  Gnome.Font.FindFromFullName (SourceEditorOptions.Options.FontName);
			Gnome.Font boldFont   =  Gnome.Font.FindFromFullName (font.FontName + " Bold " + ((int)font.Size));
			Gnome.Font italicFont =  Gnome.Font.FindFromFullName (font.FontName + " Italic " + ((int)font.Size));
			
			foreach (LineSegment line in Document.Splitter.Lines) {
				if (yPos >= pageHeight - marginBottom - 5 - widget.TextEditor.LineHeight) {
					gpc.SetFont (font);
					yPos = marginTop;
					PrintFooter (gpc, config);
					gpc.ShowPage ();
					gpc.BeginPage ("page " + page++);
					PrintHeader (gpc, config);
				}
				Chunk[] chunks = Document.SyntaxMode.GetChunks (Document, TextEditorData.ColorStyle, line, line.Offset, line.Length);
				foreach (Chunk chunk in chunks) {
					string text = Document.Buffer.GetTextAt (chunk);
					text = text.Replace ("\t", new string (' ', TextEditorOptions.Options.TabSize));
					gpc.SetRgbColor (chunk.Style.Color.Red / (double)ushort.MaxValue, 
					                 chunk.Style.Color.Green / (double)ushort.MaxValue, 
					                 chunk.Style.Color.Blue / (double)ushort.MaxValue);
					
					gpc.MoveTo (xPos, pageHeight - yPos);
					if (chunk.Style.Bold) {
						gpc.SetFont (boldFont);
					} else if (chunk.Style.Italic) {
						gpc.SetFont (italicFont);
					} else {
						gpc.SetFont (font);
					}
					gpc.Show (text);
					xPos += widget.TextEditor.GetWidth (text);
				}
				xPos = marginLeft;
				yPos += widget.TextEditor.LineHeight;
			}
			
			gpc.SetFont (font);
			PrintFooter (gpc, config);
			gpc.ShowPage ();
			gpc.EndDoc ();
		}
		
		void CreatePrintJob ()
		{
			if (printDialog != null  || printJob != null)
				return;/*
			PrintConfig config = ;
			PrintJob sourcePrintJob = new SourcePrintJob (config, Buffer);
			sourcePrintJob.upFromView = View;
			sourcePrintJob.PrintHeader = true;
			sourcePrintJob.PrintFooter = true;
			sourcePrintJob.SetHeaderFormat (GettextCatalog.GetString ("File:") +  " " +
									  StrMiddleTruncate (IdeApp.Workbench.ActiveDocument.FileName, 60), null, null, true);
			sourcePrintJob.SetFooterFormat (GettextCatalog.GetString ("MonoDevelop"), null, GettextCatalog.GetString ("Page") + " %N/%Q", true);
			sourcePrintJob.WrapMode = WrapMode.Word; */
			printJob = new Gnome.PrintJob (Gnome.PrintConfig.Default ());
			Gnome.PrintContext ctx = printJob.Context;
			MyPrint (ctx, printJob.Config); 
			printJob.Close ();
		}
		
		static string StrMiddleTruncate (string str, int truncLen)
		{
			if (str == null) 
				return "";
			if (str.Length <= truncLen) 
				return str;
			
			string delimiter = "...";
			int leftOffset = (truncLen - delimiter.Length) / 2;
			int rightOffset = str.Length - truncLen + leftOffset + delimiter.Length;
			return str.Substring (0, leftOffset) + delimiter + str.Substring (rightOffset);
		}
		#endregion
	}
} 