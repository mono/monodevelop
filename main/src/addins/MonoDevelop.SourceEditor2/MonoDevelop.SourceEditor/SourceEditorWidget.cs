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

namespace MonoDevelop.SourceEditor
{
	public partial class SourceEditorWidget : Gtk.Bin, ITextEditorExtension
	{
		SourceEditorView view;
		bool isClassBrowserVisible = true;
		bool isDisposed = false;
		bool loadingMembers = false;
		ListStore classStore;
		ListStore memberStore;
		ListStore regionStore;
		
		ICompilationUnit memberParseInfo;
		IDocumentMetaInformation metaInfo;
		bool handlingParseEvent = false;
		Tooltips tips = new Tooltips ();
		
		MonoDevelop.SourceEditor.ExtendibleTextEditor textEditor;
		MonoDevelop.SourceEditor.ExtendibleTextEditor splittedTextEditor;
		MonoDevelop.SourceEditor.ExtendibleTextEditor lastActiveEditor;
		
		public MonoDevelop.SourceEditor.ExtendibleTextEditor TextEditor {
			get {
				if (this.splittedTextEditor != null && this.splittedTextEditor.Parent != null && this.splittedTextEditor.HasFocus)
					lastActiveEditor = this.splittedTextEditor;
				if (this.textEditor != null && this.textEditor.Parent != null && this.textEditor.HasFocus)
					lastActiveEditor = this.textEditor;
				return lastActiveEditor;
			}
		}
		
		public bool IsClassBrowserVisible {
			get {
				return isClassBrowserVisible;
			}
			set {
				classBrowser.Visible = value;
				isClassBrowserVisible = value;
				if (isClassBrowserVisible)
					BindClassCombo ();
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
			if (key == Gdk.Key.Escape)
				return true;
			this.TextEditor.SimulateKeyPress (key, (uint)keyChar, modifier);
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
			this.Build();
			this.lastActiveEditor = this.textEditor = new MonoDevelop.SourceEditor.ExtendibleTextEditor (view);
			this.mainsw.Child = this.TextEditor;
			this.mainsw.ButtonPressEvent += PrepareEvent;
			this.textEditor.Errors = errors;
			
			this.textEditor.Caret.ModeChanged += delegate {
				this.UpdateLineCol ();
			};
			this.textEditor.Caret.PositionChanged += CaretPositionChanged;
			this.textEditor.SelectionChanged += delegate {
				this.UpdateLineCol ();
			};
			// Setup the columns and column renders for the comboboxes
			CellRendererPixbuf pixr = new CellRendererPixbuf ();
			pixr.Ypad = 0;
			classCombo.PackStart (pixr, false);
			classCombo.AddAttribute (pixr, "pixbuf", 0);
			CellRenderer colr = new CellRendererText();
			colr.Ypad = 0;
			classCombo.PackStart (colr, true);
			classCombo.AddAttribute (colr, "text", 1);
			
			pixr = new CellRendererPixbuf ();
			pixr.Ypad = 0;
			
			membersCombo.PackStart (pixr, false);
			membersCombo.AddAttribute (pixr, "pixbuf", 0);
			colr = new CellRendererText ();
			colr.Ypad = 0;
			membersCombo.PackStart (colr, true);
			membersCombo.AddAttribute (colr, "text", 1);
			
			regionCombo.PackStart (pixr, false);
			regionCombo.AddAttribute (pixr, "pixbuf", 0);
			colr = new CellRendererText ();
			colr.Ypad = 0;
			regionCombo.PackStart (colr, true);
			regionCombo.AddAttribute (colr, "text", 1);
			
			// Pack the controls into the editorbar just below the file name tabs.
//			EventBox tbox = new EventBox ();
//			tbox.Add (classCombo);
//			classBrowser.PackStart(tbox, true, true, 0);
//			tbox = new EventBox ();
//			tbox.Add (membersCombo);
//			classBrowser.PackStart (tbox, true, true, 0);
			
			// Set up the data stores for the comboboxes
			classStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(IType));
			classCombo.Model = classStore;	
			classCombo.Changed += new EventHandler (ClassChanged);
			tips.SetTip (classCombo, GettextCatalog.GetString ("Type list"), null);
			
			memberStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(IMember));
			memberStore.SetSortColumnId (1, Gtk.SortType.Ascending);
			membersCombo.Model = memberStore;
			membersCombo.Changed += new EventHandler (MemberChanged);
			tips.SetTip (membersCombo, GettextCatalog.GetString ("Member list"), null);
			
			regionStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(DomRegion));
			regionCombo.Model = regionStore;	
			regionCombo.Changed += new EventHandler (RegionChanged);
			tips.SetTip (regionCombo, GettextCatalog.GetString ("Region list"), null);
			
			ResetFocusChain ();
			ProjectDomService.CompilationUnitUpdated += UpdateClassBrowser;
			
			UpdateLineCol ();
			
