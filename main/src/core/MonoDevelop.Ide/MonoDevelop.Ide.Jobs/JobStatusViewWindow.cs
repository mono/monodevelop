// 
// JobStatusViewWindow.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Ide.Jobs;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Jobs
{
	class JobStatusViewWindow: Window
	{
		public JobStatusViewWindow (JobInstance ji): base (Gtk.WindowType.Toplevel)
		{
			this.TransientFor = IdeApp.Workbench.RootWindow;
			this.AcceptFocus = true;
			this.SkipPagerHint = true;
			this.DestroyWithParent = true;
//			this.Decorated = false;
			this.SkipTaskbarHint = true;
			Title = ji.Job.Description;
			Icon = MonoDevelop.Core.Gui.ImageService.GetPixbuf (ji.Job.Icon);
			Gtk.Widget w = ji.StatusWidget;
			Add (w);
		}
		
		public void ShowAndMove (int x, int y, Gtk.PositionType relPosition, bool animate)
		{
			int tx = x;
			int ty = y;
			switch (relPosition) {
				case Gtk.PositionType.Left: tx -= 10; break;
				case Gtk.PositionType.Top: ty -= 40; break;
				case Gtk.PositionType.Bottom: y -= 10; break;
				case Gtk.PositionType.Right: x -= 10; break;
			}
			
			if (animate) {
				Move (x, y);
				Show ();
				
				GLib.Timeout.Add (50, delegate {
					y = ty + (int)((double)(y - ty) * 0.7);
					x = tx + (int)((double)(x - tx) * 0.7);
					Move (x, y);
					return y != ty || x != tx;
				});
			} else {
				Move (tx, ty);
				Show ();
			}
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape) {
				Hide ();
				return true;
			}
			else
				return base.OnKeyPressEvent (evnt);
		}

		protected override bool OnDeleteEvent (Gdk.Event evnt)
		{
			Hide ();
			return true;
		}
	}
}
