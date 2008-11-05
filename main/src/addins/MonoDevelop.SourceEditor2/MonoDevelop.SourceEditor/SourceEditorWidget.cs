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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Gtk;
using Mono.TextEditor;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using Document = Mono.TextEditor.Document;

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
		
		const uint CHILD_PADDING = 3;
		
		bool shouldShowclassBrowser;
		bool canShowClassBrowser;
		bool isInitialParseUpdate = true;
		ClassQuickFinder classBrowser;
		
		bool isDisposed = false;
		
		ParsedDocument parsedDocument;
		
		MonoDevelop.SourceEditor.ExtensibleTextEditor textEditor;
		MonoDevelop.SourceEditor.ExtensibleTextEditor splittedTextEditor;
		MonoDevelop.SourceEditor.ExtensibleTextEditor lastActiveEditor;
		
		public MonoDevelop.SourceEditor.ExtensibleTextEditor TextEditor {
			get {
				if (this.splittedTextEditor != null && this.splittedTextEditor.Parent != null
				    && this.splittedTextEditor.HasFocus)
				{
					lastActiveEditor = this.splittedTextEditor;
				}
				if (this.textEditor != null && this.textEditor.Parent != null && this.textEditor.HasFocus)
				{
					lastActiveEditor = this.textEditor;
				}
				return lastActiveEditor;
			}
		}
		
		public bool ShowClassBrowser {
			get { return shouldShowclassBrowser; }
			set {
				shouldShowclassBrowser = value;
				UpdateClassBrowserVisibility ();
			}
		}
		
		bool CanShowClassBrowser {
			get { return canShowClassBrowser; }
			set {
				canShowClassBrowser = value;
				UpdateClassBrowserVisibility ();
			}
		}
		
		void UpdateClassBrowserVisibility ()
		{
			if (shouldShowclassBrowser && canShowClassBrowser) {
				if (classBrowser == null) {
					classBrowser = new ClassQuickFinder (this);
					this.PackStart (classBrowser, false, false, CHILD_PADDING);
					this.ReorderChild (classBrowser, 0);
					classBrowser.ShowAll ();
					PopulateClassCombo ();
				}
			} else {
				if (classBrowser != null) {
					this.Remove (classBrowser);
					classBrowser.Destroy (); //note: calls dispose() (?)
					classBrowser = null;
				}
			}
		}
		
		public void PopulateClassCombo ()
		{
			if (classBrowser != null && this.parsedDocument != null) {
				classBrowser.UpdateCompilationUnit (this.parsedDocument);
			}
		}
		
		public Ambience Ambience {
			get { return AmbienceService.GetAmbienceForFile (view.ContentName); }
		}
		
		#region ITextEditorExtension
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
		
		public SourceEditorWidget (SourceEditorView view)
		{
			this.view = view;
			this.SetSizeRequest (32, 32);
			this.lastActiveEditor = this.textEditor = new MonoDevelop.SourceEditor.ExtensibleTextEditor (view);
			mainsw = new ScrolledWindow ();
			mainsw.ShadowType = ShadowType.In;
			mainsw.Child = this.TextEditor;
			this.PackStart (mainsw, true, true, 0);
			this.mainsw.ButtonPressEvent += PrepareEvent;
			this.textEditor.Errors = errors;
			
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
//			this.IsClassBrowserVisible = SourceEditorOptions.Options.EnableQuickFinder;

		}

		public void SetMime (string mimeType)
		{
			//FIXME: check that the parser is able to return information that we can use
			CanShowClassBrowser = MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetParserByMime (mimeType) != null;
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
		Dictionary<int, Error> errors = new Dictionary<int, Error> ();
		uint resetTimerId;
		bool resetTimerStarted = false;
		
		FoldSegment AddMarker (List<FoldSegment> foldSegments, string text, DomRegion region, FoldingType type)
		{
			Document document = textEditorData.Document;
			if (region == null || document == null || region.Start.Line <= 0 || region.End.Line <= 0
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
				try {
					if (SourceEditorOptions.Options.ShowFoldMargin && widget.parsedDocument != null) {
						List<FoldSegment> foldSegments = new List<FoldSegment> ();
						
						foreach (FoldingRegion region in widget.parsedDocument.GenerateFolds ()) {
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
								setFolded = SourceEditorOptions.Options.DefaultRegionsFolding;
								folded = true;
								break;
							case FoldType.Comment:
								setFolded = SourceEditorOptions.Options.DefaultCommentFolding;
								folded = true;
								break;
							case FoldType.CommentInsideMember:
								setFolded = SourceEditorOptions.Options.DefaultCommentFolding;
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
							if (marker != null && setFolded) {
								// only fold on document open, later added folds are NOT folded by default.
								marker.IsFolded = widget.isInitialParseUpdate && folded;
							}
						}
						widget.textEditorData.Document.UpdateFoldSegments (foldSegments);
					}
					widget.UpdateAutocorTimer ();
					widget.PopulateClassCombo ();
					widget.isInitialParseUpdate = false;
				} catch (Exception ex) {
					LoggingService.LogError ("Unhandled exception in ParseInformationUpdaterWorkerThread", ex);
				}
				base.Stop ();
			}
		}
		
		readonly object syncObject = new object();
		ParseInformationUpdaterWorkerThread parseInformationUpdaterWorkerThread = null;
		
		void OnParseInformationChanged (object sender, ParsedDocumentEventArgs args)
		{
			if (this.isDisposed || args == null || args.ParsedDocument == null || this.view == null || this.view.ContentName != args.FileName) {
				return;
			}
			
			ParsedDocument = args.ParsedDocument;
		}
		
		public ParsedDocument ParsedDocument  {
			get {
				return this.parsedDocument;
			}
			set {
				this.parsedDocument = value;
				lock (syncObject) {
					StopParseInfoThread ();
					if (parsedDocument != null) {
						parseInformationUpdaterWorkerThread = new ParseInformationUpdaterWorkerThread (this);
						parseInformationUpdaterWorkerThread.Start ();
					}
				}
			}
		}
		
		void StopParseInfoThread ()
		{
			if (parseInformationUpdaterWorkerThread != null) {
				parseInformationUpdaterWorkerThread.Stop ();
				parseInformationUpdaterWorkerThread = null;
			}
		}
		
		void UpdateAutocorTimer ()
		{
			if (!SourceEditorOptions.Options.UnderlineErrors)
				return;
			uint timeout = 900;
			if (resetTimerStarted) {
				// Reset the timer
				GLib.Source.Remove (resetTimerId);
				resetTimerStarted = false;
			} else {
				// Start the timer for the first time
				resetTimerStarted = true;
			}
			resetTimerId = GLib.Timeout.Add (timeout, AutocorrResetMeth);
		}
		
		bool AutocorrResetMeth ()
		{
			ResetUnderlineChangement ();
			if (parsedDocument != null)
				ParseCompilationUnit (parsedDocument);
			resetTimerStarted = false;
			return false;
		}
		
		void ResetUnderlineChangement ()
		{
			if (errors.Count > 0) {
				foreach (Error error in this.errors.Values) {
					error.RemoveFromLine ();
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
		
		void UnderLineError (MonoDevelop.Projects.Dom.Error info)
		{
			if (this.isDisposed)
				return;
			// Adjust the line to Gtk line representation
//			info.Line -= 1;
			
			// If the line is already underlined
			if (errors.ContainsKey (info.Line - 1))
				return;
			
			LineSegment line = this.TextEditor.Document.GetLine (info.Line - 1);
			Error error = new Error (this.TextEditor.Document, info, line); 
			errors [info.Line - 1] = error;
			error.AddToLine ();
		}
		#endregion
		
		protected override void OnDestroyed ()
		{
			if (!isDisposed) {
				isDisposed = true;
				StopParseInfoThread ();
				
				mainsw.ButtonPressEvent -= PrepareEvent;
				
				this.textEditor = null;
				this.lastActiveEditor = null;
				this.splittedTextEditor = null;
				view = null;
				
				ProjectDomService.ParsedDocumentUpdated -= OnParseInformationChanged;
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
			
			splitContainer.Remove (mainsw);
			if (this.textEditor == lastActiveEditor) {
				secondsw.Destroy ();
				secondsw           = null;
				splittedTextEditor = null;
			} else {
				this.mainsw.Destroy ();
				this.mainsw = secondsw;
				splitContainer.Remove (secondsw);
				lastActiveEditor = this.textEditor = splittedTextEditor;
				splittedTextEditor = null;
			}
			this.Remove (splitContainer);
			splitContainer.Destroy ();
			splitContainer = null;
			
			this.PackStart (mainsw, true, true, 0);
			this.ShowAll ();
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
		public void Split (bool vSplit)
		{
 			if (splitContainer != null) 
				Unsplit ();
			
			this.Remove (this.mainsw);
			
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
			this.splittedTextEditor = new MonoDevelop.SourceEditor.ExtensibleTextEditor (view, textEditor.Document);
			this.splittedTextEditor.Extension = textEditor.Extension;
			this.splittedTextEditor.Caret.ModeChanged += delegate {
				this.UpdateLineCol ();
			};
			this.splittedTextEditor.SelectionChanged += delegate {
				this.UpdateLineCol ();
			};
			this.splittedTextEditor.Caret.PositionChanged += CaretPositionChanged;
			
			secondsw.Child = splittedTextEditor;
			splitContainer.Add2 (secondsw);
			this.PackStart (splitContainer, true, true, 0);
			this.splitContainer.Position = (vSplit ? this.Allocation.Height : this.Allocation.Width) / 2;
			this.ShowAll ();
			
		}
//		void SplitContainerSizeRequested (object sender, SizeRequestedArgs args)
//		{
//			this.splitContainer.SizeRequested -= SplitContainerSizeRequested;
//			this.splitContainer.Position = args.Requisition.Width / 2;
//			this.splitContainer.SizeRequested += SplitContainerSizeRequested;
//		}
//		
		HBox reloadBar = null;
		public void ShowFileChangedWarning ()
		{
			RemoveReloadBar ();
			
			if (reloadBar == null) {
				reloadBar = new HBox ();
				reloadBar.BorderWidth = 3;
				Gtk.Image img = MonoDevelop.Core.Gui.Services.Resources.GetImage ("gtk-dialog-warning",
				                                                                  IconSize.Menu);
				reloadBar.PackStart (img, false, false, 2);
				reloadBar.PackStart (new Gtk.Label (GettextCatalog.GetString (
					"This file has been changed outside of MonoDevelop")), false, false, 5);
				HBox box = new HBox ();
				reloadBar.PackStart (box, true, true, 10);
				
				Button b1 = new Button (GettextCatalog.GetString("Reload"));
				box.PackStart (b1, false, false, 5);
				b1.Clicked += new EventHandler (ClickedReload);
				
				Button b2 = new Button (GettextCatalog.GetString("Ignore"));
				box.PackStart (b2, false, false, 5);
				b2.Clicked += new EventHandler (ClickedIgnore);
			}
			
			view.WarnOverwrite = true;
			this.PackStart (reloadBar, false, false, CHILD_PADDING);
			this.ReorderChild (reloadBar, classBrowser != null ? 1 : 0);
			reloadBar.ShowAll ();
			view.WorkbenchWindow.ShowNotification = true;
		}
		
		public void RemoveReloadBar ()
		{
			if (reloadBar != null) {
				if (reloadBar.Parent == this)
					this.Remove (reloadBar);
				reloadBar.Destroy ();
				reloadBar = null;
			}
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
				RemoveReloadBar ();
			}
		}
		
		void ClickedIgnore (object sender, EventArgs args)
		{
			RemoveReloadBar ();
			view.WorkbenchWindow.ShowNotification = false;
		}
		
		#region Status Bar Handling
		void CaretPositionChanged (object o, DocumentLocationEventArgs args)
		{
			UpdateLineCol ();
			
			int line = TextEditor.Caret.Line + 1;
			int column = TextEditor.Caret.Column;
			
			if (classBrowser != null)
				classBrowser.UpdatePosition (line, column);
		}
		
//		void OnChanged (object o, EventArgs e)
//		{
//			UpdateLineCol ();
//			OnContentChanged (null);
//			needsUpdate = true;
//		}
		
		void UpdateLineCol ()
		{
			
			int offset = this.TextEditor.Caret.Offset;
			if (offset < 0 || offset >= this.TextEditor.Document.Length)
				return;
			DocumentLocation location = this.TextEditor.LogicalToVisualLocation (this.TextEditor.Caret.Location);
			IdeApp.Workbench.StatusBar.ShowCaretState (this.TextEditor.Caret.Line + 1,
			                                           location.Column + 1,
			                                           this.TextEditor.IsSomethingSelected ?
			                                               this.TextEditor.SelectionRange.Length
			                                               : 0,
			                                           this.TextEditor.Caret.IsInInsertMode);
		}
		
		#endregion
		
		#region Search and Replace
		SearchAndReplaceWidget searchAndReplaceWidget = null;
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
			if (searchAndReplaceWidget != null) {
				if (searchAndReplaceWidget.Parent != null)
					this.Remove (searchAndReplaceWidget);
				searchAndReplaceWidget.Destroy ();
				searchAndReplaceWidget = null;
				result = true;
			}
			
			if (gotoLineNumberWidget != null) {
				if (gotoLineNumberWidget.Parent != null)
					this.Remove (gotoLineNumberWidget);
				gotoLineNumberWidget.Destroy ();
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
		
		private void ShowSearchReplaceWidget (bool replace)
		{
			if (searchAndReplaceWidget == null) {
				KillWidgets ();
				if (TextEditor.IsSomethingSelected)
					TextEditor.SearchPattern = TextEditor.SelectedText;
				searchAndReplaceWidget = new SearchAndReplaceWidget (this);
				this.PackEnd (searchAndReplaceWidget);
				this.SetChildPacking (searchAndReplaceWidget, false, false, CHILD_PADDING, PackType.End);
		//		searchAndReplaceWidget.ShowAll ();
				this.textEditor.HighlightSearchPattern = true;
				if (this.splittedTextEditor != null) 
					this.splittedTextEditor.HighlightSearchPattern = true;
				
				ResetFocusChain ();
			}
			searchAndReplaceWidget.IsReplaceMode = replace;
			searchAndReplaceWidget.Focus ();
		}
		
		[CommandHandler (SearchCommands.GotoLineNumber)]
		public void ShowGotoLineNumberWidget ()
		{
			if (gotoLineNumberWidget == null) {
				KillWidgets ();
				gotoLineNumberWidget = new GotoLineNumberWidget (this);
				this.Add (gotoLineNumberWidget);
				this.SetChildPacking(gotoLineNumberWidget, false, false, CHILD_PADDING, PackType.End);
				gotoLineNumberWidget.ShowAll ();
				ResetFocusChain ();
				
			}
			gotoLineNumberWidget.Focus ();
		}
		
		internal void SetSearchOptions ()
		{
			this.textEditor.SearchEngine    = 
				SearchAndReplaceWidget.SearchEngine == SearchAndReplaceWidget.DefaultSearchEngine ?
					(ISearchEngine)new BasicSearchEngine ()
					: (ISearchEngine)new RegexSearchEngine ();
			this.textEditor.IsCaseSensitive = SearchAndReplaceWidget.IsCaseSensitive;
			this.textEditor.IsWholeWordOnly = SearchAndReplaceWidget.IsWholeWordOnly;
			
			string error;
			string pattern = SearchAndReplaceWidget.searchPattern;
			if (searchAndReplaceWidget != null)
				pattern = searchAndReplaceWidget.SearchPattern;
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
			SearchResult result = TextEditor.FindNext ();
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
			SearchResult result = TextEditor.FindPrevious ();
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
	
		[CommandHandler (SearchCommands.FindNextSelection)]
		public SearchResult FindNextSelection ()
		{
			SetSearchOptions ();
			SetSearchPattern();
			TextEditor.GrabFocus ();
			return FindNext ();
		}
	
		[CommandHandler (SearchCommands.FindPreviousSelection)]
		public SearchResult FindPreviousSelection ()
		{
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
	}

	class Error
	{
		public MonoDevelop.Projects.Dom.Error info;
		public LineSegment line;
		public Mono.TextEditor.Document doc;
		TextMarker marker = new UnderlineMarker ();
		
		public Error (Mono.TextEditor.Document doc, MonoDevelop.Projects.Dom.Error info, LineSegment line)
		{
			this.info = info;
			this.line = line; // may be null if no line is assigned to the error.
			this.doc  = doc;
		}
		
		public void AddToLine ()
		{
			if (line != null) {
				doc.AddMarker (line, marker);
			}
		}
		
		public void RemoveFromLine ()
		{
			if (line != null) {
				doc.RemoveMarker (line, marker);
			}
		}
	}
	
}
