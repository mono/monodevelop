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
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.SourceEditor.QuickTasks;
using MonoDevelop.Ide.TextEditing;
using System.Text;
using Mono.Addins;
using MonoDevelop.Components;
using Mono.TextEditor.Utils;
using MonoDevelop.Core.Text;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.Editor;
using MonoDevelop.SourceEditor.Wrappers;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Immutable;

namespace MonoDevelop.SourceEditor
{	
	partial class SourceEditorView : ViewContent, IBookmarkBuffer, IClipboardHandler, ITextFile,
		ICompletionWidget,  ISplittable, IFoldable, IToolboxDynamicProvider,
		ICustomFilteringToolboxConsumer, IZoomable, ITextEditorResolver, ITextEditorDataProvider,
		ICodeTemplateHandler, ICodeTemplateContextProvider, IPrintable,
	ITextEditorImpl, IEditorActionHost, ITextMarkerFactory, IUndoHandler
	{
		readonly SourceEditorWidget widget;
		bool isDisposed = false;
		DateTime lastSaveTimeUtc;
		internal object MemoryProbe = Counters.SourceViewsInMemory.CreateMemoryProbe ();
		DebugMarkerPair currentDebugLineMarker;
		DebugMarkerPair debugStackLineMarker;
		BreakpointStore breakpoints;
		DebugIconMarker hoverDebugLineMarker;
		static readonly Xwt.Drawing.Image hoverBreakpointIcon = Xwt.Drawing.Image.FromResource (typeof (BreakpointPad), "gutter-breakpoint-disabled-15.png");
		List<DebugMarkerPair> breakpointSegments = new List<DebugMarkerPair> ();
		List<PinnedWatchInfo> pinnedWatches = new List<PinnedWatchInfo> ();
		bool writeAllowed;
		bool writeAccessChecked;

		public ViewContent ViewContent {
			get {
				return this;
			}
		}
		
		public TextDocument Document {
			get {
				return widget?.TextEditor?.Document;
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

		internal ExtensibleTextEditor TextEditor {
			get {
				return widget.TextEditor;
			}
		}
		
		internal SourceEditorWidget SourceEditorWidget {
			get {
				return widget;
			}
		}
		
		public override Control Control {
			get {
				return widget != null ? widget.Vbox : null;
			}
		}
		
		public int LineCount {
			get {
				return Document.LineCount;
			}
		}

		IEnumerable<Ide.Editor.Selection> ITextEditorImpl.Selections { get { return TextEditor.GetTextEditorData ().Selections; } }

		string ITextEditorImpl.ContextMenuPath {
			get { return TextEditor.ContextMenuPath; }
			set { TextEditor.ContextMenuPath = value; }
		}
			
		public override string TabPageLabel {
			get { return GettextCatalog.GetString ("Source"); }
		}

		public override string TabAccessibilityDescription {
			get {
				return GettextCatalog.GetString ("The main source editor");
			}
		}
		

		uint removeMarkerTimeout;
		Queue<MessageBubbleTextMarker> markersToRemove = new Queue<MessageBubbleTextMarker> ();


		void RemoveMarkerQueue ()
		{
			if (removeMarkerTimeout != 0) {
				GLib.Source.Remove (removeMarkerTimeout);
				removeMarkerTimeout = 0;
			}
		}

		void ResetRemoveMarker ()
		{
			RemoveMarkerQueue ();
			removeMarkerTimeout = GLib.Timeout.Add (2000, delegate {
				while (markersToRemove.Count > 0) {
					var _m = markersToRemove.Dequeue ();
					currentErrorMarkers.Remove (_m);
					if (_m.LineSegment != null)
						widget.TextEditor.Document.RemoveMarker (_m);
				}
				removeMarkerTimeout = 0;
				return false;
			});
		}

		bool loadedInCtor = false;
		TextEditorType textEditorType;

		public TextEditorType TextEditorType {
			get {
				return textEditorType;
			}
		}

		public SourceEditorView (TextEditorType textEditorType = TextEditorType.Default) : this(new DocumentAndLoaded(new TextDocument(), true))
		{
			this.textEditorType = textEditorType;
		}

		public SourceEditorView(string fileName, string mimeType, TextEditorType textEditorType = TextEditorType.Default)
			: this(new DocumentAndLoaded(fileName, mimeType))
		{
			this.textEditorType = textEditorType;
			FileRegistry.Add(this);
		}

		public SourceEditorView(IReadonlyTextDocument document, TextEditorType textEditorType = TextEditorType.Default)
			: this(new DocumentAndLoaded(document))
		{
			this.textEditorType = textEditorType;
			if (document != null)
			{
				Document.MimeType = document.MimeType;
				Document.FileName = document.FileName;
			}
			FileRegistry.Add(this);
		}

		private SourceEditorView(DocumentAndLoaded doc)
		{
			Counters.LoadedEditors++;

			widget = new SourceEditorWidget (this, doc.Document);
			loadedInCtor = doc.Loaded;

			widget.TextEditor.Document.TextChanged += HandleTextReplaced;

			widget.TextEditor.Document.BeginUndo += HandleBeginUndo; 
			widget.TextEditor.Document.EndUndo += HandleEndUndo;

			widget.TextEditor.Document.TextChanged += OnTextReplaced;
			widget.TextEditor.Document.ReadOnlyCheckDelegate = CheckReadOnly;

			widget.TextEditor.TextViewMargin.LineShowing += TextViewMargin_LineShowing;
			//			widget.TextEditor.Document.DocumentUpdated += delegate {
			//				this.IsDirty = Document.IsDirty;
			//			};
			
			widget.TextEditor.Caret.PositionChanged += HandlePositionChanged; 
			widget.TextEditor.IconMargin.ButtonPressed += OnIconButtonPress;
			widget.TextEditor.IconMargin.MouseMoved += OnIconMarginMouseMoved;
			widget.TextEditor.IconMargin.MouseLeave += OnIconMarginMouseLeave;
			widget.TextEditor.TextArea.FocusOutEvent += TextArea_FocusOutEvent;
			ClipbardRingUpdated += UpdateClipboardRing;
			
			TextEditorService.FileExtensionAdded += HandleFileExtensionAdded;
			TextEditorService.FileExtensionRemoved += HandleFileExtensionRemoved;

			breakpoints = DebuggingService.Breakpoints;
			DebuggingService.DebugSessionStarted += OnDebugSessionStarted;
			DebuggingService.StoppedEvent += HandleTargetExited;
			DebuggingService.ExecutionLocationChanged += OnExecutionLocationChanged;
			DebuggingService.CurrentFrameChanged += OnCurrentFrameChanged;
			DebuggingService.StoppedEvent += OnCurrentFrameChanged;
			DebuggingService.ResumedEvent += OnCurrentFrameChanged;
			breakpoints.BreakpointAdded += OnBreakpointAdded;
			breakpoints.BreakpointRemoved += OnBreakpointRemoved;
			breakpoints.BreakpointStatusChanged += OnBreakpointStatusChanged;
			breakpoints.BreakpointModified += OnBreakpointStatusChanged;
			DebuggingService.PinnedWatches.WatchAdded += OnWatchAdded;
			DebuggingService.PinnedWatches.WatchRemoved += OnWatchRemoved;
			DebuggingService.PinnedWatches.WatchChanged += OnWatchChanged;
			
			TaskService.Errors.TasksAdded += UpdateTasks;
			TaskService.Errors.TasksRemoved += UpdateTasks;
			TaskService.JumpedToTask += HandleTaskServiceJumpedToTask;
			IdeApp.Preferences.ShowMessageBubbles.Changed += HandleIdeAppPreferencesShowMessageBubblesChanged;
			TaskService.TaskToggled += HandleErrorListPadTaskToggled;
			widget.TextEditor.Options.Changed += HandleWidgetTextEditorOptionsChanged;
			IdeApp.Preferences.DefaultHideMessageBubbles.Changed += HandleIdeAppPreferencesDefaultHideMessageBubblesChanged;
			// Document.AddAnnotation (this);

			Document_MimeTypeChanged(this, EventArgs.Empty);
			widget.TextEditor.Document.MimeTypeChanged += Document_MimeTypeChanged;
		}


		private struct DocumentAndLoaded {
			public readonly TextDocument Document;
			public readonly bool Loaded;

			public DocumentAndLoaded (TextDocument document, bool loaded) {
				this.Document = document;
				this.Loaded = loaded;
			}

			public DocumentAndLoaded (string fileName, string mimeType) {
				if (AutoSave.AutoSaveExists(fileName)) {
					// Don't load the document now, let Load() handle it
					this.Document = new TextDocument();
					this.Document.MimeType = mimeType;
					this.Document.FileName = fileName;

					this.Loaded = false;
				} else {
					this.Document = new TextDocument(fileName, mimeType);

					this.Loaded = true;
				}
			}

			public DocumentAndLoaded (IReadonlyTextDocument document) {
				if (document != null) {
					var textDocument = document as TextDocument;
					if (textDocument != null) {
						this.Document = textDocument;
					} else {
						// Shouldn't need this but a fallback if someone provides their own implementation of IReadonlyTextDocument
						this.Document = new TextDocument(document.Text);
					}
				} else {
					this.Document = new TextDocument();
				}

				this.Loaded = false;
			}
		}

		void Document_MimeTypeChanged (object sender, EventArgs e)
		{
			if (Document.MimeType != null) {
				widget.TextEditor.TextEditorResolverProvider = TextEditorResolverService.GetProvider (Document.MimeType);
			}
		}

		protected override void OnContentNameChanged ()
		{
			Document.FileName = ContentName;
			UpdateMimeType (Document.FileName);
			if (!String.IsNullOrEmpty (ContentName) && File.Exists (ContentName))
				lastSaveTimeUtc = File.GetLastWriteTimeUtc (ContentName);
			base.OnContentNameChanged ();
		}


		void HandleTextReplaced (object sender, TextChangeEventArgs args)
		{
			if (Document.CurrentAtomicUndoOperationType == OperationType.Format)
				return;
			for (int i = 0; i < args.TextChanges.Count; ++i) {
				var change = args.TextChanges[i];
				int startIndex = change.Offset;
				foreach (var marker in currentErrorMarkers) {
					var line = marker.LineSegment;
					if (line == null || line.Contains (change.Offset) || line.Contains (change.Offset + change.InsertionLength) || change.Offset < line.Offset && line.Offset < change.Offset + change.InsertionLength) {
						markersToRemove.Enqueue (marker);
					}
				}
			}
			ResetRemoveMarker ();

			UpdateBreakpoints ();
			UpdateWidgetPositions ();
			/*if (messageBubbleCache != null && messageBubbleCache.RemoveLine (e.Line)) {
				MessageBubbleTextMarker marker = currentErrorMarkers.FirstOrDefault (m => m.LineSegment == e.Line);
				if (marker != null) {
					widget.TextEditor.TextViewMargin.RemoveCachedLine (e.Line);
					// ensure that the line cache is renewed
					marker.GetLineHeight (widget.TextEditor);
				}
			}*/

		}

		void HandleEndUndo (object sender, TextDocument.UndoOperationEventArgs e)
		{
			OnEndUndo (EventArgs.Empty);
			IsDirty = Document.IsDirty;
		}


		void HandleBeginUndo (object sender, EventArgs e)
		{
			OnBeginUndo (EventArgs.Empty);
		}


		void HandlePositionChanged (object sender, DocumentLocationEventArgs e)
		{
			OnCaretPositionSet (EventArgs.Empty);
			FireCompletionContextChanged ();
			OnCaretPositionChanged (EventArgs.Empty);
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

		Dictionary<TopLevelWidgetExtension,Widget> widgetExtensions = new Dictionary<TopLevelWidgetExtension, Widget> ();
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
				Widget w = widgetExtension.CreateWidget ();
				w.SizeAllocated += (o, args) => UpdateWidgetPosition(widgetExtension, w);
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

				var marker = (TextLineMarker)lineExt.CreateMarker ();
				widget.TextEditor.Document.AddMarker (line, marker);
				widget.TextEditor.QueueDraw ();
				markerExtensions [extension] = new Tuple<TextLineMarker, DocumentLine> (marker, line);
			}
		}

		void HandleScrollToViewRequested (object sender, EventArgs e)
		{
			var widgetExtension = (TopLevelWidgetExtension)sender;
			Widget w;
			if (widgetExtensions.TryGetValue (widgetExtension, out w)) {
				int x, y;
				widget.TextEditor.TextArea.GetTopLevelWidgetPosition (w, out x, out y);
				var size = w.SizeRequest ();
				Application.Invoke ((o, args) => {
					widget.TextEditor.ScrollTo (new Gdk.Rectangle (x, y, size.Width, size.Height));
				});
			}
		}

		void RemoveFileExtension (FileExtension extension)
		{
			if (extension is TopLevelWidgetExtension) {
				var widgetExtension = (TopLevelWidgetExtension)extension;
				Widget w;
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
				UpdateWidgetPosition (e.Key, e.Value);
			}
		}

		void UpdateWidgetPosition (TopLevelWidgetExtension widgetExtension, Widget w)
		{
			int x, y;
			if (CalcWidgetPosition(widgetExtension, w, out x, out y))
				widget.TextEditor.TextArea.MoveTopLevelWidget(w, x, y);
			else
				w.Hide();
		}

		bool CalcWidgetPosition (TopLevelWidgetExtension widgetExtension, Widget w, out int x, out int y)
		{
			DocumentLine line = widget.TextEditor.Document.GetLine (widgetExtension.Line);
			if (line == null) {
				x = y = 0;
				return false;
			}

			int lw, lh;
			var tmpWrapper = widget.TextEditor.TextViewMargin.GetLayout (line);
			tmpWrapper.GetPixelSize (out lw, out lh);
			if (tmpWrapper.IsUncached)
				tmpWrapper.Dispose ();
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

			//We don't want Widget to appear outside TextArea(cut off)...
			x = Math.Max (0, x);
			y = Math.Max (0, y);
			return true;
		}

		protected override void OnWorkbenchWindowChanged ()
		{
			base.OnWorkbenchWindowChanged ();
			if (WorkbenchWindow != null)
				WorkbenchWindow.ActiveViewContentChanged += HandleActiveViewContentChanged;
		}

		void HandleActiveViewContentChanged (object o, ActiveViewContentEventArgs e)
		{
			widget.UpdateLineCol ();
		}
		
	//	MessageBubbleHighlightPopupWindow messageBubbleHighlightPopupWindow = null;

		void HandleWidgetTextEditorOptionsChanged (object sender, EventArgs e)
		{
			currentErrorMarkers.ForEach (marker => marker.DisposeLayout ());
		}

		void HandleTaskServiceJumpedToTask (object sender, TaskEventArgs e)
		{
			var task = e.Tasks != null ? e.Tasks.FirstOrDefault () : null;
			var doc = Document;
			if (task == null || doc == null || task.FileName != doc.FileName || TextEditor == null)
				return;
			var lineSegment = doc.GetLine (task.Line);
			if (lineSegment == null)
				return;
			var marker = (MessageBubbleTextMarker)doc.GetMarkers (lineSegment).FirstOrDefault (m => m is MessageBubbleTextMarker);
			if (marker == null)
				return;
			
			marker.SetPrimaryError (task);
			
			if (TextEditor != null && TextEditor.IsComposited) {
				/*if (messageBubbleHighlightPopupWindow != null)
					messageBubbleHighlightPopupWindow.Destroy ();*/
			/*	messageBherbbleHighlightPopupWindow = new MessageBubbleHighlightPopupWindow (this, marker);
				messageBubbleHighlightPopupWindow.Destroyed += delegate {
					messageBubbleHighlightPopupWindow = null;
				};
				messageBubbleHighlightPopupWindow.Popup ();*/
			}
		}

		void HandleIdeAppPreferencesDefaultHideMessageBubblesChanged (object sender, EventArgs e)
		{
			currentErrorMarkers.ForEach (marker => marker.IsVisible =  !IdeApp.Preferences.DefaultHideMessageBubbles);
			TextEditor.QueueDraw ();
		}

		void HandleIdeAppPreferencesShowMessageBubblesChanged (object sender, EventArgs e)
		{
			UpdateTasks (null, null);
		}

		void HandleErrorListPadTaskToggled (object sender, TaskEventArgs e)
		{
			TextEditor.QueueDraw ();
		}
		
		MessageBubbleCache messageBubbleCache;
		List<MessageBubbleTextMarker> currentErrorMarkers = new List<MessageBubbleTextMarker> ();
		CancellationTokenSource messageBubbleUpdateSource = new CancellationTokenSource ();

		void UpdateTasks (object sender, TaskEventArgs e)
		{
			TaskListEntry [] tasks = TaskService.Errors.GetFileTasks (ContentName);
			if (tasks == null)
				return;
			DisposeErrorMarkers (); // disposes messageBubbleCache as well.
			if (IdeApp.Preferences.ShowMessageBubbles == ShowMessageBubbles.Never)
				return;
			messageBubbleCache = new MessageBubbleCache (widget.TextEditor);
			CancelMessageBubbleUpdate ();
			var token = messageBubbleUpdateSource;

			Task.Run (delegate {
				var errorMarkers = new List<MessageBubbleTextMarker> ();
				foreach (TaskListEntry task in tasks) {
					if (token.IsCancellationRequested)
						return null;
					if (task.Severity == TaskSeverity.Error || task.Severity == TaskSeverity.Warning) {
						if (IdeApp.Preferences.ShowMessageBubbles == ShowMessageBubbles.ForErrors && task.Severity == TaskSeverity.Warning)
							continue;
						task.Completed = IdeApp.Preferences.DefaultHideMessageBubbles;
						var errorTextMarker = new MessageBubbleTextMarker (messageBubbleCache, task, task.Severity == TaskSeverity.Error, task.Description);
						errorMarkers.Add (errorTextMarker);
					}
				}
				return errorMarkers;
			}).ContinueWith (t => {
				if (token.IsCancellationRequested)
					return;
				Application.Invoke ((o, args) => {
					if (token.IsCancellationRequested)
						return;
					var newErrorMarkers = new List<MessageBubbleTextMarker> ();
					foreach (var marker in t.Result) {
						if (token.IsCancellationRequested)
							return;
						var lineSegment = widget.Document.GetLine (marker.Task.Line);
						if (lineSegment == null)
							continue;
						var oldMarker = widget.Document.GetMarkers (lineSegment).OfType<MessageBubbleTextMarker> ().FirstOrDefault ();
						if (oldMarker != null) {
							oldMarker.AddError (marker.Task, marker.Task.Severity == TaskSeverity.Error, marker.Task.Description);
						} else {
							widget.Document.AddMarker (lineSegment, marker, false, 0);
							newErrorMarkers.Add (marker);
						}
					}
					this.currentErrorMarkers = newErrorMarkers;
				});
			});
		}

		void CancelMessageBubbleUpdate ()
		{
			messageBubbleUpdateSource.Cancel ();
			messageBubbleUpdateSource = new CancellationTokenSource ();
		}

		void DisposeErrorMarkers ()
		{
			//the window has a reference to the markers we're destroying
			//so if the error markers get cleared out while it's running, its expose will
			//NRE and bring down MD
			/*if (messageBubbleHighlightPopupWindow != null)
				messageBubbleHighlightPopupWindow.Destroy ();*/

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

		protected virtual string ProcessSaveText (string text)
		{
			return text;
		}
		
		public override Task Save (FileSaveInformation fileSaveInformation)
		{
			return Save (fileSaveInformation.FileName, fileSaveInformation.Encoding);
		}

		public async Task Save (string fileName, Encoding encoding)
		{
			if (widget.HasMessageBar)
				return;
			if (encoding != null) {
				this.Document.VsTextDocument.Encoding = encoding;
			}
			if (ContentName != fileName) {
				FileService.RequestFileEdit ((FilePath) fileName);
				writeAllowed = true;
				writeAccessChecked = true;
			}

			if (warnOverwrite) {
				if (string.Equals (fileName, ContentName, FilePath.PathComparison)) {
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
				try {
					var formatter = CodeFormatterService.GetFormatter (Document.MimeType);
					if (formatter != null) {
						var document = WorkbenchWindow.Document;
						if (formatter.SupportsOnTheFlyFormatting) {
							using (var undo = TextEditor.OpenUndoGroup ()) {
								formatter.OnTheFlyFormat (WorkbenchWindow.Document.Editor, WorkbenchWindow.Document);
							}
						} else {
							var text = document.Editor.Text;
							var policies = document.Project != null ? document.Project.Policies : PolicyService.DefaultPolicies;
							string formattedText = formatter.FormatText (policies, text);
							if (formattedText != null && formattedText != text) {
								document.Editor.ReplaceText (0, text.Length, formattedText);
							}
						}
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error while formatting on save", e);
				}
			}

			FileRegistry.SkipNextChange (fileName);
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
					MonoDevelop.Core.Text.TextFileUtility.WriteText (fileName, Document);
				} catch (InvalidEncodingException) {
					var result = MessageService.AskQuestion (GettextCatalog.GetString ("Can't save file with current codepage."), 
						GettextCatalog.GetString ("Some unicode characters in this file could not be saved with the current encoding.\nDo you want to resave this file as Unicode ?\nYou can choose another encoding in the 'save as' dialog."),
						1,
						AlertButton.Cancel,
						new AlertButton (GettextCatalog.GetString ("Save as Unicode")));
					if (result != AlertButton.Cancel) {
						this.Document.VsTextDocument.Encoding = Encoding.UTF8;
						MonoDevelop.Core.Text.TextFileUtility.WriteText (fileName, Document);
					}
					else {
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
			} catch (UnauthorizedAccessException e) {
				LoggingService.LogError ("Error while saving file", e);
				MessageService.ShowError (GettextCatalog.GetString ("Can't save file - access denied"), e.Message);
			}

			//			if (encoding != null)
			//				se.Buffer.SourceEncoding = encoding;
			//			TextFileService.FireCommitCountChanges (this);
			await Runtime.RunInMainThread (delegate {
				Document.FileName = ContentName = fileName;
				if (Document != null) {
					UpdateMimeType (fileName);
					Document.SetNotDirtyState ();
				}
				IsDirty = false;
			});
		}
		
		public void InformLoadComplete ()
		{
		/*
			Document.MimeType = mimeType;
			string text = null;
			if (content != null) {
				text = Mono.TextEditor.Utils.TextFileUtility.GetText (content, out encoding, out hadBom);
				text = ProcessLoadText (text);
				Document.Text = text;
			}
			this.CreateDocumentParsedHandler ();
			RunFirstTimeFoldUpdate (text);
			*/
			Document.InformLoadComplete ();
		}
		
		public override Task LoadNew (Stream content, string mimeType)
		{
			throw new NotSupportedException ("Moved to TextEditorViewContent.LoadNew.");
		}
		
		public override Task Load (FileOpenInformation fileOpenInformation)
		{
			return Load (fileOpenInformation.FileName, fileOpenInformation.Encoding, fileOpenInformation.IsReloadOperation);
		}

		protected virtual string ProcessLoadText (string text)
		{
			return text;
		}

		class MyExtendingLineMarker : TextLineMarker, IExtendingTextLineMarker
		{
			public bool IsSpaceAbove {
				get {
					return true;
				}
			}

			public void Draw (MonoTextEditor editor, Cairo.Context g, int lineNr, Cairo.Rectangle lineArea)
			{
				using (var layout = new Pango.Layout (editor.PangoContext)) {
					g.Save ();
					editor.EditorTheme.TryGetColor (EditorThemeColors.Foreground, out HslColor color);
					g.SetSourceColor (color);
					g.Translate (lineArea.X, lineArea.Y - editor.LineHeight * 2);
					layout.SetText ("Line " + lineNr);
					g.ShowLayout (layout);
					g.Restore ();
				}

			}

			public double GetLineHeight (MonoTextEditor editor)
			{
				return editor.LineHeight *  3;
			}
		}

		public Task Load(string fileName, Encoding loadEncoding, bool reload = false)
		{
			var document = Document;
			if (document == null)
				return TaskUtil.Default<object>();
			document.TextChanged -= OnTextReplaced;
			
			if (warnOverwrite) {
				warnOverwrite = false;
				widget.RemoveMessageBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			// Look for a mime type for which there is a syntax mode
			bool didLoadCleanly;

			if (this.loadedInCtor) {
				this.loadedInCtor = false;
				didLoadCleanly = true;
			} else {
				if (!reload && AutoSave.AutoSaveExists(fileName)) {
					widget.ShowAutoSaveWarning(fileName);
					this.Document.VsTextDocument.Encoding = loadEncoding ?? Encoding.UTF8;
					didLoadCleanly = false;
				} else {

					UpdateMimeType(fileName);

					string text = null;
					if (loadEncoding == null) {
						text = MonoDevelop.Core.Text.TextFileUtility.ReadAllText(fileName, out loadEncoding);
					} else {
						text = MonoDevelop.Core.Text.TextFileUtility.ReadAllText(fileName, loadEncoding);
					}
					this.Document.VsTextDocument.Encoding = loadEncoding;

					text = ProcessLoadText(text);
					document.IsTextSet = false;
					if (reload) {
						document.ReplaceText(0, Document.Length, text);
						document.DiffTracker.Reset();
					} else {
						document.Text = text;
						document.DiffTracker.SetBaseDocument(Document.CreateDocumentSnapshot());
					}

					didLoadCleanly = true;
				}
			}

			// TODO: Would be much easier if the view would be created after the containers.
			ContentName = fileName;
			lastSaveTimeUtc = File.GetLastWriteTimeUtc (ContentName);
			widget.TextEditor.Caret.Offset = 0;
			UpdateExecutionLocation ();
			UpdateBreakpoints ();
			UpdatePinnedWatches ();
			LoadExtensions ();
			IsDirty = !didLoadCleanly;
			UpdateTasks (null, null);
			widget.TextEditor.TextArea.SizeAllocated += HandleTextEditorVAdjustmentChanged;
			if (didLoadCleanly) {
				widget.EnsureCorrectEolMarker (fileName);
			}

			document.TextChanged += OnTextReplaced;
			//document.AddMarker (5, new MyExtendingLineMarker ());
			//document.AddMarker (7, new MyExtendingLineMarker ());
			//document.AddMarker (10, new MyExtendingLineMarker ());

			return TaskUtil.Default<object>();
		}

		void HandleTextEditorVAdjustmentChanged (object sender, EventArgs e)
		{
			widget.TextEditor.TextArea.SizeAllocated -= HandleTextEditorVAdjustmentChanged;
			LoadSettings ();
		}

		internal void LoadSettings ()
		{
			MonoDevelop.Ide.Editor.FileSettingsStore.Settings settings;
			if (widget == null || string.IsNullOrEmpty (ContentName) || !MonoDevelop.Ide.Editor.FileSettingsStore.TryGetValue (ContentName, out settings))
				return;
			
			widget.TextEditor.Caret.Offset = settings.CaretOffset;
			widget.TextEditor.VAdjustment.Value = settings.vAdjustment;
			widget.TextEditor.HAdjustment.Value = settings.hAdjustment;
			
//			foreach (var f in widget.TextEditor.Document.FoldSegments) {
//				bool isFolded;
//				if (settings.FoldingStates.TryGetValue (f.Offset, out isFolded))
//					f.IsFolded = isFolded;
//			}
		}
		
		internal void StoreSettings ()
		{
//			var foldingStates = new Dictionary<int, bool> ();
//			foreach (var f in widget.TextEditor.Document.FoldSegments) {
//				foldingStates [f.Offset] = f.IsFolded;
//			}
			if (string.IsNullOrEmpty (ContentName))
				return;
			MonoDevelop.Ide.Editor.FileSettingsStore.Store (ContentName, new MonoDevelop.Ide.Editor.FileSettingsStore.Settings {
				CaretOffset = widget.TextEditor.Caret.Offset,
				vAdjustment = widget.TextEditor.VAdjustment.Value,
				hAdjustment = widget.TextEditor.HAdjustment.Value//,
//				FoldingStates = foldingStates
			});
		}

		bool warnOverwrite = false;

		internal void ReplaceContent (string fileName, string content, Encoding enc)
		{
			if (warnOverwrite) {
				warnOverwrite = false;
				widget.RemoveMessageBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			UpdateMimeType (fileName);
			
			Document.ReplaceText (0, Document.Length, content);
			Document.DiffTracker.Reset ();
			ContentName = fileName;
			UpdateExecutionLocation ();
			UpdateBreakpoints ();
			UpdatePinnedWatches ();
			LoadExtensions ();
			IsDirty = false;
			this.Document.VsTextDocument.Encoding = enc;
			InformLoadComplete ();
		}
	
		void UpdateMimeType (string fileName)
		{
			Document.MimeType = DesktopService.GetMimeTypeForUri (fileName);
		}
		
		public Encoding SourceEncoding {
			get { return this.Document.VsTextDocument.Encoding; }
		}

		public override void Dispose ()
		{
			if (isDisposed)
				return;
			isDisposed = true;

			CancelMessageBubbleUpdate ();
			ClearExtensions ();
			FileRegistry.Remove (this);

			StoreSettings ();
			
			Counters.LoadedEditors--;
			
		/*	if (messageBubbleHighlightPopupWindow != null)
				messageBubbleHighlightPopupWindow.Destroy ();*/

			IdeApp.Preferences.DefaultHideMessageBubbles.Changed -= HandleIdeAppPreferencesDefaultHideMessageBubblesChanged;
			IdeApp.Preferences.ShowMessageBubbles.Changed -= HandleIdeAppPreferencesShowMessageBubblesChanged;
			TaskService.TaskToggled -= HandleErrorListPadTaskToggled;
			
			DisposeErrorMarkers ();
			
			ClipbardRingUpdated -= UpdateClipboardRing;

			widget.TextEditor.Document.TextChanged -= HandleTextReplaced;
			widget.TextEditor.Document.BeginUndo -= HandleBeginUndo; 
			widget.TextEditor.Document.EndUndo -= HandleEndUndo;
			widget.TextEditor.Caret.PositionChanged -= HandlePositionChanged; 
			widget.TextEditor.IconMargin.ButtonPressed -= OnIconButtonPress;
			widget.TextEditor.IconMargin.MouseMoved -= OnIconMarginMouseMoved;
			widget.TextEditor.IconMargin.MouseLeave -= OnIconMarginMouseLeave;
			widget.TextEditor.Document.TextChanged -= OnTextReplaced;


			widget.TextEditor.Document.ReadOnlyCheckDelegate = null;
			widget.TextEditor.Options.Changed -= HandleWidgetTextEditorOptionsChanged;
			widget.TextEditor.TextViewMargin.LineShowing -= TextViewMargin_LineShowing;
			widget.TextEditor.TextArea.FocusOutEvent -= TextArea_FocusOutEvent;
			widget.TextEditor.Document.MimeTypeChanged -= Document_MimeTypeChanged;

			TextEditorService.FileExtensionAdded -= HandleFileExtensionAdded;
			TextEditorService.FileExtensionRemoved -= HandleFileExtensionRemoved;

			DebuggingService.ExecutionLocationChanged -= OnExecutionLocationChanged;
			DebuggingService.DebugSessionStarted -= OnDebugSessionStarted;
			DebuggingService.StoppedEvent -= HandleTargetExited;
			DebuggingService.CurrentFrameChanged -= OnCurrentFrameChanged;
			DebuggingService.StoppedEvent -= OnCurrentFrameChanged;
			DebuggingService.ResumedEvent -= OnCurrentFrameChanged;
			breakpoints.BreakpointAdded -= OnBreakpointAdded;
			breakpoints.BreakpointRemoved -= OnBreakpointRemoved;
			breakpoints.BreakpointStatusChanged -= OnBreakpointStatusChanged;
			breakpoints.BreakpointModified -= OnBreakpointStatusChanged;
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

			RemoveMarkerQueue ();
			widget.Dispose ();
			this.Project = null;

			base.Dispose ();
		}

		bool CheckReadOnly (int line)
		{
			if (!writeAccessChecked && !IsUntitled) {
				writeAccessChecked = true;
				try {
					writeAllowed = FileService.RequestFileEdit (ContentName);
				} catch (Exception e) {
					IdeApp.Workbench.StatusBar.ShowError (e.Message); 
					writeAllowed = false;
				}
			}
			return IsUntitled || writeAllowed;
		}

		void OnTextReplaced (object s, TextChangeEventArgs a)
		{
			IsDirty = Document.IsDirty;
			for (int j = 0; j < a.TextChanges.Count; ++j) {
				var change = a.TextChanges[j];
				var location = Document.OffsetToLocation (change.NewOffset);

				int i = 0, lines = 0;
				while (i != -1 && i < change.RemovedText.Text.Length) {
					i = change.RemovedText.Text.IndexOf ('\n', i);
					if (i != -1) {
						lines--;
						i++;
					}
				}

				if (change.InsertedText != null) {
					i = 0;
					string sb = change.InsertedText.Text;
					while (i < sb.Length) {
						if (sb [i] == '\n')
							lines++;
						i++;
					}
				}
				if (lines != 0)
					TextEditorService.NotifyLineCountChanged (this, location.Line, lines, location.Column);
			}
		}

		void OnCurrentFrameChanged (object s, EventArgs args)
		{
			UpdateExecutionLocation ();
			if (!DebuggingService.IsDebugging)
				UpdatePinnedWatches ();
		}

		void OnExecutionLocationChanged (object s, EventArgs args)
		{
			UpdateExecutionLocation ();
		}
		
		void UpdateExecutionLocation ()
		{
			if (currentDebugLineMarker != null || debugStackLineMarker != null) {
				RemoveDebugMarkers ();
				widget.TextEditor.QueueDraw ();
			}
			if (DebuggingService.IsPaused) {
				var location = CheckLocationIsInFile (DebuggingService.NextStatementLocation)
					?? CheckFrameIsInFile (DebuggingService.CurrentFrame)
					?? CheckFrameIsInFile (DebuggingService.GetCurrentVisibleFrame ());
				if (location != null) {
					RemoveDebugMarkers ();
					var segment = widget.TextEditor.Document.GetLine (location.Line);
					if (segment != null) {
						int offset, length;
						if (location.Line > 0 && location.Column > 0 && location.EndLine > 0 && location.EndColumn > 0) {
							offset = widget.TextEditor.LocationToOffset (location.Line, location.Column);
							length = widget.TextEditor.LocationToOffset (location.EndLine, location.EndColumn) - offset;
						} else {
							offset = segment.Offset;
							length = segment.Length;
						}
						if (DebuggingService.CurrentFrameIndex == 0) {
							currentDebugLineMarker = new CurrentDebugLineTextMarker (widget.TextEditor, offset, length);
							currentDebugLineMarker.AddTo (widget.TextEditor.Document, segment);
						} else {
							debugStackLineMarker = new DebugStackLineTextMarker (widget.TextEditor, offset, length);
							debugStackLineMarker.AddTo (widget.TextEditor.Document, segment);
						}
						widget.TextEditor.QueueDraw ();
					}
					return;
				}
			}
		}

		SourceLocation CheckLocationIsInFile (SourceLocation location)
		{
			if (!string.IsNullOrEmpty (ContentName) && location != null && !string.IsNullOrEmpty (location.FileName)
				&& ((FilePath)location.FileName).FullPath == ((FilePath)ContentName).FullPath)
				return location;
			return null;
		}
		
		SourceLocation CheckFrameIsInFile (StackFrame frame)
		{
			return frame != null ? CheckLocationIsInFile (frame.SourceLocation) : null;
		}

		void RemoveDebugMarkers ()
		{
			if (currentDebugLineMarker != null) {
				currentDebugLineMarker.Remove ();
				currentDebugLineMarker = null;
			}
			if (debugStackLineMarker != null) {
				debugStackLineMarker.Remove ();
				debugStackLineMarker = null;
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
				widget.TextEditor.TextArea.Remove (wi.Widget);
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
				var tmpWrapper = widget.TextEditor.TextViewMargin.GetLayout (line);
				tmpWrapper.GetPixelSize (out lw, out lh);
				if (tmpWrapper.IsUncached)
					tmpWrapper.Dispose ();
				w.OffsetX = (int)widget.TextEditor.TextViewMargin.XOffset + lw + 4;
			}
			wi.Widget = new PinnedWatchWidget (widget.TextEditor, w);
			
//			wi.Marker = new DebugValueMarker (widget.TextEditor, line, w);
			wi.Watch = w;
			pinnedWatches.Add (wi);
//			if (w.Value != null)
//				wi.Marker.AddValue (w.Value);

			widget.TextEditor.TextArea.AddTopLevelWidget (wi.Widget, w.OffsetX, w.OffsetY);
			
//			widget.TextEditor.QueueDraw ();
		}

		void OnDebugSessionStarted (object sender, EventArgs e)
		{
			UpdatePinnedWatches ();
			foreach (var marker in currentErrorMarkers) {
				marker.IsVisible = false;
			}
		}

		void HandleTargetExited (object sender, EventArgs e)
		{
			if (DebuggingService.IsDebugging)
				return;
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
					widget.TextEditor.TextArea.Remove (wi.Widget);
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
					widget.TextEditor.TextArea.MoveTopLevelWidget (wi.Widget, args.Watch.OffsetX, args.Watch.OffsetY);
					break;
				}
			}
		}
		
		void UpdateBreakpoints (bool forceUpdate = false)
		{
			var document = widget.TextEditor.Document;
			if (document == null)
				return;
			FilePath fp = Name;
	
			if (!forceUpdate) {
				int i = 0, count = 0;
				bool mismatch = false;

				lock (breakpoints) {
					foreach (var bp in breakpoints.GetBreakpointsAtFile (fp.FullPath)) {
						count++;
						if (i < breakpointSegments.Count) {
							int lineNumber = document.OffsetToLineNumber (breakpointSegments [i].TextMarker.Offset);
							if (lineNumber != bp.Line) {
								mismatch = true;
								break;
							}
							i++;
						}
					}
				}
				
				if (count != breakpointSegments.Count)
					mismatch = true;
				
				if (!mismatch)
					return;
			}
			
			HashSet<int> lineNumbers = new HashSet<int> ();
			foreach (var line in breakpointSegments) {
				if (line == null)
					continue;
				var endLine = document.OffsetToLineNumber (line.TextMarker.EndOffset);
				for (int i = document.OffsetToLineNumber (line.TextMarker.Offset); i <= endLine; i++) {
					lineNumbers.Add (i);
				}
				document.RemoveMarker (line.TextMarker);
				document.RemoveMarker (line.IconMarker);
			}
			
			breakpointSegments.Clear ();

			lock (breakpoints) {
				foreach (Breakpoint bp in breakpoints.GetBreakpointsAtFile (fp.FullPath)) {
					lineNumbers.Add (bp.Line);
					AddBreakpoint (bp);
				}
			}

			foreach (int lineNumber in lineNumbers) {
				document.RequestUpdate (new LineUpdate (lineNumber));
			}
			
			document.CommitDocumentUpdate ();
			
			// Ensure the current line marker is drawn at the top
			UpdateExecutionLocation ();
		}
		
		void AddBreakpoint (Breakpoint bp)
		{
			if (DebuggingService.PinnedWatches.IsWatcherBreakpoint (bp))
				return;
			var textEditor = widget.TextEditor;
			if (textEditor == null)
				return;
			var document = textEditor.Document;
			if (document == null)
				return;

			FilePath fp = Name;
			if (fp.FullPath == bp.FileName) {
				if (bp.Line <= 0 || bp.Line > textEditor.Document.LineCount) {
					return;
				}
				DocumentLine line = document.GetLine (bp.Line);
				var status = DebuggingService.GetBreakpointStatus(bp);
				bool tracepoint = (bp.HitAction & HitAction.Break) == HitAction.None;

				if (line == null)
					return;

				//TODO: 1. When not in debug mode use Microsoft.CodeAnalysis.CSharp.EditAndContinue.BreakpointSpans.TryGetBreakpointSpan
				//TODO: 2. When in debug mode extend Breakpoint class to have endLine and endColumn set if .mdb/.pdb has endLine/endColumn
				var offset = line.Offset;
				var lenght = line.Length;
				DebugMarkerPair marker;
				if (!bp.Enabled) {
					marker = new DisabledBreakpointTextMarker (textEditor, offset, lenght, tracepoint);
				} else if (status == BreakEventStatus.Bound || status == BreakEventStatus.Disconnected) {
					marker = new BreakpointTextMarker (textEditor, offset, lenght, tracepoint);
				} else {
					marker = new InvalidBreakpointTextMarker (textEditor, offset, lenght, tracepoint);
				}

				textEditor.QueueDraw ();
				breakpointSegments.Add (marker);
				marker.AddTo (document, line);
			}
		}
		
		void OnBreakpointAdded (object s, BreakpointEventArgs args)
		{
			if (ContentName == null || args.Breakpoint.FileName != Path.GetFullPath (ContentName))
				return;
			// Updated with a delay, to make sure it works when called as a
			// result of inserting/removing lines before a breakpoint position
			GLib.Timeout.Add (10, delegate {
				// Make sure this runs in the UI thread.
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
				// Make sure this runs in the UI thread.
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
				// Make sure this runs in the UI thread.
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
				if (!string.IsNullOrEmpty (Document.FileName)) {
					if (args.LineSegment != null) {
						int column = TextEditor.Caret.Line == args.LineNumber ? TextEditor.Caret.Column : 1;

						lock (breakpoints)
							breakpoints.Toggle (Document.FileName, args.LineNumber, column);
					}
				}
			}
		}

		void OnIconMarginMouseMoved (object sender, MarginMouseEventArgs e)
		{
			if (hoverDebugLineMarker != null) {
				if (hoverDebugLineMarker.LineSegment?.LineNumber != e.LineSegment?.LineNumber) {
					e.Editor.Document.RemoveMarker (hoverDebugLineMarker);
					hoverDebugLineMarker = null;
				}
			}

			if (hoverDebugLineMarker == null && e.LineSegment != null && e.Editor.Document.GetMarkers (e.LineSegment).FirstOrDefault (m => m is DebugIconMarker) == null) {
				hoverDebugLineMarker = new DebugIconMarker (hoverBreakpointIcon, true) {
					Tooltip = GettextCatalog.GetString ("Insert Breakpoint")
				};
				e.Editor.Document.AddMarker (e.LineSegment.LineNumber, hoverDebugLineMarker);
			}
		}

		void OnIconMarginMouseLeave (object sender, EventArgs e)
		{
			if (hoverDebugLineMarker != null) {
				Document.RemoveMarker (hoverDebugLineMarker);
				hoverDebugLineMarker = null;
			}
		}

		#region IEditableTextBuffer
		public bool EnableUndo {
			get {
				if (widget == null)
					return false;
				return /*this.TextEditor.PreeditOffset < 0 &&*/ Document.CanUndo && widget.EditorHasFocus;
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
			MiscActions.Undo (TextEditor.GetTextEditorData ());
		}
		
		public bool EnableRedo {
			get {
				if (widget == null)
					return false;
				return /*this.TextEditor.PreeditOffset < 0 && */ Document.CanRedo && widget.EditorHasFocus;
			}
		}

		public void SetCaretTo (int line, int column)
		{
			this.Document.RunWhenLoaded (() => {
				PrepareToSetCaret (line, column);
				widget.TextEditor.SetCaretTo (line, column, true);
			});
		}

		public void SetCaretTo (int line, int column, bool highlight)
		{
			this.Document.RunWhenLoaded (() => {
				PrepareToSetCaret (line, column);
				widget.TextEditor.SetCaretTo (line, column, highlight);
			});
		}
		
		public void SetCaretTo (int line, int column, bool highlight, bool centerCaret)
		{
			this.Document.RunWhenLoaded (() => {
				PrepareToSetCaret (line, column);
				widget.TextEditor.SetCaretTo (line, column, highlight, centerCaret);
			});
		}

		protected virtual void PrepareToSetCaret (int line, int column)
		{

		}

		public void Redo ()
		{
			if (MiscActions.CancelPreEditMode (TextEditor.GetTextEditorData ()))
				return;
			MiscActions.Redo (TextEditor.GetTextEditorData ());
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

		public bool HasInputFocus {
			get { return TextEditor.HasFocus; }
		}

		public void RunWhenLoaded (System.Action action)
		{
			Document.RunWhenLoaded (action);
		}

		public void RunWhenRealized (System.Action action)
		{
			Document.RunWhenRealized (action);
		}
		#endregion
		
		public int CursorPosition { 
			get {
				return TextEditor.Caret.Offset;
			}
			set {
				TextEditor.Caret.Offset = value;
			}
		}
		
		#region ITextFile
		public FilePath Name {
			get { 
				return ContentName ?? UntitledName; 
			} 
		}

		public string Text {
			get {
				return widget.TextEditor.Document.Text;
			}
			set {
				this.IsDirty = true;
				var document = this.widget.TextEditor.Document;
				document.ReplaceText (0, document.Length, value);
			}
		}
		
		public int Length { 
			get {
				return widget.TextEditor.Document.Length;
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
			if (startPosition < 0 ||  endPosition < 0 ||  startPosition > endPosition || startPosition >= doc.Length)
				return "";
			var length = Math.Min (endPosition - startPosition, doc.Length - startPosition);
			return doc.GetTextAt (startPosition, length);
		}
		
		public char GetCharAt (int position)
		{
			return widget.TextEditor.Document.GetCharAt (position);
		}
		
		public int GetPositionFromLineColumn (int line, int column)
		{
			return widget.TextEditor.Document.LocationToOffset (new DocumentLocation (line, column));
		}

		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			var location = widget.TextEditor.Document.OffsetToLocation (position);
			line = location.Line;
			column = location.Column;
		}
		#endregion
		
		#region IEditableTextFile
		public int InsertText (int position, string text)
		{
			return widget.TextEditor.Insert (position, text);
		}

		public void DeleteText (int position, int length)
		{
			widget.TextEditor.TextArea.Remove (position, length);
		}
		#endregion 
		
		#region IBookmarkBuffer
		DocumentLine GetLine (int position)
		{
			var location = Document.OffsetToLocation (position);
			return Document.GetLine (location.Line);
		}
				
		public void SetBookmarked (int position, bool mark)
		{
			var line = GetLine (position);
			if (line != null)
				widget.TextEditor.Document.SetIsBookmarked (line, mark);
		}
		
		public bool IsBookmarked (int position)
		{
			var line = GetLine (position);
			return line != null && widget.TextEditor.Document.IsBookmarked (line);
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
				return !widget.SearchWidgetHasFocus;
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
				return Document.Length;
			}
		}

		public int SelectedLength { 
			get {
				if (TextEditor.IsSomethingSelected) {
					if (TextEditor.MainSelection.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Block)
						return Math.Abs (TextEditor.MainSelection.Anchor.Column - TextEditor.MainSelection.Lead.Column);
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
			set {
				TextEditor.Caret.Offset = value;
				TextEditor.ScrollToCaret ();
			}
		}
		
		public Style GtkStyle { 
			get {
				return widget.Vbox.Style;
			}
		}

		public void Replace (int offset, int count, string text)
		{
			widget.TextEditor.GetTextEditorData ().Replace (offset, count, text);
		}
		
		public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset)
		{
			var result = new CodeCompletionContext ();
			if (widget == null)
				return result;
			var editor = widget.TextEditor;
			if (editor == null)
				return result;
			result.TriggerOffset = triggerOffset;
			var loc = editor.Caret.Location;
			result.TriggerLine = loc.Line;
			result.TriggerLineOffset = loc.Column - 1;
			var p = widget.TextEditor.LocationToPoint (loc);
			int tx, ty;
			var parentWindow = editor.ParentWindow;
			if (parentWindow != null) {
				parentWindow.GetOrigin (out tx, out ty);
			} else {
				tx = ty = 0;
			}
			tx += editor.Allocation.X + p.X;
			ty += editor.Allocation.Y + p.Y + (int)editor.LineHeight;

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
		
		public void SetCompletionText (CodeCompletionContext ctx, string partialWord, string completeWord)
		{
			if (ctx == null)
				throw new ArgumentNullException ("ctx");
			if (completeWord == null)
				throw new ArgumentNullException ("completeWord");
			SetCompletionText (ctx, partialWord, completeWord, completeWord.Length);
		}

		public static void SetCompletionText (TextEditorData data, CodeCompletionContext ctx, string partialWord, string completeWord, int wordOffset)
		{
			if (data == null || data.Document == null)
				return;

			int triggerOffset = ctx.TriggerOffset;
			int length = String.IsNullOrEmpty (partialWord) ? 0 : partialWord.Length;

			// for named arguments invoke(arg:<Expr>);
			if (completeWord.EndsWith (":", StringComparison.Ordinal)) {
				if (data.GetCharAt (triggerOffset + length) == ':')
					length++;
			}

			bool blockMode = false;
			if (data.IsSomethingSelected) {
				blockMode = data.MainSelection.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Block;
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
			int idx = completeWord.IndexOf ('|');
			if (idx >= 0) {
				completeWord = completeWord.Remove (idx, 1);
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
						data.Replace (offset, length, completeWord);
					}
					int minColumn = Math.Min (data.MainSelection.Anchor.Column, data.MainSelection.Lead.Column);
					data.MainSelection = data.MainSelection.WithRange (
						new DocumentLocation (data.Caret.Line == minLine ? maxLine : minLine, minColumn),
						data.Caret.Location
					);

					data.Document.CommitMultipleLineUpdate (data.MainSelection.MinLine, data.MainSelection.MaxLine);
					data.Caret.PreserveSelection = false;
				}
			} else {
				data.Replace (triggerOffset, length, completeWord);
			}
			
			data.Document.CommitLineUpdate (data.Caret.Line);
			if (idx >= 0)
				data.Caret.Offset = triggerOffset + idx;

		}

		public void SetCompletionText (CodeCompletionContext ctx, string partialWord, string completeWord, int wordOffset)
		{
			var data = GetTextEditorData ();
			if (data == null)
				return;
			using (var undo = data.OpenUndoGroup ()) {
				SetCompletionText (data, ctx, partialWord, completeWord, wordOffset);
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

		[CommandHandler (DebugCommands.ExpressionEvaluator)]
		protected void ShowExpressionEvaluator ()
		{
			string expression = "";

			if (TextEditor.IsSomethingSelected) {
				expression = TextEditor.SelectedText;
			} else {
				MonoDevelop.Ide.Editor.DocumentRegion region;
				var rr = TextEditor.GetLanguageItem (TextEditor.Caret.Offset, out region);
				if (rr != null)
					expression = TextEditor.GetTextBetween (
						region.BeginLine, region.BeginColumn, 
						region.EndLine, region.EndColumn);
			}

			DebuggingService.ShowExpressionEvaluator (expression);
		}

		[CommandUpdateHandler (DebugCommands.ExpressionEvaluator)]
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
			bool toggle = true;

			foreach (var segment in Document.FoldSegments) {
				if (segment.FoldingType == FoldingType.TypeMember || segment.FoldingType == FoldingType.Comment)
					if (segment.IsCollapsed)
						toggle = false;
			}


			foreach (var segment in Document.FoldSegments) {
				if (segment.FoldingType == FoldingType.TypeDefinition) {
					segment.IsCollapsed = false;
				}
				if (segment.FoldingType == FoldingType.TypeMember || segment.FoldingType == FoldingType.Comment)
					segment.IsCollapsed = toggle;
				widget.TextEditor.Document.InformFoldChanged(new FoldSegmentEventArgs(segment));
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
			var op = new SourceEditorPrintOperation (IdeApp.Workbench.ActiveDocument.Editor, Name);
			
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
			CodeSegmentPreviewWindow.CodeSegmentPreviewInformString = GettextCatalog.GetString ("Press F2 to focus");
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
				item.Icon = DesktopService.GetIconForFile ("a.txt", IconSize.Menu);
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
				tn.InsertAtCaret (WorkbenchWindow.Document);
				TextEditor.GrabFocus ();
			}
		}
		
		#region dnd
		Widget customSource;
		ItemToolboxNode dragItem;

		void IToolboxConsumer.DragItem (ItemToolboxNode item, Widget source, Gdk.DragContext ctx)
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
			return tn.GetDragPreview (WorkbenchWindow.Document);
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
			
			return textNode.IsCompatibleWith (WorkbenchWindow.Document);
		}
		
		public TargetEntry[] DragTargets { 
			get {
				return ClipboardActions.CopyOperation.TargetEntries;
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
				return TextEditor.Options.CanZoomIn;
			}
		}
		
		bool IZoomable.EnableZoomOut {
			get {
				return TextEditor.Options.CanZoomOut;
			}
		}
		
		bool IZoomable.EnableZoomReset {
			get {
				return TextEditor.Options.CanResetZoom;
			}
		}
		
		void IZoomable.ZoomIn ()
		{
			TextEditor.Options.ZoomIn ();
		}
		
		void IZoomable.ZoomOut ()
		{
			TextEditor.Options.ZoomOut ();
		}
		
		void IZoomable.ZoomReset ()
		{
			TextEditor.Options.ZoomReset ();
		}

		#region ITextEditorResolver implementation 
		
		public Microsoft.CodeAnalysis.ISymbol GetLanguageItem (int offset)
		{
			MonoDevelop.Ide.Editor.DocumentRegion region;
			return SourceEditorWidget.TextEditor.GetLanguageItem (offset, out region);
		}
		
		public Microsoft.CodeAnalysis.ISymbol GetLanguageItem (int offset, string expression)
		{
			return SourceEditorWidget.TextEditor.GetLanguageItem (offset, expression);
		}
		#endregion 
		
		#region ISupportsProjectReload implementaion
		
		public override ProjectReloadCapability ProjectReloadCapability {
			get {
				return ProjectReloadCapability.Full;
			}
		}

		#endregion
		
		#endregion
		public TextEditorData GetTextEditorData ()
		{
			var editor = TextEditor;
			if (editor == null)
				return null;
			return editor.GetTextEditorData ();
		}

		public void InsertTemplate (CodeTemplate template, MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context)
		{
			TextEditor.InsertTemplate (template, editor, context);
		}

		void CorrectIndenting ()
		{
			var doc = IdeApp.Workbench.ActiveDocument?.Editor;
			if (doc == null)
				return;
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
						formatter.CorrectIndenting (policies, doc, i);
					}
					editorData.SetSelection (version.MoveOffsetTo (editorData.Document.Version, anchor), version.MoveOffsetTo (editorData.Document.Version, lead));
				}
			} else {
				formatter.CorrectIndenting (policies, doc, TextEditor.Caret.Line);
			}
		}

		protected override object OnGetContent (Type type)
		{
			if (type.Equals (typeof(TextEditorData)))
				return TextEditor.GetTextEditorData ();
			return base.OnGetContent (type);
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

		[CommandUpdateHandler (SearchCommands.FindNext)]
		[CommandUpdateHandler (SearchCommands.FindPrevious)]
		void UpdateFindNextAndPrev (CommandInfo cinfo)
		{
			cinfo.Enabled = !string.IsNullOrEmpty (SearchAndReplaceOptions.SearchPattern);
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

		[CommandHandler (SourceEditorCommands.NextIssue)]
		void NextIssue ()
		{
			widget.NextIssue ();
		}	

		[CommandHandler (SourceEditorCommands.PrevIssue)]
		void PrevIssue ()
		{
			widget.PrevIssue ();
		}

		[CommandHandler (SourceEditorCommands.NextIssueError)]
		void NextIssueError ()
		{
			widget.NextIssueError ();
		}	

		[CommandHandler (SourceEditorCommands.PrevIssueError)]
		void PrevIssueError ()
		{
			widget.PrevIssueError ();
		}
		#endregion

		IReadonlyTextDocument ITextEditorImpl.Document {
			get {
				return widget.TextEditor.Document;
			}
		}

		event EventHandler ITextEditorImpl.SelectionChanged {
			add {
				TextEditor.SelectionChanged += value;
			}
			remove {
				TextEditor.SelectionChanged -= value;
			}
		}

		event EventHandler<Xwt.MouseMovedEventArgs> ITextEditorImpl.MouseMoved {
			add {
				TextEditor.BeginHover += value;
			}
			remove {
				TextEditor.BeginHover -= value;
			}
		}

		event EventHandler ITextEditorImpl.VAdjustmentChanged {
			add {
				TextEditor.VAdjustment.ValueChanged += value;
			}
			remove {
				TextEditor.VAdjustment.ValueChanged -= value;
			}
		}

		event EventHandler ITextEditorImpl.HAdjustmentChanged {
			add {
				TextEditor.HAdjustment.ValueChanged += value;
			}
			remove {
				TextEditor.HAdjustment.ValueChanged -= value;
			}
		}

		public event EventHandler CaretPositionChanged;
		bool hasCaretPositionChanged;
		protected virtual void OnCaretPositionChanged (EventArgs e)
		{
			if (widget.TextEditor.Document.IsInAtomicUndo) {
				hasCaretPositionChanged = true;
				return;
			}
			var handler = CaretPositionChanged;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler BeginAtomicUndoOperation;

		protected virtual void OnBeginUndo (EventArgs e)
		{
			hasCaretPositionChanged = false;
			var handler = BeginAtomicUndoOperation;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler EndAtomicUndoOperation;

		protected virtual void OnEndUndo (EventArgs e)
		{
			var handler = EndAtomicUndoOperation;
			if (handler != null)
				handler (this, e);
			if (hasCaretPositionChanged) {
				OnCaretPositionChanged (e);
				hasCaretPositionChanged = false;
			}
		}

		void ITextEditorImpl.SetSelection (int anchorOffset, int leadOffset)
		{
			TextEditor.SetSelection (anchorOffset, leadOffset);
		}

		void ITextEditorImpl.ClearSelection ()
		{
			TextEditor.ClearSelection ();
		}

		void ITextEditorImpl.CenterToCaret ()
		{
			TextEditor.CenterToCaret ();
		}

		void ITextEditorImpl.StartCaretPulseAnimation ()
		{
			TextEditor.StartCaretPulseAnimation ();
		}

		int ITextEditorImpl.EnsureCaretIsNotVirtual ()
		{
			return TextEditor.GetTextEditorData ().EnsureCaretIsNotVirtual ();
		}

		void ITextEditorImpl.FixVirtualIndentation ()
		{
			TextEditor.GetTextEditorData ().FixVirtualIndentation ();
		}

		bool viewContentCreated;
		object ITextEditorImpl.CreateNativeControl ()
		{
			if (!viewContentCreated) {
				viewContentCreated = true;
				Document.InformRealizedComplete ();
			}
			return widget != null ? widget.Vbox : null;
		}

		string ITextEditorImpl.FormatString (int offset, string code)
		{
			return TextEditor.GetTextEditorData ().FormatString (offset, code);
		}

		void ITextEditorImpl.StartInsertionMode (InsertionModeOptions insertionModeOptions)
		{
			var mode = new InsertionCursorEditMode (TextEditor, insertionModeOptions.InsertionPoints.Select (ip => new Mono.TextEditor.InsertionPoint ( 
				new DocumentLocation (ip.Location.Line, ip.Location.Column),
				(Mono.TextEditor.NewLineInsertion)ip.LineBefore,
				(Mono.TextEditor.NewLineInsertion)ip.LineAfter
			)).ToList ());
			if (mode.InsertionPoints.Count == 0) {
				return;
			}
			var helpWindow = new Mono.TextEditor.PopupWindow.InsertionCursorLayoutModeHelpWindow ();
			helpWindow.TitleText = insertionModeOptions.Operation;
			mode.HelpWindow = helpWindow;
			mode.CurIndex = insertionModeOptions.FirstSelectedInsertionPoint;
			mode.StartMode ();
			mode.Exited += delegate(object s, Mono.TextEditor.InsertionCursorEventArgs iCArgs) {
				if (insertionModeOptions.ModeExitedAction != null) {
					insertionModeOptions.ModeExitedAction (new MonoDevelop.Ide.Editor.InsertionCursorEventArgs (iCArgs.Success,
																												iCArgs.Success ? 
																												new MonoDevelop.Ide.Editor.InsertionPoint (
																													new MonoDevelop.Ide.Editor.DocumentLocation (iCArgs.InsertionPoint.Location.Line, iCArgs.InsertionPoint.Location.Column),
																													(MonoDevelop.Ide.Editor.NewLineInsertion)iCArgs.InsertionPoint.LineBefore, 
																													(MonoDevelop.Ide.Editor.NewLineInsertion)iCArgs.InsertionPoint.LineAfter) 
																												: null
																											   ));
				}
			};
		}

		void ITextEditorImpl.StartTextLinkMode (TextLinkModeOptions textLinkModeOptions)
		{
			var convertedLinks = new List<Mono.TextEditor.TextLink> ();
			foreach (var link in textLinkModeOptions.Links) {
				var convertedLink = new Mono.TextEditor.TextLink (link.Name);
				convertedLink.IsEditable = link.IsEditable;
				convertedLink.IsIdentifier = link.IsIdentifier;
				var func = link.GetStringFunc;
				if (func != null) {
					convertedLink.GetStringFunc = delegate(Func<string, string> arg) {
						return new ListDataProviderWrapper (func (arg));
					};
				}
				foreach (var segment in link.Links) {
					convertedLink.AddLink (new TextSegment (segment.Offset, segment.Length)); 
				}
				convertedLinks.Add (convertedLink); 
			}

			var tle = new TextLinkEditMode (TextEditor, 0, convertedLinks);
			tle.SetCaretPosition = false;
			if (tle.ShouldStartTextLinkMode) {
				tle.OldMode = TextEditor.CurrentMode;
				if (textLinkModeOptions.ModeExitedAction != null) {
					tle.Cancel += (sender, e) => textLinkModeOptions.ModeExitedAction (new TextLinkModeEventArgs (false));
					tle.Exited += (sender, e) => {
						for (int i = 0; i < convertedLinks.Count; i++) {
							textLinkModeOptions.Links[i].CurrentText = convertedLinks[i].CurrentText;
						}
						textLinkModeOptions.ModeExitedAction (new TextLinkModeEventArgs (true));
						
					};
				}
				var undoOperation = TextEditor.OpenUndoGroup ();
				tle.Exited += (object sender, EventArgs e) => undoOperation.Dispose ();
				tle.StartMode ();
				TextEditor.CurrentMode = tle;
			}
		}

		MonoDevelop.Ide.Editor.DocumentLocation ITextEditorImpl.PointToLocation (double xp, double yp, bool endAtEol)
		{
			var pt = TextEditor.PointToLocation (xp, yp);
			return new MonoDevelop.Ide.Editor.DocumentLocation (pt.Line, pt.Column);
		}

		Xwt.Point ITextEditorImpl.LocationToPoint (int line, int column)
		{
			var p = TextEditor.LocationToPoint (line, column);
			return new Xwt.Point (p.X, p.Y);
		}

		void ITextEditorImpl.AddMarker (IDocumentLine line, ITextLineMarker lineMarker)
		{
			var debugPair = lineMarker as DebugMarkerPair;
			if (debugPair != null) {
				debugPair.AddTo (TextEditor.Document, (DocumentLine)line);
				return;
			}
			var textLineMarker = lineMarker as TextLineMarker;
			if (textLineMarker == null)
				throw new InvalidOperationException ("Tried to add an incompatible text marker. Use the MarkerHost to create compatible ones.");

			if (lineMarker is IUnitTestMarker) {
				var actionMargin = TextEditor.ActionMargin;
				if (actionMargin != null) {
					actionMargin.IsVisible = true;
				}
			}

			TextEditor.Document.AddMarker ((DocumentLine)line, textLineMarker);
		}

		void ITextEditorImpl.RemoveMarker (ITextLineMarker lineMarker)
		{
			var debugPair = lineMarker as DebugMarkerPair;
			if (debugPair != null) {
				debugPair.Remove ();
				return;
			}
			var textLineMarker = lineMarker as TextLineMarker;
			if (textLineMarker == null)
				throw new InvalidOperationException ("Tried to add an incompatible text marker.");
			TextEditor.Document.RemoveMarker (textLineMarker);
		}

		IEnumerable<ITextLineMarker> ITextEditorImpl.GetLineMarkers (IDocumentLine line)
		{
			return Document.GetMarkers (((DocumentLine)line)).OfType<ITextLineMarker> ();
		}

		IEnumerable<ITextSegmentMarker> ITextEditorImpl.GetTextSegmentMarkersAt (MonoDevelop.Core.Text.ISegment segment)
		{
			return TextEditor.Document.GetTextSegmentMarkersAt (new TextSegment (segment.Offset, segment.Length)).OfType<ITextSegmentMarker> ();
		}

		IEnumerable<ITextSegmentMarker> ITextEditorImpl.GetTextSegmentMarkersAt (int offset)
		{
			return TextEditor.Document.GetTextSegmentMarkersAt (offset).OfType<ITextSegmentMarker> ();
		}

		void ITextEditorImpl.AddMarker (ITextSegmentMarker marker)
		{
			var textSegmentMarker = marker as TextSegmentMarker;
			if (textSegmentMarker == null)
				throw new InvalidOperationException ("Tried to add an incompatible text marker. Use the MarkerHost to create compatible ones.");
			TextEditor.Document.AddMarker (textSegmentMarker);
		}

		bool ITextEditorImpl.RemoveMarker (ITextSegmentMarker marker)
		{
			var textSegmentMarker = marker as TextSegmentMarker;
			if (textSegmentMarker == null)
				throw new InvalidOperationException ("Tried to remove an incompatible text marker.");
			return TextEditor.Document.RemoveMarker (textSegmentMarker);
		}

		IFoldSegment ITextEditorImpl.CreateFoldSegment (int offset, int length, bool isFolded)
		{
			return new FoldSegment ("...", offset, length, FoldingType.Unknown) { IsCollapsed = isFolded };
		}

		void ITextEditorImpl.SetFoldings (IEnumerable<IFoldSegment> foldings)
		{
			if (this.isDisposed || !TextEditor.Options.ShowFoldMargin)
				return;

			TextEditor.Document.UpdateFoldSegments (foldings, true);
		}

		IEnumerable<IFoldSegment> ITextEditorImpl.GetFoldingsContaining (int offset)
		{
			return TextEditor.Document.GetFoldingsFromOffset (offset).Cast<IFoldSegment> ();
		}

		IEnumerable<IFoldSegment> ITextEditorImpl.GetFoldingsIn (int offset, int length)
		{
			return TextEditor.Document.GetFoldingContaining (offset, length).Cast<IFoldSegment> ();
		}

		MonoDevelop.Ide.Editor.ITextEditorOptions ITextEditorImpl.Options {
			get {
				return((StyledSourceEditorOptions)TextEditor.Options).OptionsCore;
			}
			set {
				((StyledSourceEditorOptions)TextEditor.Options).OptionsCore = value;
			}
		}

		IReadOnlyList<Caret> ITextEditorImpl.Carets {
			get {
				return new Caret [] { TextEditor.Caret };
			}
		}

		bool ITextEditorImpl.IsSomethingSelected {
			get {
				return TextEditor.IsSomethingSelected;
			}
		}

		MonoDevelop.Ide.Editor.SelectionMode ITextEditorImpl.SelectionMode {
			get {
				return (MonoDevelop.Ide.Editor.SelectionMode)TextEditor.SelectionMode;
			}
		}

		MonoDevelop.Core.Text.ISegment ITextEditorImpl.SelectionRange {
			get {
				var range = TextEditor.SelectionRange;
				return MonoDevelop.Core.Text.TextSegment.FromBounds (range.Offset, range.EndOffset);
			}
			set {
				TextEditor.SelectionRange = new TextSegment (value.Offset, value.Length);
			}
		}
		
		int ITextEditorImpl.SelectionAnchorOffset {
			get {
				return TextEditor.SelectionAnchor;
			}
			set {
				TextEditor.SelectionAnchor = value;
			}
		}

		int ITextEditorImpl.SelectionLeadOffset {
			get {
				return TextEditor.SelectionLead;
			}
			set {
				TextEditor.SelectionLead = value;
			}
		}

		bool ITextEditorImpl.SuppressTooltips {
			get {
				return TextEditor.GetTextEditorData ().SuppressTooltips;
			}
			set {
				if (value)
					TextEditor.HideTooltip ();
				TextEditor.GetTextEditorData ().SuppressTooltips = value;
			}
		}

		MonoDevelop.Ide.Editor.DocumentRegion ITextEditorImpl.SelectionRegion {
			get {
				return new MonoDevelop.Ide.Editor.DocumentRegion (
					TextEditor.MainSelection.Start.Line,
					TextEditor.MainSelection.Start.Column,
					TextEditor.MainSelection.End.Line,
					TextEditor.MainSelection.End.Column
				);
			}
			set {
				TextEditor.MainSelection = new MonoDevelop.Ide.Editor.Selection (
					value.BeginLine,
					value.BeginColumn,
					value.EndLine,
					value.EndColumn
				);
			}
		}

		IEditorActionHost ITextEditorImpl.Actions {
			get {
				return this;
			}
		}

		double ITextEditorImpl.LineHeight {
			get {
				return TextEditor.GetTextEditorData ().LineHeight;
			}
		}

		ITextMarkerFactory ITextEditorImpl.TextMarkerFactory {
			get {
				return this;
			}
		}

		MonoDevelop.Ide.Editor.EditMode ITextEditorImpl.EditMode {
			get {
				if (TextEditor.CurrentMode is TextLinkEditMode)
					return MonoDevelop.Ide.Editor.EditMode.TextLink;
				if (TextEditor.CurrentMode is InsertionCursorEditMode)
					return MonoDevelop.Ide.Editor.EditMode.CursorInsertion;
				return MonoDevelop.Ide.Editor.EditMode.Edit;
			}
		}

		string ITextEditorImpl.GetVirtualIndentationString (int lineNumber)
		{
			if (!TextEditor.GetTextEditorData ().HasIndentationTracker)
				return TextEditor.GetLineIndent (lineNumber);
			return TextEditor.GetTextEditorData ().GetIndentationString (lineNumber, 1);
		}

		IndentationTracker ITextEditorImpl.IndentationTracker {
			get {
				return TextEditor.GetTextEditorData().IndentationTracker;
			}
			set {
				TextEditor.GetTextEditorData().IndentationTracker = value;
			}
		}

		void ITextEditorImpl.SetSelectionSurroundingProvider (SelectionSurroundingProvider surroundingProvider)
		{
			TextEditor.GetTextEditorData ().SelectionSurroundingProvider = surroundingProvider;
		}
		
		void ITextEditorImpl.SetTextPasteHandler (TextPasteHandler textPasteHandler)
		{
			var data = TextEditor.GetTextEditorData ();
			if (textPasteHandler == null) {
				data.TextPasteHandler = null;
				return;
			}
			data.TextPasteHandler = textPasteHandler;
		}

		internal Stack<EditSession> editSessions = new Stack<EditSession> ();

		public EditSession CurrentSession {
			get {
				return editSessions.Count () > 0 ? editSessions.Peek () : null;
			}
		}

		public void StartSession (EditSession session)
		{
			if (session == null)
				throw new ArgumentNullException (nameof (session));
			editSessions.Push (session);
			session.SessionStarted ();
		}

		public void EndSession ()
		{
			if (editSessions.Count == 0)
				throw new InvalidOperationException ("No edit session was started.");
			var session = editSessions.Pop ();
			session.Dispose ();
		}

		void ITextEditorImpl.ScrollTo (int offset)
		{
			TextEditor.ScrollTo (offset); 
		}

		void ITextEditorImpl.CenterTo (int offset)
		{
			TextEditor.CenterTo (offset); 
		}

		void ITextEditorImpl.ClearTooltipProviders ()
		{
			TextEditor.ClearTooltipProviders ();
		}

		IEnumerable<MonoDevelop.Ide.Editor.TooltipProvider> ITextEditorImpl.TooltipProvider {
			get {
				foreach (var p in GetTextEditorData ().TooltipProviders) {
					var wrapper = p as TooltipProviderWrapper;
					if (wrapper == null)
						continue;
					yield return wrapper.OriginalProvider;
				}
			}
		}

		void ITextEditorImpl.AddTooltipProvider (MonoDevelop.Ide.Editor.TooltipProvider provider)
		{
			TextEditor.AddTooltipProvider (new TooltipProviderWrapper (provider));
		}

		void ITextEditorImpl.RemoveTooltipProvider (MonoDevelop.Ide.Editor.TooltipProvider provider)
		{
			foreach (var p in GetTextEditorData ().TooltipProviders) {
				var wrapper = p as TooltipProviderWrapper;
				if (wrapper == null)
					continue;
				if (wrapper.OriginalProvider == provider) {
					TextEditor.RemoveTooltipProvider (p);
					return;
				}
			}
		}

		Xwt.Point ITextEditorImpl.GetEditorWindowOrigin ()
		{
			int ox, oy;
			TextEditor.GdkWindow.GetOrigin (out ox, out oy); 
			return new Xwt.Point (ox, oy);
		}

		Xwt.Rectangle ITextEditorImpl.GetEditorAllocation ()
		{
			var alloc = TextEditor.Allocation;
			return new Xwt.Rectangle (alloc.X, alloc.Y, alloc.Width, alloc.Height);
		}


		TextEditorExtension ITextEditorImpl.EditorExtension {
			get {
				return TextEditor.EditorExtension;
			}
			set {
				TextEditor.EditorExtension = value;
			}
		}


		SemanticHighlighting ITextEditorImpl.SemanticHighlighting {
			get {
				return TextEditor.SemanticHighlighting;
			}
			set {
				TextEditor.SemanticHighlighting = value;
			}
		}

		ISyntaxHighlighting ITextEditorImpl.SyntaxHighlighting {
			get {
				return TextEditor.SyntaxHighlighting;
			}
			set {
				TextEditor.SyntaxHighlighting = value;
			}
		}

		string ITextEditorImpl.GetPangoMarkup (int offset, int length, bool fitIdeStyle)
		{
			return TextEditor.GetTextEditorData ().GetMarkup (offset, length, false, replaceTabs: false, fitIdeStyle: fitIdeStyle);
		}

		string ITextEditorImpl.GetMarkup (int offset, int length, MarkupOptions options)
		{
			var data = TextEditor.GetTextEditorData ();
			switch (options.MarkupFormat) {
			case MarkupFormat.Pango:
				return data.GetMarkup (offset, length, false, replaceTabs: false, fitIdeStyle: options.FitIdeStyle);
			case MarkupFormat.Html:
				return HtmlWriter.GenerateHtml (ClipboardColoredText.GetChunks (data, new TextSegment (offset, length)).WaitAndGetResult (default (System.Threading.CancellationToken)), data.ColorStyle, data.Options);
			case MarkupFormat.RichText:
				return RtfWriter.GenerateRtf (ClipboardColoredText.GetChunks (data, new TextSegment (offset, length)).WaitAndGetResult (default (System.Threading.CancellationToken)), data.ColorStyle, data.Options);
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		void ITextEditorImpl.SetUsageTaskProviders (IEnumerable<UsageProviderEditorExtension> providers)
		{
			widget.ClearUsageTaskProvider ();
			foreach (var p in providers) {
				widget.AddUsageTaskProvider (p);
			}
		}

		void ITextEditorImpl.SetQuickTaskProviders (IEnumerable<IQuickTaskProvider> providers)
		{
			widget.ClearQuickTaskProvider ();
			foreach (var p in providers) {
				widget.AddQuickTaskProvider (p);
			}
		}


		class BracketMatcherTextMarker : TextSegmentMarker
		{
			public BracketMatcherTextMarker (int offset, int length) : base (offset, length)
			{
			}

			public override void DrawBackground (MonoTextEditor editor, Cairo.Context cr, LineMetrics metrics, int startOffset, int endOffset)
			{
				try {
					double fromX, toX;
					GetLineDrawingPosition (metrics, startOffset, out fromX, out toX);

					fromX = Math.Max (fromX, editor.TextViewMargin.XOffset);
					toX = Math.Max (toX, editor.TextViewMargin.XOffset);
					if (fromX < toX) {
						var bracketMatch = new Cairo.Rectangle (fromX + 0.5, metrics.LineYRenderStartPosition + 0.5, toX - fromX - 1, editor.LineHeight - 2);
						if (editor.TextViewMargin.BackgroundRenderer == null) {
							
							cr.SetSourceColor (SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.BracketsForeground));
							cr.Rectangle (bracketMatch);
							cr.Fill ();
						}
					}
				} catch (Exception e) {
					LoggingService.LogError ($"Error while drawing bracket matcher ({this}) startOffset={startOffset} lineCharLength={metrics.Layout.Text.Length}", e);
				}
			}

			void GetLineDrawingPosition (LineMetrics metrics, int startOffset, out double fromX, out double toX)
			{
				var startXPos = metrics.TextRenderStartPosition;
				var endXPos = metrics.TextRenderEndPosition;
				int start = this.Offset;
				int end = this.EndOffset;

				uint curIndex = 0, byteIndex = 0;
				TextViewMargin.TranslateToUTF8Index (metrics.Layout.Text, (uint)Math.Min (start - startOffset, metrics.Layout.Text.Length), ref curIndex, ref byteIndex);

				int x_pos = metrics.Layout.IndexToPos ((int)byteIndex).X;

				fromX = startXPos + (int)(x_pos / Pango.Scale.PangoScale);

				TextViewMargin.TranslateToUTF8Index (metrics.Layout.Text, (uint)Math.Min (end - startOffset, metrics.Layout.Text.Length), ref curIndex, ref byteIndex);
				x_pos = metrics.Layout.IndexToPos ((int)byteIndex).X;

				toX = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
			}
		}

		List<BracketMatcherTextMarker> bracketMarkers = new List<BracketMatcherTextMarker> ();

		void ITextEditorImpl.UpdateBraceMatchingResult (BraceMatchingResult? result)
		{
			if (result.HasValue) {
				if (bracketMarkers.Count > 0 && result.Value.LeftSegment.Offset == bracketMarkers [0].Offset)
					return;
				ClearBracketMarkers ();
				if ((result.Value.BraceMatchingProperties & BraceMatchingProperties.Hidden) == 0) {
					bracketMarkers.Add (new BracketMatcherTextMarker (result.Value.LeftSegment.Offset, result.Value.LeftSegment.Length));
					bracketMarkers.Add (new BracketMatcherTextMarker (result.Value.RightSegment.Offset, result.Value.RightSegment.Length));
					bracketMarkers.ForEach (marker => widget.TextEditor.Document.AddMarker (marker));
				}
			} else {
				ClearBracketMarkers ();
			}
		}

		void ClearBracketMarkers ()
		{
			bracketMarkers.ForEach (marker => widget.TextEditor.Document.RemoveMarker (marker));
			bracketMarkers.Clear ();
		}

		public double ZoomLevel {
			get { return TextEditor != null && TextEditor.Options != null ? TextEditor.Options.Zoom : 1d; }
			set { if (TextEditor != null && TextEditor.Options != null) TextEditor.Options.Zoom = value; }
		}
		event EventHandler ITextEditorImpl.ZoomLevelChanged {
			add {
				TextEditor.Options.ZoomChanged += value;
			}
			remove {
				TextEditor.Options.ZoomChanged -= value;
			}
		}

		public void AddOverlay (Control messageOverlayContent, Func<int> sizeFunc)
		{
			widget.AddOverlay (messageOverlayContent.GetNativeWidget<Widget> (), sizeFunc);
		}

		public void RemoveOverlay (Control messageOverlayContent)
		{
			widget.RemoveOverlay (messageOverlayContent.GetNativeWidget<Widget> ());
		}

		void TextViewMargin_LineShowing (object sender, Mono.TextEditor.LineEventArgs e)
		{
			LineShowing?.Invoke (this, new Ide.Editor.LineEventArgs (e.Line));
		}

		public IEnumerable<IDocumentLine> VisibleLines {
			get {
				foreach (var v in TextEditor.TextViewMargin.CachedLine) {
					yield return v;
				}
			}
		}

		public event EventHandler<Ide.Editor.LineEventArgs> LineShowing;




		#region IEditorActionHost implementation

		void IEditorActionHost.MoveCaretDown ()
		{
			CaretMoveActions.Down (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.MoveCaretUp ()
		{
			CaretMoveActions.Up (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.MoveCaretRight ()
		{
			CaretMoveActions.Right (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.MoveCaretLeft ()
		{
			CaretMoveActions.Left (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.MoveCaretToLineEnd ()
		{
			CaretMoveActions.LineEnd (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.MoveCaretToLineStart ()
		{
			CaretMoveActions.LineHome (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.MoveCaretToDocumentStart ()
		{
			CaretMoveActions.ToDocumentStart (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.MoveCaretToDocumentEnd ()
		{
			CaretMoveActions.ToDocumentEnd (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.Backspace ()
		{
			DeleteActions.Backspace (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.ClipboardCopy ()
		{
			ClipboardActions.Copy (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.ClipboardCut ()
		{
			ClipboardActions.Cut (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.ClipboardPaste ()
		{
			ClipboardActions.Paste (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.NewLine ()
		{
			MiscActions.InsertNewLine (TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.SwitchCaretMode ()
		{
			TextEditor.RunAction (MiscActions.SwitchCaretMode);
		}

		void IEditorActionHost.InsertTab ()
		{
			TextEditor.RunAction (MiscActions.InsertTab);
		}

		void IEditorActionHost.RemoveTab ()
		{
			TextEditor.RunAction (MiscActions.RemoveTab);
		}

		void IEditorActionHost.InsertNewLine ()
		{
			TextEditor.RunAction (MiscActions.InsertNewLine);
		}

		void IEditorActionHost.DeletePreviousWord ()
		{
			TextEditor.RunAction (DeleteActions.PreviousWord);
		}

		void IEditorActionHost.DeleteNextWord ()
		{
			TextEditor.RunAction (DeleteActions.NextWord);
		}

		void IEditorActionHost.DeletePreviousSubword ()
		{
			TextEditor.RunAction (DeleteActions.PreviousSubword);
		}

		void IEditorActionHost.DeleteNextSubword ()
		{
			TextEditor.RunAction (DeleteActions.NextSubword);
		}

		void IEditorActionHost.StartCaretPulseAnimation ()
		{
			TextEditor.StartCaretPulseAnimation ();
		}

		void IEditorActionHost.RecenterEditor ()
		{
			TextEditor.RunAction (MiscActions.RecenterEditor);
		}

		void IEditorActionHost.JoinLines ()
		{
			using (var undo = Document.OpenUndoGroup ()) {
				TextEditor.RunAction (Mono.TextEditor.Vi.ViActions.Join);
			}
		}

		void IEditorActionHost.MoveNextSubWord ()
		{
			TextEditor.RunAction (SelectionActions.MoveNextSubword);
		}

		void IEditorActionHost.MovePrevSubWord ()
		{
			TextEditor.RunAction (SelectionActions.MovePreviousSubword);
		}

		void IEditorActionHost.MoveNextWord ()
		{
			TextEditor.RunAction (CaretMoveActions.NextWord);
		}

		void IEditorActionHost.MovePrevWord ()
		{
			TextEditor.RunAction (CaretMoveActions.PreviousWord);
		}

		void IEditorActionHost.PageUp ()
		{
			TextEditor.RunAction (CaretMoveActions.PageUp);
		}

		void IEditorActionHost.PageDown ()
		{
			TextEditor.RunAction (CaretMoveActions.PageDown);
		}

		void IEditorActionHost.DeleteCurrentLine ()
		{
			TextEditor.RunAction (DeleteActions.CaretLine);
		}

		void IEditorActionHost.DeleteCurrentLineToEnd ()
		{
			TextEditor.RunAction (DeleteActions.CaretLineToEnd);
		}

		void IEditorActionHost.ScrollLineUp ()
		{
			TextEditor.RunAction (ScrollActions.Up);
		}

		void IEditorActionHost.ScrollLineDown ()
		{
			TextEditor.RunAction (ScrollActions.Down);
		}

		void IEditorActionHost.ScrollPageUp ()
		{
			TextEditor.RunAction (ScrollActions.PageUp);
		}

		void IEditorActionHost.ScrollPageDown ()
		{
			TextEditor.RunAction (ScrollActions.PageDown);
		}

		void IEditorActionHost.MoveBlockUp ()
		{
			using (var undo = TextEditor.OpenUndoGroup ()) {
				TextEditor.RunAction (MiscActions.MoveBlockUp);
				CorrectIndenting ();
			}
		}

		void IEditorActionHost.MoveBlockDown ()
		{
			using (var undo = TextEditor.OpenUndoGroup ()) {
				TextEditor.RunAction (MiscActions.MoveBlockDown);
				CorrectIndenting ();
			}
		}

		void IEditorActionHost.ToggleBlockSelectionMode ()
		{
			TextEditor.SelectionMode = TextEditor.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Normal ? MonoDevelop.Ide.Editor.SelectionMode.Block : MonoDevelop.Ide.Editor.SelectionMode.Normal;
			TextEditor.QueueDraw ();
		}

		void IEditorActionHost.IndentSelection ()
		{
			if (widget.TextEditor.IsSomethingSelected) {
				MiscActions.IndentSelection (widget.TextEditor.GetTextEditorData ());
			} else {
				int offset = widget.TextEditor.LocationToOffset (widget.TextEditor.Caret.Line, 1);
				widget.TextEditor.Insert (offset, widget.TextEditor.Options.IndentationString);
			}
		}

		void IEditorActionHost.UnIndentSelection ()
		{
			MiscActions.RemoveTab (widget.TextEditor.GetTextEditorData ());
		}

		void IEditorActionHost.ShowQuickInfo ()
		{
			widget.TextEditor.TextArea.ShowQuickInfo ();
		}

		#endregion


		#region ISegmentMarkerHost implementation

		ITextSegmentMarker ITextMarkerFactory.CreateUsageMarker (MonoDevelop.Ide.Editor.TextEditor editor, Usage usage)
		{
			return new UsageSegmentMarker (usage);
		}

		IUrlTextLineMarker ITextMarkerFactory.CreateUrlTextMarker (MonoDevelop.Ide.Editor.TextEditor editor, string value, MonoDevelop.Ide.Editor.UrlType url, string syntax, int startCol, int endCol)
		{
			return new UrlTextLineMarker (TextEditor.Document, value, (Mono.TextEditor.UrlType)url, syntax, startCol, endCol);
		}

		ICurrentDebugLineTextMarker ITextMarkerFactory.CreateCurrentDebugLineTextMarker (MonoDevelop.Ide.Editor.TextEditor editor, int offset, int length)
		{
			return new CurrentDebugLineTextMarker (TextEditor, offset, length);
		}

		ITextLineMarker ITextMarkerFactory.CreateAsmLineMarker (MonoDevelop.Ide.Editor.TextEditor editor)
		{
			return new AsmLineMarker ();
		}

		IUnitTestMarker ITextMarkerFactory.CreateUnitTestMarker (MonoDevelop.Ide.Editor.TextEditor editor, UnitTestMarkerHost host, UnitTestLocation unitTestLocation)
		{
			return new UnitTestMarker (TextEditor, host, unitTestLocation);
		}

		IMessageBubbleLineMarker ITextMarkerFactory.CreateMessageBubbleLineMarker (MonoDevelop.Ide.Editor.TextEditor editor)
		{
			return new MessageBubbleTextMarker (messageBubbleCache);
		}

		ITextLineMarker ITextMarkerFactory.CreateLineSeparatorMarker (TextEditor editor)
		{
			return new LineSeparatorMarker ();
		}

		IGenericTextSegmentMarker ITextMarkerFactory.CreateGenericTextSegmentMarker (MonoDevelop.Ide.Editor.TextEditor editor, TextSegmentMarkerEffect effect, int offset, int length)
		{
			switch (effect) {
			case TextSegmentMarkerEffect.DottedLine:
			case TextSegmentMarkerEffect.WavedLine:
				return new GenericUnderlineMarker (new TextSegment (offset, length), effect);
			case TextSegmentMarkerEffect.GrayOut:
				return new GrayOutMarker (new TextSegment (offset, length));
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		public ILinkTextMarker CreateLinkMarker (MonoDevelop.Ide.Editor.TextEditor editor, int offset, int length, Action<LinkRequest> activateLink)
		{
			return new LinkMarker (offset, length, activateLink);
		}

		ISmartTagMarker ITextMarkerFactory.CreateSmartTagMarker (MonoDevelop.Ide.Editor.TextEditor editor, int offset, MonoDevelop.Ide.Editor.DocumentLocation realLocation)
		{
			return new SmartTagMarker (offset, realLocation);
		}

		IErrorMarker ITextMarkerFactory.CreateErrorMarker (MonoDevelop.Ide.Editor.TextEditor editor, MonoDevelop.Ide.TypeSystem.Error info, int offset, int length)
		{
			return new ErrorMarker (info, offset, length);
		}
		#endregion


		#region Command handlers
		[CommandHandler (ScrollbarCommand.Top)]
		void GotoTop ()
		{
			widget.QuickTaskStrip.GotoTop ();
		}

		[CommandHandler (ScrollbarCommand.Bottom)]
		void GotoBottom ()
		{
			widget.QuickTaskStrip.GotoBottom ();
		}

		[CommandHandler (ScrollbarCommand.PgUp)]
		void GotoPgUp ()
		{
			widget.QuickTaskStrip.GotoPgUp ();
		}

		[CommandHandler (ScrollbarCommand.PgDown)]
		void GotoPgDown ()
		{
			widget.QuickTaskStrip.GotoPgDown ();
		}

		[CommandUpdateHandler (ScrollbarCommand.ShowTasks)]
		void UpdateShowMap (CommandInfo info)
		{
			widget.QuickTaskStrip.UpdateShowMap (info);
		}

		[CommandHandler (ScrollbarCommand.ShowTasks)]
		void ShowMap ()
		{
			widget.QuickTaskStrip.ShowMap ();
		}

		[CommandUpdateHandler (ScrollbarCommand.ShowMinimap)]
		void UpdateShowFull (CommandInfo info)
		{
			widget.QuickTaskStrip.UpdateShowFull (info);
		}

		[CommandHandler (ScrollbarCommand.ShowMinimap)]
		void ShowFull ()
		{
			widget.QuickTaskStrip.ShowFull ();
		}

		#endregion
	
	

		public event EventHandler FocusLost;

		void TextArea_FocusOutEvent (object o, FocusOutEventArgs args)
		{
			FocusLost?.Invoke (this, EventArgs.Empty);
		}

		void ITextEditorImpl.GrabFocus ()
		{
			var topLevelWindow = this.TextEditor.Toplevel as Gtk.Window;
			if (topLevelWindow != null)
				topLevelWindow.Present ();
			this.TextEditor.GrabFocus ();
		}

		void ITextEditorImpl.ShowTooltipWindow (Components.Window window, TooltipWindowOptions options)
		{
			var tooltipWindow = (Xwt.WindowFrame)window;
			
			var caret = TextEditor.Caret;
			var p = TextEditor.LocationToPoint (caret.Location);
			Mono.TextEditor.TooltipProvider.ShowAndPositionTooltip (TextEditor, tooltipWindow, p.X, p.Y, (int)tooltipWindow.Width, 0.5);
			TextEditor.TextArea.SetTooltip (tooltipWindow);
		}

		Task<ScopeStack> ITextEditorImpl.GetScopeStackAsync (int offset, CancellationToken cancellationToken)
		{
			return TextEditor.SyntaxHighlighting.GetScopeStackAsync (offset, cancellationToken);
		}

		double ITextEditorImpl.GetLineHeight (int line)
		{
			return TextEditor.GetLineHeight (line);
		}

		public bool HasFocus {
			get {
				return this.TextEditor.HasFocus;
			}
		}
	}
} 
