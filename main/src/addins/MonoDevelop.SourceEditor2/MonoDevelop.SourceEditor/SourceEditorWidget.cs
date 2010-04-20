// SourceEditorWidget.cs
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
using Gtk;
using Mono.TextEditor;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Commands;
using Document = Mono.TextEditor.Document;
using Services = MonoDevelop.Projects.Services;
using System.Threading;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor
{
	
	class SourceEditorWidget : Gtk.VBox, ITextEditorExtension
	{
		SourceEditorView view;
		ScrolledWindow mainsw;

		// We need a reference to TextEditorData to be able to access the
		// editor document without getting the TextEditor property. This
		// property runs some gtk code, so it can only be used from the GUI thread.
		// Other threads can use textEditorData to get the document.
		TextEditorData textEditorData;
		
		const uint CHILD_PADDING = 0;
		
		bool shouldShowclassBrowser;
		bool canShowClassBrowser;
		NavigationBar classBrowser;
		ISourceEditorOptions options;
		
		bool isDisposed = false;
		
		ParsedDocument parsedDocument;
		
		MonoDevelop.SourceEditor.ExtensibleTextEditor textEditor;
		MonoDevelop.SourceEditor.ExtensibleTextEditor splittedTextEditor;
		MonoDevelop.SourceEditor.ExtensibleTextEditor lastActiveEditor;
		
		public MonoDevelop.SourceEditor.ExtensibleTextEditor TextEditor {
			get {
				SetLastActiveEditor ();
				return lastActiveEditor;
			}
		}
		
		public TextEditorContainer TextEditorContainer {
			get {
				SetLastActiveEditor ();
				return lastActiveEditor == textEditor ? textEditorContainer : splittedTextEditorContainer;
			}
		}
		
		void SetLastActiveEditor ()
		{
			if (this.splittedTextEditor != null && this.splittedTextEditor.Parent != null && this.splittedTextEditor.HasFocus) {
				lastActiveEditor = this.splittedTextEditor;
			}
			if (this.textEditor != null && this.textEditor.Parent != null && this.textEditor.HasFocus) {
				lastActiveEditor = this.textEditor;
			}
		}
		
		
		public bool ShowClassBrowser {
			get { return shouldShowclassBrowser; }
			set {
				if (shouldShowclassBrowser == value)
					return;
				shouldShowclassBrowser = value;
				UpdateClassBrowserVisibility (false);
			}
		}
		
		bool CanShowClassBrowser {
			get { return canShowClassBrowser; }
			set {
				if (canShowClassBrowser == value)
					return;
				canShowClassBrowser = value;
				UpdateClassBrowserVisibility (false);
			}
		}
		
		void UpdateClassBrowserVisibility (bool threaded)
		{
			if (shouldShowclassBrowser && canShowClassBrowser) {
				if (classBrowser == null) {
					classBrowser = new NavigationBar (this);
					classBrowser.StatusBox.UpdateWidth ();
					this.UpdateLineCol ();
					this.PackStart (classBrowser, false, false, CHILD_PADDING);
					this.ReorderChild (classBrowser, 0);
					PopulateClassCombo (threaded);
				}
			} else {
				if (classBrowser != null) {
					this.Remove (classBrowser);
					classBrowser.Destroy ();
					classBrowser = null;
				}
			}
		}
		
		public void PopulateClassCombo (bool runInThread)
		{
			if (classBrowser == null || !CanShowClassBrowser)
				return;
			
			classBrowser.UpdateCompilationUnit (this.parsedDocument, runInThread);
		}
		
		public Ambience Ambience {
			get {
				string fileName = this.view.IsUntitled ? this.view.UntitledName : this.view.ContentName;
				return AmbienceService.GetAmbienceForFile (fileName);
			}
		}
		#region ITextEditorExtension
		
		ITextEditorExtension ITextEditorExtension.Next {
			get {
				return null;
			}
		}
		
		object ITextEditorExtension.GetExtensionCommandTarget ()
		{
			return null;
		}

		void ITextEditorExtension.TextChanged (int startIndex, int endIndex)
		{
		}

		void ITextEditorExtension.CursorPositionChanged ()
		{
		}

		bool ITextEditorExtension.KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			this.TextEditor.SimulateKeyPress (key, (uint)keyChar, modifier);
			if (key == Gdk.Key.Escape)
				return true;
			return false;
		}
		#endregion
		
		void PrepareEvent (object sender, ButtonPressEventArgs args) 
		{
			args.RetVal = true;
		}
		
		protected SourceEditorWidget (IntPtr raw) : base (raw)
		{
		}
		
		TextEditorContainer textEditorContainer;
		public SourceEditorWidget (SourceEditorView view)
		{
			this.view = view;
			this.SetSizeRequest (32, 32);
			this.lastActiveEditor = this.textEditor = new MonoDevelop.SourceEditor.ExtensibleTextEditor (view);
			mainsw = new ScrolledWindow ();
			mainsw.BorderWidth = 0;
			mainsw.ShadowType = ShadowType.In;
			this.textEditorContainer = new TextEditorContainer (textEditor);
			mainsw.Child = textEditorContainer;
			this.PackStart (mainsw, true, true, 0);
			this.mainsw.ButtonPressEvent += PrepareEvent;
			this.textEditor.Errors = errors;
			options = this.textEditor.Options;
			
			this.textEditor.Caret.ModeChanged += delegate {
				this.UpdateLineCol ();
			};
			this.textEditor.Caret.PositionChanged += CaretPositionChanged;
			this.textEditor.SelectionChanged += delegate {
				this.UpdateLineCol ();
			};
			
			textEditorData = textEditor.GetTextEditorData ();
			ResetFocusChain ();
			
			UpdateLineCol ();
			ProjectDomService.ParsedDocumentUpdated += OnParseInformationChanged;
			//			this.IsClassBrowserVisible = this.widget.TextEditor.Options.EnableQuickFinder;
			this.BorderWidth = 0;
			this.Spacing = 0;
		}
		
		protected override bool OnFocused (DirectionType direction)
		{
			bool res = base.OnFocused (direction);
			UpdateLineCol ();
			return res;
		}
		
		
		void ResetFocusChain ()
		{
			List<Widget> focusChain = new List<Widget> ();
			
			focusChain.Add (this.textEditor);
			if (this.searchAndReplaceWidget != null) {
				focusChain.Add (this.searchAndReplaceWidget);
			}
			if (this.gotoLineNumberWidget != null) {
				focusChain.Add (this.gotoLineNumberWidget);
			}
			if (this.classBrowser != null) {
				focusChain.Add (this.classBrowser);
			}
			this.FocusChain = focusChain.ToArray ();
		}
		
		#region Error underlining
		Dictionary<int, ErrorMarker> errors = new Dictionary<int, ErrorMarker> ();
		uint resetTimerId;
		
		FoldSegment AddMarker (List<FoldSegment> foldSegments, string text, DomRegion region, FoldingType type)
		{
			Document document = textEditorData.Document;
			if (document == null || region.Start.Line <= 0 || region.End.Line <= 0
			    || region.Start.Line >= document.LineCount || region.End.Line >= document.LineCount)
			{
				return null;
			}
			
			int startOffset = document.LocationToOffset (region.Start.Line - 1,  region.Start.Column - 1);
			int endOffset   = document.LocationToOffset (region.End.Line - 1,  region.End.Column - 1);
			FoldSegment result = new FoldSegment (text, startOffset, endOffset - startOffset, type);
			
			foldSegments.Add (result);
			return result;
		}
		HashSet<string> symbols = new HashSet<string> ();
		class ParseInformationUpdaterWorkerThread : WorkerThread
		{
			SourceEditorWidget widget;
			//ParseInformationEventArgs args;
			
			public ParseInformationUpdaterWorkerThread (SourceEditorWidget widget)
			{
				this.widget = widget;
			}
			protected override void InnerRun ()
			{
				Run (true);
			}
			
			public void Run (bool runInThread)
			{
				try {
					if (this.widget.options.ShowFoldMargin && widget.parsedDocument != null) {
						List<FoldSegment> foldSegments = new List<FoldSegment> ();
						bool updateSymbols = widget.parsedDocument.Defines.Count != widget.symbols.Count;
						if (!updateSymbols) {
							foreach (PreProcessorDefine define in widget.parsedDocument.Defines) {
								if (!widget.symbols.Contains (define.Define)) {
									updateSymbols = true;
									break;
								}
							}
						}
						if (updateSymbols) {
							widget.symbols.Clear ();
							foreach (PreProcessorDefine define in widget.parsedDocument.Defines) {
								widget.symbols.Add (define.Define);
							}
							widget.Document.UpdateHighlighting ();
						}
						foreach (FoldingRegion region in widget.parsedDocument.GenerateFolds ()) {
							if (runInThread && IsStopping)
								return;
							FoldingType type = FoldingType.None;
							bool setFolded = false;
							bool folded = false;
							
							//decide whether the regions should be folded by default
							switch (region.Type) {
							case FoldType.Member:
								type = FoldingType.TypeMember;
								break;
							case FoldType.Type:
								type = FoldingType.TypeDefinition;
								break;
							case FoldType.UserRegion:
								type = FoldingType.Region;
								setFolded = this.widget.options.DefaultRegionsFolding;
								folded = true;
								break;
							case FoldType.Comment:
								setFolded = this.widget.options.DefaultCommentFolding;
								folded = true;
								break;
							case FoldType.CommentInsideMember:
								setFolded = this.widget.options.DefaultCommentFolding;
								folded = false;
								break;
							case FoldType.Undefined:
								setFolded = true;
								folded = region.IsFoldedByDefault;
								break;
							}
							
							//add the region
							FoldSegment marker = widget.AddMarker (foldSegments, region.Name, 
							                                       region.Region, type);
							
							//and, if necessary, set its fold state
							if (marker != null && setFolded && widget.firstUpdate) {
								// only fold on document open, later added folds are NOT folded by default.
								marker.IsFolded = folded;
							}
							if (marker != null && region.Region.Contains (widget.textEditorData.Caret.Line, widget.textEditorData.Caret.Column))
								marker.IsFolded = false;
							
						}
						widget.textEditorData.Document.UpdateFoldSegments (foldSegments, runInThread);
						widget.firstUpdate = false;
					}
					widget.UpdateAutocorTimer ();
					widget.PopulateClassCombo (runInThread);
				} catch (Exception ex) {
					LoggingService.LogError ("Unhandled exception in ParseInformationUpdaterWorkerThread", ex);
				}
				base.Stop ();
			}
		}
		
		readonly object syncObject = new object();
		bool firstUpdate = true;
		ParseInformationUpdaterWorkerThread parseInformationUpdaterWorkerThread = null;
		
		void OnParseInformationChanged (object sender, ParsedDocumentEventArgs args)
		{
			/*
			if (this.isDisposed || args == null || args.ParsedDocument == null || this.view == null) {
				return;
			}
			
			string fileName = this.view.IsUntitled ? this.view.UntitledName : this.view.ContentName;
			if (fileName != args.FileName)
				return;
			
			ParsedDocument = args.ParsedDocument;
			bool canShowBrowser = ParsedDocument != null && ParsedDocument.CompilationUnit != null;
			if (canShowBrowser)
				Gtk.Application.Invoke (delegate { this.CanShowClassBrowser = canShowBrowser; } );
			*/
			Gtk.Application.Invoke (delegate {
				if (this.isDisposed || args == null || args.ParsedDocument == null || this.view == null) {
					return;
				}
				
				string fileName = this.view.IsUntitled ? this.view.UntitledName : this.view.ContentName;
				if (fileName != args.FileName)
					return;
				
				if (MonoDevelop.Core.PropertyService.Get ("EnableSemanticHighlighting", false)) 
					TextEditor.TextViewMargin.PurgeLayoutCache ();
				
				ParsedDocument = args.ParsedDocument;
				bool canShowBrowser = ParsedDocument != null && ParsedDocument.CompilationUnit != null;
				if (canShowBrowser)
					this.CanShowClassBrowser = canShowBrowser; 
			});
		}
		
		public ParsedDocument ParsedDocument {
			get {
				return this.parsedDocument;
			}
			set {
				SetParsedDocument (value, true);
			}
		}

		internal void SetParsedDocument (ParsedDocument newDocument, bool runInThread)
		{
			this.parsedDocument = newDocument;
			CanShowClassBrowser = newDocument != null && newDocument.CompilationUnit != null;
			if (runInThread) {
				lock (syncObject) {
					StopParseInfoThread ();
					if (parsedDocument != null) {
						parseInformationUpdaterWorkerThread = new ParseInformationUpdaterWorkerThread (this);
						parseInformationUpdaterWorkerThread.Start ();
					}
				}
			} else {
				new ParseInformationUpdaterWorkerThread (this).Run (false);
			}
		}

		
		void StopParseInfoThread ()
		{
			if (parseInformationUpdaterWorkerThread != null) {
				parseInformationUpdaterWorkerThread.Stop ();
				parseInformationUpdaterWorkerThread = null;
			}
		}
		public void WaitForParseInformationUpdaterWorkerThread ()
		{
			while (parseInformationUpdaterWorkerThread != null && !parseInformationUpdaterWorkerThread.IsStopped) {
				Thread.Sleep (50);
			}
		}
		
		void UpdateAutocorTimer ()
		{
			if (!options.UnderlineErrors)
				return;
			// this may be run in another thread, therefore we've to synchronize
			// with the gtk main loop.
			lock (this) {
				if (resetTimerId > 0) {
					GLib.Source.Remove (resetTimerId);
					resetTimerId = 0;
				}
				const uint timeout = 900;
				resetTimerId = GLib.Timeout.Add (timeout, delegate {
					lock (this) { // this runs in the gtk main loop.
						ResetUnderlineChangement ();
						if (parsedDocument != null)
							ParseCompilationUnit (parsedDocument);
						resetTimerId = 0;
					}
					return false;
				});
			}
		}
		
		void ResetUnderlineChangement ()
		{
			if (errors.Count > 0) {
				Document doc = this.TextEditor != null ? this.TextEditor.Document : null;
				if (doc != null) {
					foreach (ErrorMarker error in this.errors.Values) {
						error.RemoveFromLine (doc);
					}
				}
				errors.Clear ();
			}
		}
		
		void ParseCompilationUnit (ParsedDocument cu)
		{
			// No new errors
			if (cu.Errors == null || cu.Errors.Count < 1)
				return;
			
			// Else we underline the error
			foreach (MonoDevelop.Projects.Dom.Error info in cu.Errors)
				UnderLineError (info);
		}
		
		void UnderLineError (Error info)
		{
			if (this.isDisposed)
				return;
			// Adjust the line to Gtk line representation
//			info.Line -= 1;
			
			// If the line is already underlined
			if (errors.ContainsKey (info.Region.Start.Line - 1))
				return;
			
			LineSegment line = this.TextEditor.Document.GetLine (info.Region.Start.Line - 1);
			ErrorMarker error = new ErrorMarker (info, line);
			errors [info.Region.Start.Line - 1] = error;
			error.AddToLine (this.TextEditor.Document);
		}
		#endregion
		
		protected override void OnDestroyed ()
		{
			if (!isDisposed) {
				isDisposed = true;
				StopParseInfoThread ();
				KillWidgets ();
				mainsw.ButtonPressEvent -= PrepareEvent;
				
				this.textEditor = null;
				this.lastActiveEditor = null;
				this.splittedTextEditor = null;
				view = null;
				
				ProjectDomService.ParsedDocumentUpdated -= OnParseInformationChanged;
				IdeApp.Workbench.StatusBar.ClearCaretState ();
			}			
			base.OnDestroyed ();
		}
		
		Gtk.Paned splitContainer = null;
		public bool IsSplitted {
			get {
				return splitContainer != null;
			}
		}
		
		public bool EditorHasFocus {
			get {
				Gtk.Container c = this;
				while (c != null) {
					if (c.FocusChild == textEditor)
						return true;
					c = c.FocusChild as Gtk.Container;
				}
				return false;
			}
		}

		public SourceEditorView View {
			get {
				return view;
			}
			set {
				view = value;
			}
		}
		
		public void Unsplit ()
		{
			if (splitContainer == null)
				return;
			double vadjustment = mainsw.Vadjustment.Value;
			double hadjustment = mainsw.Hadjustment.Value;
			
			splitContainer.Remove (mainsw);
			if (this.textEditor == lastActiveEditor) {
				secondsw.Destroy ();
				secondsw = null;
				splittedTextEditor = null;
			} else {
				this.mainsw.Destroy ();
				this.mainsw = secondsw;
				vadjustment = secondsw.Vadjustment.Value;
				hadjustment = secondsw.Hadjustment.Value;
				splitContainer.Remove (secondsw);
				lastActiveEditor = this.textEditor = splittedTextEditor;
				splittedTextEditor = null;
			}
			this.Remove (splitContainer);
			splitContainer.Destroy ();
			splitContainer = null;
			
			RecreateMainSw ();
			this.PackStart (mainsw, true, true, 0);
			this.ShowAll ();
			mainsw.Vadjustment.Value = vadjustment; 
			mainsw.Hadjustment.Value = hadjustment;
		}
		
		public void SwitchWindow ()
		{
			if (splittedTextEditor.HasFocus) {
				this.textEditor.GrabFocus ();
			} else {
				this.splittedTextEditor.GrabFocus ();
			}
		}
		ScrolledWindow secondsw;
		TextEditorContainer splittedTextEditorContainer;
		
		public void Split (bool vSplit)
		{
			double vadjustment = this.mainsw.Vadjustment.Value;
			double hadjustment = this.mainsw.Hadjustment.Value;
			
			if (splitContainer != null)
				Unsplit ();
			this.Remove (this.mainsw);
			
			RecreateMainSw ();

			this.splitContainer = vSplit ? (Gtk.Paned)new VPaned () : (Gtk.Paned)new HPaned ();

			splitContainer.Add1 (mainsw);

			this.splitContainer.ButtonPressEvent += delegate(object sender, ButtonPressEventArgs args) {
				if (args.Event.Type == Gdk.EventType.TwoButtonPress && args.RetVal == null) {
					Unsplit ();
				}
			};
			secondsw = new ScrolledWindow ();
			secondsw.ShadowType = ShadowType.In;
			secondsw.ButtonPressEvent += PrepareEvent;
			this.splittedTextEditor = new MonoDevelop.SourceEditor.ExtensibleTextEditor (view, this.textEditor.Options, textEditor.Document);
			this.splittedTextEditor.Extension = textEditor.Extension;
			this.splittedTextEditor.Caret.ModeChanged += delegate {
				this.UpdateLineCol ();
			};
			this.splittedTextEditor.SelectionChanged += delegate {
				this.UpdateLineCol ();
			};
			this.splittedTextEditor.Caret.PositionChanged += CaretPositionChanged;
			
			this.splittedTextEditorContainer = new TextEditorContainer (this.splittedTextEditor);
			secondsw.Child = this.splittedTextEditorContainer;
			splitContainer.Add2 (secondsw);
			
			this.PackStart (splitContainer, true, true, 0);
			this.splitContainer.Position = (vSplit ? this.Allocation.Height : this.Allocation.Width) / 2 - 1;
			
			this.ShowAll ();
			secondsw.Vadjustment.Value = mainsw.Vadjustment.Value = vadjustment; 
			secondsw.Hadjustment.Value = mainsw.Hadjustment.Value = hadjustment;
		}

		void RecreateMainSw ()
		{
			// destroy old scrolled window to work around Bug 526721 - When splitting window vertically, 
			// the slider under left split is not shown unitl window is resized
			double vadjustment = this.mainsw.Vadjustment.Value;
			double hadjustment = this.mainsw.Hadjustment.Value;
			
			this.mainsw.Remove (textEditorContainer);
			textEditorContainer.Unparent ();
			this.mainsw.Destroy ();
			
			this.mainsw = new ScrolledWindow ();
			this.mainsw.ShadowType = ShadowType.In;
			this.mainsw.ButtonPressEvent += PrepareEvent;
			this.mainsw.Child = textEditorContainer;
			this.mainsw.Vadjustment.Value = vadjustment; 
			this.mainsw.Hadjustment.Value = hadjustment;
		}

		
//		void SplitContainerSizeRequested (object sender, SizeRequestedArgs args)
//		{
//			this.splitContainer.SizeRequested -= SplitContainerSizeRequested;
//			this.splitContainer.Position = args.Requisition.Width / 2;
//			this.splitContainer.SizeRequested += SplitContainerSizeRequested;
//		}
//		
		MonoDevelop.Components.InfoBar messageBar = null;
		
		internal static string EllipsizeMiddle (string str, int truncLen)
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
		
		public void ShowFileChangedWarning ()
		{
			RemoveMessageBar ();
			
			if (messageBar == null) {
				messageBar = new MonoDevelop.Components.InfoBar (MessageType.Warning);
				messageBar.SetMessageLabel (GettextCatalog.GetString (
					"<b>The file \"{0}\" has been changed outside of MonoDevelop.</b>\n" +
					"Do you want to keep your changes, or reload the file from disk?",
					EllipsizeMiddle (Document.FileName, 50)));
				
				Button b1 = new Button (GettextCatalog.GetString("_Reload from disk"));
				b1.Image = ImageService.GetImage (Gtk.Stock.Refresh, IconSize.Button);
				b1.Clicked += new EventHandler (ClickedReload);
				messageBar.ActionArea.Add (b1);
				
				Button b2 = new Button (GettextCatalog.GetString("_Keep changes"));
				b2.Image = ImageService.GetImage (Gtk.Stock.Cancel, IconSize.Button);
				b2.Clicked += new EventHandler (ClickedIgnore);
				messageBar.ActionArea.Add (b2);
			}
			
			view.WarnOverwrite = true;
			this.PackStart (messageBar, false, false, CHILD_PADDING);
			this.ReorderChild (messageBar, classBrowser != null ? 1 : 0);
			messageBar.ShowAll ();

			messageBar.QueueDraw ();
			
			view.WorkbenchWindow.ShowNotification = true;
		}
		
		
		public void ShowAutoSaveWarning (string fileName)
		{
			RemoveMessageBar ();
			TextEditor.Visible = false;
			if (messageBar == null) {
				messageBar = new MonoDevelop.Components.InfoBar (MessageType.Warning);
				messageBar.SetMessageLabel (GettextCatalog.GetString (
						"<b>An autosave file has been found for this file.</b>\n" +
						"This could mean that another instance of MonoDevelop is editing this " +
						"file, or that MonoDevelop crashed with unsaved changes.\n\n" +
					    "Do you want to use the original file, or load from the autosave file?"));
				
				Button b1 = new Button (GettextCatalog.GetString("_Use original file"));
				b1.Image = ImageService.GetImage (Gtk.Stock.Refresh, IconSize.Button);
				b1.Clicked += delegate {
					try {
						view.AutoSave.FileName = fileName;
						view.AutoSave.RemoveAutoSaveFile ();
						view.Load (fileName);
					} catch (Exception ex) {
						MessageService.ShowException (ex, "Could not remove the autosave file.");
					} finally {
						RemoveMessageBar ();
					}
				};
				messageBar.ActionArea.Add (b1);
				
				Button b2 = new Button (GettextCatalog.GetString("_Load from autosave"));
				b2.Image = ImageService.GetImage (Gtk.Stock.RevertToSaved, IconSize.Button);
				b2.Clicked += delegate {
					try {
						view.AutoSave.FileName = fileName;
						string content = view.AutoSave.LoadAutoSave ();
						view.AutoSave.RemoveAutoSaveFile ();
						view.Load (fileName, content, null);
						view.IsDirty = true;
					} catch (Exception ex) {
						MessageService.ShowException (ex, "Could not remove the autosave file.");
					} finally {
						RemoveMessageBar ();
					}
					
				};
				messageBar.ActionArea.Add (b2);
			}
			
			view.WarnOverwrite = true;
			this.PackStart (messageBar, false, false, CHILD_PADDING);
			this.ReorderChild (messageBar, classBrowser != null ? 1 : 0);
			messageBar.ShowAll ();

			messageBar.QueueDraw ();
			
//			view.WorkbenchWindow.ShowNotification = true;
		}
		
		
		public void RemoveMessageBar ()
		{
			if (messageBar != null) {
				if (messageBar.Parent == this)
					this.Remove (messageBar);
				messageBar.Destroy ();
				messageBar = null;
			}
			if (!TextEditor.Visible)
				TextEditor.Visible = true;
		}
		
		void ClickedReload (object sender, EventArgs args)
		{
			try {
//				double vscroll = view.VScroll;
				view.Load (view.ContentName);
//				view.VScroll = vscroll;
				view.WorkbenchWindow.ShowNotification = false;
			} catch (Exception ex) {
				MessageService.ShowException (ex, "Could not reload the file.");
			} finally {
				RemoveMessageBar ();
			}
		}
		
		void ClickedIgnore (object sender, EventArgs args)
		{
			RemoveMessageBar ();
			view.WorkbenchWindow.ShowNotification = false;
		}
		
		#region Status Bar Handling
		void CaretPositionChanged (object o, DocumentLocationEventArgs args)
		{
			UpdateLineCol ();
			
			if (classBrowser != null) {
				classBrowser.UpdatePosition (TextEditor.Caret.Line + 1, TextEditor.Caret.Column + 1);
			}
		}
		
//		void OnChanged (object o, EventArgs e)
//		{
//			UpdateLineCol ();
//			OnContentChanged (null);
//			needsUpdate = true;
//		}
		
		internal void UpdateLineCol ()
		{
			int offset = TextEditor.Caret.Offset;
			if (offset < 0 || offset > TextEditor.Document.Length)
				return;
			if (classBrowser == null || NavigationBar.HideStatusBox) {
				DocumentLocation location = TextEditor.LogicalToVisualLocation (TextEditor.Caret.Location);
				IdeApp.Workbench.StatusBar.ShowCaretState (TextEditor.Caret.Line + 1,
				                                           location.Column + 1,
				                                           TextEditor.IsSomethingSelected ? TextEditor.SelectionRange.Length : 0,
				                                           TextEditor.Caret.IsInInsertMode);
			} else {
				IdeApp.Workbench.StatusBar.ClearCaretState ();
				classBrowser.StatusBox.ShowCaretState ();
			}
		}
		
		#endregion
		
		#region Search and Replace
		Components.RoundedFrame searchAndReplaceWidgetFrame = null;
		SearchAndReplaceWidget searchAndReplaceWidget = null;
		Components.RoundedFrame gotoLineNumberWidgetFrame = null;
		GotoLineNumberWidget   gotoLineNumberWidget   = null;
		
		public void SetSearchPattern ()
		{
			string selectedText = this.TextEditor.SelectedText;
			
			if (!String.IsNullOrEmpty (selectedText)) {
				this.SetSearchPattern (selectedText);
				SearchAndReplaceWidget.searchPattern = selectedText;
//				SearchAndReplaceWidget.FireSearchPatternChanged ();
			}
		}
		
		bool KillWidgets ()
		{
			bool result = false;
			if (searchAndReplaceWidgetFrame != null) {
				searchAndReplaceWidgetFrame.Destroy ();
				searchAndReplaceWidgetFrame = null;
				searchAndReplaceWidget = null;
				result = true;
			}
			
			if (gotoLineNumberWidgetFrame != null) {
				gotoLineNumberWidgetFrame.Destroy ();
				gotoLineNumberWidgetFrame = null;
				gotoLineNumberWidget = null;
				result = true;
			}
			
			if (this.textEditor != null) 
				this.textEditor.HighlightSearchPattern = false;
			if (this.splittedTextEditor != null) 
				this.splittedTextEditor.HighlightSearchPattern = false;
			
			if (!isDisposed)
				ResetFocusChain ();
			return result;
		}
		
		public void SetSearchPattern (string searchPattern)
		{
			this.textEditor.SearchPattern = searchPattern;
			if (this.splittedTextEditor != null)
				this.splittedTextEditor.SearchPattern = searchPattern;
		}
		
		public bool DisableAutomaticSearchPatternCaseMatch {
			get;
			set;
		}
		
		internal void CheckSearchPatternCasing (string searchPattern)
		{
			if (!DisableAutomaticSearchPatternCaseMatch && PropertyService.Get ("AutoSetPatternCasing", true) && searchPattern.Any (ch => Char.IsUpper (ch))) {
				if (!SearchAndReplaceWidget.IsCaseSensitive) {
					SearchAndReplaceWidget.IsCaseSensitive = true;
					SetSearchOptions ();
				}
			}
		}
		
		internal bool RemoveSearchWidget ()
		{
			bool result = KillWidgets ();
			if (!isDisposed)
				TextEditor.GrabFocus ();
			return result;
		}
		
		[CommandHandler (SearchCommands.EmacsFindNext)]
		public void EmacsFindNext ()
		{
			if (searchAndReplaceWidget == null) {
				ShowSearchWidget ();
			} else {
				this.FindNext ();
			}
		}
		
		[CommandHandler (SearchCommands.EmacsFindPrevious)]
		public void EmacsFindPrevious ()
		{
			if (searchAndReplaceWidget == null) {
				ShowSearchWidget ();
			} else {
				this.FindPrevious ();
			}
		}
		
		[CommandHandler (SearchCommands.Find)]
		public void ShowSearchWidget ()
		{
			ShowSearchReplaceWidget (false);
		}
		
		[CommandHandler (SearchCommands.Replace)]
		public void ShowReplaceWidget ()
		{
			ShowSearchReplaceWidget (true);
		}
		
		[CommandUpdateHandler (SearchCommands.UseSelectionForFind)]
		protected void OnUpdateUseSelectionForFind (CommandInfo info)
		{
			info.Enabled = searchAndReplaceWidget != null && TextEditor.IsSomethingSelected;
		}
		
		[CommandHandler (SearchCommands.UseSelectionForFind)]
		public void UseSelectionForFind ()
		{
			SetSearchPatternToSelection ();
		}
		
		[CommandUpdateHandler (SearchCommands.UseSelectionForReplace)]
		protected void OnUpdateUseSelectionForReplace (CommandInfo info)
		{
			info.Enabled = searchAndReplaceWidget != null && TextEditor.IsSomethingSelected;
		}
		
		[CommandHandler (SearchCommands.UseSelectionForReplace)]
		public void UseSelectionForReplace ()
		{
			SetReplacePatternToSelection ();
		}
		
		
		void ShowSearchReplaceWidget (bool replace)
		{
			if (searchAndReplaceWidget == null) {
				this.textEditor.SearchPattern = SearchAndReplaceWidget.searchPattern = "";
				// reset pattern, to force an update
				
				if (TextEditor.IsSomethingSelected) {
					TextEditor.SearchPattern = TextEditor.SelectedText;
					TextEditor.TextViewMargin.MainSearchResult = TextEditor.SelectionRange;
				}
				
				if (!DisableAutomaticSearchPatternCaseMatch && PropertyService.Get ("AutoSetPatternCasing", true))
					SearchAndReplaceWidget.IsCaseSensitive = TextEditor.IsSomethingSelected;
				KillWidgets ();
				searchAndReplaceWidgetFrame = new MonoDevelop.Components.RoundedFrame ();
				//searchAndReplaceWidgetFrame.SetFillColor (MonoDevelop.Components.CairoExtensions.GdkColorToCairoColor (widget.TextEditor.ColorStyle.Default.BackgroundColor));
				searchAndReplaceWidgetFrame.SetFillColor (MonoDevelop.Components.CairoExtensions.GdkColorToCairoColor (Style.Background (StateType.Normal)));
				
				searchAndReplaceWidgetFrame.Child = searchAndReplaceWidget = new SearchAndReplaceWidget (this, searchAndReplaceWidgetFrame);
				
				searchAndReplaceWidgetFrame.ShowAll ();
				this.TextEditorContainer.AddAnimatedWidget (searchAndReplaceWidgetFrame, 300, Mono.TextEditor.Theatrics.Easing.ExponentialInOut, Mono.TextEditor.Theatrics.Blocking.Downstage, this.TextEditor.Allocation.Width - 400, -searchAndReplaceWidget.Allocation.Height);
//				this.PackEnd (searchAndReplaceWidget);
//				this.SetChildPacking (searchAndReplaceWidget, false, false, CHILD_PADDING, PackType.End);
		//		searchAndReplaceWidget.ShowAll ();
				this.textEditor.HighlightSearchPattern = true;
				this.textEditor.TextViewMargin.RefreshSearchMarker ();
				if (this.splittedTextEditor != null) {
					this.splittedTextEditor.HighlightSearchPattern = true;
					this.splittedTextEditor.TextViewMargin.RefreshSearchMarker ();
				}
				
				ResetFocusChain ();
			}
			searchAndReplaceWidget.UpdateSearchPattern ();
			searchAndReplaceWidget.IsReplaceMode = replace;
			if (searchAndReplaceWidget.SearchFocused) {
				if (replace) {
					this.Replace ();
				} else {
					this.FindNext ();
				}
			}
			searchAndReplaceWidget.Focus ();
			SetSearchOptions ();
		}
		
		[CommandHandler (SearchCommands.GotoLineNumber)]
		public void ShowGotoLineNumberWidget ()
		{
			if (gotoLineNumberWidget == null) {
				KillWidgets ();
				
				
				gotoLineNumberWidgetFrame = new MonoDevelop.Components.RoundedFrame ();
				//searchAndReplaceWidgetFrame.SetFillColor (MonoDevelop.Components.CairoExtensions.GdkColorToCairoColor (widget.TextEditor.ColorStyle.Default.BackgroundColor));
				gotoLineNumberWidgetFrame.SetFillColor (MonoDevelop.Components.CairoExtensions.GdkColorToCairoColor (Style.Background (StateType.Normal)));
				
				gotoLineNumberWidgetFrame.Child = gotoLineNumberWidget = new GotoLineNumberWidget (this, gotoLineNumberWidgetFrame);
				gotoLineNumberWidgetFrame.ShowAll ();
				
				this.TextEditorContainer.AddAnimatedWidget (gotoLineNumberWidgetFrame, 300, Mono.TextEditor.Theatrics.Easing.ExponentialInOut, Mono.TextEditor.Theatrics.Blocking.Downstage, this.TextEditor.Allocation.Width - 400, -gotoLineNumberWidget.Allocation.Height);
				
				ResetFocusChain ();
			}
			
			gotoLineNumberWidget.Focus ();
		}
		
		internal void SetSearchOptions ()
		{
			if (SearchAndReplaceWidget.SearchEngine == SearchAndReplaceWidget.DefaultSearchEngine) {
				if (!(this.textEditor.SearchEngine is BasicSearchEngine))
					this.textEditor.SearchEngine = new BasicSearchEngine ();
			} else {
				if (!(this.textEditor.SearchEngine is RegexSearchEngine))
					this.textEditor.SearchEngine = new RegexSearchEngine ();
			}
			this.textEditor.IsCaseSensitive = SearchAndReplaceWidget.IsCaseSensitive;
			this.textEditor.IsWholeWordOnly = SearchAndReplaceWidget.IsWholeWordOnly;
			
			string error;
			string pattern = SearchAndReplaceWidget.searchPattern;
			if (searchAndReplaceWidget != null)
				pattern = searchAndReplaceWidget.SearchPattern;
			
			bool valid = this.textEditor.SearchEngine.IsValidPattern (pattern, out error);
			
			if (valid) {
				this.textEditor.SearchPattern = pattern;
			}
			this.textEditor.QueueDraw ();
			if (this.splittedTextEditor != null) {
				this.splittedTextEditor.IsCaseSensitive = SearchAndReplaceWidget.IsCaseSensitive;
				this.splittedTextEditor.IsWholeWordOnly = SearchAndReplaceWidget.IsWholeWordOnly;
				if (valid) {
					this.splittedTextEditor.SearchPattern = pattern;
				}
				this.splittedTextEditor.QueueDraw ();
			}
		}
		
		[CommandHandler (SearchCommands.FindNext)]
		public SearchResult FindNext ()
		{
			return FindNext (true);
		}
		
		public SearchResult FindNext (bool focus)
		{
			SetSearchOptions ();
			SearchResult result = TextEditor.FindNext (true);
			if (focus) {
				TextEditor.GrabFocus ();
			}
			if (result == null) {
				IdeApp.Workbench.StatusBar.ShowError (GettextCatalog.GetString ("Search pattern not found"));
			} else if (result.SearchWrapped) {
				IdeApp.Workbench.StatusBar.ShowMessage (new Image (Gtk.Stock.Find, IconSize.Menu),
					GettextCatalog.GetString ("Reached bottom, continued from top"));
			} else {
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
			return result;
		}
		
		[CommandHandler (SearchCommands.FindPrevious)]
		public SearchResult FindPrevious ()
		{
			return FindPrevious (true);
		}
		
		public SearchResult FindPrevious (bool focus)
		{
			SetSearchOptions ();
			SearchResult result = TextEditor.FindPrevious (true);
			if (focus) {
				TextEditor.GrabFocus ();
			}
			if (result == null) {
				IdeApp.Workbench.StatusBar.ShowError (GettextCatalog.GetString ("Search pattern not found"));
			} else if (result.SearchWrapped) {
				IdeApp.Workbench.StatusBar.ShowMessage (
					new Image (Gtk.Stock.Find, IconSize.Menu),
					GettextCatalog.GetString ("Reached top, continued from bottom"));
			} else {
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
			return result;
		}
		
		void SetSearchPatternToSelection ()
		{
			if (TextEditor.IsSomethingSelected) {
				TextEditor.SearchPattern = TextEditor.SelectedText;
			}
			if (searchAndReplaceWidget != null)
				searchAndReplaceWidget.UpdateSearchPattern ();
		}
		
		void SetReplacePatternToSelection ()
		{
			if (searchAndReplaceWidget != null && TextEditor.IsSomethingSelected)
				searchAndReplaceWidget.ReplacePattern = TextEditor.SelectedText;
		}
		
		[CommandHandler (SearchCommands.FindNextSelection)]
		public SearchResult FindNextSelection ()
		{
			SetSearchPatternToSelection ();
			
			SetSearchOptions ();
			SetSearchPattern();
			TextEditor.GrabFocus ();
			return FindNext ();
		}
	
		[CommandHandler (SearchCommands.FindPreviousSelection)]
		public SearchResult FindPreviousSelection ()
		{
			SetSearchPatternToSelection ();
			SetSearchOptions ();
			SetSearchPattern();
			TextEditor.GrabFocus ();
			return FindPrevious ();
		}
		
		public void Replace ()
		{
			SetSearchOptions ();
			TextEditor.Replace (searchAndReplaceWidget.ReplacePattern);
			TextEditor.GrabFocus ();
		}
		
		public void ReplaceAll ()
		{
			SetSearchOptions ();
			int number = TextEditor.ReplaceAll (searchAndReplaceWidget.ReplacePattern);
			if (number == 0) {
				IdeApp.Workbench.StatusBar.ShowError (GettextCatalog.GetString ("Search pattern not found"));
			} else {
				IdeApp.Workbench.StatusBar.ShowMessage (
					GettextCatalog.GetPluralString ("Found and replaced one occurrence",
					                                "Found and replaced {0} occurrences", number, number));
			}
			TextEditor.GrabFocus ();
		}
		#endregion
	
		public Mono.TextEditor.Document Document {
			get {
				return TextEditor.Document;
			}
		}
		
		#region Help

		[CommandHandler (HelpCommands.Help)]
		internal void MonodocResolver ()
		{
			ResolveResult res = TextEditor.GetLanguageItem (TextEditor.Caret.Offset);
			string url = HelpService.GetMonoDocHelpUrl (res);
			if (url != null)
				IdeApp.HelpOperations.ShowHelp (url);
		}
		
		[CommandUpdateHandler (HelpCommands.Help)]
		internal void MonodocResolverUpdate (CommandInfo cinfo)
		{
			ResolveResult res = TextEditor.GetLanguageItem (TextEditor.Caret.Offset);
			if (res == null || !IdeApp.HelpOperations.CanShowHelp (res))
				cinfo.Bypass = true;
		}
		
		#endregion
		
		#region commenting and indentation
		[CommandUpdateHandler (EditCommands.ToggleCodeComment)]
		protected void OnUpdateToggleComment (MonoDevelop.Components.Commands.CommandInfo info)
		{
			List<string> lineComments;
			if (Document.SyntaxMode.Properties.TryGetValue ("LineComment", out lineComments)) {
				info.Visible = lineComments.Count > 0;
			} else {
				List<string> blockStarts;
				List<string> blockEnds;
				if (Document.SyntaxMode.Properties.TryGetValue ("BlockCommentStart", out blockStarts) && Document.SyntaxMode.Properties.TryGetValue ("BlockCommentEnd", out blockEnds)) {
					info.Visible = blockStarts.Count > 0 && blockEnds.Count > 0;
				}
			}
		}
		
		void ToggleCodeCommentWithBlockComments ()
		{

			List<string> blockStarts;
			if (!Document.SyntaxMode.Properties.TryGetValue ("BlockCommentStart", out blockStarts) || blockStarts.Count == 0)
				return;

			List<string> blockEnds;
			if (!Document.SyntaxMode.Properties.TryGetValue ("BlockCommentEnd", out blockEnds) || blockEnds.Count == 0)
				return;

			string blockStart = blockStarts[0];
			string blockEnd = blockEnds[0];

			Document.BeginAtomicUndo ();
			LineSegment startLine;
			LineSegment endLine;

			if (TextEditor.IsSomethingSelected) {
				startLine = Document.GetLineByOffset (textEditor.SelectionRange.Offset);
				endLine = Document.GetLineByOffset (textEditor.SelectionRange.EndOffset);
			} else {
				startLine = endLine = Document.GetLine (textEditor.Caret.Line);
			}
			string startLineText = Document.GetTextAt (startLine.Offset, startLine.EditableLength);
			string endLineText = Document.GetTextAt (endLine.Offset, endLine.EditableLength);
			if (startLineText.StartsWith (blockStart) && endLineText.EndsWith (blockEnd)) {
				textEditor.Remove (endLine.Offset + endLine.EditableLength - blockEnd.Length, blockEnd.Length);
				textEditor.Remove (startLine.Offset, blockStart.Length);
				if (TextEditor.IsSomethingSelected) {
					TextEditor.SelectionAnchor -= blockEnd.Length;
				}
			} else {
				textEditor.Insert (endLine.Offset + endLine.EditableLength, blockEnd);
				textEditor.Insert (startLine.Offset, blockStart);
				if (TextEditor.IsSomethingSelected) {
					TextEditor.SelectionAnchor += blockEnd.Length;
				}
				
			}
			
			Document.EndAtomicUndo ();
		}
		
		[CommandHandler (EditCommands.ToggleCodeComment)]
		public void ToggleCodeComment ()
		{
			bool comment = false;
			List<string> lineComments;
			if (!Document.SyntaxMode.Properties.TryGetValue ("LineComment", out lineComments) || lineComments.Count == 0) {
				ToggleCodeCommentWithBlockComments ();
				return;
			}
			string commentTag = lineComments[0];
			
			foreach (LineSegment line in this.textEditor.SelectedLines) {
				string text = Document.GetTextAt (line);
				string trimmedText = text.TrimStart ();
				if (!trimmedText.StartsWith (commentTag)) {
					comment = true;
					break;
				}
			}
			if (comment) {
				CommentSelectedLines (commentTag);
			} else {
				UncommentSelectedLines (commentTag);
			}
		}
		
		void CommentSelectedLines (string commentTag)
		{
			int startLineNr = TextEditor.IsSomethingSelected ? Document.OffsetToLineNumber (TextEditor.SelectionRange.Offset) : TextEditor.Caret.Line;
			int endLineNr   = TextEditor.IsSomethingSelected ? Document.OffsetToLineNumber (TextEditor.SelectionRange.EndOffset) : TextEditor.Caret.Line;
			if (endLineNr < 0)
				endLineNr = Document.LineCount;
			
			LineSegment anchorLine   = TextEditor.IsSomethingSelected ? TextEditor.Document.GetLineByOffset (TextEditor.SelectionAnchor) : null;
			int         anchorColumn = TextEditor.IsSomethingSelected ? TextEditor.SelectionAnchor - anchorLine.Offset : -1;
			
			Document.BeginAtomicUndo ();
			foreach (LineSegment line in TextEditor.SelectedLines) {
				TextEditor.Insert (line.Offset, commentTag);
			}
			if (TextEditor.IsSomethingSelected) {
				if (TextEditor.SelectionAnchor < TextEditor.Caret.Offset) {
					if (anchorColumn != 0) 
						TextEditor.SelectionAnchor = System.Math.Min (anchorLine.Offset + anchorLine.EditableLength, System.Math.Max (anchorLine.Offset, TextEditor.SelectionAnchor + commentTag.Length));
				} else {
					if (anchorColumn != 0) {
						TextEditor.SelectionAnchor = System.Math.Min (anchorLine.Offset + anchorLine.EditableLength, System.Math.Max (anchorLine.Offset, anchorLine.Offset + anchorColumn + commentTag.Length));
					} else {
//						TextEditor.SelectionAnchor = anchorLine.Offset;
					}
				}
			}
			
			if (TextEditor.Caret.Column != 0) {
				TextEditor.Caret.PreserveSelection = true;
				TextEditor.Caret.Column += commentTag.Length;
				TextEditor.Caret.PreserveSelection = false;
			}
			
			if (TextEditor.IsSomethingSelected) 
				TextEditor.ExtendSelectionTo (TextEditor.Caret.Offset);
			Document.EndAtomicUndo ();
			Document.CommitMultipleLineUpdate (startLineNr, endLineNr);
		}
		
		void UncommentSelectedLines (string commentTag)
		{
			int startLineNr = TextEditor.IsSomethingSelected ? Document.OffsetToLineNumber (TextEditor.SelectionRange.Offset) : TextEditor.Caret.Line;
			int endLineNr   = TextEditor.IsSomethingSelected ? Document.OffsetToLineNumber (TextEditor.SelectionRange.EndOffset) : TextEditor.Caret.Line;
			if (endLineNr < 0)
				endLineNr = Document.LineCount;
			LineSegment anchorLine   = TextEditor.IsSomethingSelected ? TextEditor.Document.GetLineByOffset (TextEditor.SelectionAnchor) : null;
			int         anchorColumn = TextEditor.IsSomethingSelected ? TextEditor.SelectionAnchor - anchorLine.Offset : -1;
			
			Document.BeginAtomicUndo ();
			int first = -1;
			int last  = 0;
			foreach (LineSegment line in TextEditor.SelectedLines) {
				string text = Document.GetTextAt (line);
				string trimmedText = text.TrimStart ();
				int length = 0;
				if (trimmedText.StartsWith (commentTag)) {
					TextEditor.Remove (line.Offset + (text.Length - trimmedText.Length), commentTag.Length);
					length = commentTag.Length;
				}
				last = length;
				if (first < 0)
					first = last;
			}
			
			if (TextEditor.IsSomethingSelected) {
				if (TextEditor.SelectionAnchor < TextEditor.Caret.Offset) {
					TextEditor.SelectionAnchor = System.Math.Min (anchorLine.Offset + anchorLine.EditableLength, System.Math.Max (anchorLine.Offset, TextEditor.SelectionAnchor - first));
				} else {
					TextEditor.SelectionAnchor = System.Math.Min (anchorLine.Offset + anchorLine.EditableLength, System.Math.Max (anchorLine.Offset, anchorLine.Offset + anchorColumn - last));
				}
			}
			
			if (TextEditor.Caret.Column != 0) {
				TextEditor.Caret.PreserveSelection = true;
				TextEditor.Caret.Column = System.Math.Max (0, TextEditor.Caret.Column - last);
				TextEditor.Caret.PreserveSelection = false;
			}
			
			if (TextEditor.IsSomethingSelected) 
				TextEditor.ExtendSelectionTo (TextEditor.Caret.Offset);
		
			Document.EndAtomicUndo ();
			Document.CommitMultipleLineUpdate (startLineNr, endLineNr);
		}
		
		[CommandHandler (EditCommands.IndentSelection)]
		public void IndentSelection ()
		{
			MiscActions.IndentSelection (TextEditor.GetTextEditorData ());
		}
		
		[CommandHandler (EditCommands.UnIndentSelection)]
		public void UnIndentSelection ()
		{
			MiscActions.RemoveIndentSelection (TextEditor.GetTextEditorData ());
		}
		#endregion
		
	}

	class ErrorMarker
	{
		public Error Info { get; private set; }
		public LineSegment Line { get; private set; }
		
		UnderlineMarker marker;
		
		public ErrorMarker (MonoDevelop.Projects.Dom.Error info, LineSegment line)
		{
			this.Info = info;
			this.Line = line; // may be null if no line is assigned to the error.
			string underlineColor;
			if (info.ErrorType == ErrorType.Warning)
				underlineColor = Mono.TextEditor.Highlighting.Style.WarningUnderlineString;
			else
				underlineColor = Mono.TextEditor.Highlighting.Style.ErrorUnderlineString;
			
			if (Info.Region.Start.Line == info.Region.End.Line)
				marker = new UnderlineMarker (underlineColor, Info.Region.Start.Column - 1, info.Region.End.Column - 1);
			else
				marker = new UnderlineMarker (underlineColor, - 1, - 1);
		}
		
		public void AddToLine (Mono.TextEditor.Document doc)
		{
			if (Line != null) {
				doc.AddMarker (Line, marker);
			}
		}
		
		public void RemoveFromLine (Mono.TextEditor.Document doc)
		{
			if (Line != null) {
				doc.RemoveMarker (Line, marker);
			}
		}
		
		
	}
	
}
