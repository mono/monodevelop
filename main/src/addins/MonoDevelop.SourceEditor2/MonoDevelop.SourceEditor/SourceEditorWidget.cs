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

using Document = Mono.TextEditor.Document;
using Services = MonoDevelop.Projects.Services;

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
		ISourceEditorOptions options;
		
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
				if (shouldShowclassBrowser == value)
					return;
				shouldShowclassBrowser = value;
				UpdateClassBrowserVisibility ();
			}
		}
		
		bool CanShowClassBrowser {
			get { return canShowClassBrowser; }
			set {
				if (canShowClassBrowser == value)
					return;
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
					classBrowser.Destroy ();
					classBrowser = null;
				}
			}
		}
		
		public void PopulateClassCombo ()
		{
			if (classBrowser == null || !CanShowClassBrowser)
				return;
			
			classBrowser.UpdateCompilationUnit (this.parsedDocument);
		}
		
		public Ambience Ambience {
			get {
				string fileName = this.view.IsUntitled ? this.view.UntitledName : this.view.ContentName;
				return AmbienceService.GetAmbienceForFile (fileName);
			}
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

		}

		public void SetMime (string mimeType)
		{
			this.isInitialParseUpdate = true;
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
					if (this.widget.options.ShowFoldMargin && widget.parsedDocument != null) {
						List<FoldSegment> foldSegments = new List<FoldSegment> ();
						
						foreach (FoldingRegion region in widget.parsedDocument.GenerateFolds ()) {
							if (IsStopping)
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
							if (marker != null && setFolded) {
								// only fold on document open, later added folds are NOT folded by default.
								marker.IsFolded = widget.isInitialParseUpdate && folded;
							}
							if (marker != null && region.Region.Contains (widget.textEditorData.Caret.Line, widget.textEditorData.Caret.Column))
								marker.IsFolded = false;
							
						}
						widget.textEditorData.Document.UpdateFoldSegments (foldSegments);
						if (widget.isInitialParseUpdate) {
							Application.Invoke (delegate {
								widget.textEditorData.Document.WaitForFoldUpdateFinished ();
								widget.TextEditor.CenterToCaret ();
							});
							widget.isInitialParseUpdate = false;
						}
					}
					widget.UpdateAutocorTimer ();
					widget.PopulateClassCombo ();
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
				
				ParsedDocument = args.ParsedDocument;
				bool canShowBrowser = ParsedDocument != null && ParsedDocument.CompilationUnit != null;
				if (canShowBrowser)
					this.CanShowClassBrowser = canShowBrowser; 
			});
		}
		
		public ParsedDocument ParsedDocument  {
			get {
				return this.parsedDocument;
			}
			set {
				this.parsedDocument = value;
				CanShowClassBrowser = value != null && value.CompilationUnit != null;
				
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
			this.splittedTextEditor = new MonoDevelop.SourceEditor.ExtensibleTextEditor (view, this.textEditor.Options, textEditor.Document);
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
		HBox messageBar = null;
		class MessageArea : HBox
		{
			public MessageArea () : base (false, 8)
			{
				BorderWidth = 3;
			}
			
			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				Style.PaintFlatBox (Style,
				                    evnt.Window,
				                    StateType.Normal,
				                    ShadowType.Out,
				                    evnt.Area,
				                    this,
				                    "tooltip",
				                    Allocation.X + 1,
				                    Allocation.Y + 1,
				                    Allocation.Width - 2,
				                    Allocation.Height - 2);
				
				return base.OnExposeEvent (evnt);
			}	
			
			bool changeStyle = false;
			protected override void OnStyleSet (Gtk.Style previous_style)
			{
				if (changeStyle)
					return;
				changeStyle = true;
				Gtk.Window win = new LanguageItemWindow (null, Gdk.ModifierType.None, null, null, null, null);
				win.EnsureStyle ();
				this.Style = win.Style;
				win.Destroy ();
				changeStyle = false;
			}
		}
		
		internal static string StrMiddleTruncate (string str, int truncLen)
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
				messageBar = new MessageArea ();
				messageBar.EnsureStyle ();
				
				Gtk.Image img = ImageService.GetImage ("gtk-dialog-warning", IconSize.Dialog);
				img.SetAlignment (0.5f, 0);
				messageBar.PackStart (img, false, false, 0);
				
				VBox labelBox = new VBox (false, 6);
				
				Gtk.Label l = new Gtk.Label ();
				l.Markup = "<b>" + string.Format (GettextCatalog.GetString ("The file »{0}« has been changed outside of MonoDevelop."), StrMiddleTruncate(Document.FileName, 50)) + "</b>";
				l.Wrap = true;
				l.SetAlignment (0, 0.5f);
				l.Selectable = true;
				l.Style = messageBar.Style;
				labelBox.PackStart (l, false, false, 5);
				
				l = new Gtk.Label ();
				l.Wrap = true;
				l.SetAlignment (0, 0.5f);
				l.Selectable = true;
				l.Markup = "<small>" + string.Format (GettextCatalog.GetString ("Do you want to drop your changes and reload the file?")) +"</small>";
				l.Style = messageBar.Style;
				labelBox.PackStart (l, false, false, 5);
				
				messageBar.PackStart (labelBox, false, false, 5);
				
				VBox box = new VBox ();
				messageBar.PackEnd (box, false, false, 10);
				
				Button b1 = new Button (GettextCatalog.GetString("_Reload"));
				b1.Image = ImageService.GetImage (Gtk.Stock.Refresh, IconSize.Button);
				box.PackStart (b1, false, false, 5);
				b1.Clicked += new EventHandler (ClickedReload);
				
				Button b2 = new Button (GettextCatalog.GetString("_Ignore"));
				b2.Image = ImageService.GetImage (Gtk.Stock.Cancel, IconSize.Button);
				box.PackStart (b2, false, false, 5);
				b2.Clicked += new EventHandler (ClickedIgnore);
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
				messageBar = new MessageArea ();
				messageBar.EnsureStyle ();
				
				Gtk.Image img = ImageService.GetImage ("gtk-dialog-warning", IconSize.Dialog);
				img.SetAlignment (0.5f, 0);
				messageBar.PackStart (img, false, false, 0);
				
				VBox labelBox = new VBox (false, 6);
				
				Gtk.Label l = new Gtk.Label ();
				l.Wrap = true;
				l.SetAlignment (0, 0.5f);
				l.Selectable = true;
				l.Markup = "<b>" + string.Format (GettextCatalog.GetString ("Found auto save file for: »{0}«"), StrMiddleTruncate(fileName, 50)) + "</b>" + Environment.NewLine + Environment.NewLine + 
					string.Format (GettextCatalog.GetString ("This may have following reasons:\n\n1) An other instance of monodevelop is running and editing this file. If this is the case be you could mess up that file.\n2) Monodevelop opened the file and crashed."));
				l.Style = messageBar.Style;
				labelBox.PackStart (l, false, false, 5);
				
				l = new Gtk.Label ();
				l.Wrap = true;
				l.SetAlignment (0, 0.5f);
				l.Selectable = true;
				l.Style = messageBar.Style;
				l.Markup = "<small>" + string.Format (GettextCatalog.GetString ("Do you want to restore the contents of the auto save file?")) +"</small>";
				
				labelBox.PackStart (l, false, false, 5);
				
				messageBar.PackStart (labelBox, false, false, 5);
				
				VBox box = new VBox ();
				messageBar.PackEnd (box, false, false, 10);
				
				Button b1 = new Button (GettextCatalog.GetString("_Load"));
				b1.Image = ImageService.GetImage (Gtk.Stock.Refresh, IconSize.Button);
				box.PackStart (b1, false, false, 5);
				b1.Clicked += delegate {
					try {
						view.AutoSave.FileName = fileName;
						view.AutoSave.RemoveAutoSaveFile ();
						view.Load (fileName);
					} catch (Exception ex) {
						MessageService.ShowException (ex, "Could not remove the auto save file.");
					} finally {
						RemoveMessageBar ();
					}
				};
				
				Button b2 = new Button (GettextCatalog.GetString("_Restore"));
				b2.Image = ImageService.GetImage (Gtk.Stock.RevertToSaved, IconSize.Button);
				box.PackStart (b2, false, false, 5);
				b2.Clicked += delegate {
					try {
						view.AutoSave.FileName = fileName;
						string content = view.AutoSave.LoadAutoSave ();
						view.AutoSave.RemoveAutoSaveFile ();
						view.Load (fileName, content, null);
						view.IsDirty = true;
					} catch (Exception ex) {
						MessageService.ShowException (ex, "Could not remove the auto save file.");
					} finally {
						RemoveMessageBar ();
					}
					
				};
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
			if (searchAndReplaceWidget.SearchFocused) {
				if (replace) {
					this.Replace ();
				} else {
					this.FindNext ();
				}
			}
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
			string url = IdeApp.HelpOperations.GetHelpUrl (res);
			if (url != null)
				IdeApp.HelpOperations.ShowHelp (url);
		}
		
		[CommandUpdateHandler (HelpCommands.Help)]
		internal void MonodocResolverUpdate (CommandInfo cinfo)
		{
			ResolveResult res = TextEditor.GetLanguageItem (TextEditor.Caret.Offset);
			if (res == null)
				cinfo.Bypass = true;
		}
		
		#endregion
		
		#region commenting and indentation
		[CommandUpdateHandler (EditCommands.ToggleCodeComment)]
		protected void OnUpdateToggleComment (MonoDevelop.Components.Commands.CommandInfo info)
		{
			string fileName = this.view.IsUntitled ? this.view.UntitledName : this.view.ContentName;
			ILanguageBinding binding = LanguageBindingService.GetBindingPerFileName (fileName);
			info.Visible = binding != null && !String.IsNullOrEmpty (binding.SingleLineCommentTag);
		}
		
		[CommandHandler (EditCommands.ToggleCodeComment)]
		public void ToggleCodeComment ()
		{
			bool comment = false;
			string fileName = this.view.IsUntitled ? this.view.UntitledName : this.view.ContentName;
			ILanguageBinding binding = LanguageBindingService.GetBindingPerFileName (fileName);
			if (binding == null || String.IsNullOrEmpty (binding.SingleLineCommentTag))
				return;
			string commentTag = binding.SingleLineCommentTag;
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
						TextEditor.SelectionAnchor = anchorLine.Offset;
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
