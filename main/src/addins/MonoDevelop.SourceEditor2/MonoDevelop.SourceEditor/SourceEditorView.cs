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
using MonoDevelop.Ide.CodeFormatting;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using MonoDevelop.SourceEditor.QuickTasks;
using MonoDevelop.Ide.TextEditing;
using System.Text;
using Mono.Addins;

namespace MonoDevelop.SourceEditor
{	
	public class SourceEditorView : AbstractViewContent, IExtensibleTextEditor, IBookmarkBuffer, IClipboardHandler, 
		ICompletionWidget,  ISplittable, IFoldable, IToolboxDynamicProvider, IEncodedTextContent,
		ICustomFilteringToolboxConsumer, IZoomable, ITextEditorResolver, Mono.TextEditor.ITextEditorDataProvider,
		ICodeTemplateHandler, ICodeTemplateContextProvider, ISupportsProjectReload, IPrintable
	{
		readonly SourceEditorWidget widget;
		bool isDisposed = false;
		DateTime lastSaveTimeUtc;
		string loadedMimeType;
		internal object MemoryProbe = Counters.SourceViewsInMemory.CreateMemoryProbe ();
		TextLineMarker currentDebugLineMarker;
		TextLineMarker debugStackLineMarker;
		int lastDebugLine = -1;
		EventHandler currentFrameChanged;
		EventHandler<BreakpointEventArgs> breakpointAdded;
		EventHandler<BreakpointEventArgs> breakpointRemoved;
		EventHandler<BreakpointEventArgs> breakpointStatusChanged;
		List<DocumentLine> breakpointSegments = new List<DocumentLine> ();
		DocumentLine debugStackSegment;
		DocumentLine currentLineSegment;
		List<PinnedWatchInfo> pinnedWatches = new List<PinnedWatchInfo> ();
		bool writeAllowed;
		bool writeAccessChecked;
		
		public Mono.TextEditor.TextDocument Document {
			get {
				return widget.TextEditor.Document;
			}
		}

		public DateTime LastSaveTimeUtc {
			get {
				return lastSaveTimeUtc;
			}
			internal set {
				lastSaveTimeUtc = value;
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
		
		uint autoSaveTimer = 0;

		void InformAutoSave ()
		{
			RemoveAutoSaveTimer ();
			autoSaveTimer = GLib.Timeout.Add (500, delegate {
				AutoSave.InformAutoSaveThread (Document);
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
		
		bool wasEdited = false;
		uint removeMarkerTimeout;
		Queue<MessageBubbleTextMarker> markersToRemove = new Queue<MessageBubbleTextMarker> ();


		void RemoveMarkerQueue ()
		{
			if (removeMarkerTimeout != 0)
				GLib.Source.Remove (removeMarkerTimeout);
		}

		void ResetRemoveMarker ()
		{
			RemoveMarkerQueue ();
			removeMarkerTimeout = GLib.Timeout.Add (2000, delegate {
				while (markersToRemove.Count > 0) {
					var _m = markersToRemove.Dequeue ();
					currentErrorMarkers.Remove (_m);
					widget.TextEditor.Document.RemoveMarker (_m);
				}
				removeMarkerTimeout = 0;
				return false;
			});
		}

		public SourceEditorView ()
		{
			Counters.LoadedEditors++;
			currentFrameChanged = (EventHandler)DispatchService.GuiDispatch (new EventHandler (OnCurrentFrameChanged));
			breakpointAdded = (EventHandler<BreakpointEventArgs>)DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointAdded));
			breakpointRemoved = (EventHandler<BreakpointEventArgs>)DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointRemoved));
			breakpointStatusChanged = (EventHandler<BreakpointEventArgs>)DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointStatusChanged));

			widget = new SourceEditorWidget (this);
			widget.TextEditor.Document.SyntaxModeChanged += delegate(object sender, SyntaxModeChangeEventArgs e) {
				var oldProvider = e.OldMode as IQuickTaskProvider;
				if (oldProvider != null)
					widget.RemoveQuickTaskProvider (oldProvider);
				var newProvider = e.NewMode as IQuickTaskProvider;
				if (newProvider != null)
					widget.AddQuickTaskProvider (newProvider);
			};
			widget.TextEditor.Document.TextReplaced += delegate(object sender, DocumentChangeEventArgs args) {
				if (!inLoad) {
					if (widget.TextEditor.Document.IsInAtomicUndo) {
						wasEdited = true;
					} else {
						InformAutoSave ();
					}
				}
				int startIndex = args.Offset;
				int endIndex = startIndex + Math.Max (args.RemovalLength, args.InsertionLength);
				if (TextChanged != null)
					TextChanged (this, new TextChangedEventArgs (startIndex, endIndex));
				foreach (var marker in currentErrorMarkers) {
					if (marker.LineSegment.Contains (args.Offset) || marker.LineSegment.Contains (args.Offset + args.InsertionLength) || args.Offset < marker.LineSegment.Offset && marker.LineSegment.Offset < args.Offset + args.InsertionLength) {
						markersToRemove.Enqueue (marker);
					}
				}
				ResetRemoveMarker ();
			};
			
			widget.TextEditor.Document.LineChanged += delegate(object sender, LineEventArgs e) {
				UpdateBreakpoints ();
				UpdateWidgetPositions ();
				if (messageBubbleCache != null && messageBubbleCache.RemoveLine (e.Line)) {
					MessageBubbleTextMarker marker = currentErrorMarkers.FirstOrDefault (m => m.LineSegment == e.Line);
					if (marker != null) {

						widget.TextEditor.TextViewMargin.RemoveCachedLine (e.Line); 
						// ensure that the line cache is renewed
						marker.GetLineHeight (widget.TextEditor);
					}
				}
			};
			
			widget.TextEditor.Document.BeginUndo += delegate {
				wasEdited = false;
			};
			
			widget.TextEditor.Document.EndUndo += delegate {
				if (wasEdited)
					InformAutoSave ();
			};
			widget.TextEditor.Document.Undone += (o, a) => AutoSave.InformAutoSaveThread (Document);
			widget.TextEditor.Document.Redone += (o, a) => AutoSave.InformAutoSaveThread (Document);
			
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
			
			this.WorkbenchWindowChanged += HandleWorkbenchWindowChanged;
			this.ContentNameChanged += delegate {
				this.Document.FileName = this.ContentName;
				if (String.IsNullOrEmpty (ContentName) || !File.Exists (ContentName))
					return;
				
				lastSaveTimeUtc = File.GetLastWriteTimeUtc (ContentName);
			};
			ClipbardRingUpdated += UpdateClipboardRing;
			
			TextEditorService.FileExtensionAdded += HandleFileExtensionAdded;
			TextEditorService.FileExtensionRemoved += HandleFileExtensionRemoved;

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
			
			TaskService.Errors.TasksAdded += UpdateTasks;
			TaskService.Errors.TasksRemoved += UpdateTasks;
			TaskService.JumpedToTask += HandleTaskServiceJumpedToTask;
			IdeApp.Preferences.ShowMessageBubblesChanged += HandleIdeAppPreferencesShowMessageBubblesChanged;
			TaskService.TaskToggled += HandleErrorListPadTaskToggled;
			widget.TextEditor.Options.Changed += HandleWidgetTextEditorOptionsChanged;
			IdeApp.Preferences.DefaultHideMessageBubblesChanged += HandleIdeAppPreferencesDefaultHideMessageBubblesChanged;
			Document.AddAnnotation (this);
			FileRegistry.Add (this);
		}

