//
// CellRendererComboBox.cs
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
using Gtk;
using Gdk;

namespace MonoDevelop.Components
{
	public class CellRendererComboBox: CellRendererText
	{
		string[] values;
		string path;
		int rowHeight;
		
		public CellRendererComboBox ()
		{
			Mode |= Gtk.CellRendererMode.Editable;
			Gtk.ComboBox dummyEntry = Gtk.ComboBox.NewText ();
			rowHeight = dummyEntry.SizeRequest ().Height;
			Ypad = 0;
		}

		public string[] Values {
			get { return values; }
			set { values = value; }
		}
		
		public override void GetSize (Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
			if (height < rowHeight)
				height = rowHeight;
		}
		
		public override CellEditable StartEditing (Gdk.Event ev, Widget widget, string path, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, CellRendererState flags)
		{
			this.path = path;

			Gtk.ComboBox combo = Gtk.ComboBox.NewText ();
			foreach (string s in values)
				combo.AppendText (s);
			
			combo.Active = Array.IndexOf (values, Text);
			combo.Changed += new EventHandler (SelectionChanged);
			return new TreeViewCellContainer (combo);
		}
		
		void SelectionChanged (object s, EventArgs a)
		{
			Gtk.ComboBox combo = (Gtk.ComboBox) s;
			if (Changed != null)
				Changed (this, new ComboSelectionChangedArgs (path, combo.Active, combo.ActiveText));
		}
		
		// Fired when the selection changes
		public event ComboSelectionChangedHandler Changed;
	}
	
	public delegate void ComboSelectionChangedHandler (object sender, ComboSelectionChangedArgs args);
	
	public class ComboSelectionChangedArgs: EventArgs 
	{
		string path;
		int active;
		string activeText;
		
		public ComboSelectionChangedArgs (string path, int active, string activeText)
		{
			this.path = path;
			this.active = active;
			this.activeText = activeText;
		}
		
		public string Path {
			get { return path; }
		}
		
		public int Active {
			get { return active; }
		}
		
		public string ActiveText {
			get { return activeText; }
		}
	}
}
