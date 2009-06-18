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
using System.Threading;

using Gtk;
#if GNOME_PRINT
using Gnome;
#endif

using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Debugger;
using Mono.Debugging.Client;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.CodeTemplates;
using Services = MonoDevelop.Projects.Services;

namespace MonoDevelop.SourceEditor
{	
	public class SourceEditorView : AbstractViewContent, IExtensibleTextEditor, IBookmarkBuffer, IClipboardHandler, 
		ICompletionWidget,  ISplittable, IFoldable, IToolboxDynamicProvider, IEncodedTextContent,
		ICustomFilteringToolboxConsumer, IZoomable, ITextEditorResolver, Mono.TextEditor.ITextEditorDataProvider,
		ICodeTemplateWidget, ITemplateWidget
#if GNOME_PRINT
		, IPrintable
#endif
	{
		SourceEditorWidget widget;
		bool isDisposed = false;
		FileSystemWatcher fileSystemWatcher;
		static bool isInWrite = false;
		DateTime lastSaveTime;
		object attributes; // Contains platform specific file attributes
		string loadedMimeType;
		
		TextMarker currentDebugLineMarker;
		TextMarker breakpointMarker;
		TextMarker breakpointDisabledMarker;
		TextMarker breakpointInvalidMarker;
		
		int lastDebugLine = -1;
		EventHandler executionLocationChanged;
		EventHandler<BreakpointEventArgs> breakpointAdded;
		EventHandler<BreakpointEventArgs> breakpointRemoved;
		EventHandler<BreakpointEventArgs> breakpointStatusChanged;
		
		List<LineSegment> breakpointSegments = new List<LineSegment> ();
		LineSegment currentLineSegment;
		
		bool writeAllowed;
		bool writeAccessChecked;
		
		public Mono.TextEditor.Document Document {
			get {
				return widget.TextEditor.Document;
			}
		}
		
		public ExtensibleTextEditor TextEditor {
			get {
				return widget.TextEditor;
			}
		}
		
		internal SourceEditorWidget SourceEditorWidget {
			get {
				return widget;
			}
		}
		
		public override Gtk.Widget Control {
			get {
				return widget;
			}
		}
		
		public int LineCount {
			get {
				return Document.LineCount;
			}
		}
		
		public override Project Project {
			get {
				return base.Project;
			}
			set {
				if (value != base.Project)
					((StyledSourceEditorOptions)SourceEditorWidget.TextEditor.Options).UpdateStyleParent (value, loadedMimeType);
				base.Project = value;
			}
		}
			
		public override string TabPageLabel {
			get { return GettextCatalog.GetString ("Source"); }
		}
		
		public SourceEditorView()
		{
			executionLocationChanged = (EventHandler) MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new EventHandler (OnExecutionLocationChanged));
			breakpointAdded = (EventHandler<BreakpointEventArgs>) MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointAdded));
			breakpointRemoved = (EventHandler<BreakpointEventArgs>) MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointRemoved));
			breakpointStatusChanged = (EventHandler<BreakpointEventArgs>) MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointStatusChanged));
			
			widget = new SourceEditorWidget (this);
			widget.TextEditor.Document.TextReplaced += delegate (object sender, ReplaceEventArgs args) {
				int startIndex = args.Offset;
				int endIndex   = startIndex + Math.Max (args.Count, args.Value != null ? args.Value.Length : 0);
				if (TextChanged != null)
					TextChanged (this, new TextChangedEventArgs (startIndex, endIndex));
				if (!inLoad)
					autoSave.InformAutoSaveThread (Document.Text);
			};
			
			widget.TextEditor.Document.TextReplacing += OnTextReplacing;
			widget.TextEditor.Document.TextReplaced += OnTextReplaced;
			widget.TextEditor.Document.ReadOnlyCheckDelegate = CheckReadOnly;
			