		void HandleFileExtensionRemoved (object sender, FileExtensionEventArgs args)
		{
			if (ContentName == null || args.Extension.File.FullPath != Path.GetFullPath (ContentName))
				return;
			RemoveFileExtension (args.Extension);
		}

		void HandleFileExtensionAdded (object sender, FileExtensionEventArgs args)
		{
			if (ContentName == null || args.Extension.File.FullPath != Path.GetFullPath (ContentName))
				return;
			AddFileExtension (args.Extension);
		}

		Dictionary<TopLevelWidgetExtension,Gtk.Widget> widgetExtensions = new Dictionary<TopLevelWidgetExtension, Widget> ();
		Dictionary<FileExtension,Tuple<TextLineMarker,DocumentLine>> markerExtensions = new Dictionary<FileExtension, Tuple<TextLineMarker,DocumentLine>> ();

		void LoadExtensions ()
		{
			if (ContentName == null)
				return;

			foreach (var ext in TextEditorService.GetFileExtensions (ContentName))
				AddFileExtension (ext);
		}

		void AddFileExtension (FileExtension extension)
		{
			if (extension is TopLevelWidgetExtension) {
				var widgetExtension = (TopLevelWidgetExtension)extension;
				var w = widgetExtension.CreateWidget ();
				int x, y;
				if (!CalcWidgetPosition (widgetExtension, w, out x, out y)) {
					w.Destroy ();
					return;
				}

				widgetExtensions [widgetExtension] = w;
				widget.TextEditor.TextArea.AddTopLevelWidget (w, x, y);
				widgetExtension.ScrollToViewRequested += HandleScrollToViewRequested;
			}
			else if (extension is TextLineMarkerExtension) {
				var lineExt = (TextLineMarkerExtension) extension;

				DocumentLine line = widget.TextEditor.Document.GetLine (lineExt.Line);
				if (line == null)
					return;

				var marker = lineExt.CreateMarker ();
				widget.TextEditor.Document.AddMarker (line, marker);
				widget.TextEditor.QueueDraw ();
				markerExtensions [extension] = new Tuple<TextLineMarker,DocumentLine> (marker, line);
			}
		}

		void HandleScrollToViewRequested (object sender, EventArgs e)
		{
			var widgetExtension = (TopLevelWidgetExtension)sender;
			Gtk.Widget w;
			if (widgetExtensions.TryGetValue (widgetExtension, out w)) {
				int x, y;
				widget.TextEditor.TextArea.GetTopLevelWidgetPosition (w, out x, out y);
				var size = w.SizeRequest ();
				Gtk.Application.Invoke (delegate {
					widget.TextEditor.ScrollTo (new Gdk.Rectangle (x, y, size.Width, size.Height));
				});
			}
		}

		void RemoveFileExtension (FileExtension extension)
		{
			if (extension is TopLevelWidgetExtension) {
				var widgetExtension = (TopLevelWidgetExtension)extension;
				Gtk.Widget w;
				if (!widgetExtensions.TryGetValue (widgetExtension, out w))
					return;
				widgetExtensions.Remove (widgetExtension);
				widget.TextEditor.TextArea.Remove (w);
				w.Destroy ();
				widgetExtension.ScrollToViewRequested -= HandleScrollToViewRequested;
			}
			else if (extension is TextLineMarkerExtension) {
				Tuple<TextLineMarker,DocumentLine> data;
				if (markerExtensions.TryGetValue (extension, out data))
					widget.TextEditor.Document.RemoveMarker (data.Item1);
			}
		}

		void ClearExtensions ()
		{
			foreach (var ex in widgetExtensions.Keys)
				ex.ScrollToViewRequested -= HandleScrollToViewRequested;
		}

		void UpdateWidgetPositions ()
		{
			foreach (var e in widgetExtensions) {
				int x,y;
				if (CalcWidgetPosition ((TopLevelWidgetExtension)e.Key, e.Value, out x, out y))
					widget.TextEditor.TextArea.MoveTopLevelWidget (e.Value, x, y);
				else
					e.Value.Hide ();
			}
		}