			this.Focused += delegate {
				UpdateLineCol ();
			};
			ProjectDomService.CompilationUnitUpdated += OnParseInformationChanged;
//			this.IsClassBrowserVisible = SourceEditorOptions.Options.EnableQuickFinder;
			
			this.Destroyed += delegate {
				this.Dispose ();
			};
		}

		void UpdateMetaInformation ()
		{
			if (this.textEditor == null || this.textEditor.Document == null)
				return;
			IParser parser = ProjectDomService.GetParserByMime (this.textEditor.Document.MimeType);
			if (parser == null)
				return;
			metaInfo = parser.CreateMetaInformation (this.textEditor.Document.OpenTextReader ());
/*			BindRegionCombo ();
			UpdateRegionCombo (TextEditor.Caret.Line + 1, TextEditor.Caret.Column);*/
		}
		
		public void SetMime (string mimeType)
		{
			IsClassBrowserVisible = MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetParserByMime (mimeType) != null;
		}
		
		void ResetFocusChain ()
		{
			this.editorBar.FocusChain = new Widget[] {
				this.textEditor,
				this.classCombo,
				this.membersCombo,
				this.regionCombo
			};
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			classCombo.WidthRequest   = allocation.Width * 2 / 6 - 6;
			membersCombo.WidthRequest = allocation.Width * 3 / 6 - 6;
			regionCombo.WidthRequest  = allocation.Width / 6;
			
			base.OnSizeAllocated (allocation);
		}
		
		#region Error underlining
		Dictionary<int, Error> errors = new Dictionary<int, Error> ();
		uint resetTimerId;
		ICompilationUnit lastCu = null;
		bool resetTimerStarted = false;
		
		FoldSegment AddMarker (List<FoldSegment> foldSegments, string text, DomRegion region, FoldingType type)
		{
			if (region == null || this.TextEditor == null || this.TextEditor.Document == null || region.Start.Line <= 0 || region.End.Line <= 0 || region.Start.Line >= this.TextEditor.Document.LineCount || region.End.Line >= this.TextEditor.Document.LineCount)
				return null;
			int startOffset = this.TextEditor.Document.LocationToOffset (region.Start.Line - 1,  region.Start.Column - 1);
			int endOffset   = this.TextEditor.Document.LocationToOffset (region.End.Line - 1,  region.End.Column - 1);
			FoldSegment result = new FoldSegment (text, startOffset, endOffset - startOffset, type);
			
			foldSegments.Add (result);
			return result;
		}
		
		void AddClass (List<FoldSegment> foldSegments, IType cl)
		{
			if (this.TextEditor == null || this.TextEditor.Document == null)
				return;
			if (cl.BodyRegion != null && cl.BodyRegion.End.Line > cl.BodyRegion.Start.Line) {
				LineSegment startLine = this.TextEditor.Document.GetLine (cl.Location.Line - 1);
				if (startLine != null) {
					int startOffset = startLine.Offset + startLine.EditableLength;
					int endOffset   = this.TextEditor.Document.LocationToOffset (cl.BodyRegion.End.Line - 1,  cl.BodyRegion.End.Column);
					foldSegments.Add (new FoldSegment ("...", startOffset, endOffset - startOffset, FoldingType.TypeDefinition));
				}
			}
			foreach (IType inner in cl.InnerTypes) 
				AddClass (foldSegments, inner);
			if (cl.ClassType == ClassType.Interface)
				return;
			foreach (IMethod method in cl.Methods) {
				if (method.Location == null || method.BodyRegion == null || method.BodyRegion.End.Line <= 0 /*|| method.Region.End.Line == method.BodyRegion.End.Line*/)
					continue;
				LineSegment startLine = this.TextEditor.Document.GetLine (method.Location.Line - 1);
				if (startLine == null)
					continue;
				int startOffset = startLine.Offset + startLine.EditableLength;
				int endOffset   = this.TextEditor.Document.LocationToOffset (method.BodyRegion.End.Line - 1,  method.BodyRegion.End.Column);
				foldSegments.Add (new FoldSegment ("...", startOffset, endOffset - startOffset, FoldingType.TypeMember));
			}
			
			foreach (IProperty property in cl.Properties) {
				if (property.Location == null || property.BodyRegion == null || property.BodyRegion.End.Line <= 0 /*|| property.Region.End.Line == property.BodyRegion.End.Line*/)
					continue;
				LineSegment startLine = this.TextEditor.Document.GetLine (property.Location.Line - 1);
				if (startLine == null)
					continue;
				
				int startOffset = startLine.Offset + startLine.EditableLength;
				int endOffset   = this.TextEditor.Document.LocationToOffset (property.BodyRegion.End.Line - 1,  property.BodyRegion.End.Column);
				foldSegments.Add (new FoldSegment ("...", startOffset, endOffset - startOffset, FoldingType.TypeMember));
			}
		}
		
		void AddUsings (List<FoldSegment> foldSegments, ICompilationUnit cu)
		{
			if (cu.Usings == null || cu.Usings.Count == 0)
				return;
			IUsing first = cu.Usings[0];
			IUsing last = cu.Usings[cu.Usings.Count - 1];
			if (first.Region == null || last.Region == null || first.Region.Start.Line == last.Region.End.Line)
				return;
			System.Console.WriteLine(first.Region);
			int startOffset = this.TextEditor.Document.LocationToOffset (first.Region.Start.Line - 1,  first.Region.Start.Column - 1);
			int endOffset   = this.TextEditor.Document.LocationToOffset (last.Region.End.Line - 1,  last.Region.End.Column - 1);
			
			foldSegments.Add (new FoldSegment ("...", startOffset, endOffset - startOffset, FoldingType.TypeMember));
		}
			
		
		class ParseInformationUpdaterWorkerThread : WorkerThread
		{
			SourceEditorWidget widget;
			//ParseInformationEventArgs args;
			
			public ParseInformationUpdaterWorkerThread (SourceEditorWidget widget)
			{
				this.widget = widget;
				//this.args = args;
			}
			
			bool IsInsideMember (FoldSegment marker, DomRegion region, IType cl)
			{
				if (region == null || !cl.BodyRegion.Contains (region.Start))
					return false;
				foreach (IMember member in cl.Members) {
					if (member.BodyRegion == null)
						continue;
					if (member.BodyRegion.Contains (region.Start) && member.BodyRegion.Contains (region.End)) 
						return true;
				}
				foreach (IType inner in cl.InnerTypes) {
					if (IsInsideMember (marker, region, inner))
						return true;
				}
				return false;
			}
			
			protected override void InnerRun ()
			{
				try {
					if (SourceEditorOptions.Options.ShowFoldMargin && widget.lastCu != null) {
						List<FoldSegment> foldSegments = new List<FoldSegment> ();
						widget.AddUsings (foldSegments, widget.lastCu);
						
						if (widget.lastCu != null) {
							foreach (IType cl in widget.lastCu.Types) {
								if (base.IsStopping)
									return;
								widget.AddClass (foldSegments, cl);
							}
							
							foreach (FoldingRegion region in widget.lastCu.FoldingRegions) {
								FoldSegment marker = widget.AddMarker
									(foldSegments, region.Name, region.Region, FoldingType.Region);
								if (marker != null) 
									marker.IsFolded =
										SourceEditorOptions.Options.DefaultRegionsFolding
										&& region.DefaultIsFolded;
							
							}
						}
						
						if (widget.metaInfo != null && widget.metaInfo.FoldingRegion != null) {
							foreach (FoldingRegion region in widget.metaInfo.FoldingRegion) {
								FoldSegment marker = widget.AddMarker (foldSegments, region.Name, region.Region, FoldingType.Region);
								if (marker != null) 
									marker.IsFolded = SourceEditorOptions.Options.DefaultRegionsFolding && region.DefaultIsFolded;
							}
							
							if (widget.metaInfo.Comments.Count > 0) {
								Comment firstComment = null;
								string commentText = null;
								DomRegion commentRegion = DomRegion.Empty;
								for (int i = 0; i < widget.metaInfo.Comments.Count; i++) {
									Comment comment = widget.metaInfo.Comments[i];
									FoldSegment marker = null;
									if (comment.CommentType == CommentType.MultiLine) {
										commentText = "/* */";
										firstComment = null;
										marker = widget.AddMarker (foldSegments, commentText, comment.Region, FoldingType.Region);
										commentRegion = comment.Region;
									} else {
										if (!comment.CommentStartsLine)
											continue;
										int j = i;
										int curLine = comment.Region.Start.Line - 1;
										DomLocation end = comment.Region.End;
										for (; j < widget.metaInfo.Comments.Count; j++) {
											Comment  curComment  = widget.metaInfo.Comments[j];
											if (curComment == null || !curComment.CommentStartsLine || curComment.CommentType != comment.CommentType || curLine + 1 != curComment.Region.Start.Line)
												break;
											end     = curComment.Region.End;
											curLine = curComment.Region.Start.Line;
										}
										if (j - i > 1) {
											commentRegion = new DomRegion(comment.Region.Start.Line, comment.Region.Start.Column, end.Line, end.Column);
											marker = widget.AddMarker (foldSegments, 
											                    comment.IsDocumentation  ? "/// " : "// "  + comment.Text + "...", 
											                    commentRegion, 
											                    FoldingType.Region);
											
											i = j - 1;
										}
									}
									if (marker != null && widget.lastCu != null && SourceEditorOptions.Options.DefaultCommentFolding) {
										bool isInsideMember = false;
										foreach (IType type in widget.lastCu.Types) {
											if (IsInsideMember (marker, commentRegion, type)) {
												isInsideMember = true;
												break;
											}
										}
										marker.IsFolded = !isInsideMember;
									}
								}
							}
						}
						widget.TextEditor.Document.UpdateFoldSegments (foldSegments);
					}
					
					widget.UpdateAutocorTimer ();
				} catch (Exception ex) {
					LoggingService.LogError ("Unhandled exception in ParseInformationUpdaterWorkerThread", ex);
				}
				base.Stop ();
			}
		}
		
		readonly object syncObject = new object();
		ParseInformationUpdaterWorkerThread parseInformationUpdaterWorkerThread = null;
		
		void OnParseInformationChanged (object sender, CompilationUnitEventArgs args)
		{
			UpdateMetaInformation ();
			if (this.isDisposed || args == null || args.Unit == null || this.view == null  || this.view.ContentName != args.Unit.FileName)
				return;
			MonoDevelop.SourceEditor.ExtendibleTextEditor editor = this.TextEditor;
			if (editor == null || editor.Document == null)
				return;
			lock (syncObject) {
				lastCu = args.Unit;
				StopParseInfoThread ();
				if (lastCu != null) {
					parseInformationUpdaterWorkerThread = new ParseInformationUpdaterWorkerThread (this);
					parseInformationUpdaterWorkerThread.Start ();
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
			uint timeout = 900;
			
			if (resetTimerStarted) {
				// Reset the timer
				GLib.Source.Remove (resetTimerId);
			} else {
				// Start the timer for the first time
				resetTimerStarted = true;
			}
			resetTimerId = GLib.Timeout.Add (timeout, AutocorrResetMeth);
		}
		
		bool AutocorrResetMeth ()
		{
			ResetUnderlineChangement ();
			if (lastCu != null)
				ParseCompilationUnit (lastCu);
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
		void ParseCompilationUnit (ICompilationUnit cu)
		{
			// No new errors
			if (cu.Errors == null || cu.Errors.Count > 0)
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
				
				this.textEditor = null;
				this.lastActiveEditor = null;
				this.splittedTextEditor = null;
				ProjectDomService.CompilationUnitUpdated -= UpdateClassBrowser;
				ProjectDomService.CompilationUnitUpdated -= OnParseInformationChanged;
			}			
			base.OnDestroyed ();
		}
		
		void UpdateClassBrowser (object sender, CompilationUnitEventArgs args)
		{
			// This event handler can get called when files other than the current content are updated. eg.
			// when loading a new document. If we didn't do this check the member combo for this tab would have
			// methods for a different class in it!
			if (view.ContentName == args.Unit.FileName && !handlingParseEvent) {
				handlingParseEvent = true;
				memberParseInfo = args.Unit;
				GLib.Timeout.Add (100, new GLib.TimeoutHandler (BindClassCombo));
			}
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
			editorBar.Remove (splitContainer);
			splitContainer.Destroy ();
			splitContainer = null;
			
			editorBar.PackStart (mainsw);
			editorBar.ShowAll ();
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
			
			editorBar.Remove (this.mainsw);
			
			this.splitContainer = vSplit ? (Gtk.Paned)new VPaned () : (Gtk.Paned)new HPaned ();
			
			splitContainer.Add1 (mainsw);
			
			this.splitContainer.ButtonPressEvent += delegate(object sender, ButtonPressEventArgs args) {
				if (args.Event.Type == Gdk.EventType.TwoButtonPress && args.RetVal == null) {
					Unsplit (); 
				}
			};
			secondsw = new ScrolledWindow ();
			secondsw.ButtonPressEvent += PrepareEvent;
			this.splittedTextEditor = new MonoDevelop.SourceEditor.ExtendibleTextEditor (view, textEditor.Document);
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
			editorBar.PackStart (splitContainer);
			this.splitContainer.Position = (vSplit ? this.Allocation.Height : this.Allocation.Width) / 2;
			editorBar.ShowAll ();
			
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
				Gtk.Image img = MonoDevelop.Core.Gui.Services.Resources.GetImage ("gtk-dialog-warning", IconSize.Menu);
				reloadBar.PackStart (img, false, false, 2);
				reloadBar.PackStart (new Gtk.Label (GettextCatalog.GetString ("This file has been changed outside of MonoDevelop")), false, false, 5);
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
			editorBar.PackStart (reloadBar, false, true, 0);
			editorBar.ReorderChild (reloadBar, this.isClassBrowserVisible ? 1 : 0);
			reloadBar.ShowAll ();
			view.WorkbenchWindow.ShowNotification = true;
		}
		
		public void RemoveReloadBar ()
		{
			if (reloadBar != null) {
				if (reloadBar.Parent == editorBar)
					editorBar.Remove (reloadBar);
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
			UpdateMethodBrowser ();
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
			IdeApp.Workbench.StatusBar.ShowCaretState (this.TextEditor.Caret.Line + 1, location.Column + 1, this.textEditor.IsSomethingSelected ? this.TextEditor.SelectionRange.Length : 0, this.TextEditor.Caret.IsInInsertMode);
		}
		
		#endregion
		
		#region Class/Member combo handling
		void UpdateMethodBrowser ()
		{
			if (!this.IsClassBrowserVisible)
				return;
			
			if (memberParseInfo == null) {
//				classBrowser.Visible = false;
				return;
			}
			
			int line = TextEditor.Caret.Line + 1;
			int column = TextEditor.Caret.Column;
			
			// Find the selected class
			
			KeyValuePair<IType, int> c = SearchClass (line);
			IType classFound = c.Key;
			
			loadingMembers = true;
			try {
				UpdateRegionCombo (line, column);
				if (classFound == null) {
					classCombo.Active = -1;
					membersCombo.Active = -1;
					memberStore.Clear ();
					this.UpdateClassComboTip (null);
					this.UpdateMemberComboTip (null);
					return;
				}
				
				TreeIter iter;
				if (c.Value != classCombo.Active) {
					classCombo.Active = c.Value; 
					BindMemberCombo (classFound);
					return;
				}
				
				// Find the member
				if (!memberStore.GetIterFirst (out iter))
					return;
				do {
					IMember mem = (IMember) memberStore.GetValue (iter, 2);
					if (IsMemberSelected (mem, line, column)) {
						membersCombo.SetActiveIter (iter);
						this.UpdateMemberComboTip (mem);
						return;
					}
				}
				while (memberStore.IterNext (ref iter));
				membersCombo.Active = -1;
				this.UpdateMemberComboTip (null);
			} finally {
				loadingMembers = false;
			}
		}
		
		class LanguageItemComparer: IComparer<IMember>
		{
			public int Compare (IMember x, IMember y)
			{
				return string.Compare (x.Name, y.Name, true);
			}
		}
		
		bool BindClassCombo ()
		{
			if (this.isDisposed || !this.isClassBrowserVisible)
				return false;
			
			loadingMembers = true;
			
			try {
				// Clear down all our local stores.
				classStore.Clear();				
				
				// check the IParseInformation member variable to see if we could get ParseInformation for the 
				// current docuement. If not we can't display class and member info so hide the browser bar.
				if (memberParseInfo == null) {
//					classBrowser.Visible = false;
					return false;
				}
				
				ReadOnlyCollection<IType> cls = memberParseInfo.Types;
				// if we've got this far then we have valid parse info - but if we have not classes the not much point
				// in displaying the browser bar
				if (cls.Count == 0) {
//					classBrowser.Visible = false;
					return false;
				}
				
//				classBrowser.Visible = true;
				List<IMember> classes = new List<IMember> ();
				foreach (IType c in cls)
					classes.Add (c);
				classes.Sort (new LanguageItemComparer ());
				foreach (IType c in classes)
					Add (c, string.Empty);
				
				int line = TextEditor.Caret.Line + 1;
//				this.GetLineColumnFromPosition(this.CursorPosition, out line, out column);
				KeyValuePair<IType, int> ckvp = SearchClass (line);
				
				IType foundClass = ckvp.Key;
				if (foundClass != null) {
					// found the right class. Now need right method
					classCombo.Active = ckvp.Value;
					BindMemberCombo (foundClass);
				} else {
					// Sometimes there might be no classes e.g. AssemblyInfo.cs
					classCombo.Active = -1;
					this.UpdateClassComboTip ( null);
				}
			} finally {
				handlingParseEvent = false;
				loadingMembers = false;
			}
			// return false to stop the GLib.Timeout
			return false;
		}
		
		void UpdateRegionCombo (int line, int column)
		{
			int regionNumber = 0;
			if (metaInfo != null && metaInfo.FoldingRegion != null) {
				foreach (FoldingRegion region in metaInfo.FoldingRegion) {
					if (region.Region.Start.Line <= line && line <= region.Region.End.Line) {
						regionCombo.Active = regionNumber;
						tips.SetTip (regionCombo, GettextCatalog.GetString ("Region {0}", region.Name), null);
						return;
					}
					regionNumber++;
				}
			}
			tips.SetTip (regionCombo, GettextCatalog.GetString ("Region list"), null);
			regionCombo.Active = -1;
		}
		
		void BindRegionCombo ()
		{
			regionCombo.Model = null;
			regionStore.Clear ();
			if (metaInfo == null || metaInfo.FoldingRegion == null) 
				return;
			foreach (FoldingRegion region in metaInfo.FoldingRegion) {
				regionStore.AppendValues (IdeApp.Services.Resources.GetIcon(Gtk.Stock.Add, IconSize.Menu), 
				                          region.Name, 
				                          region.Region);
			}
			//bool isVisible = cu.FoldingRegions.Count > 0; 
			regionCombo.Model = regionStore;
		}
		
		void BindMemberCombo (IType c)
		{
			if (!this.IsClassBrowserVisible)
				return;

			int position = 0;
			int activeIndex = -1;
			
			// find out where the current cursor position is and set the combos.
			int line   = this.TextEditor.Caret.Line + 1;
			int column = this.TextEditor.Caret.Column + 1;
			this.UpdateClassComboTip (c);
			membersCombo.Changed -= new EventHandler (MemberChanged);
			// Clear down all our local stores.
			
			membersCombo.Model = null;
			memberStore.Clear();
			this.UpdateMemberComboTip (null);
				
			//HybridDictionary methodMap = new HybridDictionary();
			
			Gdk.Pixbuf pix;
			
			List<IMember> members = new List<IMember> ();
			foreach (IMember item in c.Methods)
				 members.Add (item);
			foreach (IMember item in c.Properties)
				 members.Add (item);
			foreach (IMember item in c.Fields)
				 members.Add (item);
			members.Sort (new LanguageItemComparer ());
			
			// Add items to the member drop down 
			
			foreach (IMember mem in members) {
				pix = IdeApp.Services.Resources.GetIcon (mem.StockIcon, IconSize.Menu); 
				
				// Add the member to the list
				Ambience ambience = AmbienceService.GetAmbienceForFile (view.ContentName);
				string displayName = ambience.GetString (mem, OutputFlags.ClassBrowserEntries);
				memberStore.AppendValues (pix, displayName, mem);
				
				// Check if the current cursor position in inside this member
				if (IsMemberSelected (mem, line, column)) {
					this.UpdateMemberComboTip (mem);
					activeIndex = position;
				}
				
				position++;
			}
			membersCombo.Model = memberStore;
			
			// set active the method the cursor is in
			membersCombo.Active = activeIndex;
			membersCombo.Changed += new EventHandler (MemberChanged);
		}
		
		void MemberChanged (object sender, EventArgs e)
		{
			if (loadingMembers)
				return;

			Gtk.TreeIter iter;
			if (membersCombo.GetActiveIter (out iter)) {	    
				// Find the IMember object in our list store by name from the member combo
				IMember member = (IMember) memberStore.GetValue (iter, 2);
				int line = member.Location.Line;
				
				// Get a handle to the current document
				if (IdeApp.Workbench.ActiveDocument == null) {
					return;
				}
				
				// If we can we navigate to the line location of the IMember.
				IExtensibleTextEditor content = (IExtensibleTextEditor) IdeApp.Workbench.ActiveDocument.GetContent(typeof(IExtensibleTextEditor));
				if (content != null)
					content.SetCaretTo (Math.Max (1, line), 1);
			}
		}
		
		void ClassChanged(object sender, EventArgs e)
		{
			if (loadingMembers)
				return;
			
			Gtk.TreeIter iter;
			if (classCombo.GetActiveIter(out iter)) {
				IType selectedClass = (IType)classStore.GetValue(iter, 2);
				int line = selectedClass.Location.Line;
				
				// Get a handle to the current document
				if (IdeApp.Workbench.ActiveDocument == null) {
					return;
				}
				
				// If we can we navigate to the line location of the IMember.
				IExtensibleTextEditor content = (IExtensibleTextEditor) IdeApp.Workbench.ActiveDocument.GetContent(typeof(IExtensibleTextEditor));
				if (content != null)
					content.SetCaretTo (Math.Max (1, line), 1);
				
				// check that selected "class" isn't a delegate
				if (selectedClass.ClassType == ClassType.Delegate) {
					memberStore.Clear();
				} else {
					BindMemberCombo(selectedClass);
				}
			}
		}
		
		void RegionChanged (object sender, EventArgs e)
		{
			if (loadingMembers)
				return;
			
			Gtk.TreeIter iter;
			if (regionCombo.GetActiveIter (out iter)) {
				DomRegion selectedRegion = (DomRegion)regionStore.GetValue (iter, 2);
				
				// Get a handle to the current document
				if (IdeApp.Workbench.ActiveDocument == null) {
					return;
				}
				
				// If we can we navigate to the line location of the IMember.
				IExtensibleTextEditor content = (IExtensibleTextEditor) IdeApp.Workbench.ActiveDocument.GetContent(typeof(IExtensibleTextEditor));
				if (content != null) {
					int line = Math.Max (1, selectedRegion.Start.Line);
					content.SetCaretTo (Math.Max (1, line), 1);
					foreach (FoldSegment fold in this.textEditor.Document.GetStartFoldings (line - 1)) {
						if (fold.FoldingType == FoldingType.Region)
							fold.IsFolded = false;
					}
				}
			}
		}
		
		void Add (IType c, string prefix)
		{
			Ambience ambience = AmbienceService.GetAmbienceForFile (view.ContentName);
			Gdk.Pixbuf pix = IdeApp.Services.Resources.GetIcon (c.StockIcon, IconSize.Menu);
			string name = prefix + ambience.GetString (c, OutputFlags.ClassBrowserEntries);
			classStore.AppendValues (pix, name, c);

			foreach (IType inner in c.InnerTypes)
				Add (inner, name + ".");
		}
		
		KeyValuePair<IType, int> SearchClass (int line)
		{
			TreeIter iter;
			int i = 0, foundIndex = 0;
			IType result = null;
			if (classStore.GetIterFirst (out iter)) {
				do {
					IType c = (IType)classStore.GetValue (iter, 2);
					if (c.BodyRegion != null && c.BodyRegion.Start.Line <= line && line <= c.BodyRegion.End.Line)	{
						if (result == null || result.BodyRegion.Start.Line <= c.BodyRegion.Start.Line) {
							result = c;
							foundIndex = i;
						}
					}
					i++;
				} while (classStore.IterNext (ref iter));
			}
			return new KeyValuePair<IType, int> (result, foundIndex);
		}
		
		void UpdateClassComboTip (IMember it)
		{
			if (it != null) {
				Ambience ambience = AmbienceService.GetAmbienceForFile (view.ContentName);
				string txt = ambience.GetString (it, OutputFlags.ClassBrowserEntries);
				tips.SetTip (this.classCombo, txt, txt);
			} else {
				tips.SetTip (classCombo, GettextCatalog.GetString ("Type list"), null);
			}
		}
		
		void UpdateMemberComboTip (IMember it)
		{
			if (it != null) {
				Ambience ambience = AmbienceService.GetAmbienceForFile (view.ContentName);
				string txt = ambience.GetString (it, OutputFlags.ClassBrowserEntries);
				tips.SetTip (this.membersCombo, txt, txt);
			} else {
				tips.SetTip (membersCombo, GettextCatalog.GetString ("Member list"), null);
			}
		}
		
		bool IsMemberSelected (IMember mem, int line, int column)
		{
			if (mem is IMethod) {
				IMethod method = (IMethod) mem;
				return (method.BodyRegion != null && method.BodyRegion.Start.Line <= line && line <= method.BodyRegion.End.Line || 
				       (method.BodyRegion.Start.Line == line && 0 == method.BodyRegion.End.Line));
			} else if (mem is IProperty) {
				IProperty property = (IProperty) mem;
				return (property.BodyRegion != null && property.BodyRegion.Start.Line <= line && line <= property.BodyRegion.End.Line);
			}
			
			return (mem.Location != null && mem.Location.Line <= line && line <= mem.Location.Line);
		}
		
//		public void GetLineColumnFromPosition (int position, out int line, out int column)
//		{
//			DocumentLocation location = TextEditor.Document.OffsetToLocation (posititon);
//			line = location.Line + 1;
//			column = location.Column + 1;
//		}
		
		public void LoadClassCombo ()
		{
			BindClassCombo();
		}
		#endregion
		
		#region Search and Replace
		SearchWidget           searchWidget           = null;
		SearchAndReplaceWidget searchAndReplaceWidget = null;
		GotoLineNumberWidget   gotoLineNumberWidget   = null;
		
		public void SetSearchPattern ()
		{
			string selectedText = this.TextEditor.SelectedText;
			
			if (!String.IsNullOrEmpty (selectedText)) {
				this.SetSearchPattern (selectedText);
				SearchWidget.searchPattern = selectedText;
//				SearchWidget.FireSearchPatternChanged ();
			}
		}
		
		bool KillWidgets ()
		{
			bool result = false;
			if (searchWidget != null) {
				if (searchWidget.Parent != null)
					editorBar.Remove (searchWidget);
				searchWidget.Destroy ();
				searchWidget.Dispose ();
				searchWidget = null;
				result = true;
			} 
			if (searchAndReplaceWidget != null) {
				if (searchAndReplaceWidget.Parent != null)
					editorBar.Remove (searchAndReplaceWidget);
				searchAndReplaceWidget.Destroy ();
				searchAndReplaceWidget.Dispose ();
				searchAndReplaceWidget = null;
				result = true;
			}
			if (gotoLineNumberWidget != null) {
				if (gotoLineNumberWidget.Parent != null)
					editorBar.Remove (gotoLineNumberWidget);
				gotoLineNumberWidget.Destroy ();
				gotoLineNumberWidget.Dispose ();
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
			if (searchWidget == null) {
				ShowSearchWidget ();
			} else {
				this.FindNext ();
			}
		}
		
		[CommandHandler (SearchCommands.EmacsFindPrevious)]
		public void EmacsFindPrevious ()
		{
			if (searchWidget == null) {
				ShowSearchWidget ();
			} else {
				this.FindPrevious ();
			}
		}
		
		[CommandHandler (SearchCommands.Find)]
		public void ShowSearchWidget ()
		{
			if (searchWidget == null) {
				KillWidgets ();
				if (TextEditor.IsSomethingSelected)
					TextEditor.SearchPattern = TextEditor.SelectedText;
				searchWidget = new SearchWidget (this);
				editorBar.PackEnd (searchWidget);
				editorBar.SetChildPacking (searchWidget, false, true, 0, PackType.End);
				searchWidget.ShowAll ();
				this.textEditor.HighlightSearchPattern = true;
				if (this.splittedTextEditor != null) 
					this.splittedTextEditor.HighlightSearchPattern = true;
				this.editorBar.FocusChain = new Widget[] {
					this.textEditor,
					this.searchWidget,
					this.classCombo,
					this.membersCombo,
					this.regionCombo,
				};

			}
			searchWidget.Focus ();
		}
		
		[CommandHandler (SearchCommands.Replace)]
		public void ShowReplaceWidget ()
		{ 
			if (searchAndReplaceWidget == null) {
				KillWidgets ();
				if (TextEditor.IsSomethingSelected)
					TextEditor.SearchPattern = TextEditor.SelectedText;
				searchAndReplaceWidget = new SearchAndReplaceWidget (this);
				editorBar.PackEnd (searchAndReplaceWidget);
				editorBar.SetChildPacking (searchAndReplaceWidget, false, true, 0, PackType.End);
				searchAndReplaceWidget.ShowAll ();
				this.textEditor.HighlightSearchPattern = true;
				if (this.splittedTextEditor != null) 
					this.splittedTextEditor.HighlightSearchPattern = true;
				this.editorBar.FocusChain = new Widget[] {
					this.textEditor,
					this.searchAndReplaceWidget,
					this.classCombo,
					this.membersCombo,
					this.regionCombo,
				};
				
			}
			searchAndReplaceWidget.Focus ();
		}
		
		[CommandHandler (SearchCommands.GotoLineNumber)]
		public void ShowGotoLineNumberWidget ()
		{
			if (gotoLineNumberWidget == null) {
				KillWidgets ();
				gotoLineNumberWidget = new GotoLineNumberWidget (this);
				editorBar.Add (gotoLineNumberWidget);
				editorBar.SetChildPacking(gotoLineNumberWidget, false, true, 0, PackType.End);
				gotoLineNumberWidget.ShowAll ();
				this.editorBar.FocusChain = new Widget[] {
					this.textEditor,
					this.gotoLineNumberWidget,
					this.classCombo,
					this.membersCombo,
					this.regionCombo,
				};
				
			}
			gotoLineNumberWidget.Focus ();
		}
		
		internal void SetSearchOptions ()
		{
			this.textEditor.SearchEngine    = SearchWidget.SearchEngine == SearchWidget.DefaultSearchEngine ? (ISearchEngine)new BasicSearchEngine () : (ISearchEngine)new RegexSearchEngine ();
			this.textEditor.IsCaseSensitive = SearchWidget.IsCaseSensitive;
			this.textEditor.IsWholeWordOnly = SearchWidget.IsWholeWordOnly;
			
			string error;
			string pattern = SearchWidget.searchPattern;
			if (searchWidget != null)
				pattern = searchWidget.SearchPattern;
			if (searchAndReplaceWidget != null)
				pattern = searchAndReplaceWidget.SearchPattern;
			
			bool valid = this.textEditor.SearchEngine.IsValidPattern (pattern, out error);
			
			if (valid) {
				this.textEditor.SearchPattern = pattern;
			}
			this.textEditor.QueueDraw ();
			if (this.splittedTextEditor != null) {
				this.splittedTextEditor.IsCaseSensitive = SearchWidget.IsCaseSensitive;
				this.splittedTextEditor.IsWholeWordOnly = SearchWidget.IsWholeWordOnly;
				if (valid) {
					this.splittedTextEditor.SearchPattern = pattern;
				}
				this.splittedTextEditor.QueueDraw ();
			}
		}
		
		[CommandHandler (SearchCommands.FindNext)]
		public SearchResult FindNext ()
		{
			SetSearchOptions ();
			SearchResult result = TextEditor.FindNext ();
			TextEditor.GrabFocus ();
			if (result == null) {
				IdeApp.Workbench.StatusBar.ShowError (GettextCatalog.GetString ("Search pattern not found"));
			} else if (result.SearchWrapped) {
				IdeApp.Workbench.StatusBar.ShowMessage (new Image (Gtk.Stock.Find, IconSize.Menu), GettextCatalog.GetString ("Reached bottom, continued from top"));
			} else {
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
			return result;
		}
		
		[CommandHandler (SearchCommands.FindPrevious)]
		public SearchResult FindPrevious ()
		{
			SetSearchOptions ();
			SearchResult result = TextEditor.FindPrevious ();
			TextEditor.GrabFocus ();
			if (result == null) {
				IdeApp.Workbench.StatusBar.ShowError (GettextCatalog.GetString ("Search pattern not found"));
			} else if (result.SearchWrapped) {
				IdeApp.Workbench.StatusBar.ShowMessage (new Image (Gtk.Stock.Find, IconSize.Menu), GettextCatalog.GetString ("Reached top, continued from bottom"));
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
				IdeApp.Workbench.StatusBar.ShowMessage (GettextCatalog.GetPluralString ("Found and replaced one occurrence", "Found and replaced {0} occurrences", number, number));
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
				line.AddMarker (marker);
				doc.CommitLineUpdate (doc.OffsetToLineNumber(line.Offset));
			}
		}
		
		public void RemoveFromLine ()
		{
			if (line != null) {
				line.RemoveMarker (marker);
				doc.CommitLineUpdate (doc.OffsetToLineNumber(line.Offset));
			}
		}
	}
	
}
