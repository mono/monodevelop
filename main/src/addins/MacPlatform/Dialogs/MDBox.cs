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
using System.Linq;

namespace MonoDevelop.Platform.Mac
{
	public class MDLayoutRequest
	{
		public SizeF Size { get; set; }
		public bool Visible { get; set; }
		public bool ExpandWidth { get; set; }
		public bool ExpandHeight { get; set; }
	}
	
	interface IMDLayout
	{
		MDLayoutRequest BeginLayout ();
		NSView View { get; }
		void EndLayout (MDLayoutRequest request, PointF origin, SizeF allocation);
	}
	
	class MDBox : NSView, IEnumerable<IMDLayout>, IMDLayout
	{
		List<IMDLayout> children = new List<IMDLayout> ();
		
		public float Spacing { get; set; }
		public float PadLeft { get; set; }
		public float PadRight { get; set; }
		public float PadTop { get; set; }
		public float PadBottom { get; set; }
		public MDAlign Align { get; set; }
		
		public MDBoxDirection Direction { get; set; }
		
		public MDBox (MDBoxDirection direction, float spacing) : this (direction, spacing, 0)
		{
		}
		
		public MDBox (MDBoxDirection direction, float spacing, float padding)
		{
			PadLeft = PadRight = PadTop = PadBottom = padding;
			this.Direction = direction;
			this.Spacing = spacing;
			this.Align = MDAlign.Center;
		}
		
		public int Count { get { return children.Count; } }
		
		bool IsHorizontal { get { return Direction == MDBoxDirection.Horizontal; } }
		
		IEnumerable<IMDLayout> VisibleChildren ()
		{
			return children.Where (c => !c.View.Hidden);
		}
		
		public IEnumerator<IMDLayout> GetEnumerator ()
		{
			return children.GetEnumerator ();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return children.GetEnumerator ();
		}
		
		public void Add (IMDLayout child)
		{
			children.Add (child);
			AddSubview (child.View);
		}
		
		public void Add (NSView view)
		{
			Add (new MDAlignment (view));
		}
		
		public void Add (NSView view, bool autosize)
		{
			Add (new MDAlignment (view, autosize));
		}
		
		public void Layout ()
		{
			var request = ((IMDLayout)this).BeginLayout ();
			((IMDLayout)this).EndLayout (request, PointF.Empty, request.Size);
		}
		
		public void Layout (SizeF allocation)
		{
			var request = ((IMDLayout)this).BeginLayout ();
			((IMDLayout)this).EndLayout (request, PointF.Empty, allocation);
		}
		
		NSView IMDLayout.View {
			get { return this; }
		}
		
		MDContainerLayoutRequest request = new MDContainerLayoutRequest ();
		
		MDLayoutRequest IMDLayout.BeginLayout ()
		{
			float width = 0;
			float height = 0;
			
			request.ChildRequests.Clear ();
			request.ChildRequests.AddRange (children.Select (c => c.BeginLayout ()));
			
			foreach (var r in request.ChildRequests) {
				if (!r.Visible)
					continue;
				request.Visible = true;
				if (r.ExpandWidth)
					request.ExpandWidth = true;
				if (r.ExpandHeight)
					request.ExpandHeight = true;
				
				if (IsHorizontal) {
					if (width != 0)
						width += Spacing;
					width += r.Size.Width;
					height = Math.Max (height, r.Size.Height);
				} else {
					if (height != 0)
						height += Spacing;
					height += r.Size.Height;
					width = Math.Max (width, r.Size.Width);
				}
			}
			
			request.Size = new SizeF (width + PadLeft + PadRight, height + PadTop + PadBottom);
			return request;
		}
		