		bool CalcWidgetPosition (TopLevelWidgetExtension widgetExtension, Gtk.Widget w, out int x, out int y)
		{
			DocumentLine line = widget.TextEditor.Document.GetLine (widgetExtension.Line);
			if (line == null) {
				x = y = 0;
				return false;
			}

			int lw, lh;
			widget.TextEditor.TextViewMargin.GetLayout (line).Layout.GetPixelSize (out lw, out lh);
			lh = (int) TextEditor.TextViewMargin.GetLineHeight (widgetExtension.Line);
			x = (int)widget.TextEditor.TextViewMargin.XOffset + lw + 4;
			y = (int)widget.TextEditor.LineToY (widgetExtension.Line);
			int lineStart = (int)widget.TextEditor.TextViewMargin.XOffset;
			var size = w.SizeRequest ();

			switch (widgetExtension.HorizontalAlignment) {
			case HorizontalAlignment.LineLeft:
				x = (int)widget.TextEditor.TextViewMargin.XOffset;
				break;
			case HorizontalAlignment.LineRight:
				x = lineStart + lw + 4;
				break;
			case HorizontalAlignment.LineCenter:
				x = lineStart + (lw - size.Width) / 2;
				if (x < lineStart)
					x = lineStart;
				break;
			case HorizontalAlignment.Left:
				x = 0;
				break;
			case HorizontalAlignment.Right:
				break;
			case HorizontalAlignment.Center:
				break;
			case HorizontalAlignment.ViewLeft:
				break;
			case HorizontalAlignment.ViewRight:
				break;
			case HorizontalAlignment.ViewCenter:
				break;
			}

			switch (widgetExtension.VerticalAlignment) {
			case VerticalAlignment.LineTop:
				break; // the default
			case VerticalAlignment.LineBottom:
				y += lh - size.Height;
				break;
			case VerticalAlignment.LineCenter:
				y = y + (lh - size.Height) / 2;
				break;
			case VerticalAlignment.AboveLine:
				y -= size.Height;
				break;
			case VerticalAlignment.BelowLine:
				y += lh;
				break;
			}
			x += widgetExtension.OffsetX;
			y += widgetExtension.OffsetY;
			return true;
		}

		void HandleWorkbenchWindowChanged (object sender, EventArgs e)
		{
			if (WorkbenchWindow != null) {
				widget.TextEditor.ExtensionContext = WorkbenchWindow.ExtensionContext;
				WorkbenchWindow.ActiveViewContentChanged += HandleActiveViewContentChanged;
				this.WorkbenchWindowChanged -= HandleWorkbenchWindowChanged;
			}
		}

		void HandleActiveViewContentChanged (object o, ActiveViewContentEventArgs e)
		{
			widget.UpdateLineCol ();
		}
		
		MessageBubbleHighlightPopupWindow messageBubbleHighlightPopupWindow = null;

		void HandleWidgetTextEditorOptionsChanged (object sender, EventArgs e)
		{
			currentErrorMarkers.ForEach (marker => marker.DisposeLayout ());
		}

		void HandleTaskServiceJumpedToTask (object sender, TaskEventArgs e)
		{
			var task = e.Tasks != null ? e.Tasks.FirstOrDefault () : null;
			var doc = Document;
			if (task == null || doc == null || task.FileName != doc.FileName || this.TextEditor == null)
				return;
			var lineSegment = doc.GetLine (task.Line);
			if (lineSegment == null)
				return;
			var marker = (MessageBubbleTextMarker)lineSegment.Markers.FirstOrDefault (m => m is MessageBubbleTextMarker);
			if (marker == null)
				return;
			
			marker.SetPrimaryError (task.Description);
			
			if (TextEditor != null && TextEditor.IsComposited) {
				if (messageBubbleHighlightPopupWindow != null)
					messageBubbleHighlightPopupWindow.Destroy ();
			/*	messageBubbleHighlightPopupWindow = new MessageBubbleHighlightPopupWindow (this, marker);
				messageBubbleHighlightPopupWindow.Destroyed += delegate {
					messageBubbleHighlightPopupWindow = null;
				};
				messageBubbleHighlightPopupWindow.Popup ();*/
			}
		}

