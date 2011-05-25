// 
// CompactScrolledWindow.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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

namespace MonoDevelop.Components
{
	[System.ComponentModel.ToolboxItem (true)]
	public class CompactScrolledWindow : Gtk.ScrolledWindow
	{
		const string styleName = "MonoDevelop.Components.CompactScrolledWindow";
		
		bool showBorderLine;
		
		static CompactScrolledWindow ()
		{
			Gtk.Rc.ParseString (@"style """ + styleName + @"""
{
	GtkScrolledWindow::scrollbar-spacing = 0
}");
		}
		
		public CompactScrolledWindow () : base ()
		{
			//HACK to hide the useless padding that many themes have inside the ScrolledWindow - GTK default is 3
			Gtk.Rc.ParseString (string.Format ("widget \"*.{0}\" style \"{1}\" ", Name, styleName));
		}
		
		public bool ShowBorderLine {
			get {
				return showBorderLine;
			}
			set {
				if (showBorderLine == value)
					return;
				showBorderLine = value;
				if (showBorderLine)
					BorderWidth = 1;
				else
					BorderWidth = 0;
				QueueDraw ();
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			var ret = base.OnExposeEvent (evnt);
			if (!showBorderLine)
				return ret;
			
			var alloc = Allocation;
			double border = BorderWidth;
			var halfBorder = border / 2.0;
			
			using (var cr = Gdk.CairoHelper.Create (evnt.Window)) {
				Gdk.CairoHelper.Region (cr, evnt.Region);
				cr.Clip ();
				
				cr.Color = (HslColor)Style.Dark (Gtk.StateType.Normal);
				cr.LineWidth = border;
				cr.Translate (alloc.X, alloc.Y);
				cr.Rectangle (halfBorder, halfBorder, alloc.Width - border, alloc.Height - border);
				cr.Stroke ();
			}
			
			return ret;
		}
	}
}

