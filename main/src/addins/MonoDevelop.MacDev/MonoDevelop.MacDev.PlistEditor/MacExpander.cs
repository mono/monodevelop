// 
// DocumentTypeContainer.cs
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
	public class MacExpander : VBox
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
		
		public bool Expandable {
			get;
			set;
		}
		
		public bool Closeable {
			get;
			set;
		}
		
		class Border : DrawingArea
		{
			protected override void OnSizeRequested (ref Requisition requisition)
			{
				base.OnSizeRequested (ref requisition);
				requisition.Height = 1;
			}
			
			protected override bool OnExposeEvent (EventExpose evnt)
			{
				using (var cr = CairoHelper.Create (evnt.Window)) {
					cr.Color = (Mono.TextEditor.HslColor)Style.Dark (StateType.Normal);
					cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
					cr.Fill ();
				}
				return true;
			}
		}
		
		class ExpanderHeader : DrawingArea
		{
			MacExpander container;
			uint animationTimeout;
			ExpanderStyle expanderStyle = ExpanderStyle.Expanded;
			
			public string Label {
				get;
				set;
			}
			
			public void UpdateInitialExpanderState ()
			{
				expanderStyle = container.Expanded? ExpanderStyle.Expanded : ExpanderStyle.Collapsed;
			}
			
			public ExpanderHeader (MacExpander container)
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
				if (container.Closeable)
					requisition.Height += 4;
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
				if (container.Expandable && evnt.Button == 1)
					container.Expanded = !container.Expanded;
				return base.OnButtonReleaseEvent (evnt);
			}
			
			// constants taken from gtk
			const int DEFAULT_EXPANDER_SIZE = 10;
			const int DEFAULT_EXPANDER_SPACING = 2;
			
			Rectangle GetExpanderBounds ()
			{
				return new Rectangle (DEFAULT_EXPANDER_SPACING, 1 + (Allocation.Height - DEFAULT_EXPANDER_SIZE) / 2, DEFAULT_EXPANDER_SIZE, DEFAULT_EXPANDER_SIZE);
			}
			
			protected override bool OnExposeEvent (EventExpose evnt)
			{
				var expanderBounds = GetExpanderBounds ();
				using (var cr = CairoHelper.Create (evnt.Window)) {
					cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
					var lg = new Cairo.LinearGradient (0, 0, 0, Allocation.Height);
					var state = mouseOver ? StateType.Prelight : StateType.Normal;
					
					if (container.Closeable) {
						lg.AddColorStop (0, (Mono.TextEditor.HslColor)Style.Mid (state));
						lg.AddColorStop (1, (Mono.TextEditor.HslColor)Style.Dark (state));
					} else {
						lg.AddColorStop (0, (Mono.TextEditor.HslColor)Style.Light (state));
						lg.AddColorStop (1, (Mono.TextEditor.HslColor)Style.Mid (state));
					}
					
					cr.Pattern = lg;
					cr.Fill ();
					
					if (mouseOver && container.Expandable) {
						cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
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
					cr.MoveTo (0, 0);
					cr.LineTo (0, Allocation.Height);
					cr.LineTo (Allocation.Width, Allocation.Height);
					cr.LineTo (Allocation.Width, 0);
					if (!container.Expandable)
						cr.LineTo (0, 0);
					cr.Color = (Mono.TextEditor.HslColor)Style.Dark (StateType.Normal);
					cr.Stroke ();
					
					using (var layout = new Pango.Layout (PangoContext)) {
						layout.SetMarkup ("<b>" + Label + "</b>");
						int w, h;
						layout.GetPixelSize (out w, out h);
						
						const int padding = 4;
						
						cr.MoveTo (container.Expandable ? expanderBounds.Right + padding : padding, (Allocation.Height - h) / 2);
						cr.Color = new Cairo.Color (0, 0, 0);
						cr.ShowLayout (layout);
					}
				}
				
				if (container.Expandable) {
					
					var state2 = mouseOver ? StateType.Prelight : StateType.Normal;
					Style.PaintExpander (Style, 
						evnt.Window, 
						state2,
						evnt.Region.Clipbox, 
						this, 
						"expander",
						expanderBounds.X + expanderBounds.Width / 2, expanderBounds.Y + expanderBounds.Width / 2,
						expanderStyle);
				}
				
				return base.OnExposeEvent (evnt);
			}
		}
		
		Border border = new Border ();
		public MacExpander ()
		{
			header = new ExpanderHeader (this);
			PackStart (header, false, false, 0);
			
			contentBox = new VBox ();
			contentBox.PackEnd (border, false, false, 0);
			
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

