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
using System.Linq;
using System.Collections.Generic;
using System.IO;

using Gtk;

using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Debugger;
using Mono.Debugging.Client;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Ide.CodeTemplates;
using Services = MonoDevelop.Projects.Services;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor
{	
	public class SourceEditorView : AbstractViewContent, IExtensibleTextEditor, IBookmarkBuffer, IClipboardHandler, 
		ICompletionWidget,  ISplittable, IFoldable, IToolboxDynamicProvider, IEncodedTextContent,
		ICustomFilteringToolboxConsumer, IZoomable, ITextEditorResolver, Mono.TextEditor.ITextEditorDataProvider,
		ICodeTemplateHandler, ICodeTemplateContextProvider, ISupportsProjectReload, IPrintable
	{
		SourceEditorWidget widget;
		bool isDisposed = false;
		FileSystemWatcher fileSystemWatcher;
		static bool isInWrite = false;
		DateTime lastSaveTime;
		string loadedMimeType;
		internal object MemoryProbe = Counters.SourceViewsInMemory.CreateMemoryProbe ();
		
		TextMarker currentDebugLineMarker;
		TextMarker debugStackLineMarker;
		
		int lastDebugLine = -1;
		EventHandler currentFrameChanged;
		EventHandler<BreakpointEventArgs> breakpointAdded;
		EventHandler<BreakpointEventArgs> breakpointRemoved;
		EventHandler<BreakpointEventArgs> breakpointStatusChanged;
		EventHandler<FileEventArgs> fileChanged;
		
		List<LineSegment> breakpointSegments = new List<LineSegment> ();
		LineSegment debugStackSegment;
		LineSegment currentLineSegment;
		List<PinnedWatchInfo> pinnedWatches = new List<PinnedWatchInfo> ();
		
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
				return widget != null ? widget.Vbox : null;
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
		bool wasEdited = false;
		public SourceEditorView ()
		{
			Counters.LoadedEditors++;
			currentFrameChanged = (EventHandler)DispatchService.GuiDispatch (new EventHandler (OnCurrentFrameChanged));
			breakpointAdded = (EventHandler<BreakpointEventArgs>)DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointAdded));
			breakpointRemoved = (EventHandler<BreakpointEventArgs>)DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointRemoved));
			breakpointStatusChanged = (EventHandler<BreakpointEventArgs>)DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointStatusChanged));
			
			widget = new SourceEditorWidget (this);
			widget.TextEditor.Document.TextReplaced += delegate(object sender, ReplaceEventArgs args) {
				if (!inLoad)
					wasEdited = true;
				int startIndex = args.Offset;
				int endIndex = startIndex + Math.Max (args.Count, args.Value != null ? args.Value.Length : 0);
				if (TextChanged != null)
					TextChanged (this, new TextChangedEventArgs (startIndex, endIndex));
			};
			
			widget.TextEditor.Document.LineChanged += delegate(object sender, LineEventArgs e) {
				UpdateBreakpoints ();
				if (MessageBubbleTextMarker.RemoveLine (e.Line)) {
					MessageBubbleTextMarker marker = currentErrorMarkers.FirstOrDefault (m => m.LineSegment == e.Line);
					if (marker != null) {
						double oldHeight = marker.lastHeight;
						widget.TextEditor.TextViewMargin.RemoveCachedLine (e.Line); // ensure that the line cache is renewed
						double newHeight = marker.GetLineHeight (widget.TextEditor);
						if (oldHeight != newHeight)
							widget.Document.CommitLineToEndUpdate (widget.TextEditor.Document.OffsetToLineNumber (e.Line.Offset));
					}
				}
			};
			
			widget.TextEditor.Document.BeginUndo += delegate {
				wasEdited = false;
			};
			
			widget.TextEditor.Document.EndUndo += delegate {
				if (wasEdited)
					AutoSave.InformAutoSaveThread (Document);
			};
			
			widget.TextEditor.Document.TextReplacing += OnTextReplacing;
			widget.TextEditor.Document.TextReplaced += OnTextReplaced;
			widget.TextEditor.Document.ReadOnlyCheckDelegate = CheckReadOnly;
			
			//			widget.TextEditor.Document.DocumentUpdated += delegate {
			//				this.IsDirty = Document.IsDirty;
			//			};
			
			widget.TextEditor.Caret.PositionChanged += delegate {
				OnCaretPositionSet (EventArgs.Empty);
				FireCompletionContextChanged ();
			};
			widget.TextEditor.IconMargin.ButtonPressed += OnIconButtonPress;
			
			debugStackLineMarker = new DebugStackLineTextMarker (widget.TextEditor);
			currentDebugLineMarker = new CurrentDebugLineTextMarker (widget.TextEditor);
			
			fileSystemWatcher = new FileSystemWatcher ();
			fileSystemWatcher.Created += (FileSystemEventHandler)DispatchService.GuiDispatch (new FileSystemEventHandler (OnFileChanged));
			fileSystemWatcher.Changed += (FileSystemEventHandler)DispatchService.GuiDispatch (new FileSystemEventHandler (OnFileChanged));
			
			fileChanged = DispatchService.GuiDispatch (new EventHandler<FileEventArgs> (GotFileChanged));
			FileService.FileCreated += fileChanged;
			FileService.FileChanged += fileChanged;
			
			this.WorkbenchWindowChanged += delegate {
				if (WorkbenchWindow != null) {
					WorkbenchWindow.ActiveViewContentChanged += delegate {
						widget.UpdateLineCol ();
					};
				}
			};
			this.ContentNameChanged += delegate {
				this.Document.FileName = this.ContentName;
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
			
			DebuggingService.DebugSessionStarted += OnDebugSessionStarted;
			DebuggingService.CurrentFrameChanged += currentFrameChanged;
			DebuggingService.StoppedEvent += currentFrameChanged;
			DebuggingService.ResumedEvent += currentFrameChanged;
			DebuggingService.Breakpoints.BreakpointAdded += breakpointAdded;
			DebuggingService.Breakpoints.BreakpointRemoved += breakpointRemoved;
			DebuggingService.Breakpoints.BreakpointStatusChanged += breakpointStatusChanged;
			DebuggingService.Breakpoints.BreakpointModified += breakpointStatusChanged;
			DebuggingService.PinnedWatches.WatchAdded += OnWatchAdded;
			DebuggingService.PinnedWatches.WatchRemoved += OnWatchRemoved;
			DebuggingService.PinnedWatches.WatchChanged += OnWatchChanged;
			
			TaskService.Errors.TasksAdded   += UpdateTasks;
			TaskService.Errors.TasksRemoved += UpdateTasks;
			TaskService.JumpedToTask += HandleTaskServiceJumpedToTask;
			IdeApp.Preferences.ShowMessageBubblesChanged += HandleIdeAppPreferencesShowMessageBubblesChanged;
			MonoDevelop.Ide.Gui.Pads.ErrorListPad errorListPad = IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ().Content as MonoDevelop.Ide.Gui.Pads.ErrorListPad;
			errorListPad.TaskToggled += HandleErrorListPadTaskToggled;
			widget.TextEditor.Options.Changed += HandleWidgetTextEditorOptionsChanged;
			IdeApp.Preferences.DefaultHideMessageBubblesChanged += HandleIdeAppPreferencesDefaultHideMessageBubblesChanged;
		}
		
		MessageBubbleHighlightPopupWindow messageBubbleHighlightPopupWindow = null;

		void HandleWidgetTextEditorOptionsChanged (object sender, EventArgs e)
		{
			currentErrorMarkers.ForEach (marker => marker.DisposeLayout ());
		}

		void HandleTaskServiceJumpedToTask (object sender, TaskEventArgs e)
		{
			Task task = e.Tasks.FirstOrDefault ();
			if (task == null || task.FileName != Document.FileName)
				return;
			LineSegment lineSegment = Document.GetLine (task.Line);
			if (lineSegment == null)
				return;
			MessageBubbleTextMarker marker = (MessageBubbleTextMarker)lineSegment.Markers.FirstOrDefault (m => m is MessageBubbleTextMarker);
			if (marker == null)
				return;
			
			marker.SetPrimaryError (task.Description);
			
			if (TextEditor.IsComposited) {
				if (messageBubbleHighlightPopupWindow != null)
					messageBubbleHighlightPopupWindow.Destroy ();
				messageBubbleHighlightPopupWindow = new MessageBubbleHighlightPopupWindow (this, marker);
				messageBubbleHighlightPopupWindow.Destroyed += delegate {
					messageBubbleHighlightPopupWindow = null;
				};
				messageBubbleHighlightPopupWindow.Popup ();
			}
		}

		void HandleIdeAppPreferencesDefaultHideMessageBubblesChanged (object sender, PropertyChangedEventArgs e)
		{
			currentErrorMarkers.ForEach (marker => marker.IsVisible = !IdeApp.Preferences.DefaultHideMessageBubbles);
			this.TextEditor.QueueDraw ();
		}

		void HandleIdeAppPreferencesShowMessageBubblesChanged (object sender, PropertyChangedEventArgs e)
		{
			UpdateTasks (null, null);
		}

		void HandleErrorListPadTaskToggled (object sender, TaskEventArgs e)
		{
			this.TextEditor.QueueDraw ();
		}
		
		List<MessageBubbleTextMarker> currentErrorMarkers = new List<MessageBubbleTextMarker> ();
		void UpdateTasks (object sender, TaskEventArgs e)
		{
			Task[] tasks = TaskService.Errors.GetFileTasks (ContentName);
			if (tasks == null)
				return;
			DisposeErrorMarkers ();
			if (IdeApp.Preferences.ShowMessageBubbles == ShowMessageBubbles.Never)
				return;
			widget.Document.BeginAtomicUndo ();
			
			foreach (Task task in tasks) {
				if (task.Severity == TaskSeverity.Error || task.Severity == TaskSeverity.Warning) {
					if (IdeApp.Preferences.ShowMessageBubbles == ShowMessageBubbles.ForErrors && task.Severity == TaskSeverity.Warning)
						continue;
					LineSegment lineSegment = widget.Document.GetLine (task.Line);
					if (lineSegment == null)
						continue;
					var marker = currentErrorMarkers.FirstOrDefault (m => m.LineSegment == lineSegment);
					if (marker != null) {
						marker.AddError (task.Severity == TaskSeverity.Error, task.Description);
						continue;
					}
					MessageBubbleTextMarker errorTextMarker = new MessageBubbleTextMarker (widget.TextEditor, task, lineSegment, task.Severity == TaskSeverity.Error, task.Description);
					currentErrorMarkers.Add (errorTextMarker);
					
					errorTextMarker.IsVisible = !IdeApp.Preferences.DefaultHideMessageBubbles;
					widget.Document.AddMarker (lineSegment, errorTextMarker, false);
				}
			}
			widget.Document.EndAtomicUndo ();
			widget.TextEditor.QueueDraw ();
		}
		
		void DisposeErrorMarkers ()
		{
			currentErrorMarkers.ForEach (em => {
				widget.Document.RemoveMarker (em);
				em.Dispose ();
			});
			currentErrorMarkers.Clear ();
		}
		
		public override void Save (string fileName)
		{
			Save (fileName, this.encoding);
		}
		
		public void Save (string fileName, string encoding)
		{
			if (!string.IsNullOrEmpty (ContentName))
				AutoSave.RemoveAutoSaveFile (ContentName);

			if (ContentName != fileName) {
				if (!FileService.RequestFileEdit (fileName))
					return;
				writeAllowed = true;
				writeAccessChecked = true;
			}

			if (warnOverwrite) {
				if (fileName == ContentName) {
					if ( MessageService.AskQuestion (GettextCatalog.GetString ("This file {0} has been changed outside of MonoDevelop. Are you sure you want to overwrite the file?", fileName), AlertButton.Cancel, AlertButton.OverwriteFile) != AlertButton.OverwriteFile)
						return;
				}
				warnOverwrite = false;
				widget.RemoveMessageBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			
			if (PropertyService.Get ("AutoFormatDocumentOnSave", false)) {
				Formatter formatter = TextFileService.GetFormatter (Document.MimeType);
				if (formatter != null && formatter.SupportsOnTheFlyFormatting) {
					TextEditor.Document.BeginAtomicUndo ();
					formatter.OnTheFlyFormat (Project != null ? Project.Policies : null, TextEditor.GetTextEditorData (), 0, Document.Length);
					TextEditor.Document.EndAtomicUndo ();
				}
			}

			isInWrite = true;
			try {
				object attributes = null;
				if (File.Exists (fileName)) {
					try {
						attributes = DesktopService.GetFileAttributes (fileName);
					} catch (Exception e) {
						LoggingService.LogWarning ("Can't get file attributes", e);
					}
				}
				try {
					TextFile.WriteFile (fileName, Document.Text, encoding, hadBom);
				} catch (InvalidEncodingException) {
					var result = MessageService.AskQuestion (GettextCatalog.GetString ("Can't save file witch current codepage."), 
						GettextCatalog.GetString ("Some unicode characters in this file could not be saved with the current encoding.\nDo you want to resave this file as Unicode ?\nYou can choose another encoding in the 'save as' dialog."),
						1,
						AlertButton.Cancel,
						new AlertButton (GettextCatalog.GetString ("Save as Unicode")));
					if (result != AlertButton.Cancel) {
						this.hadBom = true;
						this.encoding = "UTF-8";
						TextFile.WriteFile (fileName, Document.Text, this.encoding, this.hadBom);
					} else {
						return;
					}
				}
				lastSaveTime = File.GetLastWriteTime (fileName);
				try {
					if (attributes != null)
						DesktopService.SetFileAttributes (fileName, attributes);
				} catch (Exception e) {
					LoggingService.LogError ("Can't set file attributes", e);
				}
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
		
		public override void DiscardChanges ()
		{
			if (!string.IsNullOrEmpty (ContentName))
				AutoSave.RemoveAutoSaveFile (ContentName);
		}
		
		public override void Load (string fileName)
		{
			Load (fileName, null);
		}
		
		public void Load (string fileName, string encoding)
		{
			// Handle the "reload" case.
			if (ContentName == fileName)
				AutoSave.RemoveAutoSaveFile (fileName);

			if (warnOverwrite) {
				warnOverwrite = false;
				widget.RemoveMessageBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			
			// Look for a mime type for which there is a syntax mode
			UpdateMimeType (fileName);
			
			if (AutoSave.AutoSaveExists (fileName)) {
				widget.ShowAutoSaveWarning (fileName);
				this.encoding = encoding;
			} else {
				TextFile file = TextFile.ReadFile (fileName, encoding);
				inLoad = true;
				Document.Text = file.Text;
				inLoad = false;
				this.encoding = file.SourceEncoding;
				this.hadBom = file.HadBOM;
			}
			
			// TODO: Would be much easier if the view would be created after the containers.
			this.WorkbenchWindowChanged += delegate {
				if (WorkbenchWindow == null)
					return;
				WorkbenchWindow.DocumentChanged += delegate {
					if (WorkbenchWindow.Document == null)
						return;
					WorkbenchWindow.Document.DocumentParsed += delegate(object sender, EventArgs e) {
						widget.UpdateParsedDocument (WorkbenchWindow.Document.ParsedDocument);
					};
				};
			};
			
			ContentName = fileName;
			
			widget.TextEditor.Caret.Offset = 0;
			UpdateExecutionLocation ();
			UpdateBreakpoints ();
			UpdatePinnedWatches ();
			this.IsDirty = false;
			UpdateTasks (null, null);
		}

		bool warnOverwrite = false;
		bool inLoad = false;
		string encoding = null;
		bool hadBom = false;
		public void Load (string fileName, string content, string encoding)
		{
			if (warnOverwrite) {
				warnOverwrite = false;
				widget.RemoveMessageBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			UpdateMimeType (fileName);
			
			inLoad = true;
			Document.Text = content;
			inLoad = false;
			this.encoding = encoding;
			ContentName = fileName;

			UpdateExecutionLocation ();
			UpdateBreakpoints ();
			UpdatePinnedWatches ();
			this.IsDirty = false;
		}
		
		void UpdateMimeType (string fileName)
		{
			// Look for a mime type for which there is a syntax mode
			string mimeType = DesktopService.GetMimeTypeForUri (fileName);
			if (loadedMimeType != mimeType) {
				loadedMimeType = mimeType;
				if (mimeType != null) {
					foreach (string mt in DesktopService.GetMimeTypeInheritanceChain (loadedMimeType)) {
						if (Mono.TextEditor.Highlighting.SyntaxModeService.GetSyntaxMode (mt) != null) {
							Document.MimeType = mt;
							widget.TextEditor.TextEditorResolverProvider = TextEditorResolverService.GetProvider (mt);
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
			Counters.LoadedEditors--;
			
			if (messageBubbleHighlightPopupWindow != null)
				messageBubbleHighlightPopupWindow.Destroy ();
			
			IdeApp.Preferences.DefaultHideMessageBubblesChanged -= HandleIdeAppPreferencesDefaultHideMessageBubblesChanged;
			IdeApp.Preferences.ShowMessageBubblesChanged -= HandleIdeAppPreferencesShowMessageBubblesChanged;
			MonoDevelop.Ide.Gui.Pads.ErrorListPad errorListPad = IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ().Content as MonoDevelop.Ide.Gui.Pads.ErrorListPad;
			errorListPad.TaskToggled -= HandleErrorListPadTaskToggled;
			
			DisposeErrorMarkers ();
			
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
				widget.TextEditor.Options.Changed -= HandleWidgetTextEditorOptionsChanged;
				// widget is destroyed with it's parent.
				// widget.Destroy ();
				widget = null;
			}
			
			DebuggingService.DebugSessionStarted -= OnDebugSessionStarted;
			DebuggingService.CurrentFrameChanged -= currentFrameChanged;
			DebuggingService.StoppedEvent -= currentFrameChanged;
			DebuggingService.ResumedEvent -= currentFrameChanged;
			DebuggingService.Breakpoints.BreakpointAdded -= breakpointAdded;
			DebuggingService.Breakpoints.BreakpointRemoved -= breakpointRemoved;
			DebuggingService.Breakpoints.BreakpointStatusChanged -= breakpointStatusChanged;
			DebuggingService.Breakpoints.BreakpointModified -= breakpointStatusChanged;
			DebuggingService.PinnedWatches.WatchAdded -= OnWatchAdded;
			DebuggingService.PinnedWatches.WatchRemoved -= OnWatchRemoved;
			DebuggingService.PinnedWatches.WatchChanged -= OnWatchChanged;
			
			TaskService.Errors.TasksAdded   -= UpdateTasks;
			TaskService.Errors.TasksRemoved -= UpdateTasks;
			TaskService.Errors.TasksChanged -= UpdateTasks;
			TaskService.JumpedToTask -= HandleTaskServiceJumpedToTask;
			
			FileService.FileCreated -= fileChanged;
			FileService.FileChanged -= fileChanged;
			
			// This is not necessary but helps when tracking down memory leaks
			
			debugStackLineMarker = null;
			currentDebugLineMarker = null;
			
			currentFrameChanged = null;
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
		
		void GotFileChanged (object sender, FileEventArgs args)
		{
			if (!isDisposed)
				HandleFileChanged (args.FileName);
		}
		
		void OnFileChanged (object sender, FileSystemEventArgs args)
		{
			if (args.ChangeType == WatcherChangeTypes.Changed || args.ChangeType == WatcherChangeTypes.Created) 
				HandleFileChanged (args.FullPath);
		}
		
		void HandleFileChanged (string fileName)
		{
			if (!isInWrite && fileName != ContentName)
				return;
			if (lastSaveTime == File.GetLastWriteTime (ContentName))
				return;
			
			if (!IsDirty && IdeApp.Workbench.AutoReloadDocuments)
				widget.Reload ();
			else
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
				TextFileService.FireLineCountChanged (this, location.Line, lines, location.Column);
		}

		void OnCurrentFrameChanged (object s, EventArgs args)
		{
			UpdateExecutionLocation ();
			if (!DebuggingService.IsDebugging)
				UpdatePinnedWatches ();
		}
		
		void UpdateExecutionLocation ()
		{
			if (DebuggingService.IsDebugging && !DebuggingService.IsRunning) {
				var frame = CheckFrameIsInFile (DebuggingService.CurrentFrame)
					?? CheckFrameIsInFile (DebuggingService.GetCurrentVisibleFrame ());
				if (frame != null) {
					if (lastDebugLine == frame.SourceLocation.Line)
						return;
					RemoveDebugMarkers ();
					lastDebugLine = frame.SourceLocation.Line;
					var segment = widget.TextEditor.Document.GetLine (lastDebugLine);
					if (segment != null) {
						if (DebuggingService.CurrentFrameIndex == 0) {
							currentLineSegment = segment;
							widget.TextEditor.Document.AddMarker (segment, currentDebugLineMarker);
						} else {
							debugStackSegment = segment;
							widget.TextEditor.Document.AddMarker (segment, debugStackLineMarker);
						}
						widget.TextEditor.QueueDraw ();
					}
					return;
				}
			}
			
			if (currentLineSegment != null || debugStackSegment != null) {
				RemoveDebugMarkers ();
				lastDebugLine = -1;
				widget.TextEditor.QueueDraw ();
			}
		}
		
		StackFrame CheckFrameIsInFile (StackFrame frame)
		{
			if (!string.IsNullOrEmpty (ContentName) && frame != null && !string.IsNullOrEmpty (frame.SourceLocation.Filename)
				&& ((FilePath)frame.SourceLocation.Filename).FullPath == ((FilePath)ContentName).FullPath)
				return frame;
			return null;
		}
		
		void RemoveDebugMarkers ()
		{
			if (currentLineSegment != null) {
				widget.TextEditor.Document.RemoveMarker (currentDebugLineMarker);
				currentLineSegment = null;
			}
			if (debugStackSegment != null) {
				widget.TextEditor.Document.RemoveMarker (debugStackLineMarker);
				debugStackSegment = null;
			}
		}
		
		struct PinnedWatchInfo {
			public PinnedWatch Watch;
			public LineSegment Line;
			public PinnedWatchWidget Widget;
//			public DebugValueMarker Marker;
		}
		
		void UpdatePinnedWatches ()
		{
			foreach (PinnedWatchInfo wi in pinnedWatches) {
				widget.TextEditorContainer.Remove (wi.Widget);
				wi.Widget.Destroy ();
			}
			pinnedWatches.Clear ();
			if (ContentName == null || !DebuggingService.IsDebugging)
				return;
			foreach (PinnedWatch w in DebuggingService.PinnedWatches.GetWatchesForFile (Path.GetFullPath (ContentName))) {
				AddWatch (w);
			}
			widget.TextEditor.QueueDraw ();
		}
		
		void AddWatch (PinnedWatch w)
		{
			LineSegment line = widget.TextEditor.Document.GetLine (w.Line);
			if (line == null)
				return;
			PinnedWatchInfo wi = new PinnedWatchInfo ();
			wi.Line = line;
			if (w.OffsetX < 0) {
				w.OffsetY = (int)widget.TextEditor.LineToY (w.Line);
				int lw, lh;
				widget.TextEditor.TextViewMargin.GetLayout (line).Layout.GetPixelSize (out lw, out lh);
				w.OffsetX = (int)widget.TextEditor.TextViewMargin.XOffset + lw + 4;
			}
			wi.Widget = new PinnedWatchWidget (widget.TextEditorContainer, w);
			
//			wi.Marker = new DebugValueMarker (widget.TextEditor, line, w);
			wi.Watch = w;
			pinnedWatches.Add (wi);
//			if (w.Value != null)
//				wi.Marker.AddValue (w.Value);

			widget.TextEditorContainer.AddTopLevelWidget (wi.Widget, w.OffsetX, w.OffsetY);
			
//			widget.TextEditor.QueueDraw ();
		}

		void OnDebugSessionStarted (object sender, EventArgs e)
		{
			UpdatePinnedWatches ();
		}
		
		void OnWatchAdded (object s, PinnedWatchEventArgs args)
		{
			if (args.Watch.File == ContentName && DebuggingService.IsDebugging)
				AddWatch (args.Watch);
		}
		
		void OnWatchRemoved (object s, PinnedWatchEventArgs args)
		{
			foreach (PinnedWatchInfo wi in pinnedWatches) {
				if (wi.Watch == args.Watch) {
					pinnedWatches.Remove (wi);
					widget.TextEditorContainer.Remove (wi.Widget);
					wi.Widget.Destroy ();
					break;
				}
			}
		}
		
		void OnWatchChanged (object s, PinnedWatchEventArgs args)
		{
			foreach (PinnedWatchInfo wi in pinnedWatches) {
				if (wi.Watch == args.Watch) {
					wi.Widget.ObjectValue = wi.Watch.Value;
					widget.TextEditorContainer.MoveTopLevelWidget (wi.Widget, args.Watch.OffsetX, args.Watch.OffsetY);
					break;
				}
			}
		}
		
		void UpdateBreakpoints ()
		{
			int i = 0, count = 0;
			bool mismatch = false;
			foreach (Breakpoint bp in DebuggingService.Breakpoints.GetBreakpoints ()) {
				count++;
				if (i < breakpointSegments.Count) {
					int lineNumber = widget.TextEditor.Document.OffsetToLineNumber (breakpointSegments[i].Offset);
					if (lineNumber != bp.Line) {
						mismatch = true;
						break;
					}
					i++;
				}
			}
			if (count != breakpointSegments.Count)
				mismatch = true;
			
			if (!mismatch)
				return;
			
			HashSet<int> lineNumbers = new HashSet<int> ();
			foreach (LineSegment line in breakpointSegments) {
				lineNumbers.Add (Document.OffsetToLineNumber (line.Offset));
				widget.TextEditor.Document.RemoveMarker (line, typeof (BreakpointTextMarker));
				widget.TextEditor.Document.RemoveMarker (line, typeof (DisabledBreakpointTextMarker));
				widget.TextEditor.Document.RemoveMarker (line, typeof (InvalidBreakpointTextMarker));
			}
	
			breakpointSegments.Clear ();
			foreach (Breakpoint bp in DebuggingService.Breakpoints.GetBreakpoints ()) {
				lineNumbers.Add (bp.Line);
				AddBreakpoint (bp);
			}
			
			foreach (int lineNumber in lineNumbers) {
				widget.Document.RequestUpdate (new LineUpdate (lineNumber));
			}
			
			widget.Document.CommitDocumentUpdate ();
			
			// Ensure the current line marker is drawn at the top
			lastDebugLine = -1;
			UpdateExecutionLocation ();
		}
		
		void AddBreakpoint (Breakpoint bp)
		{
			if (DebuggingService.PinnedWatches.IsWatcherBreakpoint (bp))
				return;
			FilePath fp = Name;
			if (fp.FullPath == bp.FileName) {
				LineSegment line = widget.TextEditor.Document.GetLine (bp.Line);
				
				if (line == null)
					return;
				if (!bp.Enabled) {
					if (bp.HitAction == HitAction.Break)
						widget.TextEditor.Document.AddMarker (line, new DisabledBreakpointTextMarker (widget.TextEditor, false));
					else
						widget.TextEditor.Document.AddMarker (line, new DisabledBreakpointTextMarker (widget.TextEditor, true));
				}
				else if (bp.IsValid (DebuggingService.DebuggerSession)) {
					if (bp.HitAction == HitAction.Break)
						widget.TextEditor.Document.AddMarker (line, new BreakpointTextMarker (widget.TextEditor, false));
					else
						widget.TextEditor.Document.AddMarker (line, new BreakpointTextMarker (widget.TextEditor, true));
				}
				else
					widget.TextEditor.Document.AddMarker (line, new InvalidBreakpointTextMarker (widget.TextEditor));
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
				if (!isDisposed)
					UpdateBreakpoints ();
				return false;
			});
		}
		
		void OnBreakpointRemoved (object s, BreakpointEventArgs args)
		{
			if (ContentName == null || args.Breakpoint.FileName != Path.GetFullPath (ContentName))
				return;
			// Updated with a delay, to make sure it works when called as a
			// result of inserting/removing lines before a breakpoint position
			GLib.Timeout.Add (10, delegate {
				if (!isDisposed)
					UpdateBreakpoints ();
				return false;
			});
		}
		
		void OnBreakpointStatusChanged (object s, BreakpointEventArgs args)
		{
			if (ContentName == null || args.Breakpoint.FileName != Path.GetFullPath (ContentName))
				return;
			// Updated with a delay, to make sure it works when called as a
			// result of inserting/removing lines before a breakpoint position
			GLib.Timeout.Add (10, delegate {
				if (!isDisposed)
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
			} else if (args.Button == 1) {
				if (!string.IsNullOrEmpty (this.Document.FileName)) {
					if (args.LineSegment != null)
						DebuggingService.Breakpoints.Toggle (this.Document.FileName, args.LineNumber);
				}
			}
		}
		
		#region IExtensibleTextEditor
		public ITextEditorExtension Extension {
			get;
			set;
		}
		
		ITextEditorExtension IExtensibleTextEditor.AttachExtension (ITextEditorExtension extension)
		{
			Extension = extension;
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
			// TODO: Maybe make this feature optional ?
/*			if (this.Document.GetCurrentUndoDepth () > 0 && !this.Document.IsDirty) {
				var buttonCancel = new AlertButton (GettextCatalog.GetString ("Don't Undo")); 
				var buttonOk = new AlertButton (GettextCatalog.GetString ("Undo")); 
				var question = GettextCatalog.GetString ("You are about to undo past the last point this file was saved. Do you want to do this?");
				var result = MessageService.GenericAlert (Gtk.Stock.DialogWarning, GettextCatalog.GetString ("Warning"),
				                                          question, 1, buttonCancel, buttonOk);
				if (result != buttonOk)
					return;
			}*/
			
			this.Document.Undo ();
		}
		
		public bool EnableRedo {
			get {
				return this.Document.CanRedo && widget.EditorHasFocus;
			}
		}

		public void SetCaretTo (int line, int column)
		{
			widget.TextEditor.SetCaretTo (line, column, true);
		}

		public void SetCaretTo (int line, int column, bool highlight)
		{
			widget.TextEditor.SetCaretTo (line, column, highlight);
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
		
		public bool HasInputFocus {
			get { return TextEditor.HasFocus; }
		}
		
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
			return this.widget.TextEditor.Document.LocationToOffset (new DocumentLocation (line, column));
		}
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			DocumentLocation location = this.widget.TextEditor.Document.OffsetToLocation (position);
			line   = location.Line;
			column = location.Column;
		}
		#endregion
		
		#region IEditableTextFile
		public int InsertText (int position, string text)
		{
			int length = this.widget.TextEditor.Insert (position, text);
			this.widget.TextEditor.Caret.Offset = position + length;
			return length;
		}
		public void DeleteText (int position, int length)
		{
			this.widget.TextEditor.Remove (position, length);
			this.widget.TextEditor.Caret.Offset = position;
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
				TextEditor.RunAction (DeleteActions.Delete);
			}
		}
		
		public void SelectAll ()
		{
			TextEditor.RunAction (SelectionActions.SelectAll);
		}
		#endregion
		
		#region ICompletionWidget
		
		public CodeCompletionContext CurrentCodeCompletionContext {
			get {
				return CreateCodeCompletionContext (TextEditor.Caret.Offset);
			}
		}
		
		public int TextLength {
			get {
				return Document.Length;
			}
		}
		public int SelectedLength { 
			get {
				if (TextEditor.IsSomethingSelected) {
					if (TextEditor.MainSelection.SelectionMode == Mono.TextEditor.SelectionMode.Block)
						return System.Math.Abs (TextEditor.MainSelection.Anchor.Column - TextEditor.MainSelection.Lead.Column);
					return TextEditor.SelectionRange.Length;
				}
				return 0;
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
				return widget.Vbox.Style.Copy ();
			}
		}
		public void Replace (int offset, int count, string text)
		{
			widget.TextEditor.GetTextEditorData ().Replace (offset, count, text);
			if (widget.TextEditor.Caret.Offset >= offset) {
				widget.TextEditor.Caret.Offset -= count;
				widget.TextEditor.Caret.Offset += text.Length;
			}
		}
		
		public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset) 
		{
			CodeCompletionContext result = new CodeCompletionContext ();
			result.TriggerOffset = triggerOffset;
			DocumentLocation loc = Document.OffsetToLocation (triggerOffset);
			result.TriggerLine   = loc.Line;
			result.TriggerLineOffset = loc.Column - 1;
			var p = DocumentToScreenLocation (loc);
			result.TriggerXCoord = p.X;
			result.TriggerYCoord = p.Y;
			result.TriggerTextHeight = (int)TextEditor.LineHeight;
			return result;
		}
		
		public Gdk.Point DocumentToScreenLocation (DocumentLocation location)
		{
			var p = widget.TextEditor.LocationToPoint (location);
			int tx, ty;
			widget.Vbox.ParentWindow.GetOrigin (out tx, out ty);
			tx += widget.TextEditorContainer.Allocation.X + p.X;
			ty += widget.TextEditorContainer.Allocation.Y + p.Y + (int)TextEditor.LineHeight;
			return new Gdk.Point (tx, ty);
		}
		
		public CodeTemplateContext GetCodeTemplateContext ()
		{
			return TextEditor.GetTemplateContext ();
		}
		
		public string GetCompletionText (CodeCompletionContext ctx)
		{
			if (ctx == null)
				return null;
			int min = Math.Min (ctx.TriggerOffset, TextEditor.Caret.Offset);
			int max = Math.Max (ctx.TriggerOffset, TextEditor.Caret.Offset);
			return Document.GetTextBetween (min, max);
		}
		
		public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
		{
			SetCompletionText (ctx, partial_word, complete_word, complete_word.Length);
		}
		
		public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int wordOffset)
		{
			TextEditorData data = this.GetTextEditorData ();
			if (data == null || data.Document == null)
				return;
			int triggerOffset = ctx.TriggerOffset;
			int length = String.IsNullOrEmpty (partial_word) ? 0 : partial_word.Length;
			bool blockMode = false;
			if (data.IsSomethingSelected) {
				blockMode = data.MainSelection.SelectionMode == Mono.TextEditor.SelectionMode.Block;
				if (blockMode) {
					data.Caret.PreserveSelection = true;
					triggerOffset = data.Caret.Offset - length;
				} else {
					if (data.SelectionRange.Offset < ctx.TriggerOffset)
						triggerOffset = ctx.TriggerOffset - data.SelectionRange.Length;
					data.DeleteSelectedText ();
				}
				length = 0;
			}

			// | in the completion text now marks the caret position
			int idx = complete_word.IndexOf ('|');
			if (idx >= 0) {
				complete_word = complete_word.Remove (idx, 1);
			} else {
				idx = wordOffset;
			}
			
			triggerOffset += data.EnsureCaretIsNotVirtual ();
			data.Document.EndAtomicUndo ();
			if (blockMode) {
				data.Document.BeginAtomicUndo ();

				int minLine = data.MainSelection.MinLine;
				int maxLine = data.MainSelection.MaxLine;
				int column = triggerOffset - data.Document.GetLineByOffset (triggerOffset).Offset;
				for (int lineNumber = minLine; lineNumber <= maxLine; lineNumber++) {
					LineSegment lineSegment = data.Document.GetLine (lineNumber);
					if (lineSegment == null)
						continue;
					int offset = lineSegment.Offset + column;
					data.Replace (offset, length, complete_word);
				}
				data.Caret.Offset = triggerOffset + idx;
				int minColumn = System.Math.Min (data.MainSelection.Anchor.Column, data.MainSelection.Lead.Column);
				data.MainSelection.Anchor = new DocumentLocation (data.Caret.Line == minLine ? maxLine : minLine, minColumn);
				data.MainSelection.Lead = new DocumentLocation (data.Caret.Line, TextEditor.Caret.Column);
				
				data.Document.CommitMultipleLineUpdate (data.MainSelection.MinLine, data.MainSelection.MaxLine);
				data.Caret.PreserveSelection = false;
			} else {
				data.Replace (triggerOffset, length, complete_word);
				data.Caret.Offset = triggerOffset + idx;
				data.Document.BeginAtomicUndo ();
			}
			
			data.Document.CommitLineUpdate (data.Caret.Line);
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
		public void ToggleAllFoldings ()
		{
			FoldActions.ToggleAllFolds (TextEditor.GetTextEditorData ());
			widget.TextEditor.ScrollToCaret ();
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
			widget.TextEditor.GetTextEditorData ().RaiseUpdateAdjustmentsRequested ();
			widget.TextEditor.ScrollToCaret ();
		}
		
		public void ToggleFolding ()
		{
			FoldActions.ToggleFold (TextEditor.GetTextEditorData ());
			widget.TextEditor.ScrollToCaret ();
		}
		#endregion
		
		#region IPrintable
		
		public bool CanPrint {
			get { return true; }
		}
		
		public void PrintDocument (PrintingSettings settings)
		{
			RunPrintOperation (PrintOperationAction.PrintDialog, settings);
		}
		
		public void PrintPreviewDocument (PrintingSettings settings)
		{
			RunPrintOperation (PrintOperationAction.Preview, settings);
		}
		
		void RunPrintOperation (PrintOperationAction action, PrintingSettings settings)
		{
			var op = new SourceEditorPrintOperation (TextEditor.Document, Name);
			
			if (settings.PrintSettings != null)
				op.PrintSettings = settings.PrintSettings;
			if (settings.PageSetup != null)
				op.DefaultPageSetup = settings.PageSetup;
			
			//FIXME: implement in-place preview
			//op.Preview += HandleOpPreview;
			
			//FIXME: implement async on platforms that support it
			var result = op.Run (action, IdeApp.Workbench.RootWindow);
			
			if (result == PrintOperationResult.Apply)
				settings.PrintSettings = op.PrintSettings;
			else if (result == PrintOperationResult.Error)
				//FIXME: can't show more details, GTK# GetError binding is bad
				MessageService.ShowError (GettextCatalog.GetString ("Print operation failed."));
		}
		
		#endregion
	
		#region Toolbox
		static List<TextToolboxNode> clipboardRing = new List<TextToolboxNode> ();
		static event EventHandler ClipbardRingUpdated;
		
		static SourceEditorView ()
		{
			CodeSegmentPreviewWindow.CodeSegmentPreviewInformString = GettextCatalog.GetString ("Press 'shift+space' for focus");
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
				item.Icon = DesktopService.GetPixbufForFile ("test.txt", Gtk.IconSize.Menu);
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
			var tn = item as ITextToolboxNode;
			if (tn != null) {
				tn.InsertAtCaret (base.WorkbenchWindow.Document);
				TextEditor.GrabFocus ();
			}
		}
		
		#region dnd
		Gtk.Widget customSource;
		ItemToolboxNode dragItem;
		void IToolboxConsumer.DragItem (ItemToolboxNode item, Gtk.Widget source, Gdk.DragContext ctx)
		{
			//FIXME: use the preview text
			string text = GetDragPreviewText (item);
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
		
		string GetDragPreviewText (ItemToolboxNode item)
		{
			ITextToolboxNode tn = item as ITextToolboxNode;
			if (tn == null) {
				LoggingService.LogWarning ("Cannot use non-ITextToolboxNode toolbox items in the text editor.");
				return null;
			}
			return tn.GetDragPreview (base.WorkbenchWindow.Document);
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
			
			//string filename = this.IsUntitled ? UntitledName : ContentName;
			//int i = filename.LastIndexOf ('.');
			//string ext = i < 0? null : filename.Substring (i + 1);
			
			return textNode.IsCompatibleWith (base.WorkbenchWindow.Document);
		}

		
		public Gtk.TargetEntry[] DragTargets { 
			get {
				return (Gtk.TargetEntry[])ClipboardActions.CopyOperation.targetList;
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
		
		#region ISupportsProjectReload implementaion
		
		ProjectReloadCapability ISupportsProjectReload.ProjectReloadCapability {
			get {
				return ProjectReloadCapability.Full;
			}
		}
		
		void ISupportsProjectReload.Update (Project project)
		{
			// The project will be assigned to the view. Nothing else to do. 
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
		
		void CorrectIndenting ()
		{
			Formatter formatter = TextFileService.GetFormatter (Document.MimeType);
			if (formatter == null || !formatter.SupportsCorrectIndenting)
				return;
			if (TextEditor.IsSomethingSelected) {
				TextEditor.Document.BeginAtomicUndo ();
				int max = TextEditor.MainSelection.MaxLine;
				for (int i = TextEditor.MainSelection.MinLine; i <= max; i++) {
					formatter.CorrectIndenting (TextEditor.GetTextEditorData (), i);
				}
				TextEditor.Document.EndAtomicUndo ();
			} else {
				formatter.CorrectIndenting (TextEditor.GetTextEditorData (), TextEditor.Caret.Line);
			}
		}
		
		[CommandHandler (TextEditorCommands.MoveBlockUp)]
		protected void OnMoveBlockUp ()
		{
			TextEditor.RunAction (MiscActions.MoveBlockUp);
			CorrectIndenting ();
		}
		
		[CommandHandler (TextEditorCommands.MoveBlockDown)]
		protected void OnMoveBlockDown ()
		{
			TextEditor.RunAction (MiscActions.MoveBlockDown);
			CorrectIndenting ();
		}
		
		
		[CommandUpdateHandler (TextEditorCommands.ToggleBlockSelectionMode)]
		protected void UpdateToggleBlockSelectionMode (CommandInfo cinfo)
		{
			cinfo.Enabled = TextEditor.IsSomethingSelected;
		}
		
		[CommandHandler (TextEditorCommands.ToggleBlockSelectionMode)]
		protected void OnToggleBlockSelectionMode ()
		{
			TextEditor.SelectionMode = TextEditor.SelectionMode == Mono.TextEditor.SelectionMode.Normal ? Mono.TextEditor.SelectionMode.Block : Mono.TextEditor.SelectionMode.Normal;
			TextEditor.QueueDraw ();
		}
		
		#region widget command handlers
		[CommandHandler (SearchCommands.EmacsFindNext)]
		public void EmacsFindNext ()
		{
			widget.EmacsFindNext ();
		}
		
		[CommandHandler (SearchCommands.EmacsFindPrevious)]
		public void EmacsFindPrevious ()
		{
			widget.EmacsFindPrevious ();
		}
		
		[CommandHandler (SearchCommands.Find)]
		public void ShowSearchWidget ()
		{
			widget.ShowSearchWidget ();
		}
		
		[CommandHandler (SearchCommands.Replace)]
		public void ShowReplaceWidget ()
		{
			widget.ShowReplaceWidget ();
		}
		
		[CommandUpdateHandler (SearchCommands.UseSelectionForFind)]
		protected void OnUpdateUseSelectionForFind (CommandInfo info)
		{
			widget.OnUpdateUseSelectionForFind (info);
		}
		
		[CommandHandler (SearchCommands.UseSelectionForFind)]
		public void UseSelectionForFind ()
		{
			widget.UseSelectionForFind ();
		}
		
		[CommandUpdateHandler (SearchCommands.UseSelectionForReplace)]
		protected void OnUpdateUseSelectionForReplace (CommandInfo info)
		{
			widget.OnUpdateUseSelectionForReplace (info);
		}
		
		[CommandHandler (SearchCommands.UseSelectionForReplace)]
		public void UseSelectionForReplace ()
		{
			widget.UseSelectionForReplace ();
		}
		
		[CommandHandler (SearchCommands.GotoLineNumber)]
		public void ShowGotoLineNumberWidget ()
		{
			widget.ShowGotoLineNumberWidget ();
		}
		
		[CommandHandler (SearchCommands.FindNext)]
		public SearchResult FindNext ()
		{
			return widget.FindNext ();
		}
		
		[CommandHandler (SearchCommands.FindPrevious)]
		public SearchResult FindPrevious ()
		{
			return widget.FindPrevious ();
		}
		
		[CommandHandler (SearchCommands.FindNextSelection)]
		public SearchResult FindNextSelection ()
		{
			return widget.FindNextSelection ();
		}
		
		[CommandHandler (SearchCommands.FindPreviousSelection)]
		public SearchResult FindPreviousSelection ()
		{
			return widget.FindPreviousSelection ();
		}
		
		[CommandHandler (HelpCommands.Help)]
		internal void MonodocResolver ()
		{
			widget.MonodocResolver ();
		}
		
		[CommandUpdateHandler (HelpCommands.Help)]
		internal void MonodocResolverUpdate (CommandInfo cinfo)
		{
			widget.MonodocResolverUpdate (cinfo);
		}
		
		[CommandUpdateHandler (EditCommands.ToggleCodeComment)]
		internal void OnUpdateToggleComment (MonoDevelop.Components.Commands.CommandInfo info)
		{
			widget.OnUpdateToggleComment (info);
		}
		
		[CommandHandler (EditCommands.ToggleCodeComment)]
		public void ToggleCodeComment ()
		{
			widget.ToggleCodeComment ();
		}
		
		[CommandUpdateHandler (SourceEditorCommands.ToggleErrorTextMarker)]
		public void OnUpdateToggleErrorTextMarker (CommandInfo info)
		{
			widget.OnUpdateToggleErrorTextMarker (info);
		}
		
		[CommandHandler (SourceEditorCommands.ToggleErrorTextMarker)]
		public void OnToggleErrorTextMarker ()
		{
			widget.OnToggleErrorTextMarker ();
		}

		[CommandHandler (EditCommands.IndentSelection)]
		public void IndentSelection ()
		{
			Mono.TextEditor.MiscActions.IndentSelection (widget.TextEditor.GetTextEditorData ());
		}
		
		[CommandHandler (EditCommands.UnIndentSelection)]
		public void UnIndentSelection ()
		{
			Mono.TextEditor.MiscActions.RemoveIndentSelection (widget.TextEditor.GetTextEditorData ());
		}
		
		#endregion
	}
} 
