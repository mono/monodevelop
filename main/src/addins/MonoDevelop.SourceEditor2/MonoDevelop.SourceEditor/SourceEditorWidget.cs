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
using Gtk;
using Mono.TextEditor;
using MonoDevelop.Projects.Parser;
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
		
		IParseInformation memberParseInfo;
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

		bool ITextEditorExtension.KeyPress (Gdk.Key key, Gdk.ModifierType modifier)
		{
			if (key == Gdk.Key.Escape)
				return true;
			this.TextEditor.SimulateKeyPress (key, modifier);
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
			
			this.textEditor.Caret.ModeChanged     += delegate {
				this.UpdateLineCol ();
			};
			this.textEditor.Caret.PositionChanged += CaretPositionChanged;
			
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
			classStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(IClass));
			classCombo.Model = classStore;	
			classCombo.Changed += new EventHandler (ClassChanged);
			tips.SetTip (classCombo, GettextCatalog.GetString ("Type list"), null);
			
			memberStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(IMember));
			memberStore.SetSortColumnId (1, Gtk.SortType.Ascending);
			membersCombo.Model = memberStore;
			membersCombo.Changed += new EventHandler (MemberChanged);
			tips.SetTip (membersCombo, GettextCatalog.GetString ("Member list"), null);

			regionStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(IRegion));
			regionCombo.Model = regionStore;	
			regionCombo.Changed += new EventHandler (RegionChanged);
			tips.SetTip (regionCombo, GettextCatalog.GetString ("Region list"), null);
			
			ResetFocusChain ();
			
			IdeApp.ProjectOperations.ParserDatabase.ParseInformationChanged += UpdateClassBrowser;
			
			UpdateLineCol ();
			
			this.Focused += delegate {
				UpdateLineCol ();
			};
			IdeApp.ProjectOperations.ParserDatabase.ParseInformationChanged += OnParseInformationChanged;
//			this.IsClassBrowserVisible = SourceEditorOptions.Options.EnableQuickFinder;
			