		void HandleIdeAppPreferencesDefaultHideMessageBubblesChanged (object sender, PropertyChangedEventArgs e)
		{
			currentErrorMarkers.ForEach (marker => marker.IsVisible =  !IdeApp.Preferences.DefaultHideMessageBubbles);
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
		
		MessageBubbleCache messageBubbleCache;
		List<MessageBubbleTextMarker> currentErrorMarkers = new List<MessageBubbleTextMarker> ();

		void UpdateTasks (object sender, TaskEventArgs e)
		{
			Task[] tasks = TaskService.Errors.GetFileTasks (ContentName);
			if (tasks == null)
				return;
			DisposeErrorMarkers (); // disposes messageBubbleCache as well.
			if (IdeApp.Preferences.ShowMessageBubbles == ShowMessageBubbles.Never)
				return;
			using (var undo = Document.OpenUndoGroup ()) {
				if (messageBubbleCache != null)
					messageBubbleCache.Dispose ();
				messageBubbleCache = new MessageBubbleCache (widget.TextEditor);
				
				foreach (Task task in tasks) {
					if (task.Severity == TaskSeverity.Error || task.Severity == TaskSeverity.Warning) {
						if (IdeApp.Preferences.ShowMessageBubbles == ShowMessageBubbles.ForErrors && task.Severity == TaskSeverity.Warning)
							continue;
						DocumentLine lineSegment = widget.Document.GetLine (task.Line);
						if (lineSegment == null)
							continue;
						var marker = currentErrorMarkers.FirstOrDefault (m => m.LineSegment == lineSegment);
						if (marker != null) {
							marker.AddError (task.Severity == TaskSeverity.Error, task.Description);
							continue;
						}
						MessageBubbleTextMarker errorTextMarker = new MessageBubbleTextMarker (messageBubbleCache, task, lineSegment, task.Severity == TaskSeverity.Error, task.Description);
						currentErrorMarkers.Add (errorTextMarker);
						
						errorTextMarker.IsVisible =  !IdeApp.Preferences.DefaultHideMessageBubbles;
						widget.Document.AddMarker (lineSegment, errorTextMarker, false);
					}
				}
			}
			widget.TextEditor.QueueDraw ();
		}
		
		void DisposeErrorMarkers ()
		{
			//the window has a reference to the markers we're destroying
			//so if the error markers get cleared out while it's running, its expose will
			//NRE and bring down MD
			if (messageBubbleHighlightPopupWindow != null)
				messageBubbleHighlightPopupWindow.Destroy ();
			
			currentErrorMarkers.ForEach (em => {
				widget.Document.RemoveMarker (em);
				em.Dispose ();
			});
			currentErrorMarkers.Clear ();
			if (messageBubbleCache != null) {
				messageBubbleCache.Dispose ();
				messageBubbleCache = null;
			}
		}
		
		public override void Save (string fileName)
		{
			Save (fileName, this.encoding);
		}

		public void Save (string fileName, Encoding encoding)
		{
			if (!widget.EnsureCorrectEolMarker (fileName))
				return;
			if (widget.HasMessageBar)
				return;
			
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
					string question = GettextCatalog.GetString (
						"This file {0} has been changed outside of {1}. Are you sure you want to overwrite the file?",
						fileName, BrandingService.ApplicationName
					);
					if (MessageService.AskQuestion (question, AlertButton.Cancel, AlertButton.OverwriteFile) != AlertButton.OverwriteFile)
						return;
				}
				warnOverwrite = false;
				widget.RemoveMessageBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			
			if (PropertyService.Get ("AutoFormatDocumentOnSave", false)) {
				var formatter = CodeFormatterService.GetFormatter (Document.MimeType);
				if (formatter != null && formatter.SupportsOnTheFlyFormatting) {
					using (var undo = TextEditor.OpenUndoGroup ()) {
						formatter.OnTheFlyFormat (WorkbenchWindow.Document, 0, Document.TextLength);
						wasEdited = false;
					}
				}
			}

			FileRegistry.SuspendFileWatch = true;
			try {
				object attributes = null;
				if (File.Exists (fileName)) {
					try {
						attributes = DesktopService.GetFileAttributes (fileName);
						var fileAttributes = File.GetAttributes (fileName);
						if (fileAttributes.HasFlag (FileAttributes.ReadOnly)) {
							var result = MessageService.AskQuestion (
								GettextCatalog.GetString ("Can't save file"),
								GettextCatalog.GetString ("The file was marked as read only. Should the file be overwritten?"),
								AlertButton.Yes,
								AlertButton.No);
							if (result == AlertButton.Yes) {
								try {
									File.SetAttributes (fileName, fileAttributes & ~FileAttributes.ReadOnly);
								} catch (Exception) {
									MessageService.ShowError (GettextCatalog.GetString ("Error"),
									                          GettextCatalog.GetString ("Operation failed."));
									return;
								}
							} else {
								return;
							}
						}
					} catch (Exception e) {
						LoggingService.LogWarning ("Can't get file attributes", e);
					}
				}
				try {
					var writeEncoding = encoding;
					var writeBom = hadBom;
					var writeText = Document.Text;
					if (writeEncoding == null) {
						writeEncoding = Encoding.UTF8;
						// Disabled. Shows up in the source control as diff, it's atm confusing for the users to see a change without
						// changed files.
						writeBom = false;
//						writeBom =!Mono.TextEditor.Utils.TextFileUtility.IsASCII (writeText);
					}
					Mono.TextEditor.Utils.TextFileUtility.WriteText (fileName, writeText, writeEncoding, writeBom);
				} catch (InvalidEncodingException) {
					var result = MessageService.AskQuestion (GettextCatalog.GetString ("Can't save file witch current codepage."), 
						GettextCatalog.GetString ("Some unicode characters in this file could not be saved with the current encoding.\nDo you want to resave this file as Unicode ?\nYou can choose another encoding in the 'save as' dialog."),
						1,
						AlertButton.Cancel,
						new AlertButton (GettextCatalog.GetString ("Save as Unicode")));
					if (result != AlertButton.Cancel) {
						this.hadBom = true;
						this.encoding = Encoding.UTF8;
						Mono.TextEditor.Utils.TextFileUtility.WriteText (fileName, Document.Text, encoding, hadBom);
					} else {
						return;
					}
				}
				lastSaveTimeUtc = File.GetLastWriteTimeUtc (fileName);
				try {
					if (attributes != null)
						DesktopService.SetFileAttributes (fileName, attributes);
				} catch (Exception e) {
					LoggingService.LogError ("Can't set file attributes", e);
				}
			} finally {
				FileRegistry.SuspendFileWatch = false;
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
		
		public override void LoadNew (Stream content, string mimeType)
		{
			Document.MimeType = mimeType;
			string text = null;
			if (content != null) {
				text = Mono.TextEditor.Utils.TextFileUtility.GetText (content, out hadBom, out encoding);
				Document.Text = text;
			}
			this.CreateDocumentParsedHandler ();
			RunFirstTimeFoldUpdate (text);
			Document.InformLoadComplete ();
		}
		
		public override void Load (string fileName)
		{
			Load (fileName, null);
		}

		void RunFirstTimeFoldUpdate (string text)
		{
			if (string.IsNullOrEmpty (text)) 
				return;
			ParsedDocument parsedDocument = null;

			var foldingParser = TypeSystemService.GetFoldingParser (Document.MimeType);
			if (foldingParser != null) {
				parsedDocument = foldingParser.Parse (Document.FileName, text);
			} else {
				var normalParser = TypeSystemService.GetParser (Document.MimeType);
				if (normalParser != null) {
					using (var sr = new StringReader (text))
						parsedDocument = normalParser.Parse (true, Document.FileName, sr, null);
				}
			}
			if (parsedDocument != null) 
				widget.UpdateParsedDocument (parsedDocument);
		}

		void CreateDocumentParsedHandler ()
		{
			this.WorkbenchWindowChanged += delegate {
				if (WorkbenchWindow == null)
					return;
				WorkbenchWindow.DocumentChanged +=  delegate {
					if (WorkbenchWindow.Document == null)
						return;
					foreach (var provider in WorkbenchWindow.Document.GetContents<IQuickTaskProvider> ()) {
						widget.AddQuickTaskProvider (provider);
					}
					foreach (var provider in WorkbenchWindow.Document.GetContents<IUsageProvider> ()) {
						widget.AddUsageTaskProvider (provider);
					}
					ownerDocument = WorkbenchWindow.Document;
					ownerDocument.DocumentParsed += HandleDocumentParsed;
				};
			};
		}

		MonoDevelop.Ide.Gui.Document ownerDocument;

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			widget.UpdateParsedDocument (ownerDocument.ParsedDocument);
		}		

		public void Load (string fileName, Encoding loadEncoding)
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
			string text = null;
			bool didLoadCleanly;
			if (AutoSave.AutoSaveExists (fileName)) {
				widget.ShowAutoSaveWarning (fileName);
				this.encoding = loadEncoding;
				didLoadCleanly = false;
			} else {

				inLoad = true;
				if (loadEncoding == null) {
					text = Mono.TextEditor.Utils.TextFileUtility.ReadAllText (fileName, out hadBom, out this.encoding);
				} else {
					text = Mono.TextEditor.Utils.TextFileUtility.ReadAllText (fileName, loadEncoding, out hadBom);
				}
				Document.Text = text;
				inLoad = false;
				didLoadCleanly = true;
			}
			
			// TODO: Would be much easier if the view would be created after the containers.
			CreateDocumentParsedHandler ();

			ContentName = fileName;
			lastSaveTimeUtc = File.GetLastWriteTimeUtc (ContentName);			
			RunFirstTimeFoldUpdate (text);

			widget.TextEditor.Caret.Offset = 0;
			UpdateExecutionLocation ();
			UpdateBreakpoints ();
			UpdatePinnedWatches ();
			LoadExtensions ();
			this.IsDirty = !didLoadCleanly;
			UpdateTasks (null, null);
			widget.TextEditor.VAdjustment.Changed += HandleTextEditorVAdjustmentChanged;
			if (didLoadCleanly)
				Document.InformLoadComplete ();
		}
		