		void IMDLayout.EndLayout (MDLayoutRequest request, PointF origin, SizeF allocation)
		{
			var childRequests =  ((MDContainerLayoutRequest) request).ChildRequests;
			
			Frame = new RectangleF (origin, allocation);
			
			allocation = new SizeF (allocation.Width - PadLeft - PadRight, allocation.Height - PadBottom - PadTop);
			origin = new PointF (PadLeft, PadBottom);
			
			var size = request.Size;
			size.Height -= (PadTop + PadBottom);
			size.Width -= (PadLeft + PadRight);
			
			int wExpandCount = 0;
			int hExpandCount = 0;
			int visibleCount = 0;
			foreach (var childRequest in childRequests) {
				if (childRequest.Visible)
					visibleCount++;
				else
					continue;
				if (childRequest.ExpandWidth)
					wExpandCount++;
				if (childRequest.ExpandHeight)
					hExpandCount++;
			}
			
			float wExpand = 0;
			if (allocation.Width > size.Width) {
				wExpand = allocation.Width - size.Width;
				if (wExpandCount > 0)
					wExpand /= wExpandCount;
			}
			float hExpand = 0;
			if (allocation.Height > size.Height) {
				hExpand = allocation.Height - size.Height;
				if (hExpandCount > 0)
					hExpand /= hExpandCount;
			}
			
			if (Direction == MDBoxDirection.Horizontal) {
				float pos = PadLeft;
				if (wExpandCount == 0) {
					if (Align == MDAlign.End)
						pos += wExpand;
					else if (Align == MDAlign.Center)
						pos += wExpand / 2;
				}
				for (int i = 0; i < childRequests.Count; i++) {
					var child = children[i];
					var childReq = childRequests[i];
					if (!childReq.Visible)
						continue;
					
					var childSize = new SizeF (childReq.Size.Width, allocation.Height);
					if (childReq.ExpandWidth) {
						childSize.Width += wExpand;
					} else if (hExpandCount == 0 && Align == MDAlign.Fill) {
						childSize.Width += wExpand / visibleCount;
					}
					
					child.EndLayout (childReq, new PointF (pos, origin.Y), childSize);
					pos += childSize.Width + Spacing;
				}
			} else {
				float pos = PadBottom;
				if (hExpandCount == 0) {
					if (Align == MDAlign.End)
						pos += hExpand;
					else if (Align == MDAlign.Center)
						pos += hExpand / 2;
				}
				for (int i = 0; i < childRequests.Count; i++) {
					var child = children[i];
					var childReq = childRequests[i];
					if (!childReq.Visible)
						continue;
					
					var childSize = new SizeF (allocation.Width, childReq.Size.Height);
					if (childReq.ExpandHeight) {
						childSize.Height += hExpand;
					} else if (hExpandCount == 0 && Align == MDAlign.Fill) {
						childSize.Height += hExpand / visibleCount;
					}
					
					child.EndLayout (childReq, new PointF (origin.X, pos), childSize);
					pos += childSize.Height + Spacing;
				}
			}
		}
		
		class MDContainerLayoutRequest : MDLayoutRequest
		{
			public List<MDLayoutRequest> ChildRequests = new List<MDLayoutRequest> ();
		}
	}
	
	public enum MDAlign
	{
		Begin, Center, End, Fill
	}
	
	public enum MDBoxDirection
	{
		Horizontal, Vertical
	}
	
	public class MDAlignment : IMDLayout
	{
		public MDAlignment (NSView view) : this (view, false)
		{
		}
		
		public MDAlignment (NSView view, bool autosize)
		{
			this.View = view;
			if (autosize) {
				Autosize ();
			} else {
				var size = view.Frame.Size;
				MinHeight = size.Height;
				MinWidth = size.Width;
			}
			XAlign = YAlign = MDAlign.Center;
		}
		
		public NSView View { get; private set; }
		public MDAlign XAlign { get; set; }
		public MDAlign YAlign { get; set; }
		public bool  ExpandHeight { get; set; }
		public bool  ExpandWidth { get; set; }
		public float MinHeight { get; set; }
		public float MinWidth { get; set; }
		public float PadLeft { get; set; }
		public float PadRight { get; set; }
		public float PadTop { get; set; }
		public float PadBottom { get; set; }
		
		public void Autosize ()
		{
			var control = View as NSControl;
			if (control == null)
				throw new InvalidOperationException ("Only NSControls can be autosized");
			control.SizeToFit ();
			var size = control.Frame.Size;
			MinHeight = size.Height;
			MinWidth = size.Width;
		}
		
		MDLayoutRequest request = new MDLayoutRequest ();
		
		MDLayoutRequest IMDLayout.BeginLayout ()
		{
			request.Size = new SizeF (MinWidth + PadLeft + PadRight, MinHeight + PadTop + PadBottom);
			request.ExpandHeight = this.ExpandHeight;
			request.ExpandWidth = this.ExpandWidth;
			request.Visible = !View.Hidden;
			return request;
		}
		
		void IMDLayout.EndLayout (MDLayoutRequest request, PointF origin, SizeF allocation)
		{
			var frame = new RectangleF (origin.X + PadLeft, origin.Y + PadBottom, 
				allocation.Width - PadLeft - PadRight, allocation.Height - PadTop - PadBottom);
			
			if (allocation.Height > request.Size.Height) {
				if (YAlign != MDAlign.Fill) {
					frame.Height = request.Size.Height - PadTop - PadBottom;
					if (YAlign == MDAlign.Center) {
						frame.Y += (allocation.Height - request.Size.Height) / 2;
					} else if (YAlign == MDAlign.End) {
						frame.Y += (allocation.Height - request.Size.Height);
					}
				}
			}
			
			if (allocation.Width > request.Size.Width) {
				if (XAlign != MDAlign.Fill) {
					frame.Width = request.Size.Width - PadLeft - PadRight;
					if (XAlign == MDAlign.Center) {
						frame.X += (allocation.Width - request.Size.Width) / 2;
					} else if (XAlign == MDAlign.End) {
						frame.X += (allocation.Width - request.Size.Width);
					}
				}
			}
			
			View.Frame = frame;
		}
	}
}