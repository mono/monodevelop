// 
// DiffWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using Gtk;
using Gdk;
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Components.Diff;

namespace MonoDevelop.VersionControl.Views
{
	public class ComparisonWidget : Bin
	{
		Adjustment vAdjustment;
		Gtk.VScrollbar vScrollBar;
		
		Adjustment hAdjustment;
		Gtk.HScrollbar leftHScrollBar, rightHScrollBar;
		
		OverviewRenderer overview;
		MiddleArea middleArea;
		
		TextEditor originalEditor, diffEditor;
		
		List<ContainerChild> children = new List<ContainerChild> ();
		
		public Adjustment Vadjustment {
			get { return this.vAdjustment; }
		}

		public Adjustment Hadjustment {
			get { return this.hAdjustment; }
		}
		
		public override ContainerChild this [Widget w] {
			get {
				foreach (ContainerChild info in children.ToArray ()) {
					if (info.Child == w)
						return info;
				}
				return null;
			}
		}
		
		public TextEditor OriginalEditor {
			get {
				return this.originalEditor;
			}
		}

		public TextEditor DiffEditor {
			get {
				return this.diffEditor;
			}
		}
		
		public Diff Diff {
			get;
			set;
		}
		
		protected ComparisonWidget (IntPtr ptr) : base (ptr)
		{
		}
		
		public ComparisonWidget (VersionControlDocumentInfo info)
		{
			vAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			vAdjustment.Changed += HandleAdjustmentChanged;
			
			vScrollBar = new VScrollbar (vAdjustment);
			AddChild (vScrollBar);
			
			hAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			hAdjustment.Changed += HandleAdjustmentChanged;
			
			leftHScrollBar = new HScrollbar (hAdjustment);
			AddChild (leftHScrollBar);
			
			rightHScrollBar = new HScrollbar (hAdjustment);
			AddChild (rightHScrollBar);
			
			originalEditor = new TextEditor ();
			AddChild (originalEditor);
			originalEditor.SetScrollAdjustments (hAdjustment, vAdjustment);
			
			diffEditor = new TextEditor ();
			
			AddChild (diffEditor);
			diffEditor.Document.ReadOnly = true;
			diffEditor.SetScrollAdjustments (hAdjustment, vAdjustment);
			this.vAdjustment.ValueChanged += delegate {
				middleArea.QueueDraw ();
			};
			
			overview = new OverviewRenderer (this);
			AddChild (overview);
			
			middleArea = new MiddleArea (this);
			AddChild (middleArea);
			
			this.DoubleBuffered = true;
			originalEditor.ExposeEvent += HandleLeftEditorExposeEvent;
			diffEditor.ExposeEvent += HandleRightEditorExposeEvent;
		}

		void HandleAdjustmentChanged (object sender, EventArgs e)
		{
			Adjustment adjustment = (Adjustment)sender;
			Scrollbar scrollbar = adjustment == vAdjustment ? (Scrollbar)vScrollBar : leftHScrollBar;
			bool newVisible = adjustment.Upper - adjustment.Lower > adjustment.PageSize;
			if (scrollbar.Visible != newVisible) {
				scrollbar.Visible = newVisible;
				QueueResize ();
			}
		}
		
		public override GLib.GType ChildType ()
		{
			return Gtk.Widget.GType;
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			base.ForAll (include_internals, callback);
			
			if (include_internals)
				children.ForEach (child => callback (child.Child));
		}
		
		public void AddChild (Gtk.Widget child)
		{
			child.Parent = this;
			children.Add (new ContainerChild (this, child));
			child.Show ();
		}
		
		protected override void OnAdded (Widget widget)
		{
			base.OnAdded (widget);
			if (widget == Child)
				widget.SetScrollAdjustments (hAdjustment, vAdjustment);
		}
		
		protected override void OnRemoved (Widget widget)
		{
			widget.Unparent ();
			foreach (var info in children.ToArray ()) {
				if (info.Child == widget) {
					children.Remove (info);
					break;
				}
			}
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			children.ForEach (child => child.Child.Destroy ());
			children.Clear ();
		}
		 
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			
			int overviewWidth = 16;
			int vwidth = vScrollBar.Visible ? vScrollBar.Requisition.Width : 1;
			int hheight = leftHScrollBar.Visible ? leftHScrollBar.Requisition.Height : 1; 
			Rectangle childRectangle = new Rectangle (allocation.X + 1, allocation.Y + 1, allocation.Width - vwidth - overviewWidth, allocation.Height - hheight);
			
