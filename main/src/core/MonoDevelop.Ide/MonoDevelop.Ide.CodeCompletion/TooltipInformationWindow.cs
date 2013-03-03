// TooltipInformationWindow.cs
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
using System.Linq;
using Mono.TextEditor.PopupWindow;

namespace MonoDevelop.Ide.CodeCompletion
{

	public class TooltipInformationWindow : PopoverWindow
	{
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
		
		MonoDevelop.Components.FixedWidthWrapLabel headlabel;
		public bool Multiple{
			get {
				return overloads.Count > 1;
			}
		}

		public void AddOverload (TooltipInformation tooltipInformation)
		{
			if (tooltipInformation == null || string.IsNullOrEmpty (tooltipInformation.SignatureMarkup))
				return;
			overloads.Add (tooltipInformation);

			if (overloads.Count > 1) {
				Theme.DrawPager = true;
				Theme.NumPages = overloads.Count;
			}

			ShowOverload ();
		}

		public void AddOverload (CompletionData data)
		{
			var tooltipInformation = data.CreateTooltipInformation (false);
			if (string.IsNullOrEmpty (tooltipInformation.SignatureMarkup))
				return;

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
			AddOverload (tooltipInformation);
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			var w = Math.Max (headlabel.WidthRequest, headlabel.RealWidth);
			requisition.Width = (int)Math.Max (w + ContentBox.LeftPadding + ContentBox.RightPadding, requisition.Width);
		}

		void ShowOverload ()
		{
			ClearDescriptions ();

			if (current_overload >= 0 && current_overload < overloads.Count) {
				var o = overloads[current_overload];
				headlabel.Markup = o.SignatureMarkup;
				headlabel.Visible = true;
				if (Theme.DrawPager && overloads.Count > 1) {
					headlabel.WidthRequest = headlabel.RealWidth + 70;
				} else {
					headlabel.WidthRequest = -1;
				}
				foreach (var cat in o.Categories) {
					descriptionBox.PackStart (CreateCategory (cat.Item1, cat.Item2), true, true, 4);
				}

				if (!string.IsNullOrEmpty (o.SummaryMarkup)) {
					descriptionBox.PackStart (CreateCategory (GettextCatalog.GetString ("Summary"), o.SummaryMarkup), true, true, 4);
				}
				if (!string.IsNullOrEmpty (o.FooterMarkup)) {

					var contentLabel = new MonoDevelop.Components.FixedWidthWrapLabel ();
					contentLabel.Wrap = Pango.WrapMode.WordChar;
					contentLabel.BreakOnCamelCasing = true;
					contentLabel.MaxWidth = 400;
					contentLabel.BreakOnPunctuation = true;
					contentLabel.Markup = o.FooterMarkup.Trim ();
					contentLabel.ModifyFg (StateType.Normal, (HslColor)foreColor);

					descriptionBox.PackEnd (contentLabel, true, true, 4);
				}

				if (string.IsNullOrEmpty (o.SummaryMarkup) && string.IsNullOrEmpty (o.FooterMarkup) && !o.Categories.Any ()) {
					descriptionBox.Hide ();
				} else {
					descriptionBox.ShowAll ();
				}
				Theme.CurrentPage = current_overload;
				QueueResize ();
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

		void ClearDescriptions ()
		{
			while (descriptionBox.Children.Length > 0) {
				var child = descriptionBox.Children [0];
				descriptionBox.Remove (child);
				child.Destroy ();
			}
		}

		public void Clear ()
		{
			ClearDescriptions ();
			overloads.Clear ();
			Theme.DrawPager = false;
			headlabel.Markup = "";
			current_overload = 0;
		}
		
		public void SetFixedWidth (int w)
		{
			if (w != -1) {
				headlabel.MaxWidth = w;
			} else {
				headlabel.MaxWidth = -1;
			}
			QueueResize ();
		}

		VBox CreateCategory (string categoryName, string categoryContentMarkup)
		{
			var vbox = new VBox ();

			vbox.Spacing = 2;

			var catLabel = new MonoDevelop.Components.FixedWidthWrapLabel ();
			catLabel.Text = categoryName;
			catLabel.ModifyFg (StateType.Normal, (HslColor)foreColor);

			vbox.PackStart (catLabel, false, true, 0);

			var contentLabel = new MonoDevelop.Components.FixedWidthWrapLabel ();
			contentLabel.Wrap = Pango.WrapMode.WordChar;
			contentLabel.BreakOnCamelCasing = true;
			contentLabel.MaxWidth = 400;
			contentLabel.BreakOnPunctuation = true;
			contentLabel.Markup = categoryContentMarkup.Trim ();
			contentLabel.ModifyFg (StateType.Normal, (HslColor)foreColor);

			vbox.PackStart (contentLabel, true, true, 0);

			return vbox;
		}

		VBox descriptionBox = new VBox (false, 0);
		VBox vb2 = new VBox (false, 0);
		Cairo.Color foreColor;
		public TooltipInformationWindow () : base ()
		{
			TypeHint = Gdk.WindowTypeHint.Tooltip;
			this.SkipTaskbarHint = true;
			this.SkipPagerHint = true;
			if (IdeApp.Workbench != null)
				this.TransientFor = IdeApp.Workbench.RootWindow;
			this.AllowShrink = false;
			this.AllowGrow = false;
			this.CanFocus = false;
			this.CanDefault = false;
			this.Events |= Gdk.EventMask.EnterNotifyMask; 
			
			headlabel = new MonoDevelop.Components.FixedWidthWrapLabel ();
			headlabel.Indent = -20;
			var des = FontService.GetFontDescription ("Editor").Copy ();
			des.Size = des.Size * 9 / 10;
			headlabel.FontDescription = des;
//			headlabel.MaxWidth = 400;
			headlabel.Wrap = Pango.WrapMode.WordChar;
			headlabel.BreakOnCamelCasing = true;
//			headlabel.BreakOnPunctuation = true;
			descriptionBox.Spacing = 4;
			VBox vb = new VBox (false, 8);
			vb.PackStart (headlabel, true, true, 0);
			vb.PackStart (descriptionBox, true, true, 0);

			HBox hb = new HBox (false, 0);
			hb.PackStart (vb, true, true, 0);
			WindowTransparencyDecorator.Attach (this);

			vb2.Spacing = 4;
			vb2.PackStart (hb, true, true, 0);
			ContentBox.Add (vb2);
			var scheme = Mono.TextEditor.Highlighting.SyntaxModeService.GetColorStyle (IdeApp.Preferences.ColorScheme);
			Theme.SetSchemeColors (scheme);
			foreColor = scheme.PlainText.Foreground;
			headlabel.ModifyFg (StateType.Normal, (HslColor)foreColor);
			ShowAll ();
			DesktopService.RemoveWindowShadow (this);
		}
	}
}