		void HandleTextEditorVAdjustmentChanged (object sender, EventArgs e)
		{
			widget.TextEditor.VAdjustment.Changed -= HandleTextEditorVAdjustmentChanged;
			LoadSettings ();
		}
		
		
		class Settings
		{
			public int CaretOffset { get; set; }

			public double vAdjustment { get; set; }

			public double hAdjustment { get; set; }
			
			public Dictionary<int, bool> FoldingStates = new Dictionary<int, bool> ();
			
			public override string ToString ()
			{
				return string.Format ("[Settings: CaretOffset={0}, vAdjustment={1}, hAdjustment={2}]", CaretOffset, vAdjustment, hAdjustment);
			}
		}
		
		static Dictionary<string, Settings> settingStore = new Dictionary<string, Settings> ();

		internal void LoadSettings ()
		{
			Settings settings;
			if (widget == null || string.IsNullOrEmpty (ContentName) || !settingStore.TryGetValue (ContentName, out settings))
				return;
			
			widget.TextEditor.Caret.Offset = settings.CaretOffset;
			widget.TextEditor.VAdjustment.Value = settings.vAdjustment;
			widget.TextEditor.HAdjustment.Value = settings.hAdjustment;
			
			foreach (var f in widget.TextEditor.Document.FoldSegments) {
				bool isFolded;
				if (settings.FoldingStates.TryGetValue (f.Offset, out isFolded))
					f.IsFolded = isFolded;
			}
		}
		
		internal void StoreSettings ()
		{
			var foldingStates = new Dictionary<int, bool> ();
			foreach (var f in widget.TextEditor.Document.FoldSegments) {
				foldingStates [f.Offset] = f.IsFolded;
			}
			if (string.IsNullOrEmpty (ContentName))
				return;
			settingStore [ContentName] = new Settings () {
				CaretOffset = widget.TextEditor.Caret.Offset,
				vAdjustment = widget.TextEditor.VAdjustment.Value,
				hAdjustment = widget.TextEditor.HAdjustment.Value,
				FoldingStates = foldingStates
			};
		}

		bool warnOverwrite = false;
		bool inLoad = false;
		Encoding encoding = null;
		bool hadBom = false;