			if (vScrollBar.Visible) {
				int right = childRectangle.Right;
				int vChildTopHeight = -1;
				int v = leftHScrollBar.Visible ? leftHScrollBar.Requisition.Height : 0;
				vScrollBar.SizeAllocate (new Rectangle (right, childRectangle.Y + vChildTopHeight, vwidth, Allocation.Height - v - vChildTopHeight));
				vScrollBar.Value = System.Math.Max (System.Math.Min (vAdjustment.Upper - vAdjustment.PageSize, vScrollBar.Value), vAdjustment.Lower);
			}
			overview.SizeAllocate (new Rectangle (allocation.Right - overviewWidth + 1, childRectangle.Y, overviewWidth - 1, childRectangle.Height));
			int spacerWidth = 42;
			int editorWidth = (childRectangle.Width - spacerWidth) / 2;
			
			
			diffEditor.SizeAllocate (new Rectangle (childRectangle.X, childRectangle.Top, editorWidth, Allocation.Height - hheight));
			originalEditor.SizeAllocate (new Rectangle (childRectangle.Right - editorWidth, childRectangle.Top, editorWidth, Allocation.Height - hheight));
			
			middleArea.SizeAllocate (new Rectangle (diffEditor.Allocation.Right, childRectangle.Top, spacerWidth + 1, childRectangle.Height));
			
			if (leftHScrollBar.Visible) {
				leftHScrollBar.SizeAllocate (new Rectangle (childRectangle.X, childRectangle.Bottom, editorWidth, hheight));
				rightHScrollBar.SizeAllocate (new Rectangle (childRectangle.Right - editorWidth, childRectangle.Bottom, editorWidth, hheight));
				leftHScrollBar.Value = rightHScrollBar.Value = System.Math.Max (System.Math.Min (hAdjustment.Upper - hAdjustment.PageSize, leftHScrollBar.Value), hAdjustment.Lower);
			}
		}
		
		static double GetWheelDelta (Scrollbar scrollbar, ScrollDirection direction)
		{
			double delta = System.Math.Pow (scrollbar.Adjustment.PageSize, 2.0 / 3.0);
			if (direction == ScrollDirection.Up || direction == ScrollDirection.Left)
				delta = -delta;
			if (scrollbar.Inverted)
				delta = -delta;
			return delta;
		}
		
		protected override bool OnScrollEvent (EventScroll evnt)
		{
			Scrollbar scrollWidget = (evnt.Direction == ScrollDirection.Up || evnt.Direction == ScrollDirection.Down) ? (Scrollbar)vScrollBar : leftHScrollBar;
			
			if (scrollWidget.Visible) {
				double newValue = scrollWidget.Adjustment.Value + GetWheelDelta (scrollWidget, evnt.Direction);
				newValue = System.Math.Max (System.Math.Min (scrollWidget.Adjustment.Upper  - scrollWidget.Adjustment.PageSize, newValue), scrollWidget.Adjustment.Lower);
				scrollWidget.Adjustment.Value = newValue;
			}
			return base.OnScrollEvent (evnt);
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			children.ForEach (child => child.Child.SizeRequest ());
		}
		
		public static Cairo.Color GetColor (Diff.Hunk hunk, double alpha)
		{
			if (hunk.Left.Count == 0)
				return new Cairo.Color (0.4, 0.8, 0.4, alpha);
			if (hunk.Right.Count == 0) 
				return new Cairo.Color (0.8, 0.4, 0.4, alpha);
			return new Cairo.Color (0.4, 0.8, 0.8, alpha);
		}
		const double fillAlpha = 0.1;
		const double lineAlpha = 0.6;
		
