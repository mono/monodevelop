// 
// SectionList.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using Gtk;
using Gdk;
using Cairo;

namespace MonoDevelop.Components
{
	class SectionList : Container
	{
		const int borderLineWidth = 1;
		const int headerPadding = 2;
		
		List<Section> sections = new List<Section> ();
		
		Gdk.Window inputWindow;
		Pango.Layout layout;
		int headerHeight = 20;
		int activeIndex = 0;
		int hoverIndex = -1;
		bool trackingHover = false;
		
		public int ActiveIndex {
			get { return activeIndex; }
			set {
				if (value >= sections.Count || value < 0)
					throw new ArgumentOutOfRangeException ();
				
				if (activeIndex == value)
					return;
				
				int oldIndex = activeIndex;
				activeIndex = value;
				//FIXME: implement real focus support
				FocusChain = new Widget[] { sections[value].Child };
				UpdateVisibility ();
				RepaintSectionHeader (oldIndex);
				RepaintSectionHeader (activeIndex);
				OnActiveSectionChanged ();
				sections[activeIndex].OnActivated ();
			}
		}
		
		public Section ActiveSection {
			get {
				return ActiveIndex >= 0 && sections.Count > 0? sections[activeIndex] : null; 
			}
			set {
				int idx = sections.IndexOf (value);
				if (idx < 0)
					throw new ArgumentException ("Section is not in this container");
				ActiveIndex = idx;
			}
		}
		
		void OnActiveSectionChanged ()
		{
			var evt = ActiveSectionChanged;
			if (evt != null)
				evt (this, EventArgs.Empty);
		}
		
		public event EventHandler ActiveSectionChanged;
		
		public SectionList ()
		{
			GtkWorkarounds.FixContainerLeak (this);
			
			this.WidgetFlags |= WidgetFlags.NoWindow;
			WidthRequest = 100;
			EnsureLayout ();
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			
			var alloc = Allocation;
			int bw = (int) BorderWidth;
			
			var attributes = new Gdk.WindowAttr () {
				WindowType = Gdk.WindowType.Child,
				Wclass = Gdk.WindowClass.InputOnly,
				EventMask = (int) (
					EventMask.EnterNotifyMask |
					EventMask.LeaveNotifyMask |
					EventMask.PointerMotionMask |
					EventMask.ButtonPressMask | 
					EventMask.ButtonReleaseMask
				),
				X = alloc.X + bw,
				Y = alloc.Y + bw,
				Width = alloc.Width - bw * 2,
				Height = alloc.Height - bw * 2,
			};
			
			var attrMask = Gdk.WindowAttributesType.X | Gdk.WindowAttributesType.Y;
			
			inputWindow = new Gdk.Window (Parent.GdkWindow, attributes, (int) attrMask);
			inputWindow.UserData = Handle;
		}
		
		protected override void OnUnrealized ()
		{
			base.OnUnrealized ();
			
			inputWindow.UserData = IntPtr.Zero;
			inputWindow.Destroy ();
			inputWindow = null;
		}
		
		protected override void OnMapped ()
		{
			//show our window before the children, so they're on top
			inputWindow.Show ();
			base.OnMapped ();
		}
		
		protected override void OnUnmapped ()
		{
			base.OnUnmapped ();
			inputWindow.Hide ();
		}
		
		public Section AddSection (string title, Widget child)
		{
			var s = new Section (this, title, child);
			sections.Add (s);
			child.Parent = this;
			return s;
		}
		
		protected override void OnStyleSet (Style previous)
		{
			base.OnStyleSet (previous);
			KillLayout ();
			EnsureLayout ();
		}
		
		void EnsureLayout ()
		{
			if (layout != null)
				layout.Dispose ();
			layout = new Pango.Layout (PangoContext);
			
			layout.SetText ("lp");
			int w, h;
			layout.GetPixelSize (out w, out h);
			headerHeight = Math.Max (20, h + headerPadding + headerPadding);
		}
		
		void KillLayout ()
		{
			if (layout == null)
				return;
			layout.Dispose ();
			layout = null;
		}

		protected override void OnDestroyed ()
		{
			KillLayout ();
			base.OnDestroyed ();
		}

		protected override void OnAdded (Widget widget)
		{
			throw new InvalidOperationException ("Cannot add widget directly, must add a section");
		}
		
		protected override void OnRemoved (Widget widget)
		{
			for (int i = 0; i < sections.Count; i++) {
				var section = sections[i];
				if (section.Child == widget) {
					widget.Unparent ();
					section.Parent = null;
					section.Child = null;
					sections.RemoveAt (i);
					break;
				}
			}
		}
		
