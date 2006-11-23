
using System;
using Gtk;
using Gdk;

namespace MonoDevelop.VersionControl.Views
{
	class CellRendererDiff: Gtk.CellRendererText
	{
		Pango.Layout layout;
		Pango.FontDescription font;
		bool diffMode;
			
		public CellRendererDiff()
		{
			font = Pango.FontDescription.FromString ((string) new GConf.Client ().Get ("/desktop/gnome/interface/monospace_font_name"));
		}
		
		public void InitCell (Widget container, bool diffMode, string text)
		{
			this.diffMode = diffMode; 
			
			layout = new Pango.Layout (container.PangoContext);
			layout.SingleParagraphMode = false;
			if (diffMode)
				layout.FontDescription = font;
			layout.SetMarkup (text);
		}

		protected override void Render (Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
		{
			int width, height;
			layout.GetPixelSize (out width, out height);
			if (diffMode) {
				window.DrawRectangle (widget.Style.BaseGC (Gtk.StateType.Normal), true, cell_area.X, cell_area.Y, cell_area.Width - 1, cell_area.Height - 1);
				window.DrawLayout (widget.Style.TextGC (StateType.Normal), cell_area.X + 2, cell_area.Y + 2, layout);
				window.DrawRectangle (widget.Style.DarkGC (Gtk.StateType.Prelight), false, cell_area.X, cell_area.Y, cell_area.Width - 1, cell_area.Height - 1);
			} else {
				int y = cell_area.Y + (cell_area.Height - height)/2;
				window.DrawLayout (widget.Style.TextGC (GetState(flags)), cell_area.X, y, layout);
			}
		}
		
		public override void GetSize (Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			layout.GetPixelSize (out width, out height);
			x_offset = y_offset = 0;
			if (diffMode) {
				// Add some spacing for the margin
				width += 4;
				height += 4;
			}
		}
		
		StateType GetState (CellRendererState flags)
		{
			if ((flags & CellRendererState.Selected) != 0)
				return StateType.Selected;
			else
				return StateType.Normal;
		}
	}
}
