//
// TemplateCellRendererText.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
//

using System;
using System.Linq;
using Gdk;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.Ide.Projects
{
	class GtkTemplateCellRenderer : CellRendererText
	{
		Rectangle languageRect;
		int dropdownTriangleWidth = 8;
		int dropdownTriangleHeight = 5;
		const int dropdownTriangleRightHandPadding = 8;
		const int languageRightHandPadding = 4;
		const int languageLeftHandPadding = 9;
		const int iconTextPadding = 9;
		int groupTemplateHeadingTotalYPadding = 24;
		int recentTemplateHeadingTotalYPadding = 30;
		const int groupTemplateHeadingYOffset = 4;
		const int categoryTextPaddingX = 4;

		int minLanguageRectWidth;

		public SolutionTemplate Template { get; set; }
		public string SelectedLanguage { get; set; }
		public Xwt.Drawing.Image TemplateIcon { get; set; }
		public string TemplateCategory { get; set; }
		public bool RenderRecentTemplate { get; set; }

		public GtkTemplateCellRenderer ()
		{
			minLanguageRectWidth = languageLeftHandPadding +
				dropdownTriangleWidth +
				dropdownTriangleRightHandPadding +
				languageRightHandPadding + 10;

			if (IsYosemiteOrHigher ()) {
				groupTemplateHeadingTotalYPadding -= 1;
				recentTemplateHeadingTotalYPadding -= 1;
			}
		}

		static bool IsYosemiteOrHigher ()
		{
			return Platform.IsMac && (Platform.OSVersion >= MacSystemInformation.Yosemite);
		}

		public bool IsLanguageButtonPressed (EventButton button)
		{
			return !RenderRecentTemplate && languageRect.Contains ((int)button.X, (int)button.Y);
		}

		public Rectangle GetLanguageRect ()
		{
			return languageRect;
		}

		public override void GetSize (Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
			if (TemplateIcon != null) {
				height = (int)TemplateIcon.Height + ((int)Ypad * 2);
			} else {
				height += RenderRecentTemplate ? recentTemplateHeadingTotalYPadding : groupTemplateHeadingTotalYPadding;
			}
		}

		protected override void Render (Drawable window, Widget widget, Rectangle background_area, Rectangle cell_area, Rectangle expose_area, CellRendererState flags)
		{
			if (Template == null) {
				DrawTemplateCategoryText (window, widget, cell_area, flags);
				return;
			}

			using (var ctx = CairoHelper.Create (window)) {
				using (var layout = new Pango.Layout (widget.PangoContext)) {

					Rectangle iconRect = DrawIcon (ctx, widget, cell_area, flags);

					if (!RenderRecentTemplate && (!Template.AvailableLanguages.Any () || !IsTemplateRowSelected (widget, flags))) {
						DrawTemplateNameText (window, widget, cell_area, iconRect, Rectangle.Zero, flags);
						return;
					}

					int textHeight = 0;
					int textWidth = 0;

					SetMarkup (layout, GetSelectedLanguage ());
					layout.GetPixelSize (out textWidth, out textHeight);

					languageRect = GetLanguageButtonRectangle (window, widget, cell_area, textHeight, textWidth);

					DrawTemplateNameText (window, widget, cell_area, iconRect, languageRect, flags);
					if (RenderRecentTemplate)
						DrawCategoryText (ctx, widget, cell_area, iconRect, languageRect, flags);


					StateType state = StateType.Normal;
					if (!RenderRecentTemplate) {
						RoundBorder (ctx, languageRect.X, languageRect.Y, languageRect.Width, languageRect.Height);
						SetSourceColor (ctx, Styles.NewProjectDialog.TemplateLanguageButtonBackground.ToCairoColor ());
						ctx.Fill ();
					} else
						state = GetState (widget, flags);

					int languageTextX = languageRect.X + languageLeftHandPadding;
					if (!TemplateHasMultipleLanguages ()) {
						languageTextX = languageRect.X + (languageRect.Width - textWidth) / 2;
					}
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

		void DrawTemplateCategoryText (Drawable window, Widget widget, Rectangle cell_area, CellRendererState flags)
		{
			StateType state = GetState (widget, flags);

			using (var layout = new Pango.Layout (widget.PangoContext)) {

				layout.Ellipsize = Pango.EllipsizeMode.End;
				int textPixelWidth = widget.Allocation.Width - ((int)Xpad * 2);
				layout.Width = (int)(textPixelWidth * Pango.Scale.PangoScale);

				layout.SetMarkup (TemplateCategory);

				int w, h;
				layout.GetPixelSize (out w, out h);

				int textX = cell_area.X + (int)Xpad + categoryTextPaddingX;
				int textY = cell_area.Y + (cell_area.Height - h) / 2 + groupTemplateHeadingYOffset;
				window.DrawLayout (widget.Style.TextGC (state), textX, textY, layout);
			}
		}

		Rectangle DrawIcon (Cairo.Context ctx, Widget widget, Rectangle cell_area, CellRendererState flags)
		{
			var iconRect = new Rectangle (cell_area.X + (int)Xpad, cell_area.Y + (int)Ypad, (int)TemplateIcon.Width, (int)TemplateIcon.Height);

			var img = TemplateIcon;
			if ((flags & Gtk.CellRendererState.Selected) != 0)
				img = img.WithStyles ("sel");
			ctx.DrawImage (widget, img, iconRect.X, iconRect.Y);

			return iconRect;
		}

		void DrawTemplateNameText (Drawable window, Widget widget, Rectangle cell_area, Rectangle iconRect, Rectangle languageRect, CellRendererState flags)
		{
			StateType state = GetState (widget, flags);

			using (var layout = new Pango.Layout (widget.PangoContext)) {

				layout.Ellipsize = Pango.EllipsizeMode.End;
				int textPixelWidth = widget.Allocation.Width - ((int)Xpad * 2) - iconRect.Width - iconTextPadding - languageRect.Width;
				layout.Width = (int)(textPixelWidth * Pango.Scale.PangoScale);

				layout.SetMarkup (GLib.Markup.EscapeText (Template.Name));

				int w, h;
				layout.GetPixelSize (out w, out h);
				int textY = cell_area.Y + (RenderRecentTemplate ? (2) : (cell_area.Height - h) / 2);

				window.DrawLayout (widget.Style.TextGC (state), iconRect.Right + iconTextPadding, textY, layout);
			}
		}

		void DrawCategoryText (Cairo.Context ctx, Widget widget, Rectangle cell_area, Rectangle iconRect, Rectangle languageRect, CellRendererState flags)
		{
			StateType state = GetState (widget, flags);
			var isSelected = state == StateType.Selected || state == StateType.Active;

			using (var layout = new Pango.Layout (widget.PangoContext)) {

				layout.Ellipsize = Pango.EllipsizeMode.End;
				int textPixelWidth = widget.Allocation.Width - ((int)Xpad * 2) - iconRect.Width - iconTextPadding - languageRect.Width;
				layout.Width = (int)(textPixelWidth * Pango.Scale.PangoScale);
				layout.FontDescription = Fonts.FontExtensions.CopyModified (widget.Style.FontDesc, -1);

				layout.SetMarkup (GLib.Markup.EscapeText (TemplateCategory));

				int w, h;
				layout.GetPixelSize (out w, out h);
				int textY = cell_area.Y + ((cell_area.Height - h) - 2);

				ctx.MoveTo (iconRect.Right + iconTextPadding, textY);
				ctx.SetSourceColor ((isSelected ? Styles.BaseSelectionTextColor : Styles.DimTextColor).ToCairoColor ());
				ctx.ShowLayout (layout);
			}
		}

		static bool IsTemplateRowSelected (Widget widget, CellRendererState flags)
		{
			StateType stateType = GetState (widget, flags);
			return (stateType == StateType.Selected) || (stateType == StateType.Active);
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

		void SetMarkup (Pango.Layout layout, string text)
		{
			string markup = "<span size='smaller'>" + text + "</span>";
			layout.SetMarkup (markup);
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
			var x = widget.Allocation.Width - languageRectangleWidth - (int)Xpad;

			return new Rectangle (x, y, languageRectangleWidth, languageRectangleHeight);
		}

		bool TemplateHasMultipleLanguages ()
		{
			return !RenderRecentTemplate && Template.AvailableLanguages.Count > 1;
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