		protected override void OnShown ()
		{
			UpdateVisibility ();
			base.OnShown ();
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			int wr = 0, hr = 0;
			foreach (var section in sections) {
				var req = section.Child.SizeRequest ();
				wr = Math.Max (wr, req.Width);
				hr = Math.Max (hr, req.Height);
			}
			
			hr += sections.Count * headerHeight + borderLineWidth;
			
			int bw2 = ((int)BorderWidth + borderLineWidth) * 2;
			
			hr += bw2;
			wr += bw2;
			
			hr = Math.Max (hr, HeightRequest);
			wr = Math.Max (wr, WidthRequest);
			
			requisition.Height = hr;
			requisition.Width = wr;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (sections.Count == 0)
				return;
			
			int bw = (int) BorderWidth + borderLineWidth;
			int bw2 = bw * 2;
			allocation.Width -= bw2;
			allocation.Height -= bw2;
			allocation.X += bw;
			allocation.Y += bw;
			
			if (IsRealized) {
				inputWindow.MoveResize (allocation);
			}
			
			allocation.Height -= (borderLineWidth + headerHeight) * sections.Count;
			allocation.Y += (headerHeight + borderLineWidth) * (activeIndex + 1);
			
			sections[activeIndex].Child.SizeAllocate (allocation);
		}
		
		void UpdateVisibility ()
		{
			for (int i = 0; i < sections.Count; i++) {
				var section = sections[i];
				if (activeIndex == i) {
					section.Child.Show ();
				} else {
					section.Child.Hide ();
				}
			}
		}
		
		static Cairo.Color Convert (Gdk.Color color)
		{
			return new Cairo.Color (
				color.Red   / (double) ushort.MaxValue,
				color.Green / (double) ushort.MaxValue,
				color.Blue  / (double) ushort.MaxValue);
		}
		
		//FIXME: respect damage regions not just the whole areas, and skip more work when possible
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (sections.Count == 0)
				return false;
			
			var alloc = Allocation;
			
			int bw = (int) BorderWidth;
			double halfLineWidth = borderLineWidth / 2.0;
			int bw2 = bw * 2;
			int w = alloc.Width - bw2;
			int h = alloc.Height - bw2;
			
			using (var cr = CairoHelper.Create (evnt.Window)) {
				CairoHelper.Region (cr, evnt.Region);
				cr.Clip ();
				
				cr.Translate (alloc.X + bw, alloc.Y + bw);
				
				var borderCol = Convert (Style.Dark (StateType.Normal));
				cr.SetSourceColor (borderCol);
				cr.Rectangle (halfLineWidth, halfLineWidth, w - borderLineWidth, h - borderLineWidth);
				cr.LineWidth = borderLineWidth;
				cr.Stroke ();
				
				cr.Translate (borderLineWidth, borderLineWidth);
				w = w - (2 * borderLineWidth);
				
				using (LinearGradient unselectedGrad = new LinearGradient (0, 0, 0, headerHeight),
				       hoverGrad = new LinearGradient (0, 0, 0, headerHeight),
				       selectedGrad = new LinearGradient (0, 0, 0, headerHeight)
				       )
				{
					var unselectedCol = Convert (Style.Mid (StateType.Normal));
					var unselectedTextCol = Convert (Style.Text (StateType.Normal));
					unselectedCol.A = 0.6;
					unselectedGrad.AddColorStop (0, unselectedCol);
					unselectedCol.A = 1;
					unselectedGrad.AddColorStop (1, unselectedCol);

					var hoverCol = Convert (Style.Mid (StateType.Prelight));
					var hoverTextCol = Convert (Style.Text (StateType.Prelight));
					hoverCol.A = 0.6;
					hoverGrad.AddColorStop (0, unselectedCol);
					hoverCol.A = 1;
					hoverGrad.AddColorStop (1, unselectedCol);

					var selectedCol = Convert (Style.Mid (StateType.Normal));
					var selectedTextCol = Convert (Style.Text (StateType.Normal));
					selectedCol.A = 0.6;
					selectedGrad.AddColorStop (0, selectedCol);
					selectedCol.A = 1;
					selectedGrad.AddColorStop (1, selectedCol);
					
					for (int i = 0; i < sections.Count; i++) {
						var section = sections[i];
						bool isActive = activeIndex == i;
						bool isHover = hoverIndex == i;
						
						cr.Rectangle (0, 0, w, headerHeight);
						cr.SetSource (isActive? selectedGrad : (isHover? hoverGrad : unselectedGrad));
						cr.Fill ();
						
						cr.SetSourceColor (isActive? selectedTextCol : (isHover? hoverTextCol : unselectedTextCol));
						layout.SetText (section.Title);
						layout.Ellipsize = Pango.EllipsizeMode.End;
						layout.Width = (int) ((w - headerPadding - headerPadding) * Pango.Scale.PangoScale);
						cr.MoveTo (headerPadding, headerPadding);
						Pango.CairoHelper.ShowLayout (cr, layout);
						
						cr.MoveTo (-halfLineWidth, i > activeIndex? -halfLineWidth : headerHeight + halfLineWidth);
						cr.RelLineTo (w + borderLineWidth, 0.0);
						cr.SetSourceColor (borderCol);
						cr.Stroke ();
						
						cr.Translate (0, headerHeight + borderLineWidth);
						if (isActive)
							cr.Translate (0, section.Child.Allocation.Height + borderLineWidth);
					}
				}
			}
			
