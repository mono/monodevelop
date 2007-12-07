// FileIconLoader.cs
//
// Author:
//   Todd Berman  <tberman@off.net>
//
// Copyright (c) 2004 Todd Berman  <tberman@off.net>
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
//

using System;
using System.Collections;
using System.IO;

using Gtk;

namespace MonoDevelop.Core.Gui.Utils
{
	public sealed class FileIconLoader
	{
		static Gdk.Pixbuf defaultIcon;
		static Hashtable iconHash;

		static FileIconLoader ()
		{
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
				foreach (char c in filename) {
					// FIXME: This is a temporary workaround. In some systems, files with
					// accented characters make LookupSync crash. Still trying to find out why.
					if ((int)c < 32 || (int)c > 127)
						return GetPixbufForType ("gnome-fs-regular", size);
				}
				filename = filename.Replace ("%", "%25");
				filename = filename.Replace ("#", "%23");
				filename = filename.Replace ("?", "%3F");
				string icon = null;
				try {
					icon = Runtime.PlatformService.IconForFile (filename);
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
