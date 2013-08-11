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
using Mono.TextEditor;

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
				QueueResize ();
			}
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			//if showing a border line, request a little more space
			if (showBorderLine) {
				requisition.Height += HScrollbar.Visible? 1 : 2;
				requisition.Width += VScrollbar.Visible? 1 : 2;
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			//shrink the allocation we pass to the base layout, to allow for our extra border
			if (showBorderLine) {
				allocation.X += 1;
				allocation.Y += 1;
				allocation.Height -= HScrollbar.Visible? 1 : 2;
				allocation.Width -= VScrollbar.Visible? 1 : 2;
			}
			base.OnSizeAllocated (allocation);


			//expand the scrollbars so they render over the new border
			if (showBorderLine) {
				bool hasHScroll = HScrollbar.Visible;
				bool hasVScroll = VScrollbar.Visible;

				if (hasHScroll) {
					var alloc = HScrollbar.Allocation;
					alloc.X -= 1;
					alloc.Width +=  hasVScroll? 1 :2;
					HScrollbar.SizeAllocate (alloc);
				}
				if (hasVScroll) {
					var alloc = VScrollbar.Allocation;
					alloc.Y -= 1;
					alloc.Height += hasHScroll? 1 : 2;
					VScrollbar.SizeAllocate (alloc);
				}
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			var ret = base.OnExposeEvent (evnt);
			if (!showBorderLine)
				return ret;

			bool hasHScroll = HScrollbar.Visible;
			bool hasVScroll = VScrollbar.Visible;

			//this is the rectangle that defines where we will draw the border lines
			//note that Allocation was set by the base, but we altered it during allocation, so take that into account
			var rect = Allocation;
			var borderWidth = (int) BorderWidth;
			rect.X += borderWidth - 1;
			rect.Y += borderWidth - 1;
			rect.Width -= borderWidth + borderWidth - 2;
			rect.Height -= borderWidth + borderWidth - 2;

			//if there will be scrollbars, bring the end of the lines to the middle of the scrollbar so it looks nice
			if (hasHScroll) {
				rect.Height -= HScrollbar.Allocation.Height / 2;
			}

			if (hasVScroll) {
				rect.Width -= VScrollbar.Allocation.Width / 2;
			}

			double lineWidth = 1;
			var halfLineWidth = lineWidth / 2.0;

			//draw the border lines
			using (var cr = Gdk.CairoHelper.Create (evnt.Window)) {
				Gdk.CairoHelper.Region (cr, evnt.Region);
				cr.Clip ();
				
				cr.SetSourceColor ((HslColor)Style.Dark (Gtk.StateType.Normal));
				cr.LineWidth = lineWidth;
				cr.Translate (rect.X, rect.Y);

				//top
				cr.MoveTo (0, halfLineWidth);
				cr.LineTo (rect.Width, halfLineWidth);

				//bottom. redundant if there's a horizontal scrollbar.
				if (!hasHScroll) {
					cr.MoveTo (0, rect.Height - halfLineWidth);
					cr.LineTo (rect.Width, rect.Height - halfLineWidth);
				}

				//left
				cr.MoveTo (halfLineWidth, 0);
				cr.LineTo (halfLineWidth, rect.Height);

				//right. redundant if there's a vertical scrollbar.
				if (!hasVScroll) {
					cr.MoveTo (rect.Width - halfLineWidth, 0);
					cr.LineTo (rect.Width - halfLineWidth, rect.Height);
				}

				cr.Stroke ();
			}
			
			return ret;
		}
	}
}

