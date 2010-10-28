// 
// MacSelectFileDialogHandler.cs
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
using System.Collections.Generic;
using System.Drawing;
using MonoMac.AppKit;

namespace MonoDevelop.Platform.Mac
{
	class MDBox : IEnumerable<MDBoxChild>
	{
		List<MDBoxChild> children = new List<MDBoxChild> ();
		
		public float Spacing { get; set; }
		public MDBoxDirection Direction { get; set; }
	//	public MDBoxHAlign HAlign { get; set; }
	//	public MDBoxVAlign VAlign { get; set; }
		
		public MDBox (MDBoxDirection direction, float spacing)
		{
			this.Direction = direction;
			this.Spacing = spacing;
		}
		
		public int Count { get { return children.Count; } }
		
		bool IsHorizontal { get { return Direction == MDBoxDirection.Horizontal; } }
		
		public NSView CreateView ()
		{
			float width = 0;
			float height = 0;
			bool resizable = false;
			
			//size controls to fit, expand to min size, calculate total dimensions
			foreach (var child in children) {
				var view = child.View;
				var rect = view.Frame;
				if (rect.Width < child.MinWidth)
					rect.Width = child.MinWidth;
				if (rect.Height < child.MinHeight)
					rect.Height = child.MinHeight;
				view.Frame = rect;
				
				float cwidth = rect.Width + child.PadRight + child.PadLeft;
				float cheight = rect.Height + child.PadTop + child.PadBottom;
				
				if (IsHorizontal) {
					if (width != 0)
						width += Spacing;
					width += cwidth;
					height = Math.Max (height, cheight);
					
					if (child.Expand) {
						resizable = true;
						view.AutoresizingMask |= NSViewResizingMask.HeightSizable;
					}
				} else {
					if (height != 0)
						height += Spacing;
					height += cheight;
					width = Math.Max (width, cwidth);
					
					if (child.Expand) {
						resizable = true;
						view.AutoresizingMask |= NSViewResizingMask.WidthSizable;
					}
				}
			}
			
			float pos = 0;
			if (Direction == MDBoxDirection.Horizontal) {
				float center = height / 2;
				foreach (var child in children) {
					var rect = child.View.Frame;
					if (pos != 0)
						pos += Spacing;
					pos += child.PadLeft;
					float ctlCenter = (rect.Height / 2) + child.PadTop;
					child.View.Frame = new RectangleF (pos, center - ctlCenter, rect.Width, rect.Height);
					pos += child.PadRight + rect.Width;
				}
			} else {
				float center = width / 2;
				foreach (var child in children) {
					var rect = child.View.Frame;
					if (pos != 0)
						pos += Spacing;
					pos += child.PadTop;
					float ctlCenter = (rect.Width / 2) + child.PadLeft;
					child.View.Frame = new RectangleF (center - ctlCenter, pos, rect.Width, rect.Height);
					pos += child.PadBottom + rect.Height;
				}
			}
			
			var boxView = new NSView (new RectangleF (0, 0, width, height));
			if (resizable) {
				boxView.AutoresizesSubviews = true;
				boxView.AutoresizingMask |= (IsHorizontal?
					NSViewResizingMask.WidthSizable : NSViewResizingMask.HeightSizable);
			}
			foreach (var child in children)
				boxView.AddSubview (child.View);
			return boxView;
		}
		
		public IEnumerator<MDBoxChild> GetEnumerator ()
		{
			return children.GetEnumerator ();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return children.GetEnumerator ();
		}
		
		public void Add (MDBoxChild child)
		{
			children.Add (child);
		}
		
		public void Add (NSView view)
		{
			children.Add (new MDBoxChild (view));
		}
		
		public void Add (NSView view, bool autosize)
		{
			children.Add (new MDBoxChild (view, autosize));
		}
	}
	

	
	public enum MDBoxHAlign
	{
		Left, Center, Right
	}
	
	public enum MDBoxVAlign
	{
		Top, Center, Bottom
	}
	
	public enum MDBoxDirection
	{
		Horizontal, Vertical
	}
	
	public class MDBoxChild
	{
		public MDBoxChild (NSView view) : this (view, false)
		{
		}
		
		public MDBoxChild (NSView view, bool autosize)
		{
			this.View = view;
			if (autosize)
				((NSControl) view).SizeToFit ();
		}
		
		public NSView View { get; private set; }
	//	public float XAlign { get; set; }
	//	public float YAlign { get; set; }
	//	public bool Fill { get; set; }
		public bool Expand { get; set; }
		public float MinWidth { get; set; }
		public float MinHeight { get; set; }
		public float PadLeft { get; set; }
		public float PadRight { get; set; }
		public float PadTop { get; set; }
		public float PadBottom { get; set; }
	}
}