		void HandleLeftEditorExposeEvent (object o, ExposeEventArgs args)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (args.Event.Window)) {
				if (Diff != null) {
					foreach (Diff.Hunk hunk in Diff) {
						if (!hunk.Same) {
							int y1 = originalEditor.LineToVisualY (hunk.Right.Start) - (int)originalEditor.VAdjustment.Value;
							int y2 = originalEditor.LineToVisualY (hunk.Right.Start + hunk.Right.Count) - (int)originalEditor.VAdjustment.Value;
							if (y1 == y2)
								y2 = y1 + 1;
							cr.Rectangle (0, y1, originalEditor.Allocation.Width, y2 - y1);
							cr.Color = GetColor (hunk, fillAlpha);
							cr.Fill ();
							
							cr.Color = GetColor (hunk, lineAlpha);
							cr.MoveTo (0, y1);
							cr.LineTo (originalEditor.Allocation.Width, y1);
							cr.Stroke ();
							
							cr.MoveTo (0, y2);
							cr.LineTo (originalEditor.Allocation.Width, y2);
							cr.Stroke ();
						}
					}
				}
			}
		}
		
		void HandleRightEditorExposeEvent (object o, ExposeEventArgs args)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (args.Event.Window)) {
				if (Diff != null) {
					foreach (Diff.Hunk hunk in Diff) {
						if (!hunk.Same) {
							int y1 = diffEditor.LineToVisualY (hunk.Left.Start) - (int)diffEditor.VAdjustment.Value;
							int y2 = diffEditor.LineToVisualY (hunk.Left.Start + hunk.Left.Count) - (int)diffEditor.VAdjustment.Value;
							
							if (y1 == y2)
								y2 = y1 + 1;
							
							cr.Rectangle (0, y1, diffEditor.Allocation.Width, y2 - y1);
							cr.Color = GetColor (hunk, fillAlpha);
							cr.Fill ();
							
							cr.Color = GetColor (hunk, lineAlpha);
							cr.MoveTo (0, y1);
							cr.LineTo (diffEditor.Allocation.Width, y1);
							cr.Stroke ();
							
							cr.MoveTo (0, y2);
							cr.LineTo (diffEditor.Allocation.Width, y2);
							cr.Stroke ();
						}
					}
				}
			}
		}
		
		class MiddleArea : DrawingArea 
		{
			ComparisonWidget widget;
			public MiddleArea (ComparisonWidget widget)
			{
				this.widget = widget;
				this.Events |= EventMask.PointerMotionMask | EventMask.ButtonPressMask;
			}
			
			Diff.Hunk selectedHunk = null;
			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				int delta = widget.OriginalEditor.Allocation.Y - Allocation.Y;
				Diff.Hunk selectedHunk = null;
				foreach (Diff.Hunk hunk in widget.Diff) {
					if (!hunk.Same) {
						int y1 = delta + widget.OriginalEditor.LineToVisualY (hunk.Right.Start) - (int)widget.OriginalEditor.VAdjustment.Value;
						int y2 = delta + widget.OriginalEditor.LineToVisualY (hunk.Right.Start + hunk.Right.Count) - (int)widget.OriginalEditor.VAdjustment.Value;
						if (y1 == y2)
							y2 = y1 + 1;
						
						int z1 = delta + widget.DiffEditor.LineToVisualY (hunk.Left.Start) - (int)widget.DiffEditor.VAdjustment.Value;
						int z2 = delta + widget.DiffEditor.LineToVisualY (hunk.Left.Start + hunk.Left.Count) - (int)widget.DiffEditor.VAdjustment.Value;
						
						if (z1 == z2)
							z2 = z1 + 1;
						
						int x = (Allocation.Width) / 2;
						int y = ((y1 + y2) / 2 + (z1 + z2) / 2) / 2;
						if (Math.Sqrt (System.Math.Abs (x - evnt.X) * System.Math.Abs (x - evnt.X) +
						               System.Math.Abs (y - evnt.Y) * System.Math.Abs (y - evnt.Y)) < 10) {
							selectedHunk = hunk;
							break;
						}
					}
				}
				
				if (this.selectedHunk != selectedHunk) {
					this.selectedHunk = selectedHunk;
					QueueDraw ();
				}
				
				return base.OnMotionNotifyEvent (evnt);
			}
			
			protected override bool OnButtonPressEvent (EventButton evnt)
			{
				if (selectedHunk != null) {
					Console.WriteLine (selectedHunk.Left.Start + "/" + selectedHunk.Right.Start);
					LineSegment start = widget.OriginalEditor.Document.GetLine (selectedHunk.Right.Start);
					LineSegment end   = widget.OriginalEditor.Document.GetLine (selectedHunk.Right.Start + selectedHunk.Right.Count - 1);
					if (selectedHunk.Right.Count > 0)
						widget.OriginalEditor.Remove (start.Offset, end.EndOffset - start.Offset);
					int offset = start.Offset;
					
					if (selectedHunk.Left.Count > 0) {
						start = widget.DiffEditor.Document.GetLine (selectedHunk.Left.Start);
						end   = widget.DiffEditor.Document.GetLine (selectedHunk.Left.Start + selectedHunk.Left.Count - 1);
						widget.OriginalEditor.Insert (offset, widget.DiffEditor.Document.GetTextAt (start.Offset, end.EndOffset - start.Offset));
					}
					
				}
				return base.OnButtonPressEvent (evnt);
			}
			
			protected override bool OnExposeEvent (EventExpose evnt)
			{
				using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
					int delta = widget.OriginalEditor.Allocation.Y - Allocation.Y;
					if (widget.Diff != null) {
						foreach (Diff.Hunk hunk in widget.Diff) {
							if (!hunk.Same) {
								int y1 = delta + widget.DiffEditor.LineToVisualY (hunk.Right.Start) - (int)widget.OriginalEditor.VAdjustment.Value;
								int y2 = delta + widget.DiffEditor.LineToVisualY (hunk.Right.Start + hunk.Right.Count) - (int)widget.OriginalEditor.VAdjustment.Value;
								if (y1 == y2)
									y2 = y1 + 1;
								
								int z1 = delta + widget.OriginalEditor.LineToVisualY (hunk.Left.Start) - (int)widget.DiffEditor.VAdjustment.Value;
								int z2 = delta + widget.OriginalEditor.LineToVisualY (hunk.Left.Start + hunk.Left.Count) - (int)widget.DiffEditor.VAdjustment.Value;
								
								if (z1 == z2)
									z2 = z1 + 1;
								cr.MoveTo (Allocation.Width, y1);
								cr.LineTo (0, z1);
								cr.LineTo (0, z2);
								cr.LineTo (Allocation.Width, y2);
								cr.ClosePath ();
								cr.Color = GetColor (hunk, fillAlpha);
								cr.Fill ();
								
								cr.Color = GetColor (hunk, lineAlpha);
								cr.MoveTo (Allocation.Width, y1);
								cr.LineTo (0, z1);
								cr.Stroke ();
								
								cr.MoveTo (Allocation.Width, y2);
								cr.LineTo (0, z2);
								cr.Stroke ();
								int x = Allocation.Width / 2;
								int y = ((y1 + y2) / 2 + (z1 + z2) / 2) / 2;
								cr.Save ();
								cr.Translate (x, y);
								cr.Scale (10, 10);
								cr.Arc (0, 0, 1, 0, 2 * Math.PI);
								
								cr.Color = GetColor (hunk, 1);
								cr.FillPreserve ();
								
								Cairo.RadialGradient shadowGradient = new Cairo.RadialGradient (0.0, 0.0, .6,  0.0, 0.0, 1.0);
								shadowGradient.AddColorStop (0, new Cairo.Color (0, 0, 0, 0));
								shadowGradient.AddColorStop (1, new Cairo.Color (0, 0, 0, 0.5));
								cr.Source = shadowGradient;
								cr.FillPreserve ();
								
								if (selectedHunk != null && hunk.Left.Start == selectedHunk.Left.Start && hunk.Right.Start == selectedHunk.Right.Start ) {
									cr.Scale (12, 12);
									shadowGradient = new Cairo.RadialGradient (0.0, 0.0, .6,  0.0, 0.0, 1.0);
									shadowGradient.AddColorStop (0, new Cairo.Color (1, 1, 1, 0.5));
									shadowGradient.AddColorStop (1, new Cairo.Color (1, 1, 1, 0.2));
									cr.Source = shadowGradient;
									cr.Fill ();
								}
								cr.Restore ();
							}
						}
					}
				}
				var result = base.OnExposeEvent (evnt);
				
				Gdk.GC gc = Style.DarkGC (State);
				evnt.Window.DrawLine (gc, Allocation.X, Allocation.Top, Allocation.X, Allocation.Bottom);
				evnt.Window.DrawLine (gc, Allocation.Right, Allocation.Top, Allocation.Right, Allocation.Bottom);
				
				evnt.Window.DrawLine (gc, Allocation.Left, Allocation.Y, Allocation.Right, Allocation.Y);
				evnt.Window.DrawLine (gc, Allocation.Left, Allocation.Bottom, Allocation.Right, Allocation.Bottom);
				
				return result;
			}
		}
		
		class OverviewRenderer : DrawingArea 
		{
			ComparisonWidget widget;
				
			public OverviewRenderer (ComparisonWidget widget)
			{
				this.widget = widget;
				widget.vScrollBar.ValueChanged += delegate {
					QueueDraw ();
				};
				WidthRequest = 50;
				
				Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.ButtonMotionMask;
				
				Show ();
			}
			
			public void MouseMove (double y)
			{
				var adj = widget.vScrollBar.Adjustment;
				double position = ((double)y / Allocation.Height - (double)widget.vScrollBar.Allocation.Height/adj.Upper/2) * adj.Upper;
				if (position < 0) position = 0;
				if (position + widget.vScrollBar.Allocation.Height > adj.Upper) position = adj.Upper - widget.vScrollBar.Allocation.Height;
				widget.vScrollBar.Adjustment.Value = position;
			}

			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				if (button != 0)
					MouseMove (evnt.Y);
				return base.OnMotionNotifyEvent (evnt);
			}
			
			uint button;
			protected override bool OnButtonPressEvent (EventButton evnt)
			{
				button |= evnt.Button;
				MouseMove (evnt.Y);
				return base.OnButtonPressEvent (evnt);
			}
			
			protected override bool OnButtonReleaseEvent (EventButton evnt)
			{
				button &= ~evnt.Button;
				return base.OnButtonReleaseEvent (evnt);
			}
			
			protected override bool OnExposeEvent (Gdk.EventExpose e)
			{
				var scroller = widget.vScrollBar;
				
				using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
					cr.LineWidth = 1;
					
					int count = 0;
					foreach (Hunk h in widget.Diff) {
						IncPos(h, ref count);
					}
					
					int start = 0;
					foreach (Hunk h in widget.Diff) {
						int size = 0;
						IncPos(h, ref size);
						
						cr.Rectangle (0.5, 0.5 + Allocation.Height * start / count, Allocation.Width, Math.Max (1, Allocation.Height * size / count));
						if (h.Same) {
							cr.Color = new Cairo.Color (1, 1, 1);
						} else if (h.Original ().Count == 0) {
							cr.Color = new Cairo.Color (0.4, 0.8, 0.4);
						} else if (h.Changes (0).Count == 0) {
							cr.Color = new Cairo.Color (0.8, 0.4, 0.4);
						} else {
							cr.Color = new Cairo.Color (0.4, 0.8, 0.8);
						}
						cr.Fill ();
						start += size;
					}
					
					cr.Rectangle (1,
					              (int)(Allocation.Height*scroller.Adjustment.Value/scroller.Adjustment.Upper),
					              Allocation.Width - 2,
					              (int)(Allocation.Height*((double)scroller.Allocation.Height/scroller.Adjustment.Upper)));
					cr.Color = new Cairo.Color (0, 0, 0, 0.5);
					cr.StrokePreserve ();
					
					cr.Color = new Cairo.Color (0, 0, 0, 0.03);
					cr.Fill ();
				}
				
/*				gc.RgbFgColor = ColorGrey;
				e.Window.DrawRectangle(gc, false,
					1,
					(int)(Allocation.Height*scroller.Adjustment.Value/scroller.Adjustment.Upper) + 1,
					Allocation.Width-3,
					(int)(Allocation.Height*((double)scroller.Allocation.Height/scroller.Adjustment.Upper))-2);
*/
				return true;
			}
			
			void IncPos(Hunk h, ref int pos)
			{
				pos += h.MaxLines();
/*				if (sidebyside)
					pos += h.MaxLines();
				else if (h.Same)
					pos += h.Original().Count;
				else {
					pos += h.Original().Count;
					for (int i = 0; i < h.ChangedLists; i++)
						pos += h.Changes(i).Count;
				}*/
			}
		}	
	}
}

