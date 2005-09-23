using System;
using System.Collections;

using Gtk;

namespace MonoDevelop.Gui.Utils
{
	public sealed class FileIconLoader
	{
		static Gdk.Pixbuf defaultIcon;
		static Gnome.ThumbnailFactory thumbnailFactory;
		static Hashtable iconHash;

		static FileIconLoader ()
		{
			thumbnailFactory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Normal);
			iconHash = new Hashtable ();
		}

		private FileIconLoader ()
		{
		}

		public static Gdk.Pixbuf DefaultIcon {
			get {
				if (defaultIcon == null)
					defaultIcon = new Gdk.Pixbuf (typeof(FileIconLoader).Assembly, "gnome-fs-regular.png");
				return defaultIcon;
			}
		}

		// FIXME: is there a GTK replacement for Gnome.Icon.LookupSync?
		public static Gdk.Pixbuf GetPixbufForFile (string filename, int size)
		{
			Gnome.IconLookupResultFlags result;
			string icon;
			try {
				if (filename == "Documentation")
					icon = "gnome-fs-regular";
				else
					icon = Gnome.Icon.LookupSync (IconTheme.Default, thumbnailFactory, filename, "", Gnome.IconLookupFlags.None, out result);
			} catch {
				icon = "gnome-fs-regular";
			}
			return GetPixbufForType (icon, size);
		}

		public static Gdk.Pixbuf GetPixbufForType (string type, int size)
		{
			// FIXME: is caching these really worth it?
			// we have to cache them in both type and size
			Gdk.Pixbuf bf = (Gdk.Pixbuf) iconHash [type+size];
			if (bf == null) {
				try {
					bf = IconTheme.Default.LoadIcon (type, size, (IconLookupFlags) 0);
				}
				catch {
					bf = DefaultIcon;
					if (bf.Height > size)
						bf = bf.ScaleSimple (size, size, Gdk.InterpType.Bilinear);
				}
				iconHash [type+size] = bf;
			}
			return bf;
		}
	}
}
