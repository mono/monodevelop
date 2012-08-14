// DeclarationViewWindow.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Ide.CodeCompletion
{
	class DeclarationViewArrow : Gtk.EventBox
	{
		readonly bool left;

		public DeclarationViewArrow (bool left)
		{
			this.left =  left;
			AppPaintable = true;
			VisibleWindow = false;
			SetSizeRequest (10, 9);
			Show ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				var h = Allocation.Height;
				var w = Allocation.Width;
				context.Translate (Allocation.X, Allocation.Y);
				if (left) {
					context.MoveTo (0, h / 2);
					context.LineTo (w / 2, h);
					context.LineTo (w / 2, h * 2 / 3);
					context.LineTo (w, h * 2 / 3);
					context.LineTo (w, h * 1 / 3);
					context.LineTo (w / 2, h * 1 / 3);
					context.LineTo (w / 2, 0);
				} else {
					context.MoveTo (w, h / 2);
					context.LineTo (w / 2, h);
					context.LineTo (w / 2, h * 2 / 3);
					context.LineTo (0, h * 2 / 3);
					context.LineTo (0, h * 1 / 3);
					context.LineTo (w / 2, h * 1 / 3);
					context.LineTo (w / 2, 0);

				}
				context.ClosePath ();
				context.Color = new Cairo.Color (0, 0, 0, 0.6);
				context.Fill ();
			}

			return base.OnExposeEvent (evnt);
		}

	}

	class DelecationViewPagerBubbles : Gtk.EventBox
	{
		const int bubblePadding = 1;
		const int bubbleRadius  = 3;

		public int Bubbles {
			get;
			set;
		}

		public int ActiveBubble {
			get;
			set;
		}

		public DelecationViewPagerBubbles ()
		{
			AppPaintable = true;
			VisibleWindow = false;
			Show ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.Translate (Allocation.X, Allocation.Y);
				var bubbleWidth = bubbleRadius * 2 + bubblePadding;
				double x = Math.Max (0, (Allocation.Width - (Bubbles * bubbleWidth)) / 2);
				for (int i = 0 ; i < Bubbles; i++) {
					context.Arc (x, Allocation.Height / 2, bubbleRadius, 0, Math.PI * 2);
					context.Color = new Cairo.Color (0, 0, 0, i == ActiveBubble ? 0.8 : 0.3);
					context.Fill ();
					x += bubbleWidth;
				}
			}
			return base.OnExposeEvent (evnt);
		}

	}

	class DeclarationViewWindow : PopoverWindow
	{
		static char[] newline = { '\n' };
		
		List<TooltipInformation> overloads = new List<TooltipInformation> ();
		int current_overload;
		
		public int CurrentOverload {
			get {
				return this.current_overload; 
			}
			set {
				this.current_overload = value;
				ShowOverload ();
			}
		}

		public int Overloads {
			get {
				return overloads.Count;
			}
		}
		
		MonoDevelop.Components.FixedWidthWrapLabel headlabel, bodylabel;
		HBox helpbox;
		DelecationViewPagerBubbles infoBubbles = new DelecationViewPagerBubbles ();

		public bool Multiple{
			get {
				return overloads.Count > 1;
			}
		}

		public void AddOverload (CompletionData data)
		{
			var tooltipInformation = data.CreateTooltipInformation (false);

			using (var layout = new Pango.Layout (PangoContext)) {
				var des = FontService.GetFontDescription ("Editor");
				layout.FontDescription = des;
				layout.SetMarkup (tooltipInformation.SignatureMarkup);
				int w, h;
				layout.GetPixelSize (out w, out h);
				if (w >= Allocation.Width - 10) {
					tooltipInformation = data.CreateTooltipInformation (true);
				}
			}


			overloads.Add (tooltipInformation);
			if (overloads.Count == 2)
				helpbox.Visible = true;
			infoBubbles.Bubbles = overloads.Count;

			ShowOverload ();
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Width = Math.Min (Allocation.Width, requisition.Width);
			requisition.Height = Math.Min (Allocation.Height, requisition.Height);
			base.OnSizeRequested (ref requisition);
		}

		void ShowOverload ()
		{
			if (current_overload >= 0 && current_overload < overloads.Count) {
				QueueResize ();
				var o = overloads[current_overload];
				headlabel.Markup = o.SignatureMarkup;
				headlabel.Visible = true;
				if (!string.IsNullOrEmpty (o.SummaryMarkup)) {
					bodylabel.Markup = "<span size=\"smaller\">Summary</span>"+ Environment.NewLine + o.SummaryMarkup;
					bodylabel.Visible = true;
				} else {
					bodylabel.Visible = false;
				}
				infoBubbles.ActiveBubble = current_overload;
			}
		}

		public void OverloadLeft ()
		{
			if (current_overload == 0) {
				if (overloads.Count > 0)
					current_overload = overloads.Count - 1;
			} else {
				current_overload--;
			}
			ShowOverload ();
		}

		public void OverloadRight ()
		{
			if (current_overload == overloads.Count - 1) {
				current_overload = 0;
			} else {
				if (overloads.Count > 0)
					current_overload++;
			}
			ShowOverload ();
		}

		public void Clear ()
		{
			overloads.Clear ();
			helpbox.Visible = false;
			current_overload = 0;
		}
		
		public void SetFixedWidth (int w)
		{
			if (w != -1) {
				headlabel.MaxWidth = w;
			} else {
				headlabel.MaxWidth = -1;
			}
			bodylabel.MaxWidth = headlabel.RealWidth > 350 ? headlabel.RealWidth : 350;
			QueueResize ();
		}

		public DeclarationViewWindow () : base ()
		{
			this.AllowShrink = false;
			this.AllowGrow = false;
			
			headlabel = new MonoDevelop.Components.FixedWidthWrapLabel ();
			headlabel.Indent = -20;
			var des = FontService.GetFontDescription ("Editor");
			
			headlabel.FontDescription = des;

			headlabel.Wrap = Pango.WrapMode.WordChar;
			headlabel.BreakOnCamelCasing = true;
			headlabel.BreakOnPunctuation = true;
			
			bodylabel = new MonoDevelop.Components.FixedWidthWrapLabel ();
			bodylabel.Wrap = Pango.WrapMode.WordChar;
			bodylabel.BreakOnCamelCasing = true;
			bodylabel.BreakOnPunctuation = true;
			
			VBox vb = new VBox (false, 0);
			vb.PackStart (headlabel, true, true, 0);
			vb.PackStart (bodylabel, true, true, 3);

			HBox hb = new HBox (false, 0);
			hb.Spacing = 4;
			hb.PackStart (vb, true, true, 0);

			helpbox = new HBox (false, 0);
			var leftArrow = new DeclarationViewArrow (true);
			helpbox.PackStart (leftArrow, false, false, 0);
			helpbox.PackStart (infoBubbles, true, true, 0);
			var rightArrow = new DeclarationViewArrow (false);
			helpbox.PackEnd (rightArrow, false, false, 0);
			helpbox.BorderWidth = 2;
			
			VBox vb2 = new VBox (false, 0);
			vb2.Spacing = 4;
			vb2.PackStart (hb, true, true, 0);
			vb2.PackStart (helpbox, false, true, 0);
			this.Add (vb2);
			var scheme = Mono.TextEditor.Highlighting.SyntaxModeService.GetColorStyle (Style, PropertyService.Get<string> ("ColorScheme"));
			this.BackgroundColor = scheme.Tooltip.CairoBackgroundColor;
			ShowAll ();
		}
	}
}
