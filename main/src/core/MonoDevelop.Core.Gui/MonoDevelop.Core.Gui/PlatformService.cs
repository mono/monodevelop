//
// PlatformService.cs
//
// Author:
//   Geoff Norton  <gnorton@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Core.Gui
{
	public abstract class PlatformService : AbstractService
	{
		Gdk.Pixbuf defaultIcon;
		Hashtable iconHash = new Hashtable ();
		
		public abstract DesktopApplication GetDefaultApplication (string mimetype);
		public abstract DesktopApplication [] GetAllApplications (string mimetype);
		public abstract string GetMimeTypeDescription (string mt);
		public abstract string DefaultMonospaceFont { get; }
		public abstract string Name { get; }
		public abstract string GetIconForFile (string filename);
		
		public virtual string GetMimeTypeForUri (string uri) {
			FileInfo file = new FileInfo (uri);

			switch (file.Extension) {
			case ".cs":
				return "text/x-csharp";
			case ".boo":
				return "text/x-boo";
			case ".vb":
				return "text/x-vb";
			case ".xml":
				return "application/xml";
			case ".xaml":
				return "application/xaml+xml";
			}
			
			switch (file.Name.ToLower ()) {
				case "changelog":
					return "text/x-changelog";
			}
			return "text/plain";
		}

		public virtual void ShowUrl (string url)
		{
			Process.Start (url);
		}

		public virtual Gdk.Pixbuf DefaultFileIcon {
			get {
				if (defaultIcon == null)
					defaultIcon = new Gdk.Pixbuf (typeof(PlatformService).Assembly, "gnome-fs-regular.png");
				return defaultIcon;
			}
		}
		
		public virtual Gdk.Pixbuf GetPixbufForFile (string filename, int size)
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
					icon = GetIconForFile (filename);
				} catch {}
				if (icon == null || icon.Length == 0)
					return GetPixbufForType ("gnome-fs-regular", size);
				else
					return GetPixbufForType (icon, size);
			}

			return GetPixbufForType ("gnome-fs-regular", size);
		}
		
		public virtual Gdk.Pixbuf GetPixbufForType (string type, int size)
		{
			Gdk.Pixbuf bf = (Gdk.Pixbuf) iconHash [type+size];
			if (bf == null) {
				try {
					bf = Gtk.IconTheme.Default.LoadIcon (type, size, (Gtk.IconLookupFlags) 0);
				}
				catch {
					bf = DefaultFileIcon;
					if (bf.Height > size)
						bf = bf.ScaleSimple (size, size, Gdk.InterpType.Bilinear);
				}
				iconHash [type+size] = bf;
			}
			return bf;
		}
	}
}
