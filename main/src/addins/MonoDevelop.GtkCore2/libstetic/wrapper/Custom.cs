using System;
using Gtk;

namespace Stetic 
{
	public class Custom : Gtk.DrawingArea 
	{
		public Custom () {}

		public Custom (IntPtr raw) : base (raw) {}

		// from glade
		static private string[] custom_bg_xpm = {
			"8 8 4 1",
			" 	c None",
			".	c #BBBBBB",
			"+	c #D6D6D6",
			"@	c #6B5EFF",
			".+..+...",
			"+..@@@..",
			"..@...++",
			"..@...++",
			"+.@..+..",
			".++@@@..",
			"..++....",
			"..++...."
		};

		Gdk.Pixmap pixmap;
		
		protected override void OnRealized ()
		{
			base.OnRealized ();

			Gdk.Pixmap mask;
			pixmap = Gdk.Pixmap.CreateFromXpmD (GdkWindow, out mask, new Gdk.Color (99, 99, 99), custom_bg_xpm);
		}

		string creationFunction, string1, string2;
		int int1, int2;

		public string CreationFunction {
			get {
				return creationFunction;
			}
			set {
				creationFunction = value;
			}
		}

		public string LastModificationTime {
			get {
				return null;
			}
			set {
				;
			}
		}

		public string String1 {
			get {
				return string1;
			}
			set {
				string1 = value;
			}
		}

		public string String2 {
			get {
				return string2;
			}
			set {
				string2 = value;
			}
		}

		public int Int1 {
			get {
				return int1;
			}
			set {
				int1 = value;
			}
		}

		public int Int2 {
			get {
				return int2;
			}
			set {
				int2 = value;
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evt)
		{
			if (!IsDrawable)
				return false;

			int width, height;
			GdkWindow.GetSize (out width, out height);

			Gdk.GC light, dark;
			light = Style.LightGC (StateType.Normal);
			dark = Style.DarkGC (StateType.Normal);

			// Looks like GdkWindow.SetBackPixmap doesn't work very well,
			// so draw the pixmap manually.
			light.Fill = Gdk.Fill.Tiled;
			light.Tile = pixmap;
			GdkWindow.DrawRectangle (light, true, 0, 0, width, height);
			light.Fill = Gdk.Fill.Solid;

			GdkWindow.DrawLine (light, 0, 0, width - 1, 0);
			GdkWindow.DrawLine (light, 0, 0, 0, height - 1);
			GdkWindow.DrawLine (dark, 0, height - 1, width - 1, height - 1);
			GdkWindow.DrawLine (dark, width - 1, 0, width - 1, height - 1);

			return base.OnExposeEvent (evt);
		}
	}
}
