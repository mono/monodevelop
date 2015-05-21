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
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Threading.Tasks;
using System.Threading;

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

		public async Task AddOverload (CompletionData data, CancellationToken cancelToken)
		{
			var tooltipInformation = await data.CreateTooltipInformation (false, cancelToken);
			if (tooltipInformation.IsEmpty || cancelToken.IsCancellationRequested)
				return;

			using (var layout = new Pango.Layout (PangoContext)) {
				layout.FontDescription = FontService.GetFontDescription ("Editor");
				layout.SetMarkup (tooltipInformation.SignatureMarkup);
				int w, h;
				layout.GetPixelSize (out w, out h);
				if (w >= Allocation.Width - 10) {
					tooltipInformation = await data.CreateTooltipInformation (true, cancelToken);
				}
			}
			if (cancelToken.IsCancellationRequested)
				return;
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
					descriptionBox.PackStart (CreateCategory (GetHeaderMarkup (cat.Item1), cat.Item2, foreColor), true, true, 4);
				}

				if (!string.IsNullOrEmpty (o.SummaryMarkup)) {
					descriptionBox.PackStart (CreateCategory (GetHeaderMarkup (GettextCatalog.GetString ("Summary")), o.SummaryMarkup, foreColor), true, true, 4);
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

		internal static string GetHeaderMarkup (string headerName)
		{
			return headerName;
			// return "<span foreground=\"#a7a79c\" size=\"larger\">" + headerName + "</span>";
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

		internal static VBox CreateCategory (string categoryName, string categoryContentMarkup, Cairo.Color foreColor)
		{
			var vbox = new VBox ();

			vbox.Spacing = 8;

			if (categoryName != null) {
				var catLabel = new FixedWidthWrapLabel ();
				catLabel.Markup = categoryName;
				catLabel.ModifyFg (StateType.Normal, foreColor.ToGdkColor ());
				catLabel.FontDescription = FontService.GetFontDescription ("Editor");
				vbox.PackStart (catLabel, false, true, 0);
			}

			var contentLabel = new FixedWidthWrapLabel ();
			HBox hbox = new HBox ();

			// hbox.PackStart (new Label(), false, true, 10);


			contentLabel.Wrap = Pango.WrapMode.WordChar;
			contentLabel.BreakOnCamelCasing = false;
			contentLabel.BreakOnPunctuation = false;
			contentLabel.MaxWidth = 400;
			contentLabel.Markup = categoryContentMarkup.Trim ();
			contentLabel.ModifyFg (StateType.Normal, foreColor.ToGdkColor ());
			contentLabel.FontDescription = FontService.GetFontDescription ("Editor");

			hbox.PackStart (contentLabel, true, true, 0);
			vbox.PackStart (hbox, true, true, 0);

			return vbox;
		}

		readonly VBox descriptionBox = new VBox (false, 0);
		readonly VBox vb2 = new VBox (false, 0);
		Cairo.Color foreColor;

		internal void SetDefaultScheme ()
		{
			var scheme = SyntaxModeService.GetColorStyle (IdeApp.Preferences.ColorScheme);
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