//			widget.TextEditor.Document.DocumentUpdated += delegate {
//				this.IsDirty = Document.IsDirty;
//			};
			
			widget.TextEditor.Caret.PositionChanged += delegate {
				FireCompletionContextChanged ();
			};
			
			widget.TextEditor.IconMargin.ButtonPressed += OnIconButtonPress;
			
			widget.ShowAll ();
			
			currentDebugLineMarker   = new CurrentDebugLineTextMarker (widget.TextEditor);
			breakpointMarker         = new BreakpointTextMarker (widget.TextEditor);
			breakpointDisabledMarker = new DisabledBreakpointTextMarker (widget.TextEditor);
			breakpointInvalidMarker  = new InvalidBreakpointTextMarker (widget.TextEditor);
			
			
			fileSystemWatcher = new FileSystemWatcher ();
			fileSystemWatcher.Created += (FileSystemEventHandler)MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new FileSystemEventHandler (OnFileChanged));	
			fileSystemWatcher.Changed += (FileSystemEventHandler)MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new FileSystemEventHandler (OnFileChanged));
			
			this.ContentNameChanged += delegate {
				this.Document.FileName   = this.ContentName;
				isInWrite = true;
				if (String.IsNullOrEmpty (ContentName) || !File.Exists (ContentName))
					return;
				
				fileSystemWatcher.EnableRaisingEvents = false;
				lastSaveTime = File.GetLastWriteTime (ContentName);
				fileSystemWatcher.Path = Path.GetDirectoryName (ContentName);
				fileSystemWatcher.Filter = Path.GetFileName (ContentName);
				isInWrite = false;
				fileSystemWatcher.EnableRaisingEvents = true;
			};
			ClipbardRingUpdated += UpdateClipboardRing;
			
			DebuggingService.ExecutionLocationChanged += executionLocationChanged;
			DebuggingService.Breakpoints.BreakpointAdded += breakpointAdded;
			DebuggingService.Breakpoints.BreakpointRemoved += breakpointRemoved;
			DebuggingService.Breakpoints.BreakpointStatusChanged += breakpointStatusChanged;
		}
		AutoSave autoSave = new AutoSave ();
		
		internal AutoSave AutoSave {
			get {
				return autoSave;
			}
		}
		
		public override void Save (string fileName)
		{
			Save (fileName, this.encoding);
		}
		
		public void Save (string fileName, string encoding)
		{
			autoSave.FileName = fileName;
			autoSave.RemoveAutoSaveFile ();
		
			if (ContentName != fileName) {
				if (!FileService.RequestFileEdit (fileName))
					return;
				writeAllowed = true;
				writeAccessChecked = true;
			}
			
			if (warnOverwrite) {
				if (fileName == ContentName) {
					if (MonoDevelop.Core.Gui.MessageService.AskQuestion (GettextCatalog.GetString ("This file {0} has been changed outside of MonoDevelop. Are you sure you want to overwrite the file?", fileName), MonoDevelop.Core.Gui.AlertButton.Cancel, MonoDevelop.Core.Gui.AlertButton.OverwriteFile) != MonoDevelop.Core.Gui.AlertButton.OverwriteFile)
						return;
				}
				warnOverwrite = false;
				widget.RemoveMessageBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			
			isInWrite = true;
			try {
				TextFile.WriteFile (fileName, Document.Text, encoding);
				lastSaveTime = File.GetLastWriteTime (fileName);
				IdeApp.Services.PlatformService.SetFileAttributes (fileName, attributes);
			} finally {
				isInWrite = false;
			}
				
//			if (encoding != null)
//				se.Buffer.SourceEncoding = encoding;
//			TextFileService.FireCommitCountChanges (this);
			
			ContentName = fileName; 
			UpdateMimeType (fileName);
			Document.SetNotDirtyState ();
			this.IsDirty = false;
		}
		
		public override void Load (string fileName)
		{
			Load (fileName, null);
		}
		
		public void Load (string fileName, string encoding)
		{
			// Handle the "reload" case.
			if (autoSave.FileName == fileName) {
				autoSave.RemoveAutoSaveFile ();
			}
			
			attributes = IdeApp.Services.PlatformService.GetFileAttributes (fileName);
			autoSave.FileName = fileName;
			if (warnOverwrite) {
				warnOverwrite = false;
				widget.RemoveMessageBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			
			// Look for a mime type for which there is a syntax mode
			UpdateMimeType (fileName);
			
			widget.InformLoad ();
			
			if (AutoSave.AutoSaveExists (fileName)) {
				widget.ShowAutoSaveWarning (fileName);
				this.encoding = encoding;
			} else {
				TextFile file = TextFile.ReadFile (fileName, encoding);
				inLoad = true;
				Document.Text = file.Text;
				inLoad = false;
				this.encoding = file.SourceEncoding;
			}
			ContentName = fileName;
	//			widget.ParsedDocument = ProjectDomService.GetParsedDocument (fileName);
	//			InitializeFormatter ();
		
			UpdateExecutionLocation ();
			UpdateBreakpoints ();

			widget.PopulateClassCombo ();
			this.IsDirty = false;
		}

		bool warnOverwrite = false;
		bool inLoad = false;
		string encoding = null;
		public void Load (string fileName, string content, string encoding)
		{
			autoSave.FileName = fileName;
			if (warnOverwrite) {
				warnOverwrite = false;
				widget.RemoveMessageBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			UpdateMimeType (fileName);
			widget.InformLoad ();
			inLoad = true;
			Document.Text = content;
			inLoad = false;
			this.encoding = encoding;
			ContentName = fileName;

			UpdateExecutionLocation ();
			UpdateBreakpoints ();

			widget.PopulateClassCombo ();
			this.IsDirty = false;
		}
		
		void UpdateMimeType (string fileName)
		{
			// Look for a mime type for which there is a syntax mode
			string mimeType = IdeApp.Services.PlatformService.GetMimeTypeForUri (fileName);
			if (loadedMimeType != mimeType) {
				loadedMimeType = mimeType;
				if (mimeType != null) {
					foreach (string mt in IdeApp.Services.PlatformService.GetMimeTypeInheritanceChain (loadedMimeType)) {
						if (Mono.TextEditor.Highlighting.SyntaxModeService.GetSyntaxMode (mt) != null) {
							Document.MimeType = mt;
							break;
						}
					}
				}
				((StyledSourceEditorOptions)SourceEditorWidget.TextEditor.Options).UpdateStyleParent (Project, loadedMimeType);
			}
		}
		
		public string SourceEncoding {
			get { return encoding; }
		}
		
		public override void Dispose()
		{
			this.isDisposed= true;
			if (autoSave != null) {
				autoSave.RemoveAutoSaveFile ();
				autoSave.Dispose ();
				autoSave = null;
			}
			
			ClipbardRingUpdated -= UpdateClipboardRing;
			if (fileSystemWatcher != null) {
				fileSystemWatcher.EnableRaisingEvents = false;
				fileSystemWatcher.Dispose ();
				fileSystemWatcher = null;
			}
			
			if (widget != null) {
				widget.TextEditor.Document.TextReplacing -= OnTextReplacing;
				widget.TextEditor.Document.TextReplacing -= OnTextReplaced;
				widget.TextEditor.Document.ReadOnlyCheckDelegate = null;
				
				widget.Destroy ();
				widget = null;
			}
			
			DebuggingService.ExecutionLocationChanged -= executionLocationChanged;
			DebuggingService.Breakpoints.BreakpointAdded -= breakpointAdded;
			DebuggingService.Breakpoints.BreakpointRemoved -= breakpointRemoved;
			DebuggingService.Breakpoints.BreakpointStatusChanged -= breakpointStatusChanged;
			
			// This is not necessary but helps when tracking down memory leaks
			
			currentDebugLineMarker = null;
			breakpointMarker = null;
			breakpointDisabledMarker = null;
			breakpointInvalidMarker = null;
			
			executionLocationChanged = null;
			breakpointAdded = null;
			breakpointRemoved = null;
			breakpointStatusChanged = null;
		}
		
		public ProjectDom GetParserContext ()
		{
			//Project project = IdeApp.ProjectOperations.CurrentSelectedProject;
			if (Project != null)
				return ProjectDomService.GetProjectDom (Project);
			return ProjectDom.Empty;
		}
		
		public Ambience GetAmbience ()
		{
			Project project = Project;
			if (project != null)
				return project.Ambience;
			string file = this.IsUntitled ? this.UntitledName : this.ContentName;
			return AmbienceService.GetAmbienceForFile (file);
		}
		
		void OnFileChanged (object sender, FileSystemEventArgs args)
		{
			if (!isInWrite && args.FullPath != ContentName)
				return;
			if (lastSaveTime == File.GetLastWriteTime (ContentName))
				return;
			
			if (args.ChangeType == WatcherChangeTypes.Changed || args.ChangeType == WatcherChangeTypes.Created) 
				widget.ShowFileChangedWarning ();
		}
		
		bool CheckReadOnly (int line)
		{
			if (!writeAccessChecked && !IsUntitled) {
				writeAccessChecked = true;
				writeAllowed = FileService.RequestFileEdit (ContentName);
			}
			return IsUntitled || writeAllowed;
		}
		
		string oldReplaceText;
		
		void OnTextReplacing (object s, ReplaceEventArgs a)
		{
			if (a.Count > 0)  {
				oldReplaceText = widget.TextEditor.Document.GetTextAt (a.Offset, a.Count);
			} else {
				oldReplaceText = "";
			}
		}
		
		void OnTextReplaced (object s, ReplaceEventArgs a)
		{
			this.IsDirty = Document.IsDirty;
			
			FireCompletionContextChanged ();
			
			DocumentLocation location = Document.OffsetToLocation (a.Offset);
			
			int i=0, lines=0;
			while (i != -1 && i < oldReplaceText.Length) {
				i = oldReplaceText.IndexOf ('\n', i);
				if (i != -1) {
					lines--;
					i++;
				}
			}

			if (a.Value != null) {
				i=0;
				string sb = a.Value;
				while (i < sb.Length) {
					if (sb [i] == '\n')
						lines++;
					i++;
				}
			}
			if (lines != 0)
				TextFileService.FireLineCountChanged (this, location.Line + 1, lines, location.Column + 1);
		}

		void OnExecutionLocationChanged (object s, EventArgs args)
		{
			UpdateExecutionLocation ();
		}
		
		void UpdateExecutionLocation ()
		{
			if (DebuggingService.IsDebugging && 
			    !DebuggingService.IsRunning &&
				DebuggingService.CurrentFilename != FilePath.Null &&
			    DebuggingService.CurrentFilename.FullPath == Path.GetFullPath (ContentName)
		    ) {
				if (lastDebugLine == DebuggingService.CurrentLineNumber)
					return;
				if (currentLineSegment != null)
					widget.TextEditor.Document.RemoveMarker (currentLineSegment, currentDebugLineMarker);
				lastDebugLine = DebuggingService.CurrentLineNumber;
				currentLineSegment = widget.TextEditor.Document.GetLine (lastDebugLine-1);
				widget.TextEditor.Document.AddMarker (currentLineSegment, currentDebugLineMarker);
				widget.TextEditor.QueueDraw ();
			} else if (currentLineSegment != null) {
				widget.TextEditor.Document.RemoveMarker (currentLineSegment, currentDebugLineMarker);
				lastDebugLine = -1;
				currentLineSegment = null;
				widget.TextEditor.QueueDraw ();
			}
		}
		
		void UpdateBreakpoints ()
		{
			foreach (LineSegment line in breakpointSegments) {
				widget.TextEditor.Document.RemoveMarker (line, breakpointMarker);
				widget.TextEditor.Document.RemoveMarker (line, breakpointDisabledMarker);
				widget.TextEditor.Document.RemoveMarker (line, breakpointInvalidMarker);
			}
			breakpointSegments.Clear ();
			foreach (Breakpoint bp in DebuggingService.Breakpoints.GetBreakpoints ())
				AddBreakpoint (bp);
			widget.TextEditor.QueueDraw ();
			
			// Ensure the current line marker is drawn at the top
			lastDebugLine = -1;
			UpdateExecutionLocation ();
		}
		
		void AddBreakpoint (Breakpoint bp)
		{
			FilePath fp = ContentName;
			if (fp.FullPath == bp.FileName) {
				LineSegment line = widget.TextEditor.Document.GetLine (bp.Line-1);
				if (line == null)
					return;
				if (!bp.Enabled)
					widget.TextEditor.Document.AddMarker (line, breakpointDisabledMarker);
				else if (bp.IsValid (DebuggingService.DebuggerSession))
					widget.TextEditor.Document.AddMarker (line, breakpointMarker);
				else
					widget.TextEditor.Document.AddMarker (line, breakpointInvalidMarker);
				widget.TextEditor.QueueDraw ();
				breakpointSegments.Add (line);
			}
		}
		
		void OnBreakpointAdded (object s, BreakpointEventArgs args)
		{
			if (ContentName == null || args.Breakpoint.FileName != Path.GetFullPath (ContentName))
				return;
			// Updated with a delay, to make sure it works when called as a
			// result of inserting/removing lines before a breakpoint position
			GLib.Timeout.Add (10, delegate {
				UpdateBreakpoints ();
				return false;
			});
		}
		
		void OnBreakpointRemoved (object s, BreakpointEventArgs args)
		{
			if (args.Breakpoint.FileName != Path.GetFullPath (ContentName))
				return;
			// Updated with a delay, to make sure it works when called as a
			// result of inserting/removing lines before a breakpoint position
			GLib.Timeout.Add (10, delegate {
				UpdateBreakpoints ();
				return false;
			});
		}
		
		void OnBreakpointStatusChanged (object s, BreakpointEventArgs args)
		{
			if (args.Breakpoint.FileName != Path.GetFullPath (ContentName))
				return;
			// Updated with a delay, to make sure it works when called as a
			// result of inserting/removing lines before a breakpoint position
			GLib.Timeout.Add (10, delegate {
				UpdateBreakpoints ();
				return false;
			});
		}
		
		void OnIconButtonPress (object s, MarginMouseEventArgs args)
		{
			if (args.Button == 3) {
				TextEditor.Caret.Line = args.LineNumber;
				TextEditor.Caret.Column = 1;
				IdeApp.CommandService.ShowContextMenu ("/MonoDevelop/SourceEditor2/IconContextMenu/Editor");
			}
			else if (args.Button == 1) {
				if (!string.IsNullOrEmpty (this.Document.FileName))
					DebuggingService.Breakpoints.Toggle (this.Document.FileName, args.LineNumber + 1);
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
		public bool EnableUndo {
			get {
				return this.Document.CanUndo && widget.EditorHasFocus;
			}
		}
		
		public void Undo()
		{
			this.Document.Undo ();
		}
		
		public bool EnableRedo {
			get {
				return this.Document.CanRedo && widget.EditorHasFocus;
			}
		}
		
		public void SetCaretTo (int line, int column)
		{
			GLib.Timeout.Add (20,  delegate {
				if (this.isDisposed)
					return false;
				line = Math.Min (line, Document.LineCount);
				
				widget.TextEditor.Caret.Location = new DocumentLocation (line - 1, column - 1);
				
				widget.TextEditor.GrabFocus ();
				widget.TextEditor.CenterToCaret ();
				OnCaretPositionSet (EventArgs.Empty);
				return false;
			});
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
				return TextEditor.IsSomethingSelected ? Document.GetTextAt (TextEditor.SelectionRange) : "";
			}
			set {
				TextEditor.DeleteSelectedText ();
				int length = TextEditor.Insert (TextEditor.Caret.Offset, value);
				TextEditor.SelectionRange = new Segment (TextEditor.Caret.Offset, length);
				TextEditor.Caret.Offset += length; 
			}
		}
		protected virtual void OnCaretPositionSet (EventArgs args)
		{
			if (CaretPositionSet != null) 
				CaretPositionSet (this, args);
		}
		public event EventHandler CaretPositionSet;
		public event EventHandler<TextChangedEventArgs> TextChanged;
		#endregion
		
		#region ITextBuffer
		public int CursorPosition { 
			get {
				return TextEditor.Caret.Offset;
			}
			set {
				TextEditor.Caret.Offset = value;
			}
		}

		public int SelectionStartPosition { 
			get {
				if (!TextEditor.IsSomethingSelected)
					return TextEditor.Caret.Offset;
				return TextEditor.SelectionRange.Offset;
			}
		}
		public int SelectionEndPosition { 
			get {
				if (!TextEditor.IsSomethingSelected)
					return TextEditor.Caret.Offset;
				return TextEditor.SelectionRange.EndOffset;
			}
		}
		
		public void Select (int startPosition, int endPosition)
		{
			TextEditor.SelectionRange = new Segment (startPosition, endPosition - startPosition);
			TextEditor.ScrollToCaret ();
		}
		
		public void ShowPosition (int position)
		{
			// TODO
		}
		#endregion
		
		#region ITextFile
		public FilePath Name {
			get { 
				return this.ContentName ?? this.UntitledName; 
			} 
		}

		public string Text {
			get {
				return this.widget.TextEditor.Document.Text;
			}
			set {
				this.IsDirty = true;
				this.widget.TextEditor.Document.Text = value;
				if (TextChanged != null)
					TextChanged (this, new TextChangedEventArgs (0, Length));
			}
		}
		
		public int Length { 
			get {
				return this.widget.TextEditor.Document.Length;
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
			return this.widget.TextEditor.Document.GetTextAt (startPosition, endPosition - startPosition);
		}
		
		public char GetCharAt (int position)
		{
			return this.widget.TextEditor.Document.GetCharAt (position);
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
		public int InsertText (int position, string text)
		{
			int length = this.widget.TextEditor.Insert (position, text);
			if (text != null && this.widget.TextEditor.Caret.Offset >= position) 
				this.widget.TextEditor.Caret.Offset += length;
			return length;
		}
		public void DeleteText (int position, int length)
		{
			this.widget.TextEditor.Remove (position, length);
			if (this.widget.TextEditor.Caret.Offset >= position) 
				this.widget.TextEditor.Caret.Offset -= length;
		}
		#endregion 
		
		#region IBookmarkBuffer
		LineSegment GetLine (int position)
		{
			DocumentLocation location = Document.OffsetToLocation (position);
			return Document.GetLine (location.Line);
		}
				
		public void SetBookmarked (int position, bool mark)
		{
			LineSegment line = GetLine (position);
			if (line != null && line.IsBookmarked != mark) {
				int lineNumber = widget.TextEditor.Document.OffsetToLineNumber (line.Offset);
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
			TextEditor.RunAction (BookmarkActions.GotoPrevious);
		}
		
		public void NextBookmark ()
		{
			TextEditor.RunAction (BookmarkActions.GotoNext);
		}
		public void ClearBookmarks ()
		{
			TextEditor.RunAction (BookmarkActions.ClearAll);
		}
		#endregion
		
		#region IClipboardHandler
		public bool EnableCut {
			get {
				return widget.EditorHasFocus;
			}
		}
		public bool EnableCopy {
			get {
				return EnableCut;
			}
		}
		public bool EnablePaste {
			get {
				return widget.EditorHasFocus;
			}
		}
		public bool EnableDelete {
			get {
				return widget.EditorHasFocus;
			}
		}
		public bool EnableSelectAll {
			get {
				return widget.EditorHasFocus;
			}
		}
		
		public void Cut ()
		{
			TextEditor.RunAction (ClipboardActions.Cut);
		}
		
		public void Copy ()
		{
			TextEditor.RunAction (ClipboardActions.Copy);
		}
		
		public void Paste ()
		{
			TextEditor.RunAction (ClipboardActions.Paste);
		}
		
		public void Delete ()
		{
			if (TextEditor.IsSomethingSelected) {
				TextEditor.DeleteSelectedText ();
			} else {
				TextEditor.RunAction (DeleteActions.CaretLine);
			}
		}
		
		public void SelectAll ()
		{
			TextEditor.RunAction (SelectionActions.SelectAll);
		}
		#endregion
		
		#region ICompletionWidget
		public int TextLength {
			get {
				return Document.Length;
			}
		}
		public int SelectedLength { 
			get {
				return TextEditor.IsSomethingSelected ? TextEditor.SelectionRange.Length : 0;
			}
		}
//		public string GetText (int startOffset, int endOffset)
//		{
//			return this.widget.TextEditor.Document.Buffer.GetTextAt (startOffset, endOffset - startOffset);
//		}
		public char GetChar (int offset)
		{
			return Document.GetCharAt (offset);
		}
		
		public Gtk.Style GtkStyle { 
			get {
				return widget.Style.Copy ();
			}
		}

		public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset) 
		{
			CodeCompletionContext result = new CodeCompletionContext ();
			result.TriggerOffset = triggerOffset;
			DocumentLocation loc = Document.OffsetToLocation (triggerOffset);
			result.TriggerLine   = loc.Line + 1;
			result.TriggerLineOffset = loc.Column + 1;
			Gdk.Point p = this.widget.TextEditor.DocumentToVisualLocation (loc);
			int tx, ty;
			
			widget.ParentWindow.GetOrigin (out tx, out ty);
			tx += TextEditor.Allocation.X;
			ty += TextEditor.Allocation.Y;
			result.TriggerXCoord = tx + p.X + TextEditor.TextViewMargin.XOffset - (int)TextEditor.HAdjustment.Value;
			result.TriggerYCoord = ty + p.Y - (int)TextEditor.VAdjustment.Value + TextEditor.LineHeight;
			result.TriggerTextHeight = TextEditor.LineHeight;
			return result;
		}
		
		public CodeTemplateContext GetCodeTemplateContext ()
		{
			return TextEditor.GetTemplateContext ();
		}
		
		public string GetCompletionText (ICodeCompletionContext ctx)
		{
			if (ctx == null)
				return null;
			int min = Math.Min (ctx.TriggerOffset, TextEditor.Caret.Offset);
			int max = Math.Max (ctx.TriggerOffset, TextEditor.Caret.Offset);
			return Document.GetTextBetween (min, max);
		}
		
		public void SetCompletionText (ICodeCompletionContext ctx, string partial_word, string complete_word)
		{
			int triggerOffset = ctx.TriggerOffset;
			if (TextEditor.IsSomethingSelected) {
				if (TextEditor.SelectionRange.Offset < ctx.TriggerOffset)
					triggerOffset = ctx.TriggerOffset - TextEditor.SelectionRange.Length;
				TextEditor.DeleteSelectedText ();
			}

			int idx = complete_word.IndexOf ('|'); // | in the completion text now marks the caret position
			if (idx >= 0) {
				complete_word = complete_word.Remove (idx, 1);
			} else {
				idx = complete_word.Length;
			}
			int length = String.IsNullOrEmpty (partial_word) ? 0 : partial_word.Length;
			
			triggerOffset += TextEditor.GetTextEditorData ().EnsureCaretIsNotVirtual ();
			this.widget.TextEditor.Document.EndAtomicUndo ();
			this.widget.TextEditor.Replace (triggerOffset, length, complete_word);
			this.widget.TextEditor.Caret.Offset += complete_word.Length - length;
			this.widget.TextEditor.Document.BeginAtomicUndo ();
		}
		
		void FireCompletionContextChanged ()
		{
			if (CompletionContextChanged != null)
				CompletionContextChanged (this, EventArgs.Empty);
		}
		
		public event EventHandler CompletionContextChanged;
		#endregion
		
		#region commenting and indentation

		[CommandHandler (MonoDevelop.Debugger.DebugCommands.ExpressionEvaluator)]
		protected void ShowExpressionEvaluator ()
		{
			string expression;
			if (TextEditor.IsSomethingSelected)
				expression = TextEditor.SelectedText;
			else
				expression = TextEditor.GetExpression (TextEditor.Caret.Offset);
			
			DebuggingService.ShowExpressionEvaluator (expression);
		}

		[CommandUpdateHandler (MonoDevelop.Debugger.DebugCommands.ExpressionEvaluator)]
		protected void UpdateShowExpressionEvaluator (CommandInfo cinfo)
		{
			if (DebuggingService.IsDebugging)
				cinfo.Enabled = DebuggingService.CurrentFrame != null;
			else
				cinfo.Visible = false;
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
		
		public void SwitchWindow ()
		{
			widget.SwitchWindow ();
		}
		
		#endregion
		
		#region IFoldable
		void ToggleFoldings (IEnumerable<FoldSegment> segments)
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
			widget.TextEditor.Caret.MoveCaretBeforeFoldings ();
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
				if (segment.FoldingType == FoldingType.TypeDefinition)
					segment.IsFolded = false;
				if (segment.FoldingType == FoldingType.TypeMember)
					segment.IsFolded = true;
			}
			widget.TextEditor.Caret.MoveCaretBeforeFoldings ();
			Document.RequestUpdate (new UpdateAll ());
			Document.CommitDocumentUpdate ();
		}
		
		public void ToggleFolding ()
		{
			ToggleFoldings (Document.GetStartFoldings (Document.GetLine (TextEditor.Caret.Line)));
		}
		#endregion
		
#if GNOME_PRINT
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
					MessageService.ShowError (GettextCatalog.GetString ("Print operation failed."));
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
			totalPages = Document.LineCount / linesPerPage;
			xPos = marginLeft;
			string fontName = this.TextEditor.Options.FontName;
			Gnome.Font font =  Gnome.Font.FindClosestFromFullName (fontName);
			if (font == null) {
				LoggingService.LogError ("Can't find font: '" + fontName + "', trying default." );
				font = Gnome.Font.FindClosestFromFullName (IdeApp.Services.PlatformService.DefaultMonospaceFont);
			}
			if (font == null) {
				LoggingService.LogError ("Unable to load font." );
				MessageService.ShowError ("Unable to initialize Font, aborting.");
				return;
			}
			Gnome.Font boldFont   =  Gnome.Font.FindFromFullName (font.FontName + " Bold " + ((int)font.Size));
			Gnome.Font italicFont =  Gnome.Font.FindFromFullName (font.FontName + " Italic " + ((int)font.Size));
			
			gpc.BeginPage ("page " + page++);
			PrintHeader (gpc, config);
			foreach (LineSegment line in Document.Lines) {
				if (yPos >= pageHeight - marginBottom - 5 - widget.TextEditor.LineHeight) {
					gpc.SetFont (font);
					yPos = marginTop;
					PrintFooter (gpc, config);
					gpc.ShowPage ();
					gpc.BeginPage ("page " + page++);
					PrintHeader (gpc, config);
				}
				Chunk[] chunks = Document.SyntaxMode.GetChunks (Document, TextEditor.ColorStyle, line, line.Offset, line.Length);
				foreach (Chunk chunk in chunks) {
					string text = Document.GetTextAt (chunk);
					text = text.Replace ("\t", new string (' ', this.TextEditor.Options.TabSize));
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
					xPos += widget.TextEditor.TextViewMargin.GetWidth (text);
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
		
		
		#endregion
#endif
	
		#region Toolbox
		static List<TextToolboxNode> clipboardRing = new List<TextToolboxNode> ();
		static event EventHandler ClipbardRingUpdated;
		
		static SourceEditorView ()
		{
			ClipboardActions.CopyOperation.Copy += delegate (string text) {
				if (String.IsNullOrEmpty (text))
					return;
				foreach (TextToolboxNode node in clipboardRing) {
					if (node.Text == text) {
						clipboardRing.Remove (node);
						break;
					}
				}
				TextToolboxNode item = new TextToolboxNode (text);
				string[] lines = text.Split ('\n');
				for (int i = 0; i < 3 && i < lines.Length; i++) {
					if (i > 0)
						item.Description += Environment.NewLine;
					string line = lines[i];
					if (line.Length > 16)
						line = line.Substring (0, 16) + "...";
					item.Description += line;
				}
				item.Category = GettextCatalog.GetString ("Clipboard ring");
				item.Icon = IdeApp.Services.PlatformService.GetPixbufForFile ("test.txt", Gtk.IconSize.Menu);
				item.Name = text.Length > 16 ? text.Substring (0, 16) + "..." : text;
				item.Name = item.Name.Replace ("\t", "\\t");
				item.Name = item.Name.Replace ("\n", "\\n");
				clipboardRing.Add (item);
				while (clipboardRing.Count > 12) {
					clipboardRing.RemoveAt (0);
				}
				if (ClipbardRingUpdated != null)
					ClipbardRingUpdated (null, EventArgs.Empty);
			};
		}
		
		public void UpdateClipboardRing (object sender, EventArgs e)
		{
			if (ItemsChanged != null)
				ItemsChanged (this, EventArgs.Empty);
		}
		
		public IEnumerable<ItemToolboxNode> GetDynamicItems (IToolboxConsumer consumer)
		{
			foreach (TextToolboxNode item in clipboardRing)
				yield return item;
			//FIXME: make this work again
//			CategoryToolboxNode category = new CategoryToolboxNode (GettextCatalog.GetString ("Clipboard ring"));
//			category.IsDropTarget    = false;
//			category.CanIconizeItems = false;
//			category.IsSorted        = false;
//			foreach (TextToolboxNode item in clipboardRing) {
//				category.Add (item);
//			}
//			
//			if (clipboardRing.Count == 0) {
//				TextToolboxNode item = new TextToolboxNode (null);
//				item.Category = GettextCatalog.GetString ("Clipboard ring");
//				item.Name = null;
//				//category.Add (item);
//			}
//			return new BaseToolboxNode [] { category };
		}
		
		public event EventHandler ItemsChanged;
		
		void IToolboxConsumer.ConsumeItem (ItemToolboxNode item)
		{
			if (item is TemplateToolboxNode) {
				InsertTemplate (((TemplateToolboxNode)item).Template, new MonoDevelop.Ide.Gui.Document (base.WorkbenchWindow));
				TextEditor.GrabFocus ();
				return;
			}
			string text = GetText (item);
			if (string.IsNullOrEmpty (text))
				return;
			TextEditor.InsertAtCaret (text);
			TextEditor.GrabFocus ();
		}
		
		#region dnd
		Gtk.Widget customSource;
		ItemToolboxNode dragItem;
		void IToolboxConsumer.DragItem (ItemToolboxNode item, Gtk.Widget source, Gdk.DragContext ctx)
		{
			string text = GetText (item);
			if (string.IsNullOrEmpty (text))
				return;
			dragItem = item;
			customSource = source;
			customSource.DragDataGet += HandleDragDataGet;
			customSource.DragEnd += HandleDragEnd;
		}
		
		void HandleDragEnd(object o, DragEndArgs args)
		{
			if (customSource != null) {
				customSource.DragDataGet -= HandleDragDataGet;
				customSource.DragEnd -= HandleDragEnd;
				customSource = null;
			}
		}
		
		void HandleDragDataGet(object o, DragDataGetArgs args)
		{
			if (dragItem != null) {
				TextEditor.CaretToDragCaretPosition ();
				((IToolboxConsumer)this).ConsumeItem (dragItem);
				dragItem = null;
			}
		}
		#endregion
		
		string GetText (ItemToolboxNode item)
		{
			TemplateToolboxNode templateToolboxNode = item as TemplateToolboxNode;
			if (templateToolboxNode != null)
				return templateToolboxNode.Template.Shortcut;
			
			ITextToolboxNode tn = item as ITextToolboxNode;
			if (tn == null) {
				LoggingService.LogWarning ("Cannot use non-ITextToolboxNode toolbox items in the text editor.");
				return null;
			}
			string filename = this.IsUntitled ? UntitledName : ContentName;
			return tn.GetTextForFile (filename, this.Project);
		}
		
		System.ComponentModel.ToolboxItemFilterAttribute[] IToolboxConsumer.ToolboxFilterAttributes {
			get {
				return new System.ComponentModel.ToolboxItemFilterAttribute[] {};
			}
		}
			
		bool ICustomFilteringToolboxConsumer.SupportsItem (ItemToolboxNode item)
		{
			ITextToolboxNode textNode = item as ITextToolboxNode;
			if (textNode == null)
				return false;
			
			string filename = this.IsUntitled ? UntitledName : ContentName;
			//int i = filename.LastIndexOf ('.');
			//string ext = i < 0? null : filename.Substring (i + 1);
			
			return textNode.IsCompatibleWith (filename, this.Project);
		}

		
		public Gtk.TargetEntry[] DragTargets { 
			get {
				return (Gtk.TargetEntry[])ClipboardActions.CopyOperation.TargetList (TextEditor.SelectionMode);
			}
		}
				
		bool IToolboxConsumer.CustomFilterSupports (ItemToolboxNode item)
		{
			return false;
		}
		
		string IToolboxConsumer.DefaultItemDomain { 
			get {
				return "Text";
			}
		}
		#endregion
		
		#region IZoomable
		bool IZoomable.EnableZoomIn {
			get {
				return this.TextEditor.Options.CanZoomIn;
			}
		}
		
		bool IZoomable.EnableZoomOut {
			get {
				return this.TextEditor.Options.CanZoomOut;
			}
		}
		
		bool IZoomable.EnableZoomReset {
			get {
				return this.TextEditor.Options.CanResetZoom;
			}
		}
		
		void IZoomable.ZoomIn ()
		{
			this.TextEditor.Options.ZoomIn ();
		}
		
		void IZoomable.ZoomOut ()
		{
			this.TextEditor.Options.ZoomOut ();
		}
		
		void IZoomable.ZoomReset ()
		{
			this.TextEditor.Options.ZoomReset ();
		}

		#region ITextEditorResolver implementation 
		
		public ResolveResult GetLanguageItem (int offset)
		{
			return this.SourceEditorWidget.TextEditor.GetLanguageItem (offset);
		}
		
		public ResolveResult GetLanguageItem (int offset, string expression)
		{
			return this.SourceEditorWidget.TextEditor.GetLanguageItem (offset, expression);
		}
		#endregion 
		
		#endregion
		public Mono.TextEditor.TextEditorData GetTextEditorData ()
		{
			return TextEditor.GetTextEditorData ();
		}
		
		public void InsertTemplate (CodeTemplate template, MonoDevelop.Ide.Gui.Document doc)
		{
			TextEditor.InsertTemplate (template, doc);
		}
		
		
		[CommandHandler (TextEditorCommands.GotoMatchingBrace)]
		protected void OnGotoMatchingBrace ()
		{
			TextEditor.RunAction (MiscActions.GotoMatchingBracket);
		}
		
	}
} 
