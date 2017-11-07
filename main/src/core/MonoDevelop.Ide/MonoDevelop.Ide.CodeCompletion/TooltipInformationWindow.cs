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
using MonoDevelop.Ide.Gui;
using System.Linq;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Ide.CodeCompletion
{

	public class TooltipInformationWindow : XwtThemedPopup
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
			try {
				var tooltipInformation = await data.CreateTooltipInformation (false, cancelToken);
				if (tooltipInformation == null || tooltipInformation.IsEmpty || cancelToken.IsCancellationRequested)
					return;

				using (var layout = new Pango.Layout (headLabel.PangoContext)) {
					layout.FontDescription = Theme.Font.ToPangoFont ();
					layout.SetMarkup (tooltipInformation.SignatureMarkup);
					int w, h;
					layout.GetPixelSize (out w, out h);
					if (w >= Size.Width - 10) {
						tooltipInformation = await data.CreateTooltipInformation (true, cancelToken);
					}
				}
				if (cancelToken.IsCancellationRequested)
					return;
				AddOverload (tooltipInformation);
			} catch (Exception e) {
				LoggingService.LogError ("Error while adding overload : " + data, e);
			}
		}
		int labelMaxWidth = 480;

		public int LabelMaxWidth { get { return labelMaxWidth; } set { labelMaxWidth = Math.Max (100, value); } }

		void ShowOverload ()
		{
			Opacity = 0;
			ClearDescriptions ();

			if (current_overload >= 0 && current_overload < overloads.Count) {
				var o = overloads[current_overload];
				headLabel.Markup = o.SignatureMarkup;
				headLabel.Visible = !string.IsNullOrEmpty (o.SignatureMarkup);

				headLabel.MaxWidth = LabelMaxWidth;
				if (Visible)
					headLabel.MaxWidth = ((int) (Screen?.VisibleBounds.Width / 5 ?? LabelMaxWidth));

				if (Theme.DrawPager && overloads.Count > 1) {
					headLabel.WidthRequest = headLabel.RealWidth + 70;
				} else {
					headLabel.WidthRequest = -1;
				}
				foreach (var cat in o.Categories) {
					descriptionBox.PackStart (CreateCategory (GetHeaderMarkup (cat.Item1), cat.Item2, foreColor, Theme.Font.ToPangoFont (), LabelMaxWidth - 80), true, true, 4);
				}

				if (!string.IsNullOrEmpty (o.SummaryMarkup)) {
					descriptionBox.PackStart (CreateCategory (GetHeaderMarkup (GettextCatalog.GetString ("Summary")), o.SummaryMarkup, foreColor, Theme.Font.ToPangoFont (), LabelMaxWidth - 80), true, true, 4);
				}
				if (!string.IsNullOrEmpty (o.FooterMarkup)) {

					var contentLabel = new FixedWidthWrapLabel ();
					contentLabel.Wrap = Pango.WrapMode.WordChar;
					contentLabel.BreakOnCamelCasing = false;
					contentLabel.BreakOnPunctuation = false;
					contentLabel.MaxWidth = LabelMaxWidth - 80;
					contentLabel.Markup = o.FooterMarkup.Trim ();
					contentLabel.ModifyFg (StateType.Normal, foreColor.ToGdkColor ());
					contentLabel.FontDescription = Theme.Font.ToPangoFont ();

					descriptionBox.PackEnd (contentLabel, true, true, 4);
				}

				if (string.IsNullOrEmpty (o.SummaryMarkup) && string.IsNullOrEmpty (o.FooterMarkup) && !o.Categories.Any ()) {
					descriptionBox.Hide ();
				} else {
					descriptionBox.ShowAll ();
				}
				Theme.CurrentPage = current_overload;
				// if the target is not on the left or top side, we may loose the arrow alignment on our target
				// and must reposition
				if (!CurrentPosition.HasFlag (PopupPosition.Left) &&
				    !CurrentPosition.HasFlag (PopupPosition.Top))
					RepositionWindow ();

				// Setting the opicity delayed to 1 is a hack to ensure smooth animation popup see "Bug 32046 - Janky animations on tooltips"
				Xwt.Application.TimeoutInvoke (50, delegate {
					Opacity = 1;
					return false;
				});
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

		internal static VBox CreateCategory (string categoryName, string categoryContentMarkup, Cairo.Color foreColor, Pango.FontDescription font, int labelMaxWidth = 400)
		{
			var vbox = new VBox ();

			vbox.Spacing = 8;

			if (categoryName != null) {
				var catLabel = new FixedWidthWrapLabel ();
				catLabel.Markup = categoryName;
				catLabel.ModifyFg (StateType.Normal, foreColor.ToGdkColor ());
				catLabel.FontDescription = font.Copy ();
				catLabel.FontDescription.Weight = Pango.Weight.Bold;
				catLabel.FontDescription.Size = catLabel.FontDescription.Size + (int)(1 * Pango.Scale.PangoScale);
				vbox.PackStart (catLabel, false, true, 0);
			}

			var contentLabel = new FixedWidthWrapLabel ();
			HBox hbox = new HBox ();

			// hbox.PackStart (new Label(), false, true, 10);


			contentLabel.Wrap = Pango.WrapMode.WordChar;
			contentLabel.Spacing = 1;
			contentLabel.BreakOnCamelCasing = false;
			contentLabel.BreakOnPunctuation = false;
			contentLabel.MaxWidth = labelMaxWidth;
			contentLabel.Markup = categoryContentMarkup.Trim ();
			contentLabel.ModifyFg (StateType.Normal, foreColor.ToGdkColor ());
			contentLabel.FontDescription = font;

			hbox.PackStart (contentLabel, true, true, 0);
			vbox.PackStart (hbox, true, true, 0);

			return vbox;
		}

		readonly VBox descriptionBox = new VBox (false, 0);
		readonly VBox vb2 = new VBox (false, 0);
		Cairo.Color foreColor;

		internal void SetDefaultScheme ()
		{
			var scheme = SyntaxHighlightingService.GetEditorTheme (IdeApp.Preferences.ColorScheme);
			if (!scheme.FitsIdeTheme (IdeApp.Preferences.UserInterfaceTheme))
				scheme = SyntaxHighlightingService.GetDefaultColorStyle (IdeApp.Preferences.UserInterfaceTheme);

			Theme.SetSchemeColors (scheme);
			foreColor = Styles.PopoverWindow.DefaultTextColor.ToCairoColor ();
			headLabel.ModifyFg (StateType.Normal, foreColor.ToGdkColor ());
			headLabel.FontDescription = FontService.GetFontDescription ("Editor").CopyModified (Styles.FontScale11);
			Theme.Font = FontService.SansFont.CopyModified (Styles.FontScale11).ToXwtFont ();
			Theme.ShadowColor = Styles.PopoverWindow.ShadowColor;
			if (this.Visible)
				ShowOverload ();
		}

		public TooltipInformationWindow ()
		{
			headLabel = new FixedWidthWrapLabel ();
			headLabel.Indent = 0;
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

			vb2.SizeRequested += (o, args) => {
				var w = Math.Max (headLabel.WidthRequest, headLabel.RealWidth);
				var req = new Gtk.Requisition ();
				req.Height = args.Requisition.Height;
				req.Width = (int)Math.Max (w + PaddingLeft + PaddingTop, args.Requisition.Width);
				args.Args[0] = req;
			};

			Content = BackendHost.ToolkitEngine.WrapWidget (vb2, Xwt.NativeWidgetSizing.DefaultPreferredSize);

			vb2.ShowAll ();
			SetDefaultScheme ();
			Styles.Changed += HandleThemeChanged;
			IdeApp.Preferences.ColorScheme.Changed += HandleThemeChanged;
		}

		protected override bool OnPagerLeftClicked ()
		{
			OverloadLeft ();
			return base.OnPagerLeftClicked ();
		}

		protected override bool OnPagerRightClicked ()
		{
			OverloadRight ();
			return base.OnPagerRightClicked ();
		}

		void HandleThemeChanged (object sender, EventArgs e)
		{
			SetDefaultScheme ();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				Styles.Changed -= HandleThemeChanged;
				IdeApp.Preferences.ColorScheme.Changed -= HandleThemeChanged;
			}
			base.Dispose (disposing);
		}
	}
}
