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
using System.Linq;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;

namespace MonoDevelop.SourceEditor
{
	
	class ClassQuickFinder : HBox
	{
		bool loadingMembers = false;
		bool handlingParseEvent = false;
		ParsedDocument parsedDocument;
		
		DropDownBox typeCombo = new DropDownBox ();
		DropDownBox membersCombo = new DropDownBox ();
		DropDownBox regionCombo = new DropDownBox ();
		
		SourceEditorWidget editor;
		
		Tooltips tips = new Tooltips ();
		
		public ClassQuickFinder (SourceEditorWidget editor)
		{
			this.editor = editor;
				
			typeCombo.DataProvider = new TypeDataProvider (this);
			typeCombo.ItemSet += TypeChanged;
			UpdateTypeComboTip (null);
			this.PackStart (typeCombo);

			membersCombo.DataProvider = new MemberDataProvider (this);
			membersCombo.ItemSet += MemberChanged;
			UpdateMemberComboTip (null);
			this.PackStart (membersCombo);
			
			regionCombo.DataProvider = new RegionDataProvider (this);
			regionCombo.ItemSet += RegionChanged;
			UpdateRegionComboTip (null);
			this.PackStart (regionCombo);
			
			int w, h;
			Gtk.Icon.SizeLookup (IconSize.Menu, out w, out h);
			typeCombo.DefaultIconHeight = membersCombo.DefaultIconHeight = regionCombo.DefaultIconHeight = Math.Max (h, 16);
			typeCombo.DefaultIconWidth = membersCombo.DefaultIconWidth = regionCombo.DefaultIconWidth = Math.Max (w, 16);
			
			this.FocusChain = new Widget[] { typeCombo, membersCombo, regionCombo };
			
			this.ShowAll ();
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			typeCombo.WidthRequest   = allocation.Width * 2 / 6 - 6;
			membersCombo.WidthRequest = allocation.Width * 3 / 6 - 6;
			regionCombo.WidthRequest  = allocation.Width / 6;
			
			base.OnSizeAllocated (allocation);
		}
		
		
		public void UpdatePosition (int line, int column)
		{
			if (parsedDocument == null || parsedDocument.CompilationUnit == null) {
				return;
			}
			loadingMembers = true;
			try {
				UpdateRegionCombo (line, column);
				IType foundType = UpdateTypeCombo (line, column);
				UpdateMemberCombo (foundType, line, column);
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
			UpdatePosition (editor.TextEditor.Caret.Line + 1, editor.TextEditor.Caret.Column + 1);
			handlingParseEvent = false;
			// return false to stop the GLib.Timeout
			return false;
		}
		
#region RegionDataProvider
		class RegionDataProvider : DropDownBoxListWindow.IListDataProvider
		{
			ClassQuickFinder parent;
			
			public RegionDataProvider (ClassQuickFinder parent)
			{
				this.parent = parent;
			}
			
			public int IconCount {
				get {
					if (parent.parsedDocument == null)
						return 0;
					return parent.parsedDocument.UserRegions.Count ();
				}
			}
			
			public void Reset ()
			{
				
			}
			
			public string GetText (int n)
			{
				if (parent.parsedDocument == null)
					return "";
				return parent.parsedDocument.UserRegions.ElementAt (n).Name; //GettextCatalog.GetString ("Region {0}", parent.parsedDocument.UserRegions.ElementAt (n).Name);
			}
			
			public Gdk.Pixbuf GetIcon (int n)
			{
				return ImageService.GetPixbuf (Gtk.Stock.Add, IconSize.Menu);
			}
			
			public object GetTag (int n)
			{
				if (parent.parsedDocument == null)
					return null;
				return parent.parsedDocument.UserRegions.ElementAt (n);
			}
		}
		
		void UpdateRegionCombo (int line, int column)
		{
			if (parsedDocument == null)
				return;
			bool hasRegions = false;
			foreach (FoldingRegion region in parsedDocument.UserRegions) {
				hasRegions = true;
				if (region.Region.Start.Line <= line && line <= region.Region.End.Line) {
					if (regionCombo.CurrentItem == region)
						return;
					regionCombo.SetItem (region.Name, //GettextCatalog.GetString ("Region {0}", region.Name),
					                     ImageService.GetPixbuf (Gtk.Stock.Add, IconSize.Menu),
					                     region);
					UpdateRegionComboTip (region);
					return;
				}
			}
			regionCombo.Sensitive = hasRegions;
			if (regionCombo.CurrentItem != null) {
				regionCombo.SetItem ("", null, null);
				UpdateRegionComboTip (null);
			}
		}
		
		void UpdateRegionComboTip (FoldingRegion region)
		{
			if (region == null) {
				tips.SetTip (regionCombo, GettextCatalog.GetString ("Region list"), null);
				return;
			}
			tips.SetTip (regionCombo, GettextCatalog.GetString ("Region {0}", region.Name), null);
		}
		
		void RegionChanged (object sender, EventArgs e)
		{
			if (loadingMembers || regionCombo.CurrentItem == null)
				return;
			
			FoldingRegion selectedRegion = (FoldingRegion)this.regionCombo.CurrentItem;
			
			// If we can, we navigate to the line location of the IMember.
			int line = Math.Max (1, selectedRegion.Region.Start.Line);
			JumpTo (Math.Max (1, line), 1);
		}
#endregion

#region MemberDataProvider
		class MemberDataProvider : DropDownBoxListWindow.IListDataProvider, System.Collections.Generic.IComparer<IMember>
		{
			ClassQuickFinder parent;
			List<IMember> memberList = new List<IMember> ();
			public MemberDataProvider (ClassQuickFinder parent)
			{
				this.parent = parent;
			}
			
			public void Reset ()
			{
				memberList.Clear ();
				IType type = parent.typeCombo.CurrentItem as IType;
				if (type == null)
					return;
				memberList.AddRange (type.Members);
				memberList.Sort (this);
			}
			
			public int IconCount {
				get {
					return memberList.Count;
				}
			}
			
			public string GetText (int n)
			{
				return parent.editor.Ambience.GetString (memberList[n], OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters);
			}
			
			public Gdk.Pixbuf GetIcon (int n)
			{
				return ImageService.GetPixbuf (memberList[n].StockIcon, IconSize.Menu);
			}
			
			public object GetTag (int n)
			{
				return memberList[n];
			}
			
			int System.Collections.Generic.IComparer<IMember>.Compare (IMember x, IMember y)
			{
				string nameX = parent.editor.Ambience.GetString (x, OutputFlags.None);
				string nameY = parent.editor.Ambience.GetString (y, OutputFlags.None);
				return String.Compare (nameX, nameY, StringComparison.OrdinalIgnoreCase);
			}
		}
		
		void UpdateMemberCombo (IType parent, int line, int column)
		{
			if (parent == null || parent.ClassType == ClassType.Delegate) {
				membersCombo.Sensitive = false;
			} else {
				membersCombo.Sensitive = true;
				foreach (IMember member in parent.Members) {
					if (member.Location.Line == line || member.BodyRegion.Contains (line, column)) {
						if (membersCombo.CurrentItem == member)
							return;
						membersCombo.SetItem (editor.Ambience.GetString (member, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters),
						                      MonoDevelop.Core.Gui.ImageService.GetPixbuf (member.StockIcon, IconSize.Menu),
						                      member);
						UpdateMemberComboTip (member);
						return;
					}
				}
			}
			if (membersCombo.CurrentItem != null) {
				membersCombo.SetItem ("", null, null);
				UpdateMemberComboTip (null);
			}
		}
		
		void MemberChanged (object sender, EventArgs e)
		{
			if (loadingMembers || membersCombo.CurrentItem == null)
				return;
			IMember member = (IMember)membersCombo.CurrentItem;
			int line = member.Location.Line;
			
			// If we can, we navigate to the line location of the IMember.
			JumpTo (Math.Max (1, line), 1);
			UpdateMemberComboTip (member);
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
		
#endregion

#region TypeDataProvider
		class TypeDataProvider : DropDownBoxListWindow.IListDataProvider, System.Collections.Generic.IComparer<IType>
		{
			ClassQuickFinder parent;
			List<IType> typeList = new List<IType> ();
			
			public TypeDataProvider (ClassQuickFinder parent)
			{
				this.parent = parent;
			}
			
			public void Reset ()
			{
				typeList.Clear ();
				if (parent.parsedDocument == null || parent.parsedDocument.CompilationUnit == null)
					return;
				Stack<IType> types = new Stack<IType> (parent.parsedDocument.CompilationUnit.Types);
				while (types.Count > 0) {
					IType type = types.Pop ();
					typeList.Add (type);
					foreach (IType innerType in type.InnerTypes)
						types.Push (innerType);
				}
				typeList.Sort (this);
			}
			
			public int IconCount {
				get {
					return typeList.Count;
				}
			}
			
			public string GetText (int n)
			{
				return parent.editor.Ambience.GetString (typeList[n], OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters);
			}
			
			public Gdk.Pixbuf GetIcon (int n)
			{
				return ImageService.GetPixbuf (typeList[n].StockIcon, IconSize.Menu);
			}
			
			public object GetTag (int n)
			{
				return typeList[n];
			}
			
			int System.Collections.Generic.IComparer<IType>.Compare (IType x, IType y)
			{
				return String.Compare (x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
			}
		}
		
		
		IType UpdateTypeCombo (int line, int column)
		{
			IType c = parsedDocument.CompilationUnit.GetTypeAt (line, column);
			if (typeCombo.CurrentItem == c)
				return c;
			if (c == null) {
				typeCombo.SetItem ("", null, null);
			} else {
				typeCombo.SetItem (editor.Ambience.GetString (c, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters),
				                   MonoDevelop.Core.Gui.ImageService.GetPixbuf (c.StockIcon, IconSize.Menu),
				                   c);
				
			}
			UpdateTypeComboTip (c);
			return c;
		}
		
		void TypeChanged (object sender, EventArgs e)
		{
			if (loadingMembers || typeCombo.CurrentItem == null)
				return;
			
			IType selectedClass = (IType)typeCombo.CurrentItem;
			int line = selectedClass.Location.Line;
			UpdateTypeComboTip (selectedClass);
			
			// If we can, we navigate to the line location of the IMember.
			JumpTo (Math.Max (1, line), 1);
		}
		void UpdateTypeComboTip (IMember it)
		{
			if (it != null) {
				Ambience ambience = editor.Ambience;
				string txt = ambience.GetString (it, OutputFlags.ClassBrowserEntries);
				tips.SetTip (this.typeCombo, txt, txt);
			} else {
				tips.SetTip (typeCombo, GettextCatalog.GetString ("Type list"), null);
			}
		}
#endregion
		void JumpTo (int line, int column)
		{
			MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor extEditor = 
				MonoDevelop.Ide.Gui.IdeApp.Workbench.ActiveDocument.GetContent
					<MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor> ();
			if (extEditor != null)
				extEditor.SetCaretTo (Math.Max (1, line), column);
		}
		
		
/*		bool IsMemberSelected (IMember mem, int line, int column)
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
		}*/
		
//		public void GetLineColumnFromPosition (int position, out int line, out int column)
//		{
//			DocumentLocation location = TextEditor.Document.OffsetToLocation (posititon);
//			line = location.Line + 1;
//			column = location.Column + 1;
//		}
		
		protected override void OnDestroyed ()
		{
			if (editor != null) 
				editor = null;
			
			if (tips != null) {
				tips.Destroy ();
				tips = null;
			}
			base.OnDestroyed ();
		}
	}
}
