using System;
using System.Collections;
using System.IO;

using Gtk;

namespace MonoDevelop.Core.Gui.Utils
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
			if (filename == "Documentation") {
				return GetPixbufForType ("gnome-fs-regular", size);
			} else if (Directory.Exists (filename)) {
				return GetPixbufForType ("gnome-fs-directory", size);
			} else if (File.Exists (filename)) {
				filename = filename.Replace ("%", "%25");
				filename = filename.Replace ("#", "%23");
				filename = filename.Replace ("?", "%3F");
				string icon = null;
				try {
					Gnome.IconLookupResultFlags result;
					icon = Gnome.Icon.LookupSync (IconTheme.Default, thumbnailFactory, filename, null, Gnome.IconLookupFlags.None, out result);
				} catch {}
				if (icon == null || icon.Length == 0)
					return GetPixbufForType ("gnome-fs-regular", size);
				else
					return GetPixbufForType (icon, size);
			}

			return GetPixbufForType ("gnome-fs-regular", size);
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
