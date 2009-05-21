//
// DockBar.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Components.Docking
{
	class DockBar: Gtk.EventBox
	{
		Gtk.PositionType position;
		Box box;
		DockFrame frame;
		
		public DockBar (DockFrame frame, Gtk.PositionType position)
		{
			this.frame = frame;
			this.position = position;
			Gtk.Alignment al = new Alignment (0,0,0,0);
			if (Orientation == Gtk.Orientation.Horizontal)
				box = new HBox ();
			else
				box = new VBox ();
				
			switch (position) {
				case PositionType.Top: al.BottomPadding = 2; break;
				case PositionType.Bottom: al.TopPadding = 2; break;
				case PositionType.Left: al.RightPadding = 2; break;
				case PositionType.Right: al.LeftPadding = 2; break;
			}
			
			box.Spacing = 3;
			al.Add (box);
			Add (al);
			ShowAll ();
		}
		
		public Gtk.Orientation Orientation {
			get {
				return (position == PositionType.Left || position == PositionType.Right) ? Gtk.Orientation.Vertical : Gtk.Orientation.Horizontal;
			}
		}
		
		public Gtk.PositionType Position {
			get {
				return position;
			}
		}

		public DockFrame Frame {
			get {
				return frame;
			}
		}
		
		public DockBarItem AddItem (DockItem item, int size)
		{
			DockBarItem it = new DockBarItem (this, item, size);
			box.PackStart (it, false, false, 0);
			it.ShowAll ();
			if (!Visible)
				Show ();
			return it;
		}
		
		internal void RemoveItem (DockBarItem it)
		{
			box.Remove (it);
			if (box.Children.Length == 0)
				Hide ();
		}
		
		public void UpdateTitle (DockItem item)
		{
			foreach (DockBarItem it in box.Children) {
				if (it.DockItem == item) {
					it.UpdateTab ();
					break;
				}
			}
		}
	}
}

