//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gdk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;

namespace Mono.TextEditor
{
	class CodePreviewWindow : Gtk.Window
	{
		string fontName;

		Pango.FontDescription fontDescription;
		Pango.Layout layout;

		string footerText, text, markup;
		Pango.FontDescription footerFontDescription;
		Pango.Layout footerLayout;

		readonly int maxWidth, maxHeight;

		readonly HslColor colorText, colorBg, colorFold;

		public CodePreviewWindow (
			Gdk.Window parentWindow,
			string fontName = null,
			EditorTheme theme = null)
			: base (Gtk.WindowType.Popup)
		{
			ParentWindow = parentWindow ?? MonoDevelop.Ide.IdeApp.Workbench.RootWindow.GdkWindow;

			AppPaintable = true;
			SkipPagerHint = SkipTaskbarHint = true;
			TypeHint = WindowTypeHint.Menu;

			this.fontName = fontName = fontName ?? DefaultSourceEditorOptions.Instance.FontName;

			layout = PangoUtil.CreateLayout (this);
			fontDescription = Pango.FontDescription.FromString (fontName);
			fontDescription.Size = (int)(fontDescription.Size * 0.8f);
			layout.FontDescription = fontDescription;
			layout.Ellipsize = Pango.EllipsizeMode.End;

			var geometry = Screen.GetUsableMonitorGeometry (Screen.GetMonitorAtWindow (ParentWindow));
			maxWidth = geometry.Width * 2 / 5;
			maxHeight = geometry.Height * 2 / 5;

			layout.SetText ("n");
			layout.GetPixelSize (out int _, out int lineHeight);
			MaximumLineCount = maxHeight / lineHeight;

			theme = theme ?? DefaultSourceEditorOptions.Instance.GetEditorTheme ();
			colorText = SyntaxHighlightingService.GetColor (theme, EditorThemeColors.Foreground);
			colorBg = SyntaxHighlightingService.GetColor (theme, EditorThemeColors.Background);
			colorFold = SyntaxHighlightingService.GetColor (theme, EditorThemeColors.CollapsedText);
		}

		/// <summary>
		/// The maximum number of lines that will fit in this dialog
		/// </summary>
		public int MaximumLineCount { get; }

		public string Text {
			set {
				text = value;
				markup = null;
				layout.SetText (value);
				UpdateSize ();
			}
			get => text;
		}

		public string Markup {
			set {
				markup = value;
				text = null;
				layout.SetMarkup (value);
				UpdateSize ();
			}
			get => markup;
		}

		protected string FooterText {
			get {
				return footerText;
			}
			set {
				if (!string.IsNullOrEmpty (value)) {
					if (footerLayout == null) {
						footerLayout = PangoUtil.CreateLayout (this);
						footerFontDescription = Pango.FontDescription.FromString (fontName);
						footerFontDescription.Size = (int)(footerFontDescription.Size * 0.7f);
						footerLayout.FontDescription = footerFontDescription;
					}
					footerLayout?.SetText (value);
				}
				footerText = value;

				if (Text != null || markup != null) {
					UpdateSize ();
					QueueDraw ();
				}
			}
		}

		public int FooterTextHeight { get; private set; }

		public bool HasFooterText => !string.IsNullOrEmpty (FooterText);

		public void UpdateSize ()
		{
			layout.GetPixelSize (out var w, out var h);

			if (HasFooterText) {
				footerLayout.GetPixelSize (out var w2, out var h2);
				w = Math.Max (w, w2);
				h += h2;
				FooterTextHeight = h2;
			} else {
				FooterTextHeight = 0;
			}

			SetSizeRequest (
				Math.Max (1, Math.Min (w + 3, maxWidth) + 5),
				Math.Max (1, Math.Min (h + 3, maxHeight)) + 5);
		}
		
		protected override void OnDestroyed ()
		{
			layout = layout.Kill ();
			footerLayout = footerLayout.Kill ();
			fontDescription = fontDescription.Kill ();
			footerFontDescription = footerFontDescription.Kill ();

			base.OnDestroyed ();
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var cr = CairoHelper.Create (GdkWindow)) {
				CairoHelper.Region (cr, evnt.Region);
				cr.Clip ();
				cr.Translate (Allocation.X, Allocation.Y);
				Draw (cr);
			}
			return true;
		}

		void Draw (Cairo.Context cr)
		{
			var allocation = Allocation;

			cr.LineWidth = 1;

			cr.Rectangle (0, 0, allocation.Width, allocation.Height);
			cr.SetSourceColor (colorBg);
			cr.Fill ();

			cr.Save ();
			cr.Translate (5, 4);
			cr.SetSourceColor (colorText);
			Pango.CairoHelper.ShowLayout (cr, layout);
			cr.Restore ();

			cr.SetSourceColor (colorBg);
			cr.Rectangle (1.5, 1.5, allocation.Width - 3, allocation.Height - 3);
			cr.Stroke ();

			cr.SetSourceColor (colorFold);
			cr.Rectangle (0.5, 0.5, allocation.Width - 1, allocation.Height - 1);
			cr.Stroke ();

			if (!HasFooterText) {
				return;
			}

			footerLayout.GetPixelSize (out var w, out var h);
			cr.SetSourceColor (colorBg);
			cr.Rectangle (allocation.Width - w - 3, allocation.Height - h, w + 2, h - 1);
			cr.Fill ();

			cr.Save ();
			cr.SetSourceColor (colorFold);
			cr.Translate (allocation.Width - w - 4, allocation.Height - h - 3);
			Pango.CairoHelper.ShowLayout (cr, footerLayout);
			cr.Restore ();
		}
	}
}
