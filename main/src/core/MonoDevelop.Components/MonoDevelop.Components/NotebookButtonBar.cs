// NotebookButtonBar.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using Gtk;

namespace MonoDevelop.Components
{
	[System.ComponentModel.Category("MonoDevelop.Components")]
	[System.ComponentModel.ToolboxItem(true)]
	public class NotebookButtonBar: Toolbar
	{
		Notebook notebook;
		bool updating;
		
		public NotebookButtonBar()
		{
			IconSize = IconSize.Menu;
			ToolbarStyle = ToolbarStyle.BothHoriz;
			ShowArrow = false;
			ShowAll ();
		}
		
		public Widget Notebook {
			get { return notebook; }
			set {
				notebook = value as Notebook;
				notebook.SwitchPage += OnPageChanged;
				UpdateButtons ();
				ShowPage (notebook.Page);
			}
		}
		
		void OnPageChanged (object s, Gtk.SwitchPageArgs args)
		{
			if (updating) return;
			updating = true;
			
			Gtk.Widget[] buttons = Children;
			for (int n=0; n<buttons.Length; n++) {
				ToggleToolButton b = (ToggleToolButton) buttons [n];
				b.Active = (n == args.PageNum);
			}

			updating = false;
		}
		
		void UpdateButtons ()
		{
			updating = true;
			
			foreach (Gtk.Widget c in Children)
				Remove (c);
			foreach (Gtk.Widget page in notebook.Children) {
				Gtk.Widget t = notebook.GetTabLabel (page);
				notebook.SetTabLabel (page, new Gtk.Label (""));
				Gtk.Widget nw;
				if (t is Gtk.Label)
					nw = new Gtk.Label (((Gtk.Label)t).Text);
				else
					nw = new Gtk.Label ("");
				ToggleToolButton button = new ToggleToolButton ();
				button.IsImportant = true;
				button.LabelWidget = t;
				button.Clicked += new EventHandler (OnButtonToggled);
				button.ShowAll ();
				Insert (button, -1);
			}
			updating = false;
		}
		
		public void SetButton (int index, string label, string icon)
		{
			ToggleToolButton button = (ToggleToolButton) Children [index];
			button.Label = label;
			button.StockId = icon;
		}
		
		protected ToggleToolButton AddButton (string label, Gtk.Widget page)
		{
			if (notebook == null)
				return null;
			updating = true;
			ToggleToolButton button = new ToggleToolButton ();
			button.Label = label;
			button.IsImportant = true;
			button.Clicked += new EventHandler (OnButtonToggled);
			button.ShowAll ();
			Insert (button, -1);
			if (page != null)
				notebook.AppendPage (page, new Gtk.Label ());
			updating = false;
			return button;
		}
		
		public void RemoveButton (int npage)
		{
			if (notebook == null)
				return;
			notebook.RemovePage (npage);
			Gtk.Widget cw = Children [npage];
			Remove (cw);
			cw.Destroy ();
			ShowPage (0);
		}
		
		void OnButtonToggled (object s, EventArgs args)
		{
			int i = Array.IndexOf (Children, s);
			if (i != -1)
				ShowPage (i);
		}
		
		public virtual void ShowPage (int npage)
		{
			if (notebook.CurrentPage == npage) {
				ToggleToolButton but = (ToggleToolButton) Children [npage];
				but.Active = true;
				return;
			}
				
			if (updating) return;
			
			notebook.CurrentPage = npage;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
		    GdkWindow.DrawRectangle (Style.BackgroundGC (State), true, Allocation);
            
            foreach (Widget child in Children) {
                PropagateExpose (child, evnt);
            }
		
		    return true;
		}
	}
}
