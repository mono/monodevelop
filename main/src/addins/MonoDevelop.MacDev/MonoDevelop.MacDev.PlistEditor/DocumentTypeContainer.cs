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
	public class DocumentTypeContainer : VBox
	{
		ExpanderHeader header;
		VBox contentBox;
		Label noContentLabel;
		
		public string ContentLabel {
			get {
				
				return header.Label;
			}
			set {
				header.Label = value;
			}
		}
		
		public string NoContentMessage {
			get {
				return noContentLabel.Text;
			}
			set {
				noContentLabel.Text = value;
			}
		}
		
		public bool Expanded {
			get {
				return contentBox.Visible;
			}
			set {
				contentBox.Visible = value;
			}
		}
		
		public bool Expandable {
			get;
			set;
		}
		
		public class ExpanderHeader : DrawingArea
		{
			DocumentTypeContainer container;
			
			public string Label {
				get;
				set;
			}
			
			public ExpanderHeader (DocumentTypeContainer container)
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
				if (container.Expandable)
					container.Expanded = !container.Expanded;
				return base.OnButtonPressEvent (evnt);
			}
			
			// constants taken from gtk
			const int DEFAULT_EXPANDER_SIZE = 10;
			const int DEFAULT_EXPANDER_SPACING = 2;
			
			Rectangle GetExpanderBounds ()
			{
				return new Rectangle (DEFAULT_EXPANDER_SPACING, (Allocation.Height - DEFAULT_EXPANDER_SIZE) / 2, DEFAULT_EXPANDER_SIZE, DEFAULT_EXPANDER_SIZE);
			}
			
			protected override bool OnExposeEvent (EventExpose evnt)
			{
				using (var cr = CairoHelper.Create (evnt.Window)) {
					cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
					var lg = new Cairo.LinearGradient (0, 0, 0, Allocation.Height);
					var state = mouseOver ? StateType.Prelight : StateType.Normal;
					lg.AddColorStop (0, (Mono.TextEditor.HslColor)Style.Light (state));
					lg.AddColorStop (1, (Mono.TextEditor.HslColor)Style.Mid (state));
					cr.Pattern = lg;
					cr.FillPreserve ();
					
					if (mouseOver && container.Expandable) {
						double rx = mx;
						double ry = my;
						Cairo.RadialGradient gradient = new Cairo.RadialGradient (rx, ry, Allocation.Width * 2, rx, ry, 2);
						gradient.AddColorStop (0, new Cairo.Color (0 ,0, 0, 0));
						Cairo.Color color = (Mono.TextEditor.HslColor)Style.Light (StateType.Normal);
						color.A = 0.2;
						gradient.AddColorStop (1, color);
						cr.Pattern = gradient;
						cr.FillPreserve ();
					}
					
					cr.Color = (Mono.TextEditor.HslColor)Style.Dark (StateType.Normal);
					cr.Stroke ();
					
					using (var layout = new Pango.Layout (PangoContext)) {
						layout.SetMarkup ("<b>" + Label + "</b>");
						int w, h;
						layout.GetPixelSize (out w, out h);
						
						cr.MoveTo (container.Expandable ? 16 : 4, (Allocation.Height - h) / 2);
						cr.Color = new Cairo.Color (0, 0, 0);
						cr.ShowLayout (layout);
					}
				}
				
				var bounds = GetExpanderBounds ();
				
				var state2 = StateType.Normal;
				Style.PaintExpander (Style, 
					evnt.Window, 
					state2,
					evnt.Region.Clipbox, 
					this, 
					"expander",
					0, evnt.Region.Clipbox.Height / 2,
					this.container.Expanded ? ExpanderStyle.Expanded : ExpanderStyle.Collapsed);
				return base.OnExposeEvent (evnt);
			}
		}
		
		public DocumentTypeContainer ()
		{
			header = new ExpanderHeader (this);
			PackStart (header, false, false, 0);
			
			contentBox = new VBox ();
			noContentLabel = new Label ();
			contentBox.PackStart (noContentLabel, true, true, 32);
			PackStart (contentBox, true, true, 0);
		}
		
		public void SetWidget (Gtk.Widget widget)
		{
			contentBox.Remove (noContentLabel);
			contentBox.PackStart (widget, true, true, 0);
		}
	}
}

