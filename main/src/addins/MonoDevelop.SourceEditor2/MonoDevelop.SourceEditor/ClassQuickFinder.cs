// 
// ClassQuickFinder.cs
// 
// Author:
//   Mike Krüger <mkrueger@novell.com>
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;

namespace MonoDevelop.SourceEditor
{
	
	class ClassQuickFinder : HBox
	{
		bool loadingMembers = false;
		bool handlingParseEvent = false;
		ParsedDocument parsedDocument;
		
		ListStore classStore;
		ListStore memberStore;
		ListStore regionStore;
		
		ComboBox classCombo = new ComboBox ();
		ComboBox membersCombo = new ComboBox ();
		ComboBox regionCombo = new ComboBox ();
		
		SourceEditorWidget editor;
		
		Tooltips tips = new Tooltips ();
		
		public ClassQuickFinder (SourceEditorWidget editor)
		{
			this.editor = editor;
			
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
			
			this.PackStart (classCombo);
			this.PackStart (membersCombo);
			this.PackStart (regionCombo);
			
			this.FocusChain = new Widget[] { classCombo, membersCombo, regionCombo };
			
			this.ShowAll ();
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			classCombo.WidthRequest   = allocation.Width * 2 / 6 - 6;
			membersCombo.WidthRequest = allocation.Width * 3 / 6 - 6;
			regionCombo.WidthRequest  = allocation.Width / 6;
			
			base.OnSizeAllocated (allocation);
		}
		
		
		public void UpdatePosition (int line, int column)
		{
			if (parsedDocument == null) {
				return;
			}
			
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
		
		public void UpdateCompilationUnit (ParsedDocument parsedDocument)
		{
			if (handlingParseEvent)
				return;
			
			handlingParseEvent = true;
			
			this.parsedDocument = parsedDocument;
			
			GLib.Timeout.Add (100, new GLib.TimeoutHandler (Repopulate));
		}
		
		bool Repopulate ()
		{
			if (!this.IsRealized)
				return false;
			
			loadingMembers = true;
			
			try {
				// Clear down all our local stores.
				classStore.Clear();				
				
				// check the IParseInformation member variable to see if we could get ParseInformation for the 
				// current docuement. If not we can't display class and member info so hide the browser bar.
				if (parsedDocument == null || parsedDocument.CompilationUnit == null) {
//					classBrowser.Visible = false;
					return false;
				}
				
				ReadOnlyCollection<IType> cls = parsedDocument.CompilationUnit.Types;
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
				
				int line = editor.TextEditor.Caret.Line + 1;
				int column = editor.TextEditor.Caret.Column + 1;
				
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
				
				BindRegionCombo ();
				UpdateRegionCombo (line, column);
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
			if (parsedDocument != null && parsedDocument.UserRegions != null) {
				foreach (FoldingRegion region in parsedDocument.UserRegions) {
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
			if (parsedDocument == null || parsedDocument.UserRegions == null) 
				return;
			foreach (FoldingRegion region in parsedDocument.UserRegions) {
				regionStore.AppendValues (
					MonoDevelop.Core.Gui.Services.Resources.GetIcon (Gtk.Stock.Add, IconSize.Menu), 
					region.Name, 
					region.Region);
			}
			//bool isVisible = cu.FoldingRegions.Count > 0; 
			regionCombo.Model = regionStore;
		}
		
		void BindMemberCombo (IType c)
		{
			int position = 0;
			int activeIndex = -1;
			
			// find out where the current cursor position is and set the combos.
			int line   = editor.TextEditor.Caret.Line + 1;
			int column = editor.TextEditor.Caret.Column + 1;
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
			
			Ambience ambience = editor.Ambience;
			
			foreach (IMember mem in members) {
				pix = MonoDevelop.Core.Gui.Services.Resources.GetIcon (mem.StockIcon, IconSize.Menu); 
				
				// Add the member to the list
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
				
				// If we can, we navigate to the line location of the IMember.
				JumpTo (Math.Max (1, line), 1);
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
				
				// If we can, we navigate to the line location of the IMember.
				JumpTo (Math.Max (1, line), 1);
				
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
				
				// If we can, we navigate to the line location of the IMember.
				int line = Math.Max (1, selectedRegion.Start.Line);
				JumpTo (Math.Max (1, line), 1);
			}
		}
		
		void JumpTo (int line, int column)
		{
			MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor extEditor = 
				MonoDevelop.Ide.Gui.IdeApp.Workbench.ActiveDocument.GetContent
					<MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor> ();
			if (extEditor != null)
				extEditor.SetCaretTo (Math.Max (1, line), column);
		}
		
		void Add (IType c, string prefix)
		{
			Ambience ambience = editor.Ambience;
			Gdk.Pixbuf pix = MonoDevelop.Core.Gui.Services.Resources.GetIcon (c.StockIcon, IconSize.Menu);
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
					if (c.BodyRegion.Start.Line <= line && line <= c.BodyRegion.End.Line)	{
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
				Ambience ambience = editor.Ambience;
				string txt = ambience.GetString (it, OutputFlags.ClassBrowserEntries);
				tips.SetTip (this.classCombo, txt, txt);
			} else {
				tips.SetTip (classCombo, GettextCatalog.GetString ("Type list"), null);
			}
		}
		
		void UpdateMemberComboTip (IMember it)
		{
			if (it != null) {
				Ambience ambience = editor.Ambience;
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
				return (method.BodyRegion.Start.Line <= line && line <= method.BodyRegion.End.Line || 
				       (method.BodyRegion.Start.Line == line && 0 == method.BodyRegion.End.Line));
			} else if (mem is IProperty) {
				IProperty property = (IProperty) mem;
				return (property.BodyRegion.Start.Line <= line && line <= property.BodyRegion.End.Line);
			}
			
			return (mem.Location.Line <= line && line <= mem.Location.Line);
		}
		
//		public void GetLineColumnFromPosition (int position, out int line, out int column)
//		{
//			DocumentLocation location = TextEditor.Document.OffsetToLocation (posititon);
//			line = location.Line + 1;
//			column = location.Column + 1;
//		}
		
		protected override void OnDestroyed ()
		{
			if (editor != null) {
				editor = null;
				classCombo.Changed -= ClassChanged;
				membersCombo.Changed -= MemberChanged;
				regionCombo.Changed -= RegionChanged;
			}
		
			base.OnDestroyed ();
		}
	}
}
