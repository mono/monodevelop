//
// GnomePlatform.cs
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
using System.Runtime.InteropServices;

using MonoDevelop.Core.Gui;
using Gnome;
using Gnome.Vfs;

namespace MonoDevelop.Platform
{
	public class GnomePlatform : PlatformService
	{
		Gnome.ThumbnailFactory thumbnailFactory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Normal);

		static GnomePlatform () {
			Gnome.Vfs.Vfs.Initialize ();
		}

		public override DesktopApplication GetDefaultApplication (string mimeType) {
			MimeApplication app = Mime.GetDefaultApplication (mimeType);
			if (app != null)
				return (DesktopApplication) Marshal.PtrToStructure (app.Handle, typeof(DesktopApplication));
			else
				return new DesktopApplication ();
		}
		
		public override DesktopApplication [] GetAllApplications (string mimeType) {
			MimeApplication[] apps = Mime.GetAllApplications (mimeType);
			ArrayList list = new ArrayList ();
			foreach (MimeApplication app in apps) {
				DesktopApplication dap = (DesktopApplication) Marshal.PtrToStructure (app.Handle, typeof(DesktopApplication));
				list.Add (dap);
			}
			return (DesktopApplication[]) list.ToArray (typeof(DesktopApplication));
		}

		protected override string OnGetMimeTypeDescription (string mt)
		{
			return Mime.GetDescription (mt);
		}

		protected override string OnGetMimeTypeForUri (string uri)
		{
			return uri != null ? Gnome.Vfs.MimeType.GetMimeTypeForUri (ConvertFileNameToVFS (uri)) : null;
		}
		
		protected override bool OnGetMimeTypeIsText (string mimeType)
		{
			// If gedit can open the file, this editor also can do it
			foreach (DesktopApplication app in GetAllApplications (mimeType))
				if (app.Command == "gedit")
					return true;
			return base.OnGetMimeTypeIsText (mimeType);
		}


		public override void ShowUrl (string url)
		{
			Gnome.Url.Show (url);
		}
		
		public override string DefaultMonospaceFont {
			get { return (string) (new GConf.Client ().Get ("/desktop/gnome/interface/monospace_font_name")); }
		}
		
		public override string Name {
			get { return "Gnome"; }
		}

		protected override string OnGetIconForFile (string filename)
		{
			if (filename == "Documentation") {
				return "gnome-fs-regular";
			} 
			if (System.IO.Directory.Exists (filename)) {
				return "gnome-fs-directory";
			} else if (System.IO.File.Exists (filename)) {
				filename = EscapeFileName (filename);
				if (filename == null)
					return "gnome-fs-regular";
				
				string icon = null;
				Gnome.IconLookupResultFlags result;
				try {
					icon = Gnome.Icon.LookupSync (IconTheme.Default, thumbnailFactory, filename, null, Gnome.IconLookupFlags.None, out result);
				} catch {}
				if (icon != null && icon.Length > 0)
					return icon;
			}			
			return "gnome-fs-regular";
			
		}
		
		protected override Gdk.Pixbuf OnGetPixbufForFile (string filename, Gtk.IconSize size)
		{
			string icon = OnGetIconForFile (filename);
			return GetPixbufForType (icon, size);
		}
		
		string EscapeFileName (string filename)
		{
			foreach (char c in filename) {
				// FIXME: This is a temporary workaround. In some systems, files with
				// accented characters make LookupSync crash. Still trying to find out why.
				if ((int)c < 32 || (int)c > 127)
					return null;
			}
			return ConvertFileNameToVFS (filename);
		}
		
		static string ConvertFileNameToVFS (string fileName)
		{
			string result = fileName;
			result = result.Replace ("%", "%25");
			result = result.Replace ("#", "%23");
			result = result.Replace ("?", "%3F");
			return result;
		}		
		
		Gdk.Pixbuf GetGnomeIcon (string name, Gtk.IconSize size)
		{
			try {
				return Gtk.IconTheme.Default.LoadIcon (name, 16, (Gtk.IconLookupFlags) 0);
			}
			catch {
				return null;
			}
		}
	}
}
