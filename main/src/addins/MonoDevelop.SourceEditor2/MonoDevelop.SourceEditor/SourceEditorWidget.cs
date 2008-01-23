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
		IParseInformation memberParseInfo;
		bool handlingParseEvent = false;
		Tooltips tips = new Tooltips ();		
		
		MonoDevelop.SourceEditor.ExtendibleTextEditor textEditor;
		public MonoDevelop.SourceEditor.ExtendibleTextEditor TextEditor {
			get {
				return this.textEditor;
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
			this.textEditor.SimulateKeyPress (key, modifier);
			return true;
		}
#endregion
		
		void PrepareEvent (object sender, ButtonPressEventArgs args) 
		{
			args.RetVal = true;
		}
		
		public SourceEditorWidget (SourceEditorView view)
		{
			this.view = view;
			this.Build();
			this.textEditor = new MonoDevelop.SourceEditor.ExtendibleTextEditor (view);
			this.mainsw.Child = this.textEditor;
			this.mainsw.ButtonPressEvent += PrepareEvent;
			
			this.textEditor.ShowAll ();

			this.TextEditor.Caret.ModeChanged     += CaretModeChanged;
			this.TextEditor.Caret.PositionChanged += CaretPositionChanged;
			
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
			
			// Pack the controls into the editorbar just below the file name tabs.
			EventBox tbox = new EventBox ();
			tbox.Add (classCombo);
			classBrowser.PackStart(tbox, true, true, 0);
			tbox = new EventBox ();
			tbox.Add (membersCombo);
			classBrowser.PackStart (tbox, true, true, 0);
			
			// Set up the data stores for the comboboxes
			classStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(IClass));
			classCombo.Model = classStore;	
			memberStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(IMember));
			memberStore.SetSortColumnId (1, Gtk.SortType.Ascending);
			membersCombo.Model = memberStore;
   			membersCombo.Changed += new EventHandler (MemberChanged);
			classCombo.Changed += new EventHandler (ClassChanged);
			IdeApp.ProjectOperations.ParserDatabase.ParseInformationChanged += new ParseInformationEventHandler(UpdateClassBrowser);
			
			
			UpdateLineCol ();
			CaretModeChanged (this, EventArgs.Empty);
			this.Focused += delegate {
				UpdateLineCol ();
				CaretModeChanged (this, EventArgs.Empty);
			};
		}
		
		public override void Dispose ()
		{
			isDisposed = true;
			IdeApp.ProjectOperations.ParserDatabase.ParseInformationChanged -= new ParseInformationEventHandler(UpdateClassBrowser);
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
		public void Unsplit ()
		{
			if (splitContainer == null)
				return;
			
			splitContainer.Remove (mainsw);
			editorBar.Remove (splitContainer);
			
			splitContainer.Destroy ();
			splitContainer = null;
			editorBar.PackEnd (mainsw);
			editorBar.ShowAll ();
		}
		
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
			ScrolledWindow secondsw = new ScrolledWindow ();
			secondsw.ButtonPressEvent += PrepareEvent;
			Mono.TextEditor.TextEditor secondEditor = new Mono.TextEditor.TextEditor (TextEditor.Document);
			secondsw.Child = secondEditor;
			splitContainer.Add2 (secondsw);
			editorBar.PackEnd (splitContainer);
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
				
				reloadBar.ShowAll ();
			}
			view.WarnOverwrite = true;
			editorBar.PackStart (reloadBar, false, true, 0);
			reloadBar.ShowAll ();
			view.WorkbenchWindow.ShowNotification = true;
		}
		
		public void RemoveReloadBar ()
		{
			if (reloadBar != null)
				editorBar.Remove (reloadBar);
		}
		
		void ClickedReload (object sender, EventArgs args)
		{
			try {
//				double vscroll = view.VScroll;
				view.Load (view.ContentName);
				editorBar.Remove (reloadBar);
//				view.VScroll = vscroll;
				view.WorkbenchWindow.ShowNotification = false;
			} catch (Exception ex) {
				MonoDevelop.Core.Gui.Services.MessageService.ShowError (ex, "Could not reload the file.");
			}
		}
		
		void ClickedIgnore (object sender, EventArgs args)
		{
			editorBar.Remove (reloadBar);
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
			int offset = this.textEditor.Caret.Offset;
			if (offset < 0 || offset >= this.textEditor.Document.Buffer.Length)
				return;
			char ch = this.textEditor.Document.Buffer.GetCharAt (offset);
			DocumentLocation location = this.textEditor.Document.LogicalToVisualLocation (this.textEditor.Caret.Location);
			IdeApp.Workbench.StatusBar.SetCaretPosition (location.Line, location.Column, ch);
		}
		
		void CaretModeChanged (object sender, EventArgs e)
		{
			IdeApp.Workbench.StatusBar.SetInsertMode (!this.TextEditor.Caret.IsInInsertMode);
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
				if (classFound == null) {
					classCombo.Active = -1;
					membersCombo.Active = -1;
					memberStore.Clear ();
					UpdateComboTip (classCombo, null);
					UpdateComboTip (membersCombo, null);
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
						UpdateComboTip (membersCombo, mem);
						return;
					}
				}
				while (memberStore.IterNext (ref iter));
				
				membersCombo.Active = -1;
				UpdateComboTip (membersCombo, null);
			}
			finally {
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
					UpdateComboTip (classCombo, null);
				}
			} finally {
				handlingParseEvent = false;
				loadingMembers = false;
			}
			
			// return false to stop the GLib.Timeout
			return false;
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
			UpdateComboTip (classCombo, c);
			membersCombo.Changed -= new EventHandler (MemberChanged);
			// Clear down all our local stores.
			
			membersCombo.Model = null;
			memberStore.Clear();
			UpdateComboTip (membersCombo, null);
				
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
			
			foreach (IMember mem in members)
			{
				pix = IdeApp.Services.Resources.GetIcon(IdeApp.Services.Icons.GetIcon (mem), IconSize.Menu); 
				
				// Add the member to the list
				MonoDevelop.Projects.Ambience.Ambience am = view.GetAmbience ();
				string displayName = am.Convert (mem, MonoDevelop.Projects.Ambience.ConversionFlags.UseIntrinsicTypeNames |
				                                      MonoDevelop.Projects.Ambience.ConversionFlags.ShowParameters |
				                                      MonoDevelop.Projects.Ambience.ConversionFlags.ShowParameterNames |
				                                      MonoDevelop.Projects.Ambience.ConversionFlags.ShowGenericParameters);
				memberStore.AppendValues (pix, displayName, mem);
				
				// Check if the current cursor position in inside this member
				if (IsMemberSelected (mem, line, column)) {
					UpdateComboTip (membersCombo, mem);
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
				IViewContent content = (IViewContent) IdeApp.Workbench.ActiveDocument.GetContent(typeof(IViewContent));
				if (content is IPositionable) {
					((IPositionable)content).JumpTo (Math.Max (1, line), 1);
				}
			}
		}
		
		void ClassChanged(object sender, EventArgs e)
		{
			if (loadingMembers)
				return;
			
			Gtk.TreeIter iter;
			if (classCombo.GetActiveIter(out iter)) 	{
				IClass selectedClass = (IClass)classStore.GetValue(iter, 2);
				int line = selectedClass.Region.BeginLine;
				
				// Get a handle to the current document
				if (IdeApp.Workbench.ActiveDocument == null) {
					return;
				}
				
				// If we can we navigate to the line location of the IMember.
				IViewContent content = (IViewContent)IdeApp.Workbench.ActiveDocument.GetContent(typeof(IViewContent));
				if (content is IPositionable) {
					((IPositionable)content).JumpTo (Math.Max (1, line), 1);
				}
				
				// check that selected "class" isn't a delegate
				if (selectedClass.ClassType == ClassType.Delegate) {
					memberStore.Clear();
				} else {
					BindMemberCombo(selectedClass);
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
		
		void UpdateComboTip (ComboBox combo, ILanguageItem it)
		{
			MonoDevelop.Projects.Ambience.Ambience am = view.GetAmbience ();
			string txt;
			if (it != null)
				txt = am.Convert (it, MonoDevelop.Projects.Ambience.ConversionFlags.All);
			else
				txt = null;
			tips.SetTip (combo.Parent, txt, txt);
		}
		
		bool IsMemberSelected (IMember mem, int line, int column)
		{
			if (mem is IMethod) {
				IMethod method = (IMethod) mem;
				return (method.BodyRegion != null && method.BodyRegion.BeginLine <= line && line <= method.BodyRegion.EndLine);
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
		public void SetSearchPattern ()
		{
			string selectedText = this.TextEditor.TextEditorData.SelectedText;
			
			if (!String.IsNullOrEmpty (selectedText))
				SearchReplaceManager.SearchOptions.SearchPattern = selectedText.Split ('\n')[0];
		}
		
		[CommandHandler (SearchCommands.Find)]
		public void Find()
		{
			SetSearchPattern();
			SearchReplaceManager.ShowFindWindow ();
		}
		
		[CommandHandler (SearchCommands.FindNext)]
		public void FindNext ()
		{
			SearchReplaceManager.FindNext ();
		}
	
		[CommandHandler (SearchCommands.FindPrevious)]
		public void FindPrevious ()
		{
			SearchReplaceManager.FindPrevious ();
		}
	
		[CommandHandler (SearchCommands.FindNextSelection)]
		public void FindNextSelection ()
		{
			SetSearchPattern();
			SearchReplaceManager.FindNext ();
		}
	
		[CommandHandler (SearchCommands.FindPreviousSelection)]
		public void FindPreviousSelection ()
		{
			SetSearchPattern();
			SearchReplaceManager.FindPrevious ();
		}
	
		[CommandHandler (SearchCommands.Replace)]
		public void Replace ()
		{ 
			SetSearchPattern ();
			SearchReplaceManager.ShowFindReplaceWindow ();
			
		}
		
//		[CommandHandler (EditorCommands.GotoLineNumber)]
//		public void GotoLineNumber ()
//		{
//			if (!GotoLineNumberDialog.IsVisible)
//				using (GotoLineNumberDialog gnd = new GotoLineNumberDialog ())
//					gnd.Run ();
//		}
		
	}
}
