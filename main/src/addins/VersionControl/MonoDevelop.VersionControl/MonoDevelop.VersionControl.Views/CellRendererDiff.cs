
using System;
using System.Collections;
using Gtk;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl.Views
{
	class CellRendererDiff: Gtk.CellRendererText, IDisposable
	{
		Pango.Layout layout;
		Pango.FontDescription font;
		bool diffMode;
		int width, height, lineHeight;
		string[] lines;

		public CellRendererDiff()
		{
			font = Pango.FontDescription.FromString (IdeApp.Services.PlatformService.DefaultMonospaceFont);
		}
		
		void DisposeLayout ()
		{
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
		}
		#region IDisposable implementation
		bool isDisposed = false;
		public override void Dispose ()
		{
			isDisposed = true;
			DisposeLayout ();
			if (font != null) {
				font.Dispose ();
				font = null;
			}
			base.Dispose ();
		}
		#endregion
		
		public void Reset ()
		{
		}
		
		public void InitCell (Widget container, bool diffMode, string text, string path)
		{
			if (isDisposed)
				return;
			this.diffMode = diffMode;
			
			if (diffMode) {
				if (text.Length > 0) {
					lines = text.Split ('\n');
					int maxlen = -1;
					int maxlin = -1;
					for (int n=0; n<lines.Length; n++) {
						if (lines [n].Length > maxlen) {
							maxlen = lines [n].Length;
							maxlin = n;
						}
					}
					DisposeLayout ();
					layout = CreateLayout (container, lines [maxlin]);
					layout.GetPixelSize (out width, out lineHeight);
					height = lineHeight * lines.Length;
				}
				else
					width = height = 0;
			}
			else {
				DisposeLayout ();
				layout = CreateLayout (container, text);
				layout.GetPixelSize (out width, out height);
			}
		}
		
		Pango.Layout CreateLayout (Widget container, string text)
		{
			Pango.Layout layout = new Pango.Layout (container.PangoContext);
			layout.SingleParagraphMode = false;
			if (diffMode) {
				layout.FontDescription = font;
				layout.SetText (text);
			}
			else
				layout.SetMarkup (text);
			return layout;
		}

		protected override void Render (Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
		{
			if (isDisposed)
				return;
			if (diffMode) {
				int w, maxy;
				window.GetSize (out w, out maxy);
				
				int recty = cell_area.Y;
				int recth = cell_area.Height - 1;
				if (recty < 0) {
					recth += recty + 1;
					recty = -1;
				}
				if (recth > maxy + 2)
					recth = maxy + 2;
				
				window.DrawRectangle (widget.Style.BaseGC (Gtk.StateType.Normal), true, cell_area.X, recty, cell_area.Width - 1, recth);

				Gdk.GC normalGC = widget.Style.TextGC (StateType.Normal);
				Gdk.GC removedGC = new Gdk.GC (window);
				removedGC.Copy (normalGC);
				removedGC.RgbFgColor = new Color (255, 0, 0);
				Gdk.GC addedGC = new Gdk.GC (window);
				addedGC.Copy (normalGC);
				addedGC.RgbFgColor = new Color (0, 0, 255);
				Gdk.GC infoGC = new Gdk.GC (window);
				infoGC.Copy (normalGC);
				infoGC.RgbFgColor = new Color (0xa5, 0x2a, 0x2a);
				
				int y = cell_area.Y + 2;
				
				for (int n=0; n<lines.Length; n++, y += lineHeight) {
					if (y + lineHeight < 0)
						continue;
					if (y > maxy)
						break;
					string line = lines [n];
					if (line.Length == 0)
						continue;
					
					Gdk.GC gc;
					switch (line[0]) {
					case '-': gc = removedGC; break;
					case '+': gc = addedGC; break;
					case '@': gc = infoGC; break;
					default: gc = normalGC; break;
					}
					
					layout.SetText (line);
					window.DrawLayout (gc, cell_area.X + 2, y, layout);
				}
				window.DrawRectangle (widget.Style.DarkGC (Gtk.StateType.Prelight), false, cell_area.X, recty, cell_area.Width - 1, recth);
			} else {
				int y = cell_area.Y + (cell_area.Height - height)/2;
				window.DrawLayout (widget.Style.TextGC (GetState(flags)), cell_area.X, y, layout);
			}
		}
		
		public override void GetSize (Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int c_width, out int c_height)
		{
			x_offset = y_offset = 0;
			c_width = width;
			c_height = height;
			
			if (diffMode) {
				// Add some spacing for the margin
				c_width += 4;
				c_height += 4;
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
