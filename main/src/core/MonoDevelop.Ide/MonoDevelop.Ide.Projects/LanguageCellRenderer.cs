//
// LanguageCellRenderer.cs
//
// Author:
//       iain <>
//
// Copyright (c) 2017 
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
using Gdk;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.Ide.Projects
{
	public class LanguageCellRenderer : CellRendererText
	{
		Rectangle languageRect;
		int dropdownTriangleWidth = 8;
		int dropdownTriangleHeight = 5;
		const int dropdownTriangleRightHandPadding = 8;
		const int languageRightHandPadding = 4;
		const int languageLeftHandPadding = 9;

		int minLanguageRectWidth;

		public SolutionTemplate Template { get; set; }
		string selectedLanguage;
		public string SelectedLanguage { 
			get {
				return selectedLanguage;
			}
			set {
				selectedLanguage = value;
				Text = value;
			}
		}
		public bool RenderRecentTemplate { get; set; }

		int textWidth = 0;

		public LanguageCellRenderer ()
		{
			minLanguageRectWidth = languageLeftHandPadding +
				dropdownTriangleWidth +
				dropdownTriangleRightHandPadding +
				languageRightHandPadding + 10;
		}

		public Rectangle GetLanguageRect ()
		{
			return languageRect;
		}

		public override void GetSize (Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);

			int languageRectangleWidth = textWidth + languageLeftHandPadding;
			if (TemplateHasMultipleLanguages ()) {
				languageRectangleWidth += languageRightHandPadding + dropdownTriangleWidth + dropdownTriangleRightHandPadding;
			} else {
				languageRectangleWidth += languageLeftHandPadding;
				languageRectangleWidth = Math.Max (languageRectangleWidth, minLanguageRectWidth);
			}

			width = languageRectangleWidth;
		}

		protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
		{
			if (Template == null) {
				return;
			}

			if (!Template.AvailableLanguages.Any () || !IsTemplateRowSelected (widget, flags)) {
				return;
			}

			using (var ctx = CairoHelper.Create (window)) {
				using (var layout = new Pango.Layout (widget.PangoContext)) {
					int textHeight = 0;

					SetMarkup (layout, GetSelectedLanguage ());
					layout.GetPixelSize (out textWidth, out textHeight);

					languageRect = GetLanguageButtonRectangle (window, widget, cell_area, textHeight, textWidth);

					StateType state = StateType.Normal;
					if (!RenderRecentTemplate) {
						RoundBorder (ctx, languageRect.X, languageRect.Y, languageRect.Width, languageRect.Height);
						SetSourceColor (ctx, Styles.NewProjectDialog.TemplateLanguageButtonBackground.ToCairoColor ());
						ctx.Fill ();
					} else {
						state = GetState (widget, flags);
					}

					int tw = TemplateHasMultipleLanguages () ? textWidth + dropdownTriangleWidth + 2 : textWidth;
					int languageTextX = languageRect.X + ((languageRect.Width - tw) / 2);
					int languageTextY = languageRect.Y + (languageRect.Height - textHeight) / 2;

					window.DrawLayout (widget.Style.TextGC (state), languageTextX, languageTextY, layout);

					if (TemplateHasMultipleLanguages ()) {
						int triangleX = languageTextX + textWidth + languageRightHandPadding;
						int triangleY = languageRect.Y + (languageRect.Height - dropdownTriangleHeight) / 2;
						DrawTriangle (ctx, triangleX, triangleY);
					}
				}
			}
		}

		void DrawTriangle (Cairo.Context ctx, int x, int y)
		{
			int width = dropdownTriangleWidth;
			int height = dropdownTriangleHeight;

			SetSourceColor (ctx, Styles.NewProjectDialog.TemplateLanguageButtonTriangle.ToCairoColor ());
			ctx.MoveTo (x, y);
			ctx.LineTo (x + width, y);
			ctx.LineTo (x + (width / 2), y + height);
			ctx.LineTo (x, y);
			ctx.Fill ();
		}

		Rectangle GetLanguageButtonRectangle (Drawable window, Widget widget, Rectangle cell_area, int textHeight, int textWidth)
		{
			int languageRectangleHeight = cell_area.Height - 8;
			int languageRectangleWidth = textWidth + languageLeftHandPadding;
			if (TemplateHasMultipleLanguages ()) {
				languageRectangleWidth += languageRightHandPadding + dropdownTriangleWidth + dropdownTriangleRightHandPadding;
			} else {
				languageRectangleWidth += languageLeftHandPadding;
				languageRectangleWidth = Math.Max (languageRectangleWidth, minLanguageRectWidth);
			}

			var dy = (cell_area.Height - languageRectangleHeight) / 2 - 1;
			var y = cell_area.Y + dy;
			//var x = widget.Allocation.Width - languageRectangleWidth - (int)Xpad;
			var x = cell_area.X;

			return new Rectangle (x, y, languageRectangleWidth, languageRectangleHeight);
		}

		internal bool IsLanguageButtonPressed (EventButton button)
		{
			return !RenderRecentTemplate && languageRect.Contains ((int)button.X, (int)button.Y);
		}

		void SetMarkup (Pango.Layout layout, string text)
		{
			string markup = "<span size='smaller'>" + text + "</span>";
			layout.SetMarkup (markup);
		}

		static bool IsTemplateRowSelected (Widget widget, CellRendererState flags)
		{
			StateType stateType = GetState (widget, flags);
			return (stateType == StateType.Selected) || (stateType == StateType.Active);
		}

		bool TemplateHasMultipleLanguages ()
		{
			return !RenderRecentTemplate && Template != null && Template.AvailableLanguages.Count > 1;
		}

		static StateType GetState (Widget widget, CellRendererState flags)
		{
			StateType stateType = StateType.Normal;
			if ((flags & CellRendererState.Prelit) != 0)
				stateType = StateType.Prelight;
			if ((flags & CellRendererState.Focused) != 0)
				stateType = StateType.Normal;
			if ((flags & CellRendererState.Insensitive) != 0)
				stateType = StateType.Insensitive;
			if ((flags & CellRendererState.Selected) != 0)
				stateType = widget.HasFocus ? StateType.Selected : StateType.Active;
			return stateType;
		}

		string GetSelectedLanguage ()
		{
			if (!Template.AvailableLanguages.Any ())
				return String.Empty;
			else if (RenderRecentTemplate)
				return Template.Language;
			else if (Template.AvailableLanguages.Contains (SelectedLanguage))
				return SelectedLanguage;

			return Template.AvailableLanguages.First ();
		}

		// Taken from MonoDevelop.Components.SearchEntry.
		static void RoundBorder (Cairo.Context ctx, double x, double y, double w, double h)
		{
			double r = h / 2;
			ctx.Arc (x + r, y + r, r, Math.PI / 2, Math.PI + Math.PI / 2);
			ctx.LineTo (x + w - r, y);

			ctx.Arc (x + w - r, y + r, r, Math.PI + Math.PI / 2, Math.PI + Math.PI + Math.PI / 2);

			ctx.LineTo (x + r, y + h);

			ctx.ClosePath ();
		}

		// Taken from Mono.TextEditor.HelperMethods.
		public static void SetSourceColor (Cairo.Context cr, Cairo.Color color)
		{
			cr.SetSourceRGBA (color.R, color.G, color.B, color.A);
		}
	}
}