//			this.d += delegate {
//				this.textEditor.Destroy ();
//			};
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
		
		FoldSegment AddMarker (List<FoldSegment> foldSegments, string text, IRegion region, FoldingType type)
		{
			if (region == null || this.TextEditor == null || this.TextEditor.Document == null || region.BeginLine <= 0 || region.EndLine <= 0 || region.BeginLine >= this.TextEditor.Document.LineCount || region.EndLine >= this.TextEditor.Document.LineCount)
				return null;
			int startOffset = this.TextEditor.Document.LocationToOffset (region.BeginLine - 1,  region.BeginColumn - 1);
			int endOffset   = this.TextEditor.Document.LocationToOffset (region.EndLine - 1,  region.EndColumn - 1);
			FoldSegment result = new FoldSegment (text, startOffset, endOffset - startOffset, type);
			
			foldSegments.Add (result);
			return result;
		}
		
		void AddClass (List<FoldSegment> foldSegments, IClass cl)
		{
			if (this.TextEditor == null || this.TextEditor.Document == null)
				return;
			if (cl.BodyRegion != null)
				AddMarker (foldSegments, "...", cl.BodyRegion, FoldingType.TypeDefinition);
			foreach (IClass inner in cl.InnerClasses) 
				AddClass (foldSegments, inner);
			
			if (cl.ClassType == ClassType.Interface)
				return;
			foreach (IMethod method in cl.Methods) {
				if (method.Region == null || method.BodyRegion == null || method.BodyRegion.EndLine <= 0)
					continue;
				int startOffset = this.TextEditor.Document.LocationToOffset (method.Region.EndLine - 1,  method.Region.EndColumn - 1);
				int endOffset   = this.TextEditor.Document.LocationToOffset (method.BodyRegion.EndLine - 1,  method.BodyRegion.EndColumn - 1);
				foldSegments.Add (new FoldSegment ("...", startOffset, endOffset - startOffset, FoldingType.TypeMember));
			}
			
			foreach (IProperty property in cl.Properties) {
				if (property.Region == null || property.BodyRegion == null || property.BodyRegion.EndLine <= 0)
					continue;
				int startOffset = this.TextEditor.Document.LocationToOffset (property.Region.EndLine - 1,  property.Region.EndColumn - 1);
				int endOffset   = this.TextEditor.Document.LocationToOffset (property.BodyRegion.EndLine - 1,  property.BodyRegion.EndColumn - 1);
				foldSegments.Add (new FoldSegment ("...", startOffset, endOffset - startOffset, FoldingType.TypeMember));
			}
		}
		
		
		void AddUsings (List<FoldSegment> foldSegments, ICompilationUnit cu)
		{
			if (cu.Usings == null || cu.Usings.Count == 1)
				return;
			IUsing first = cu.Usings[0];
			IUsing last = cu.Usings[cu.Usings.Count - 1];
			int startOffset = this.TextEditor.Document.LocationToOffset (first.Region.BeginLine - 1,  first.Region.BeginColumn - 1);
			int endOffset   = this.TextEditor.Document.LocationToOffset (last.Region.EndLine - 1,  last.Region.EndColumn - 1);
			foldSegments.Add (new FoldSegment ("...", startOffset, endOffset - startOffset, FoldingType.TypeMember));
		}
			
		
		class ParseInformationUpdaterWorkerThread : WorkerThread
		{
			SourceEditorWidget widget;
			//ParseInformationEventArgs args;
			
			public ParseInformationUpdaterWorkerThread (SourceEditorWidget widget, ParseInformationEventArgs args)
			{
				this.widget = widget;
				//this.args = args;
			}
			
			protected override void InnerRun ()
			{
				if (SourceEditorOptions.Options.ShowFoldMargin && widget.lastCu != null) {
					List<FoldSegment> foldSegments = new List<FoldSegment> ();
					widget.AddUsings (foldSegments, widget.lastCu);
					foreach (MonoDevelop.Projects.Parser.FoldingRegion region in widget.lastCu.FoldingRegions) {
						if (base.IsStopping)
							return;
						FoldSegment marker = widget.AddMarker (foldSegments, region.Name, region.Region, FoldingType.Region);
						if (marker != null)
							marker.IsFolded = !widget.TextEditor.Document.HasFoldSegments;
					}
					foreach (IClass cl in widget.lastCu.Classes) {
						if (base.IsStopping)
							return;
						widget.AddClass (foldSegments, cl);
					}
					widget.TextEditor.Document.UpdateFoldSegments (foldSegments);
				}
					
				widget.UpdateAutocorTimer ();
				base.Stop ();
			}
		}
		
		readonly object syncObject = new object();
		ParseInformationUpdaterWorkerThread parseInformationUpdaterWorkerThread = null;
		
		void OnParseInformationChanged (object sender, ParseInformationEventArgs args)
		{
			if (args == null || args.ParseInformation == null || this.view == null  || this.view.ContentName != args.FileName)
				return;
			MonoDevelop.SourceEditor.ExtendibleTextEditor editor = this.TextEditor;
			if (editor == null || editor.Document == null)
				return;
			lock (syncObject) {
				lastCu = args.ParseInformation.MostRecentCompilationUnit as ICompilationUnit;
				if (parseInformationUpdaterWorkerThread != null) 
					parseInformationUpdaterWorkerThread.Stop ();
				
				if (lastCu != null) {
					parseInformationUpdaterWorkerThread = new ParseInformationUpdaterWorkerThread (this, args);
					parseInformationUpdaterWorkerThread.Start ();
				}
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
		void ParseCompilationUnit (ICompilationUnitBase cu)
		{
			// No new errors
			if (!cu.ErrorsDuringCompile || cu.ErrorInformation == null)
				return;
			
			// Else we underline the error
			foreach (ErrorInfo info in cu.ErrorInformation)
				UnderLineError (info);
		}
		class Error
		{
			public ErrorInfo info;
			public LineSegment line;
			public Mono.TextEditor.Document doc;
			TextMarker marker = new TextMarker ();
			
			public Error (Mono.TextEditor.Document doc, ErrorInfo info, LineSegment line)
			{
				this.info = info;
				this.line = line;
				this.doc  = doc;
			}
			
			public void AddToLine ()
			{
				line.AddMarker (marker);
				doc.CommitLineUpdate (doc.OffsetToLineNumber(line.Offset));
			}
			public void RemoveFromLine ()
			{
				line.RemoveMarker (marker);
				doc.CommitLineUpdate (doc.OffsetToLineNumber(line.Offset));
			}
		}
		void UnderLineError (ErrorInfo info)
		{
			// Adjust the line to Gtk line representation
			info.Line -= 1;
			
			// If the line is already underlined
			if (errors.ContainsKey (info.Line))
				return;
			
			LineSegment line = this.TextEditor.Document.GetLine (info.Line);
			Error error = new Error (this.TextEditor.Document, info, line); 
			errors [info.Line] = error;
			error.AddToLine ();
		}
		#endregion
		
		public override void Dispose ()
		{
			if (isDisposed)
				return;
			isDisposed = true;
			Unsplit ();
			RemoveReloadBar ();
			if (this.textEditor != null) {
				this.textEditor.Dispose ();
				this.textEditor = null;
			}
			this.lastActiveEditor = null;
			this.splittedTextEditor = null;
			RemoveSearchWidget ();
			IdeApp.ProjectOperations.ParserDatabase.ParseInformationChanged -= UpdateClassBrowser;
			IdeApp.ProjectOperations.ParserDatabase.ParseInformationChanged -= OnParseInformationChanged;
		}
		
		void UpdateClassBrowser (object sender, ParseInformationEventArgs args)
		{
			// This event handler can get called when files other than the current content are updated. eg.
			// when loading a new document. If we didn't do this check the member combo for this tab would have
			// methods for a different class in it!
			if (view.ContentName == args.FileName && !handlingParseEvent) {
				handlingParseEvent = true;
				memberParseInfo = args.ParseInformation;
				GLib.Timeout.Add (1000, new GLib.TimeoutHandler (BindClassCombo));
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
				editorBar.Remove (reloadBar);
				reloadBar.Destroy ();
				reloadBar.Dispose ();
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
			DocumentLocation location = this.TextEditor.Document.LogicalToVisualLocation (this.TextEditor.Caret.Location);
			IdeApp.Workbench.StatusBar.ShowCaretState (location.Line + 1, location.Column + 1, this.TextEditor.Caret.Column, this.TextEditor.Caret.IsInInsertMode);
		}
		
		#endregion
		
		#region Class/Member combo handling
		void UpdateMethodBrowser ()
		{
			if (!this.IsClassBrowserVisible)
				return;
			
			if (memberParseInfo == null) {
				classBrowser.Visible = false;
				return;
			}
			
			int line = TextEditor.Caret.Line + 1;
			int column = TextEditor.Caret.Column;
			
			// Find the selected class
			
			KeyValuePair<IClass, int> c = SearchClass (line);
			IClass classFound = c.Key;
			
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
		
		class LanguageItemComparer: IComparer<ILanguageItem>
		{
			public int Compare (ILanguageItem x, ILanguageItem y)
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
				BindRegionCombo ();
				
				// Clear down all our local stores.
				classStore.Clear();				
				
				// check the IParseInformation member variable to see if we could get ParseInformation for the 
				// current docuement. If not we can't display class and member info so hide the browser bar.
				if (memberParseInfo == null) {
					classBrowser.Visible = false;
					return false;
				}
				
				ClassCollection cls = ((ICompilationUnit)memberParseInfo.MostRecentCompilationUnit).Classes;
				// if we've got this far then we have valid parse info - but if we have not classes the not much point
				// in displaying the browser bar
				if (cls.Count == 0) {
					classBrowser.Visible = false;
					return false;
				}
				
				classBrowser.Visible = true;
				List<ILanguageItem> classes = new List<ILanguageItem> ();
				foreach (IClass c in cls)
					classes.Add (c);
				classes.Sort (new LanguageItemComparer ());
				foreach (IClass c in classes)
					Add (c, "");
				
				int line = TextEditor.Caret.Line + 1;
//				this.GetLineColumnFromPosition(this.CursorPosition, out line, out column);
				KeyValuePair<IClass, int> ckvp = SearchClass (line);
				
				IClass foundClass = ckvp.Key;
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
			if (memberParseInfo == null) 
				return;
			ICompilationUnit cu = memberParseInfo.MostRecentCompilationUnit as ICompilationUnit;
			if (cu != null && cu.FoldingRegions != null) {
				foreach (FoldingRegion region in cu.FoldingRegions) {
					if (region.Region.BeginLine <= line && line <= region.Region.EndLine) {
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
			if (memberParseInfo == null) 
				return;
			ICompilationUnit cu = memberParseInfo.MostRecentCompilationUnit as ICompilationUnit;
			if (cu == null || cu.FoldingRegions == null) 
				return;
			foreach (FoldingRegion region in cu.FoldingRegions) {
				regionStore.AppendValues (IdeApp.Services.Resources.GetIcon(Gtk.Stock.Add, IconSize.Menu), 
				                          region.Name, 
				                          region.Region);
			}
			//bool isVisible = cu.FoldingRegions.Count > 0; 
			regionCombo.Model = regionStore;
		}
		
		void BindMemberCombo (IClass c)
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
			
			List<ILanguageItem> members = new List<ILanguageItem> ();
			foreach (ILanguageItem item in c.Methods)
				 members.Add (item);
			foreach (ILanguageItem item in c.Properties)
				 members.Add (item);
			foreach (ILanguageItem item in c.Fields)
				 members.Add (item);
			members.Sort (new LanguageItemComparer ());
			
			// Add items to the member drop down 
			
			foreach (IMember mem in members) {
				pix = IdeApp.Services.Resources.GetIcon (IdeApp.Services.Icons.GetIcon (mem), IconSize.Menu); 
				
				// Add the member to the list
				MonoDevelop.Projects.Ambience.Ambience am = view.GetAmbience ();
				string displayName = am.Convert (mem, MonoDevelop.Projects.Ambience.ConversionFlags.UseIntrinsicTypeNames |
				                                      MonoDevelop.Projects.Ambience.ConversionFlags.ShowParameters |
				                                      MonoDevelop.Projects.Ambience.ConversionFlags.ShowParameterNames |
				                                      MonoDevelop.Projects.Ambience.ConversionFlags.ShowGenericParameters);
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
				int line = member.Region.BeginLine;
				
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
				IClass selectedClass = (IClass)classStore.GetValue(iter, 2);
				int line = selectedClass.Region.BeginLine;
				
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
				IRegion selectedRegion = (IRegion)regionStore.GetValue (iter, 2);
				
				// Get a handle to the current document
				if (IdeApp.Workbench.ActiveDocument == null) {
					return;
				}
				
				// If we can we navigate to the line location of the IMember.
				IExtensibleTextEditor content = (IExtensibleTextEditor) IdeApp.Workbench.ActiveDocument.GetContent(typeof(IExtensibleTextEditor));
				if (content != null) {
					int line = Math.Max (1, selectedRegion.BeginLine);
					content.SetCaretTo (Math.Max (1, line), 1);
					foreach (FoldSegment fold in this.textEditor.Document.GetStartFoldings (line - 1)) {
						if (fold.FoldingType == FoldingType.Region)
							fold.IsFolded = false;
					}
				}
			}
		}
		
		void Add (IClass c, string prefix)
		{
			MonoDevelop.Projects.Ambience.Ambience am = view.GetAmbience ();
			Gdk.Pixbuf pix = IdeApp.Services.Resources.GetIcon (IdeApp.Services.Icons.GetIcon (c), IconSize.Menu);
			string name = prefix + am.Convert (c, MonoDevelop.Projects.Ambience.ConversionFlags.ShowGenericParameters);
			classStore.AppendValues (pix, name, c);

			foreach (IClass inner in c.InnerClasses)
				Add (inner, name + ".");
		}
		
		KeyValuePair<IClass, int> SearchClass (int line)
		{
			TreeIter iter;
			int i = 0, foundIndex = 0;
			IClass result = null;
			if (classStore.GetIterFirst (out iter)) {
				do {
					IClass c = (IClass)classStore.GetValue (iter, 2);
					if (c.BodyRegion != null && c.BodyRegion.BeginLine <= line && line <= c.BodyRegion.EndLine)	{
						if (result == null || result.BodyRegion.BeginLine <= c.BodyRegion.BeginLine) {
							result = c;
							foundIndex = i;
						}
					}
					i++;
				} while (classStore.IterNext (ref iter));
			}
			return new KeyValuePair<IClass, int> (result, foundIndex);
		}
		
		void UpdateClassComboTip (ILanguageItem it)
		{
			if (it != null) {
				MonoDevelop.Projects.Ambience.Ambience am = view.GetAmbience ();
				string txt = am.Convert (it, MonoDevelop.Projects.Ambience.ConversionFlags.All);
				tips.SetTip (this.classCombo, txt, txt);
			} else {
				tips.SetTip (classCombo, GettextCatalog.GetString ("Type list"), null);
			}
		}
		
		void UpdateMemberComboTip (ILanguageItem it)
		{
			if (it != null) {
				MonoDevelop.Projects.Ambience.Ambience am = view.GetAmbience ();
				string txt = am.Convert (it, MonoDevelop.Projects.Ambience.ConversionFlags.All);
				tips.SetTip (this.membersCombo, txt, txt);
			} else {
				tips.SetTip (membersCombo, GettextCatalog.GetString ("Member list"), null);
			}
		}
		
		bool IsMemberSelected (IMember mem, int line, int column)
		{
			if (mem is IMethod) {
				IMethod method = (IMethod) mem;
				return (method.BodyRegion != null && method.BodyRegion.BeginLine <= line && line <= method.BodyRegion.EndLine || 
				       (method.BodyRegion.BeginLine == line && 0 == method.BodyRegion.EndLine));
			} else if (mem is IProperty) {
				IProperty property = (IProperty) mem;
				return (property.BodyRegion != null && property.BodyRegion.BeginLine <= line && line <= property.BodyRegion.EndLine);
			}
			
			return (mem.Region != null && mem.Region.BeginLine <= line && line <= mem.Region.EndLine);
		}
		
//		public void GetLineColumnFromPosition (int position, out int line, out int column)
//		{
//			DocumentLocation location = TextEditor.Document.OffsetToLocation (posititon);
//			line = location.Line + 1;
//			column = location.Column + 1;
//		}
		
		public void LoadClassCombo ()
		{
			IFileParserContext context = IdeApp.ProjectOperations.ParserDatabase.GetFileParserContext (view.ContentName);
			this.memberParseInfo = context.ParseFile (view.ContentName);
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
				SearchWidget.FireSearchPatternChanged ();
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
			this.textEditor.IsCaseSensitive = SearchWidget.IsCaseSensitive;
			this.textEditor.IsWholeWordOnly = SearchWidget.IsWholeWordOnly;
			this.textEditor.SearchPattern = SearchWidget.searchPattern;
			this.textEditor.QueueDraw ();
			if (this.splittedTextEditor != null) {
				this.splittedTextEditor.IsCaseSensitive = SearchWidget.IsCaseSensitive;
				this.splittedTextEditor.IsWholeWordOnly = SearchWidget.IsWholeWordOnly;
				this.splittedTextEditor.SearchPattern = SearchWidget.searchPattern;
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
				IdeApp.Workbench.StatusBar.ShowMessage (GettextCatalog.GetString ("Ready"));
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
}