		public void Load (string fileName, string content, Encoding encoding)
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
			RunFirstTimeFoldUpdate (content);
			CreateDocumentParsedHandler ();
			UpdateExecutionLocation ();
			UpdateBreakpoints ();
			UpdatePinnedWatches ();
			LoadExtensions ();
			IsDirty = false;
			Document.InformLoadComplete ();
		}
		
		void UpdateMimeType (string fileName)
		{
			// Look for a mime type for which there is a syntax mode
			string mimeType = DesktopService.GetMimeTypeForUri (fileName);
			if (loadedMimeType != mimeType) {
				loadedMimeType = mimeType;
				if (mimeType != null) {
					foreach (string mt in DesktopService.GetMimeTypeInheritanceChain (loadedMimeType)) {
						if (Mono.TextEditor.Highlighting.SyntaxModeService.GetSyntaxMode (null, mt) != null) {
							Document.MimeType = mt;
							widget.TextEditor.TextEditorResolverProvider = TextEditorResolverService.GetProvider (mt);
							break;
						}
					}
				}
				((StyledSourceEditorOptions)SourceEditorWidget.TextEditor.Options).UpdateStyleParent (Project, loadedMimeType);
			}
		}
		
		public Encoding SourceEncoding {
			get { return encoding; }
		}

		public override void Dispose ()
		{
			ClearExtensions ();
			FileRegistry.Remove (this);
			RemoveAutoSaveTimer ();
			
			StoreSettings ();
			
			this.isDisposed = true;
			Counters.LoadedEditors--;
			
			if (messageBubbleHighlightPopupWindow != null)
				messageBubbleHighlightPopupWindow.Destroy ();

			IdeApp.Preferences.DefaultHideMessageBubblesChanged -= HandleIdeAppPreferencesDefaultHideMessageBubblesChanged;
			IdeApp.Preferences.ShowMessageBubblesChanged -= HandleIdeAppPreferencesShowMessageBubblesChanged;
			TaskService.TaskToggled -= HandleErrorListPadTaskToggled;
			
			DisposeErrorMarkers ();
			
			ClipbardRingUpdated -= UpdateClipboardRing;

			widget.TextEditor.Document.TextReplacing -= OnTextReplacing;
			widget.TextEditor.Document.TextReplacing -= OnTextReplaced;
			widget.TextEditor.Document.ReadOnlyCheckDelegate = null;
			widget.TextEditor.Options.Changed -= HandleWidgetTextEditorOptionsChanged;

			TextEditorService.FileExtensionAdded -= HandleFileExtensionAdded;
			TextEditorService.FileExtensionRemoved -= HandleFileExtensionRemoved;

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
			
			TaskService.Errors.TasksAdded -= UpdateTasks;
			TaskService.Errors.TasksRemoved -= UpdateTasks;
			TaskService.Errors.TasksChanged -= UpdateTasks;
			TaskService.JumpedToTask -= HandleTaskServiceJumpedToTask;
			
			// This is not necessary but helps when tracking down memory leaks
			
			debugStackLineMarker = null;
			currentDebugLineMarker = null;
			
			currentFrameChanged = null;
			breakpointAdded = null;
			breakpointRemoved = null;
			breakpointStatusChanged = null;

			if (ownerDocument != null) {
				ownerDocument.DocumentParsed -= HandleDocumentParsed;
				ownerDocument = null;
			}

			RemoveMarkerQueue ();
		}
		
		public Ambience GetAmbience ()
		{
			string file = this.IsUntitled ? this.UntitledName : this.ContentName;
			return AmbienceService.GetAmbienceForFile (file);
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
		
		void OnTextReplacing (object s, DocumentChangeEventArgs a)
		{
			oldReplaceText = a.RemovedText.Text;
		}
		
		void OnTextReplaced (object s, DocumentChangeEventArgs a)
		{
			this.IsDirty = Document.IsDirty;
			
			DocumentLocation location = Document.OffsetToLocation (a.Offset);
			
			int i = 0, lines = 0;
			while (i != -1 && i < oldReplaceText.Length) {
				i = oldReplaceText.IndexOf ('\n', i);
				if (i != -1) {
					lines--;
					i++;
				}
			}

			if (a.InsertedText != null) {
				i = 0;
				string sb = a.InsertedText.Text;
				while (i < sb.Length) {
					if (sb [i] == '\n')
						lines++;
					i++;
				}
			}
			if (lines != 0)
				TextEditorService.NotifyLineCountChanged (this, location.Line, lines, location.Column);
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
			if (!string.IsNullOrEmpty (ContentName) && frame != null && !string.IsNullOrEmpty (frame.SourceLocation.FileName)
				&& ((FilePath)frame.SourceLocation.FileName).FullPath == ((FilePath)ContentName).FullPath)
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
		
		struct PinnedWatchInfo
		{
			public PinnedWatch Watch;
			public DocumentLine Line;
			public PinnedWatchWidget Widget;
//			public DebugValueMarker Marker;
		}
		
		void UpdatePinnedWatches ()
		{
			foreach (PinnedWatchInfo wi in pinnedWatches) {
				widget.TextEditor.Remove (wi.Widget);
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
			DocumentLine line = widget.TextEditor.Document.GetLine (w.Line);
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
			wi.Widget = new PinnedWatchWidget (widget.TextEditor, w);
			
//			wi.Marker = new DebugValueMarker (widget.TextEditor, line, w);
			wi.Watch = w;
			pinnedWatches.Add (wi);
//			if (w.Value != null)
//				wi.Marker.AddValue (w.Value);

			widget.TextEditor.AddTopLevelWidget (wi.Widget, w.OffsetX, w.OffsetY);
			
//			widget.TextEditor.QueueDraw ();
		}

		void OnDebugSessionStarted (object sender, EventArgs e)
		{
			UpdatePinnedWatches ();
			foreach (var marker in currentErrorMarkers) {
				marker.IsVisible = false;
			}
			DebuggingService.DebuggerSession.TargetExited += HandleTargetExited;
		}

		void HandleTargetExited (object sender, EventArgs e)
		{
			foreach (var marker in currentErrorMarkers) {
				marker.IsVisible = true;
			}
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
					widget.TextEditor.Remove (wi.Widget);
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
					widget.TextEditor.MoveTopLevelWidget (wi.Widget, args.Watch.OffsetX, args.Watch.OffsetY);
					break;
				}
			}
		}
		
		void UpdateBreakpoints (bool forceUpdate = false)
		{
			FilePath fp = Name;
			
			if (!forceUpdate) {
				int i = 0, count = 0;
				bool mismatch = false;
				foreach (Breakpoint bp in DebuggingService.Breakpoints.GetBreakpointsAtFile (fp.FullPath)) {
					count++;
					if (i < breakpointSegments.Count) {
						int lineNumber = widget.TextEditor.Document.OffsetToLineNumber (breakpointSegments [i].Offset);
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
			}
			
			HashSet<int> lineNumbers = new HashSet<int> ();
			foreach (DocumentLine line in breakpointSegments) {
				lineNumbers.Add (Document.OffsetToLineNumber (line.Offset));
				widget.TextEditor.Document.RemoveMarker (line, typeof(BreakpointTextMarker));
				widget.TextEditor.Document.RemoveMarker (line, typeof(DisabledBreakpointTextMarker));
				widget.TextEditor.Document.RemoveMarker (line, typeof(InvalidBreakpointTextMarker));
			}
			
			breakpointSegments.Clear ();
			foreach (Breakpoint bp in DebuggingService.Breakpoints.GetBreakpointsAtFile (fp.FullPath)) {
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
				DocumentLine line = widget.TextEditor.Document.GetLine (bp.Line);
				var status = bp.GetStatus (DebuggingService.DebuggerSession);
				
				if (line == null)
					return;
				if (!bp.Enabled) {
					if (bp.HitAction == HitAction.Break)
						widget.TextEditor.Document.AddMarker (line, new DisabledBreakpointTextMarker (widget.TextEditor, false));
					else
						widget.TextEditor.Document.AddMarker (line, new DisabledBreakpointTextMarker (widget.TextEditor, true));
				} else if (status == BreakEventStatus.Bound || status == BreakEventStatus.Disconnected) {
					if (bp.HitAction == HitAction.Break)
						widget.TextEditor.Document.AddMarker (line, new BreakpointTextMarker (widget.TextEditor, false));
					else
						widget.TextEditor.Document.AddMarker (line, new BreakpointTextMarker (widget.TextEditor, true));
				} else
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
					UpdateBreakpoints (true);
				return false;
			});
		}
		
		void OnIconButtonPress (object s, MarginMouseEventArgs args)
		{
			if (args.LineNumber < DocumentLocation.MinLine)
				return;

			if (args.TriggersContextMenu ()) {
				if (TextEditor.Caret.Line != args.LineNumber) {
					TextEditor.Caret.Line = args.LineNumber;
					TextEditor.Caret.Column = 1;
				}

				IdeApp.CommandService.ShowContextMenu (
					TextEditor,
					args.RawEvent as Gdk.EventButton,
					WorkbenchWindow.ExtensionContext ?? AddinManager.AddinEngine,
					"/MonoDevelop/SourceEditor2/IconContextMenu/Editor");
			} else if (args.Button == 1) {
				if (!string.IsNullOrEmpty (this.Document.FileName)) {
					if (args.LineSegment != null) {
						int column = TextEditor.Caret.Line == args.LineNumber ? TextEditor.Caret.Column : 1;

						DebuggingService.Breakpoints.Toggle (this.Document.FileName, args.LineNumber, column);
					}
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
				if (widget == null)
					return false;
				return /*this.TextEditor.PreeditOffset < 0 &&*/ this.Document.CanUndo && widget.EditorHasFocus;
			}
		}
		
		public void Undo ()
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
			if (MiscActions.CancelPreEditMode (TextEditor.GetTextEditorData ()))
				return;
			MiscActions.Undo (this.TextEditor.GetTextEditorData ());
		}
		
		public bool EnableRedo {
			get {
				if (widget == null)
					return false;
				return /*this.TextEditor.PreeditOffset < 0 && */ this.Document.CanRedo && widget.EditorHasFocus;
			}
		}

		public void SetCaretTo (int line, int column)
		{
			this.Document.RunWhenLoaded (() => widget.TextEditor.SetCaretTo (line, column, true));
		}

		public void SetCaretTo (int line, int column, bool highlight)
		{
			this.Document.RunWhenLoaded (() => widget.TextEditor.SetCaretTo (line, column, highlight));
		}
		
		public void SetCaretTo (int line, int column, bool highlight, bool centerCaret)
		{
			this.Document.RunWhenLoaded (() => widget.TextEditor.SetCaretTo (line, column, highlight, centerCaret));
		}

		public void Redo ()
		{
			if (MiscActions.CancelPreEditMode (TextEditor.GetTextEditorData ()))
				return;
			MiscActions.Redo (this.TextEditor.GetTextEditorData ());
		}
		
		public IDisposable OpenUndoGroup ()
		{
			return Document.OpenUndoGroup ();
		}
		
		public string SelectedText { 
			get {
				return TextEditor.IsSomethingSelected ? Document.GetTextAt (TextEditor.SelectionRange) : "";
			}
			set {
				TextEditor.DeleteSelectedText ();
				var offset = TextEditor.Caret.Offset;
				int length = TextEditor.Insert (offset, value);
				TextEditor.SelectionRange = new TextSegment (offset, length);
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
			TextEditor.SelectionRange = new TextSegment (startPosition, endPosition - startPosition);
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
				return this.widget.TextEditor.Document.TextLength;
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
			var doc = widget.TextEditor.Document;
			if (startPosition < 0 ||  endPosition < 0 ||  startPosition > endPosition || startPosition >= doc.TextLength)
				return "";
			var length = Math.Min (endPosition - startPosition, doc.TextLength - startPosition);
			return doc.GetTextAt (startPosition, length);
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
			line = location.Line;
			column = location.Column;
		}
		#endregion
		
		#region IEditableTextFile
		public int InsertText (int position, string text)
		{
			return this.widget.TextEditor.Insert (position, text);
		}

		public void DeleteText (int position, int length)
		{
			this.widget.TextEditor.Remove (position, length);
		}
		#endregion 
		
		#region IBookmarkBuffer
		DocumentLine GetLine (int position)
		{
			DocumentLocation location = Document.OffsetToLocation (position);
			return Document.GetLine (location.Line);
		}
				
		public void SetBookmarked (int position, bool mark)
		{
			DocumentLine line = GetLine (position);
			if (line != null && line.IsBookmarked != mark) {
				int lineNumber = widget.TextEditor.Document.OffsetToLineNumber (line.Offset);
				line.IsBookmarked = mark;
				widget.TextEditor.Document.RequestUpdate (new LineUpdate (lineNumber));
				widget.TextEditor.Document.CommitDocumentUpdate ();
			}
		}
		
		public bool IsBookmarked (int position)
		{
			DocumentLine line = GetLine (position);
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
				if (widget.SearchWidgetHasFocus)
					return false;
				return widget.TextEditor.HasFocus || widget.TextEditor.IsSomethingSelected;
			}
		}

		public bool EnableCopy {
			get {
				return EnableCut;
			}
		}

		public bool EnablePaste {
			get {
				return EnableCut;
			}
		}

		public bool EnableDelete {
			get {
				return EnableCut;
			}
		}

		public bool EnableSelectAll {
			get {
				return EnableCut;
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
				return Document.TextLength;
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
		
		public int CaretOffset {
			get {
				return TextEditor.Caret.Offset;
			}
		}
		
		public Gtk.Style GtkStyle { 
			get {
				return widget.Vbox.Style.Copy ();
			}
		}

		public void Replace (int offset, int count, string text)
		{
			widget.TextEditor.GetTextEditorData ().Replace (offset, count, text);
		}
		
		public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset)
		{
			CodeCompletionContext result = new CodeCompletionContext ();
			result.TriggerOffset = triggerOffset;
			DocumentLocation loc = widget.TextEditor.Caret.Location;
			result.TriggerLine = loc.Line;
			result.TriggerLineOffset = loc.Column - 1;
/*			var p = new Cairo.Point ((int)this.widget.TextEditor.TextViewMargin.CaretVisualLocation.X,
			                         (int)this.widget.TextEditor.TextViewMargin.CaretVisualLocation.Y);
			if (widget.TextEditor.Caret.Location.Column == loc.Column)*/
			var p = this.widget.TextEditor.LocationToPoint (loc);
			int tx, ty;
			widget.TextEditor.ParentWindow.GetOrigin (out tx, out ty);
			tx += widget.TextEditor.Allocation.X + (int)p.X;
			ty += widget.TextEditor.Allocation.Y + (int)p.Y + (int)TextEditor.LineHeight;

			result.TriggerXCoord = tx;
			result.TriggerYCoord = ty;
			result.TriggerTextHeight = (int)TextEditor.GetLineHeight (loc.Line);
			return result;
		}
		
		public Gdk.Point DocumentToScreenLocation (DocumentLocation location)
		{
			var p = widget.TextEditor.LocationToPoint (location);
			int tx, ty;
			widget.Vbox.ParentWindow.GetOrigin (out tx, out ty);
			tx += widget.TextEditor.Allocation.X + p.X;
			ty += widget.TextEditor.Allocation.Y + p.Y + (int)TextEditor.LineHeight;
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

		public static void SetCompletionText (TextEditorData data, CodeCompletionContext ctx, string partial_word, string complete_word, int wordOffset)
		{
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
			}
			
			triggerOffset += data.EnsureCaretIsNotVirtual ();
			if (blockMode) {
				using (var undo = data.OpenUndoGroup ()) {

					int minLine = data.MainSelection.MinLine;
					int maxLine = data.MainSelection.MaxLine;
					int column = triggerOffset - data.Document.GetLineByOffset (triggerOffset).Offset;
					for (int lineNumber = minLine; lineNumber <= maxLine; lineNumber++) {
						DocumentLine lineSegment = data.Document.GetLine (lineNumber);
						if (lineSegment == null)
							continue;
						int offset = lineSegment.Offset + column;
						data.Replace (offset, length, complete_word);
					}
					int minColumn = System.Math.Min (data.MainSelection.Anchor.Column, data.MainSelection.Lead.Column);
					data.MainSelection.Anchor = new DocumentLocation (data.Caret.Line == minLine ? maxLine : minLine, minColumn);
					data.MainSelection.Lead = data.Caret.Location;
					
					data.Document.CommitMultipleLineUpdate (data.MainSelection.MinLine, data.MainSelection.MaxLine);
					data.Caret.PreserveSelection = false;
				}
			} else {
				data.Replace (triggerOffset, length, complete_word);
			}
			
			data.Document.CommitLineUpdate (data.Caret.Line);
			if (idx >= 0)
				data.Caret.Offset = triggerOffset + idx;

		}

		public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int wordOffset)
		{
			var data = GetTextEditorData ();
			if (data == null)
				return;
			using (var undo = data.OpenUndoGroup ()) {
				SetCompletionText (data, ctx, partial_word, complete_word, wordOffset);
				var formatter = CodeFormatterService.GetFormatter (data.MimeType);
				if (formatter != null && complete_word.IndexOfAny (new [] {' ', '\t', '{', '}'}) > 0 && formatter.SupportsOnTheFlyFormatting) {
					formatter.OnTheFlyFormat (WorkbenchWindow.Document, ctx.TriggerOffset, ctx.TriggerOffset + complete_word.Length);
				}
			}
		}
		
		internal void FireCompletionContextChanged ()
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
			string expression = "";
			if (TextEditor.IsSomethingSelected)
				expression = TextEditor.SelectedText;
			else {
				DomRegion region;
				var rr = TextEditor.GetLanguageItem (TextEditor.Caret.Offset, out region);
				if (rr != null && !rr.IsError)
					expression = TextEditor.GetTextBetween (region.Begin, region.End);
			}
			if (!string.IsNullOrEmpty (expression))
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
				if (segment.FoldingType == FoldingType.TypeMember || segment.FoldingType == FoldingType.Comment)
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
			CodeSegmentPreviewWindow.CodeSegmentPreviewInformString = GettextCatalog.GetString ("Press 'F2' for focus");
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
					string line = lines [i];
					if (line.Length > 16)
						line = line.Substring (0, 16) + "...";
					item.Description += line;
				}
				item.Category = GettextCatalog.GetString ("Clipboard ring");
				item.Icon = DesktopService.GetPixbufForFile ("a.txt", Gtk.IconSize.Menu);
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
		
		void HandleDragEnd (object o, DragEndArgs args)
		{
			if (customSource != null) {
				customSource.DragDataGet -= HandleDragDataGet;
				customSource.DragEnd -= HandleDragEnd;
				customSource = null;
			}
		}
		
		void HandleDragDataGet (object o, DragDataGetArgs args)
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
			DomRegion region;
			return this.SourceEditorWidget.TextEditor.GetLanguageItem (offset, out region);
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
			var editor = TextEditor;
			if (editor == null)
				return null;
			return editor.GetTextEditorData ();
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
			var formatter = CodeFormatterService.GetFormatter (Document.MimeType);
			if (formatter == null || !formatter.SupportsCorrectingIndent)
				return;
			var policies = Project != null ? Project.Policies : null;
			var editorData = TextEditor.GetTextEditorData ();
			if (TextEditor.IsSomethingSelected) {
				using (var undo = TextEditor.OpenUndoGroup ()) {
					var selection = TextEditor.MainSelection;
					var anchor = selection.GetAnchorOffset (editorData);
					var lead = selection.GetLeadOffset (editorData);
					var version = TextEditor.Document.Version;
					int max = selection.MaxLine;
					for (int i = TextEditor.MainSelection.MinLine; i <= max; i++) {
						formatter.CorrectIndenting (policies, editorData, i);
					}
					editorData.SetSelection (version.MoveOffsetTo (editorData.Document.Version, anchor), version.MoveOffsetTo (editorData.Document.Version, lead));
				}
			} else {
				formatter.CorrectIndenting (policies, editorData, TextEditor.Caret.Line);
			}
		}

		[CommandUpdateHandler (TextEditorCommands.MoveBlockUp)]
		[CommandUpdateHandler (TextEditorCommands.MoveBlockDown)]
		void MoveBlockUpdateHandler (CommandInfo cinfo)
		{
			cinfo.Enabled = widget.EditorHasFocus;
		}

		[CommandHandler (TextEditorCommands.MoveBlockUp)]
		protected void OnMoveBlockUp ()
		{
			using (var undo = TextEditor.OpenUndoGroup ()) {
				TextEditor.RunAction (MiscActions.MoveBlockUp);
				CorrectIndenting ();
			}
		}
		
		[CommandHandler (TextEditorCommands.MoveBlockDown)]
		protected void OnMoveBlockDown ()
		{
			using (var undo = TextEditor.OpenUndoGroup ()) {
				TextEditor.RunAction (MiscActions.MoveBlockDown);
				CorrectIndenting ();
			}
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
			if (widget.TextEditor.IsSomethingSelected) {
				MiscActions.IndentSelection (widget.TextEditor.GetTextEditorData ());
			} else {
				int offset = widget.TextEditor.LocationToOffset (widget.TextEditor.Caret.Line, 1);
				widget.TextEditor.Insert (offset, widget.TextEditor.Options.IndentationString);
			}
		}
		
		[CommandHandler (EditCommands.UnIndentSelection)]
		public void UnIndentSelection ()
		{
			Mono.TextEditor.MiscActions.RemoveTab (widget.TextEditor.GetTextEditorData ());
		}
		
		[CommandHandler (EditCommands.InsertGuid)]
		public void InsertGuid ()
		{
			TextEditor.InsertAtCaret (Guid.NewGuid ().ToString ());
		}
		#endregion
	}
} 
