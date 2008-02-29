//
// CombinedDesignView.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;

using Gtk;
using Gdk;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class CombinedDesignView : AbstractViewContent
	{
		IViewContent content;
		Gtk.Notebook notebook;
		VBox box;
		Toolbar toolbar;
		
		bool updating;
		
		public CombinedDesignView (IViewContent content)
		{
			this.content = content;
			if (content is IEditableTextBuffer) {
				((IEditableTextBuffer)content).CaretPositionSet += delegate {
					ShowPage (0);
				};
			}
			content.ContentChanged += new EventHandler (OnTextContentChanged);
			content.DirtyChanged += new EventHandler (OnTextDirtyChanged);
			
			notebook = new Gtk.Notebook ();
			
			// Main notebook
			
			notebook.TabPos = Gtk.PositionType.Bottom;
			notebook.ShowTabs = false;
			notebook.ShowBorder = false;
			notebook.Show ();
			box = new VBox ();
			
			// Bottom toolbar
			
			toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.SmallToolbar;
			toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
			toolbar.ShowArrow = false;
			
			AddButton (GettextCatalog.GetString ("Source Code"), content.Control).Active = true;
			
			toolbar.ShowAll ();
			
			box.PackStart (notebook, true, true, 0);
			box.PackStart (toolbar, false, false, 0);
			
			box.Show ();
			
			IdeApp.Workbench.ActiveDocumentChanged += new EventHandler (OnActiveDocumentChanged);
			content.Control.Realized += delegate {
				if (content != null && content.WorkbenchWindow != null) 
					content.WorkbenchWindow.ActiveViewContent = notebook.CurrentPageWidget == content.Control ? content : this;
			};
			notebook.SwitchPage += delegate {
				if (content != null && content.WorkbenchWindow != null) 
					content.WorkbenchWindow.ActiveViewContent = notebook.CurrentPageWidget == content.Control ? content : this;
			};
		}
		
		public virtual Stetic.Designer Designer {
			get { return null; }
		}
		
		protected ToggleToolButton AddButton (string label, Gtk.Widget page)
		{
			updating = true;
			ToggleToolButton button = new ToggleToolButton ();
			button.Label = label;
			button.IsImportant = true;
			button.Clicked += new EventHandler (OnButtonToggled);
			button.ShowAll ();
			toolbar.Insert (button, -1);
			notebook.AppendPage (page, new Gtk.Label ());
			updating = false;
			return button;
		}
		
		public void RemoveButton (int npage)
		{
			notebook.RemovePage (npage);
			Gtk.Widget cw = toolbar.Children [npage];
			toolbar.Remove (cw);
			cw.Destroy ();
			ShowPage (0);
		}
		
		public override MonoDevelop.Projects.Project Project {
			get { return base.Project; }
			set { 
				base.Project = value; 
				content.Project = value; 
			}
		}
		
		protected override void OnWorkbenchWindowChanged (EventArgs e)
		{
			base.OnWorkbenchWindowChanged (e);
			content.WorkbenchWindow = WorkbenchWindow;
		}
		
		void OnButtonToggled (object s, EventArgs args)
		{
			int i = Array.IndexOf (toolbar.Children, s);
			if (i != -1)
				ShowPage (i);
		}
		
		public virtual void ShowPage (int npage)
		{
			if (notebook.CurrentPage == npage)
				return;
				
			if (updating) return;
			updating = true;
			
			notebook.CurrentPage = npage;
			Gtk.Widget[] buttons = toolbar.Children;
			for (int n=0; n<buttons.Length; n++) {
				ToggleToolButton b = (ToggleToolButton) buttons [n];
				b.Active = (n == npage);
			}
			updating = false;
		}
		
		public override void Dispose ()
		{
			content.ContentChanged -= new EventHandler (OnTextContentChanged);
			content.DirtyChanged -= new EventHandler (OnTextDirtyChanged);
			IdeApp.Workbench.ActiveDocumentChanged -= new EventHandler (OnActiveDocumentChanged);
			content.Dispose ();
			
			// Remove and destroy the contents of the Notebook, since the destroy event is
			// not propagated to pages in some gtk versions.
			
			foreach (Gtk.Widget cw in notebook.Children) {
				Gtk.Widget lw = notebook.GetTabLabel (cw);
				notebook.Remove (cw);
				cw.Destroy ();
				if (lw != null)
					lw.Destroy ();
			}
			
			content = null;
			box = null;
			base.Dispose ();
		}
		
		public override void Load (string fileName)
		{
			ContentName = fileName;
			content.Load (fileName);
		}
		
		public override Gtk.Widget Control {
			get { return box; }
		}
		
		public override void Save (string fileName)
		{
			content.Save (fileName);
		}
		
		public override bool IsDirty {
			get {
				return content.IsDirty;
			}
			set {
				content.IsDirty = value;
			}
		}
		
		public override bool IsReadOnly
		{
			get {
				return content.IsReadOnly;
			}
		}
		
		public virtual void AddCurrentWidgetToClass ()
		{
		}
		
		public virtual void JumpToSignalHandler (Stetic.Signal signal)
		{
		}
		
		void OnTextContentChanged (object s, EventArgs args)
		{
			OnContentChanged (args);
		}
		
		void OnTextDirtyChanged (object s, EventArgs args)
		{
			OnDirtyChanged (args);
		}
		
		void OnActiveDocumentChanged (object s, EventArgs args)
		{
			if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.GetContent<CombinedDesignView>() == this)
				OnDocumentActivated ();
		}
		
		protected virtual void OnDocumentActivated ()
		{
		}
		
		public override object GetContent (Type type)
		{
//			if (type == typeof(IEditableTextBuffer)) {
//				// Intercept the IPositionable interface, since we need to
//				// switch to the text editor when jumping to a line
//				if (content.GetContent (type) != null)
//					return this;
//				else
//					return null;
//			}
//			
			object ob = base.GetContent (type);
			if (ob == null)
				return content.GetContent (type);
			else
				return ob;
		}

		public void JumpTo (int line, int column)
		{
			IEditableTextBuffer ip = (IEditableTextBuffer) content.GetContent (typeof(IEditableTextBuffer));
			if (ip != null) {
				ShowPage (0);
				ip.SetCaretTo (line, column);
			}
		}
	}
}

