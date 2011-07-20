// 
// ClosableExpander.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using System.ComponentModel;

using Gdk;

using Mono.TextEditor;
using MonoDevelop.Components;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.MacDev.PlistEditor
{
	[ToolboxItem(true)]
	public class ClosableExpander : VBox
	{
		ExpanderHeader header;
		VBox contentBox;
		
		public string ContentLabel {
			get {
				
				return header.Label;
			}
			set {
				header.Label = !string.IsNullOrEmpty (value) ? value : GettextCatalog.GetString ("Untitled");
				QueueDraw ();
			}
		}
		
		public bool Expanded {
			get {
				return contentBox.Visible;
			}
			set {
				contentBox.Visible = value;
				header.StartTimeout ();
			}
		}
		
		public event EventHandler Closed;
		
		public void InvokeClose ()
		{
			if (Closed != null)
				Closed (this, EventArgs.Empty);
		}
		
		class ExpanderHeader : DrawingArea
		{
			ClosableExpander container;
			uint animationTimeout;
			ExpanderStyle expanderStyle;
			
			public string Label {
				get;
				set;
			}
			
			public void UpdateInitialExpanderState ()
			{
				expanderStyle = container.Expanded? ExpanderStyle.Expanded : ExpanderStyle.Collapsed;
			}
			
			public ExpanderHeader (ClosableExpander container)
			{
				this.container = container;
				Events |= EventMask.AllEventsMask;
			}
			
			protected override void OnSizeRequested (ref Requisition requisition)
			{
				base.OnSizeRequested (ref requisition);
				using (var layout = new Pango.Layout (PangoContext)) {
					layout.SetMarkup ("<b>" + Label + "</b>");
					int w, h;
					layout.GetPixelSize (out w, out h);
					requisition.Height = h + 4;
				}
				requisition.Height += 2;
			}
			
			void RemoveTimeout ()
			{
				if (animationTimeout != 0) {
					GLib.Source.Remove (animationTimeout);
					animationTimeout = 0;
				}
			}
			
			public void StartTimeout ()
			{
				RemoveTimeout ();
				animationTimeout = GLib.Timeout.Add (50, delegate {
					bool finished = false;
					if (container.Expanded) {
						if (expanderStyle == ExpanderStyle.Collapsed) {
							expanderStyle = ExpanderStyle.SemiExpanded;
						} else {
							expanderStyle = ExpanderStyle.Expanded;
							finished = true;
						}
					} else {
						if (expanderStyle == ExpanderStyle.Expanded) {
							expanderStyle = ExpanderStyle.SemiCollapsed;
						} else {
							expanderStyle = ExpanderStyle.Collapsed;
							finished = true;
						}
					}
					QueueDraw ();
					if (finished) 
						animationTimeout = 0;
					return !finished;
				});
			}
			
			protected override void OnDestroyed ()
			{
				base.OnDestroyed ();
				RemoveTimeout ();
			}
			
			bool mouseOver;
			protected override bool OnEnterNotifyEvent (EventCrossing evnt)
			{
				mouseOver = true;
				QueueDraw ();
				return base.OnEnterNotifyEvent (evnt);
			}
			
			protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
			{
				mouseOver = false;
				QueueDraw ();
				return base.OnLeaveNotifyEvent (evnt);
			}
			
			double mx, my;
			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				mx = evnt.X;
				my = evnt.Y;
				QueueDraw ();
				
				return base.OnMotionNotifyEvent (evnt);
			}
			
			protected override bool OnButtonPressEvent (EventButton evnt)
			{
				return base.OnButtonPressEvent (evnt);
			}
			
			protected override bool OnButtonReleaseEvent (EventButton evnt)
			{
				if (evnt.Button == 1) {
					if (IsCloseSelected) {
						container.InvokeClose ();
					} else {
						container.Expanded = !container.Expanded;
					}
				}
				return base.OnButtonReleaseEvent (evnt);
			}
			
			// constants taken from gtk
			const int DEFAULT_EXPANDER_SIZE = 10;
			const int DEFAULT_EXPANDER_SPACING = 4;
			
			Rectangle GetExpanderBounds ()
			{
				return new Rectangle (2 + DEFAULT_EXPANDER_SPACING, 1 + (Allocation.Height - DEFAULT_EXPANDER_SIZE) / 2, DEFAULT_EXPANDER_SIZE, DEFAULT_EXPANDER_SIZE);
			}
			
			bool IsCloseSelected {
				get {
					return mouseOver && Math.Abs (Allocation.Width - 14 - mx) < 8;
				}
			}

			public void DrawCloseButton (Cairo.Context cr)
			{
				var yArc = Allocation.Height / 2;
				var xArc = Allocation.Width - 14;
				
				cr.Arc (xArc, yArc, 6, 0, Math.PI * 2);
				cr.Color = IsCloseSelected ? new Cairo.Color (0, 0, 0) : new Cairo.Color (1, 1, 1);
				cr.Fill ();
				
				cr.Arc (xArc, yArc, 6, 0, Math.PI * 2);
				cr.ClosePath ();
				cr.Color = (Mono.TextEditor.HslColor)Style.Dark (StateType.Normal);
				cr.Stroke ();
				
				cr.Color = IsCloseSelected ? new Cairo.Color (1, 1, 1) : new Cairo.Color (0, 0, 0);
				cr.LineWidth = 1;
				cr.MoveTo (xArc - 3, yArc - 3);
				cr.LineTo (xArc + 3, yArc + 3);
				cr.Stroke ();
				
				cr.MoveTo (xArc + 3, yArc - 3);
				cr.LineTo (xArc - 3, yArc + 3);
				cr.Stroke ();
			}
			
			protected override bool OnExposeEvent (EventExpose evnt)
			{
				Style.PaintBox (Style, evnt.Window, StateType.Insensitive, ShadowType.None, Allocation, this, "base", 0, 0, Allocation.Width, Allocation.Height);
				var expanderBounds = GetExpanderBounds ();
				
				using (var cr = CairoHelper.Create (evnt.Window)) {
					CairoCorners corners = CairoCorners.TopLeft | CairoCorners.TopRight;
					if (!container.Expanded)
						corners = CairoCorners.All;
					int r = 10;
					CairoExtensions.RoundedRectangle (cr, 0, 0, Allocation.Width, Allocation.Height, r, corners);
					
					
					var lg = new Cairo.LinearGradient (0, 0, 0, Allocation.Height);
					var state = mouseOver ? StateType.Prelight : StateType.Normal;
					
					lg.AddColorStop (0, (Mono.TextEditor.HslColor)Style.Mid (state));
					lg.AddColorStop (1, (Mono.TextEditor.HslColor)Style.Dark (state));
					
					cr.Pattern = lg;
					cr.Fill ();
					
					if (mouseOver) {
						CairoExtensions.RoundedRectangle (cr, 0, 0, Allocation.Width, Allocation.Height, r, corners);
						double rx = mx;
						double ry = my;
						Cairo.RadialGradient gradient = new Cairo.RadialGradient (rx, ry, Allocation.Width * 2, rx, ry, 2);
						gradient.AddColorStop (0, new Cairo.Color (0 ,0, 0, 0));
						Cairo.Color color = (Mono.TextEditor.HslColor)Style.Light (StateType.Normal);
						color.A = 0.2;
						gradient.AddColorStop (1, color);
						cr.Pattern = gradient;
						cr.Fill ();
					}
					cr.LineWidth = 1;
					CairoExtensions.RoundedRectangle (cr, 0.5, 0.5, Allocation.Width-1, Allocation.Height - 1, r, corners);
					cr.Color = (Mono.TextEditor.HslColor)Style.Dark (StateType.Normal);
					cr.Stroke ();
					
					using (var layout = new Pango.Layout (PangoContext)) {
						layout.SetMarkup (Label);
						int w, h;
						layout.GetPixelSize (out w, out h);
						
						const int padding = 4;
						cr.MoveTo (expanderBounds.Right + padding, (Allocation.Height - h) / 2);
						cr.Color = new Cairo.Color (0, 0, 0);
						cr.ShowLayout (layout);
					}
					
					DrawCloseButton (cr);
				}
				
				
				var state2 = mouseOver && !IsCloseSelected ? StateType.Prelight : StateType.Normal;
				Style.PaintExpander (Style, 
					evnt.Window, 
					state2,
					evnt.Region.Clipbox, 
					this, 
					"expander",
					expanderBounds.X + expanderBounds.Width / 2, expanderBounds.Y + expanderBounds.Width / 2,
					expanderStyle);
				
				
				
				return true;
			}
		}
		
		public ClosableExpander ()
		{
			header = new ExpanderHeader (this);
			PackStart (header, false, false, 0);
			
			contentBox = new VBox ();
			contentBox.ExposeEvent += delegate(object o, ExposeEventArgs args) {
				
				using (var cr = CairoHelper.Create (args.Event.Window)) {
					CairoCorners corners = CairoCorners.BottomLeft | CairoCorners.BottomRight;
					int r = 10;
					CairoExtensions.RoundedRectangle (cr, contentBox.Allocation.X + 0.5, contentBox.Allocation.Y+ 0.5, contentBox.Allocation.Width - 1, contentBox.Allocation.Height - 1, r, corners, true);
					cr.LineWidth = 1;
					cr.Color = (Mono.TextEditor.HslColor)Style.Dark (StateType.Normal);
					cr.Stroke ();
				}
			};
			PackStart (contentBox, true, true, 0);
			ShowAll ();
		}
		
		public void SetWidget (Gtk.Widget widget)
		{
			contentBox.PackStart (widget, true, true, 0);
			header.UpdateInitialExpanderState ();
		}
	}
}