			PropagateExpose (sections[activeIndex].Child, evnt);
			return true;// base.OnExposeEvent (evnt);
		}

		protected override void ForAll (bool include_internals, Callback callback)
		{
			for (int i = 0; i < sections.Count; i++) {
				var section = sections[i];
				callback (section.Child);
				//callbacks can remove the widget
				if (sections.Count > 0 && section != sections[i])
					i--;
			}
		}
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			trackingHover = true;
			var hoverIdx = GetSectionHeaderAtPosition ((int)evnt.X, (int)evnt.Y);
			SetHoverIndex (hoverIdx);
			return base.OnEnterNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			trackingHover = false;
			SetHoverIndex (-1);
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			if (trackingHover) {
				var hoverIdx = GetSectionHeaderAtPosition ((int)evnt.X, (int)evnt.Y);
				SetHoverIndex (hoverIdx);
			}
			return base.OnMotionNotifyEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			var idx = GetSectionHeaderAtPosition ((int)evnt.X, (int)evnt.Y);
			if (idx >= 0) {
				ActiveIndex = idx;
				return true;
			}
			return base.OnButtonPressEvent (evnt);
		}
		
		int GetSectionHeaderAtPosition (int x, int y)
		{
			if (sections.Count == 0)
				return -1;
			
			//the event window already is within the border so only need to calc width
			var alloc = Allocation;
			int borderWidth = (int) BorderWidth;
			
			if (y < 0 || x < 0 || x > (alloc.Width - borderWidth - borderWidth))
				return -1;
			
			int childWidgetStart = (headerHeight + borderLineWidth) * (activeIndex + 1);
			if (y < childWidgetStart)
				return y / (headerHeight + borderLineWidth);
			
			y -= ActiveSection.Child.Allocation.Height;
			if (y < childWidgetStart)
				return -1;
			
			int idx = y / (headerHeight + borderLineWidth);
			return idx < sections.Count? idx : -1;
		}
		
		Gdk.Rectangle GetSectionHeaderArea (int index)
		{
			int borderWidth = (int) BorderWidth;
			var rect = Allocation;
			rect.X += borderWidth;
			rect.Y += borderWidth;
			rect.Width -= borderWidth * 2;
			rect.Height = headerHeight + borderLineWidth + borderLineWidth;
			
			rect.Y += (headerHeight + borderLineWidth) * (index);
			if (index > activeIndex)
				rect.Y += ActiveSection.Child.Allocation.Height;
			
			return rect;
		}
		
		void SetHoverIndex (int index)
		{
			if (hoverIndex == index)
				return;
			int old = hoverIndex;
			hoverIndex = index;
			RepaintSectionHeader (old);
			RepaintSectionHeader (index);
		}
		
		void RepaintSectionHeader (int index)
		{
			if (index < 0 || index >= sections.Count)
				return;
			var rect = GetSectionHeaderArea (index);
			QueueDrawArea (rect.X, rect.Y, rect.Width, rect.Height);
		}
		
		public class Section
		{
			string title;
			public SectionList Parent { get; internal set; }
			public Widget Child { get; internal set; }
			
			internal Section (SectionList parent, string title, Widget child)
			{
				this.Parent = parent;
				this.title = title;
				this.Child = child;
			}
			
			public bool IsActive {
				get { return Parent.ActiveSection == this; }
				set { Parent.ActiveSection = this; }
			}
			
			public string Title {
				get { return this.title; }
				set {
					this.title = value;
					int idx = Parent.sections.IndexOf (this);
					Parent.RepaintSectionHeader (idx);
				}
			}
			
			internal void OnActivated ()
			{
				var evt = Activated;
				if (evt != null)
					evt (this, EventArgs.Empty);
			}
			
			public event EventHandler Activated;
		}
	}
}