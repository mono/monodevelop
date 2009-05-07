using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using MonoDevelop.Core.Gui;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Win32;

namespace MonoDevelop.Platform
{
	public class WindowsPlatform : PlatformService
	{
		public override string DefaultMonospaceFont {
			get {
				// Vista has the very beautiful Consolas
				if (Environment.OSVersion.Version.Major >= 6)
					return "Consolas 10";
					
				return "Courier New 10";
			}
		}

		public override DesktopApplication[] GetAllApplications (string mimetype)
		{
			return new DesktopApplication[] { new DesktopApplication () };
		}

		public override DesktopApplication GetDefaultApplication (string mimetype)
		{
			return new DesktopApplication ();
		}

		public override string Name {
			get { return "Windows"; }
		}
		
		public override object GetFileAttributes (string fileName)
		{
			return null;
		}
		
		public override void SetFileAttributes (string fileName, object attributes)
		{
		}
		
		protected override string OnGetMimeTypeForUri (string uri)
		{
			string ext = Path.GetExtension (uri).ToLower ();
			
			RegistryKey typeKey = Registry.ClassesRoot.OpenSubKey (ext, false);
			if (typeKey == null)
				return base.OnGetMimeTypeForUri (uri);
			try {
				string type = (string) typeKey.GetValue ("Content Type");
				if (type == null) {
					string ptype = (string) typeKey.GetValue ("PerceivedType");
					if (ptype == "text")
						type = "text/plain";
				}
				return type ?? base.OnGetMimeTypeForUri (uri);
			}
			finally {
				typeKey.Close ();
			}
		}
		
		protected override Gdk.Pixbuf OnGetPixbufForType (string type, Gtk.IconSize size)
		{
			return base.OnGetPixbufForType (type, size);
		}
		
		Dictionary<string, Gdk.Pixbuf> icons = new Dictionary<string, Gdk.Pixbuf> ();
		
		protected override Gdk.Pixbuf OnGetPixbufForFile (string filename, Gtk.IconSize size)
		{
			SHFILEINFO shinfo = new SHFILEINFO();
			Win32.SHGetFileInfo (filename, 0, ref shinfo, (uint) Marshal.SizeOf (shinfo), Win32.SHGFI_ICON | Win32.SHGFI_SMALLICON | Win32.SHGFI_ICONLOCATION | Win32.SHGFI_TYPENAME);
			if (shinfo.iIcon == IntPtr.Zero)
				return null;
			string key = shinfo.iIcon.ToString () + " - " + shinfo.szDisplayName;
			Gdk.Pixbuf pix;
			if (!icons.TryGetValue (key, out pix)) {
				System.Drawing.Icon icon = System.Drawing.Icon.FromHandle (shinfo.hIcon);
				pix = CreateFromResource (icon.ToBitmap ());
				icons[key] = pix;
			}
			return pix;
		}
		
		public Gdk.Pixbuf CreateFromResource (Bitmap bitmap)
		{
			MemoryStream ms = new MemoryStream ();
			bitmap.Save (ms, ImageFormat.Png);
			ms.Position = 0;
			return new Gdk.Pixbuf (ms);
		}
	}
}
