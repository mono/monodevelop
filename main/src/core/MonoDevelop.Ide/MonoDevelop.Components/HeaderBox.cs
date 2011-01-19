// 
// HeaderBox.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Components
{
	public class HeaderBox: Bin
	{
		Gtk.Widget child;
		int topMargin;
		int bottomMargin;
		int leftMargin;
		int rightMargin;
		
		int topPadding;
		int bottomPadding;
		int leftPadding;
		int rightPadding;
		
		public HeaderBox ()
		{
		}
		
		public HeaderBox (int topMargin, int bottomMargin, int leftMargin, int rightMargin)
		{
			SetMargins (topMargin, bottomMargin, leftMargin, rightMargin);
		}
		
		public void SetMargins (int topMargin, int bottomMargin, int leftMargin, int rightMargin)
		{
			this.topMargin = topMargin;
			this.bottomMargin = bottomMargin;
			this.leftMargin = leftMargin;
			this.rightMargin = rightMargin;
		}
		
		public void SetPadding (int topPadding, int bottomPadding, int leftPadding, int rightPadding)
		{
			this.topPadding = topPadding;
			this.bottomPadding = bottomPadding;
			this.leftPadding = leftPadding;
			this.rightPadding = rightPadding;
		}
		
		public bool GradientBackround { get; set; }

		protected override void OnAdded (Widget widget)
		{
			base.OnAdded (widget);
			child = widget;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			if (child != null) {
				requisition = child.SizeRequest ();
				requisition.Width += leftMargin + rightMargin + leftPadding + rightPadding;
				requisition.Height += topMargin + bottomMargin + topPadding + bottomPadding;
			} else {
				requisition.Width = 0;
				requisition.Height = 0;
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (allocation.Width > leftMargin + rightMargin + leftPadding + rightPadding) {
				allocation.X += leftMargin + leftPadding;
				allocation.Width -= leftMargin + rightMargin + leftPadding + rightPadding;
			}
			if (allocation.Height > topMargin + bottomMargin + topPadding + bottomPadding) {
				allocation.Y += topMargin + topPadding;
				allocation.Height -= topMargin + bottomMargin + topPadding + bottomPadding;
			}
			if (child != null)
				child.SizeAllocate (allocation);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			Gdk.Rectangle rect;
			
			if (GradientBackround) {
				rect = new Gdk.Rectangle (Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
				HslColor gcol = Style.Background (Gtk.StateType.Normal);
				
				using (Cairo.Context cr = Gdk.CairoHelper.Create (GdkWindow)) {
					cr.NewPath ();
					cr.MoveTo (rect.X, rect.Y);
					cr.RelLineTo (rect.Width, 0);
					cr.RelLineTo (0, rect.Height);
					cr.RelLineTo (-rect.Width, 0);
					cr.RelLineTo (0, -rect.Height);
					cr.ClosePath ();
					Cairo.Gradient pat = new Cairo.LinearGradient (rect.X, rect.Y, rect.X, rect.Bottom);
					Cairo.Color color1 = gcol;
					pat.AddColorStop (0, color1);
					gcol.L -= 0.1;
					if (gcol.L < 0) gcol.L = 0;
					pat.AddColorStop (1, gcol);
					cr.Pattern = pat;
					cr.FillPreserve ();
				}
			}
			
			bool res = base.OnExposeEvent (evnt);
			
			Gdk.GC borderColor = Style.DarkGC (Gtk.StateType.Normal);
			
			rect = Allocation;
			for (int n=0; n<topMargin; n++)
				GdkWindow.DrawLine (borderColor, rect.X, rect.Y + n, rect.Right - 1, rect.Y + n);
			
			for (int n=0; n<bottomMargin; n++)
				GdkWindow.DrawLine (borderColor, rect.X, rect.Bottom - n - 1, rect.Right - 1, rect.Bottom - n - 1);
			
			for (int n=0; n<leftMargin; n++)
				GdkWindow.DrawLine (borderColor, rect.X + n, rect.Y, rect.X + n, rect.Bottom - 1);
			
			for (int n=0; n<rightMargin; n++)
				GdkWindow.DrawLine (borderColor, rect.Right - n - 1, rect.Y, rect.Right - n - 1, rect.Bottom - 1);
			
			return res;
		}
	}
}

