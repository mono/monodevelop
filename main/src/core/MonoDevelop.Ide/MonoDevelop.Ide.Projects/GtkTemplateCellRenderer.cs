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
		const int iconTextPadding = 9;
		int groupTemplateHeadingTotalYPadding = 24;
		int recentTemplateHeadingTotalYPadding = 30;
		const int groupTemplateHeadingYOffset = 4;
		const int categoryTextPaddingX = 4;

		public SolutionTemplate Template { get; set; }
		public string SelectedLanguage { get; set; }
		public Xwt.Drawing.Image TemplateIcon { get; set; }
		public string TemplateCategory { get; set; }
		public bool RenderRecentTemplate { get; set; }

		public GtkTemplateCellRenderer ()
		{
			if (IsYosemiteOrHigher ()) {
				groupTemplateHeadingTotalYPadding -= 1;
				recentTemplateHeadingTotalYPadding -= 1;
			}
		}

		static bool IsYosemiteOrHigher ()
		{
			return Platform.IsMac && (Platform.OSVersion >= MacSystemInformation.Yosemite);
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

					DrawTemplateNameText (window, widget, cell_area, iconRect, flags);
					if (RenderRecentTemplate)
						DrawCategoryText (ctx, widget, cell_area, iconRect, flags);
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

		void DrawTemplateNameText (Drawable window, Widget widget, Rectangle cell_area, Rectangle iconRect, CellRendererState flags)
		{
			StateType state = GetState (widget, flags);

			using (var layout = new Pango.Layout (widget.PangoContext)) {

				layout.Ellipsize = Pango.EllipsizeMode.End;
				int textPixelWidth = cell_area.Width - ((int)Xpad * 2) - iconRect.Width - iconTextPadding;
				layout.Width = (int)(textPixelWidth * Pango.Scale.PangoScale);

				layout.SetMarkup (GLib.Markup.EscapeText (Template.Name));

				int w, h;
				layout.GetPixelSize (out w, out h);
				int textY = cell_area.Y + (RenderRecentTemplate ? (2) : (cell_area.Height - h) / 2);

				window.DrawLayout (widget.Style.TextGC (state), iconRect.Right + iconTextPadding, textY, layout);
			}
		}

		void DrawCategoryText (Cairo.Context ctx, Widget widget, Rectangle cell_area, Rectangle iconRect, CellRendererState flags)
		{
			StateType state = GetState (widget, flags);
			var isSelected = state == StateType.Selected || state == StateType.Active;

			using (var layout = new Pango.Layout (widget.PangoContext)) {

				layout.Ellipsize = Pango.EllipsizeMode.End;
				int textPixelWidth = cell_area.Width - ((int)Xpad * 2) - iconRect.Width - iconTextPadding;
				layout.Width = (int)(textPixelWidth * Pango.Scale.PangoScale);
				layout.FontDescription = Fonts.FontExtensions.CopyModified (widget.Style.FontDesc, -1);

				layout.SetMarkup (GLib.Markup.EscapeText (TemplateCategory));

				int w, h;
				layout.GetPixelSize (out w, out h);
				int textY = cell_area.Y + ((cell_area.Height - h) - 2);

				ctx.MoveTo (iconRect.Right + iconTextPadding, textY);
				ctx.SetSourceColor ((isSelected ? Styles.BaseSelectionTextColor : Styles.SecondaryTextColor).ToCairoColor ());
				ctx.ShowLayout (layout);
			}
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
	}
}

