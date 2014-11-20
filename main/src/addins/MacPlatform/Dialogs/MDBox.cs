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
using AppKit;

using CoreGraphics;

namespace MonoDevelop.MacIntegration
{
	class MDLabel : NSTextField
	{
		public MDLabel (string text): base (new CGRect (0f, 6f, 100f, 20f))
		{
			StringValue = text;
			DrawsBackground = false;
			Bordered = false;
			Editable = false;
			Selectable = false;
		}
	}
	
	interface IMDLayout : ILayout
	{
		NSView View { get; }
	}
	
	class MDBox : LayoutBox, IMDLayout
	{
		bool ownsView;
		
		public MDBox (LayoutDirection direction, float spacing, float padding) : this (null, direction, spacing, padding)
		{
		}
		
		public MDBox (NSView unownedView, LayoutDirection direction, float spacing, float padding) : base (direction, spacing, padding)
		{
			View = unownedView ?? new NSView ();
			ownsView = unownedView == null;
		}
		
		public void Layout ()
		{
			var request = BeginLayout ();
			EndLayout (request, CGPoint.Empty, request.Size);
		}
		
		public void Layout (CGSize allocation)
		{
			var request = BeginLayout ();
			EndLayout (request, CGPoint.Empty, allocation);
		}
		
		public void Add (NSView view)
		{
			Add (new MDAlignment (view));
		}
		
		public void Add (NSView view, bool autosize)
		{
			Add (new MDAlignment (view, autosize));
		}
		
		public override void EndLayout (LayoutRequest request, CGPoint origin, CGSize allocation)
		{
			if (ownsView) {
				View.Frame = new CGRect (origin, allocation);
				base.EndLayout (request, CGPoint.Empty, allocation);
			} else {
				base.EndLayout (request, origin, allocation);
			}
		}
		
		protected override void OnChildAdded (ILayout child)
		{
			var l = child as IMDLayout;
			//if child is viewless, its view may be the same view as this
			if (l != null && l.View.Handle != View.Handle)
				View.AddSubview (l.View);
		}
		
		public NSView View { get; private set; }
	}
	
	class MDAlignment : LayoutAlignment, IMDLayout
	{
		public MDAlignment (NSView view) : this (view, false)
		{
		}
		
		public MDAlignment (NSView view, bool autosize)
		{
			this.View = view;
			if (autosize) {
				if (!(view is NSControl))
					throw new ArgumentException ("Only NSControls can be autosized", "view");
				Autosize ();
			} else {
				var size = view.Frame.Size;
				MinHeight = (float)size.Height;
				MinWidth = (float)size.Width;
			}
		}
		
		public override LayoutRequest BeginLayout ()
		{
			Visible = !View.Hidden;
			return base.BeginLayout ();
		}
		
		public NSView View { get; private set; }
		
		public void Autosize ()
		{
			((NSControl)View).SizeToFit ();
			var size = View.Frame.Size;
			MinHeight = (float)size.Height;
			MinWidth = (float)size.Width;
		}
		
		protected override void OnLayoutEnded (CGRect frame)
		{
			View.Frame = frame;
		}
	}
}