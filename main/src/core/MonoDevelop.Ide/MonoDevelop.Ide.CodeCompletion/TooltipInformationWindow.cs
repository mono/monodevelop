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
		readonly List<TooltipInformation> overloads = new List<TooltipInformation> ();
		int current_overload;
		
		public int CurrentOverload {
			get {
				return current_overload; 
			}
			set {
				current_overload = value;
				ShowOverload ();
			}
		}

		public int Overloads {
			get {
				return overloads.Count;
			}
		}
		
		readonly FixedWidthWrapLabel headLabel;
		public bool Multiple{
			get {
				return overloads.Count > 1;
			}
		}

		public void AddOverload (TooltipInformation tooltipInformation)
		{
			if (tooltipInformation == null || tooltipInformation.IsEmpty)
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
			if (tooltipInformation.IsEmpty)
				return;

			using (var layout = new Pango.Layout (PangoContext)) {
				layout.FontDescription = FontService.GetFontDescription ("Editor");
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
			var w = Math.Max (headLabel.WidthRequest, headLabel.RealWidth);
			requisition.Width = (int)Math.Max (w + ContentBox.LeftPadding + ContentBox.RightPadding, requisition.Width);
		}

		void ShowOverload ()
		{
			ClearDescriptions ();

			if (current_overload >= 0 && current_overload < overloads.Count) {
				var o = overloads[current_overload];
				headLabel.Markup = o.SignatureMarkup;
				headLabel.Visible = !string.IsNullOrEmpty (o.SignatureMarkup);
				int x, y;
				GetPosition (out x, out y);
				var geometry = DesktopService.GetUsableMonitorGeometry (Screen, Screen.GetMonitorAtPoint (x, y));
				headLabel.MaxWidth = Math.Max (geometry.Width / 5, 480);

				if (Theme.DrawPager && overloads.Count > 1) {
					headLabel.WidthRequest = headLabel.RealWidth + 70;
				} else {
					headLabel.WidthRequest = -1;
				}
				foreach (var cat in o.Categories) {
					descriptionBox.PackStart (CreateCategory (cat.Item1, cat.Item2), true, true, 4);
				}

				if (!string.IsNullOrEmpty (o.SummaryMarkup)) {
					descriptionBox.PackStart (CreateCategory (GettextCatalog.GetString ("Summary"), o.SummaryMarkup), true, true, 4);
				}
				if (!string.IsNullOrEmpty (o.FooterMarkup)) {

					var contentLabel = new FixedWidthWrapLabel ();
					contentLabel.Wrap = Pango.WrapMode.WordChar;
					contentLabel.BreakOnCamelCasing = false;
					contentLabel.BreakOnPunctuation = false;
					contentLabel.MaxWidth = 400;
					contentLabel.Markup = o.FooterMarkup.Trim ();
					contentLabel.ModifyFg (StateType.Normal, foreColor.ToGdkColor ());
					contentLabel.FontDescription = FontService.GetFontDescription ("Editor");

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
			headLabel.Markup = "";
			current_overload = 0;
		}

		VBox CreateCategory (string categoryName, string categoryContentMarkup)
		{
			var vbox = new VBox ();

			vbox.Spacing = 2;

			if (categoryName != null) {
				var catLabel = new FixedWidthWrapLabel ();
				catLabel.Text = categoryName;
				catLabel.ModifyFg (StateType.Normal, foreColor.ToGdkColor ());
				catLabel.FontDescription = FontService.GetFontDescription ("Editor");
				vbox.PackStart (catLabel, false, true, 0);
			}

			var contentLabel = new FixedWidthWrapLabel ();
			contentLabel.Wrap = Pango.WrapMode.WordChar;
			contentLabel.BreakOnCamelCasing = false;
			contentLabel.BreakOnPunctuation = false;
			contentLabel.MaxWidth = 400;
			contentLabel.Markup = categoryContentMarkup.Trim ();
			contentLabel.ModifyFg (StateType.Normal, foreColor.ToGdkColor ());
			contentLabel.FontDescription = FontService.GetFontDescription ("Editor");

			vbox.PackStart (contentLabel, true, true, 0);

			return vbox;
		}

		readonly VBox descriptionBox = new VBox (false, 0);
		readonly VBox vb2 = new VBox (false, 0);
		Cairo.Color foreColor;

		internal void SetDefaultScheme ()
		{
			var scheme = Mono.TextEditor.Highlighting.SyntaxModeService.GetColorStyle (IdeApp.Preferences.ColorScheme);
			Theme.SetSchemeColors (scheme);
			foreColor = scheme.PlainText.Foreground;
			headLabel.ModifyFg (StateType.Normal, foreColor.ToGdkColor ());
		}

		public TooltipInformationWindow () : base ()
		{
			TypeHint = Gdk.WindowTypeHint.Tooltip;
			this.SkipTaskbarHint = true;
			this.SkipPagerHint = true;
			this.AllowShrink = false;
			this.AllowGrow = false;
			this.CanFocus = false;
			this.CanDefault = false;
			this.Events |= Gdk.EventMask.EnterNotifyMask; 
			
			headLabel = new FixedWidthWrapLabel ();
			headLabel.Indent = -20;
			headLabel.FontDescription = FontService.GetFontDescription ("Editor").CopyModified (1.1);
			headLabel.Wrap = Pango.WrapMode.WordChar;
			headLabel.BreakOnCamelCasing = false;
			headLabel.BreakOnPunctuation = false;

			descriptionBox.Spacing = 4;

			VBox vb = new VBox (false, 8);
			vb.PackStart (headLabel, true, true, 4);
			vb.PackStart (descriptionBox, true, true, 4);

			HBox hb = new HBox (false, 4);
			hb.PackStart (vb, true, true, 6);

			WindowTransparencyDecorator.Attach (this);

			vb2.Spacing = 4;
			vb2.PackStart (hb, true, true, 0);
			ContentBox.Add (vb2);

			SetDefaultScheme ();

			ShowAll ();
			DesktopService.RemoveWindowShadow (this);
		}
	}
}
