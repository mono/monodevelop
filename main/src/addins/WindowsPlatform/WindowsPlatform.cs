using System;
using MonoDevelop.Core.Gui;
using System.Runtime.InteropServices;
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
			//TODO: use the registry to look up the applications for the file extension
			return new DesktopApplication[] { new DesktopApplication () };
		}

		public override DesktopApplication GetDefaultApplication (string mimetype)
		{
			//TODO: use the registry to look up the applications for the file extension
			// Apps are listed in the OpenWithList or OpenWithProgIds branches of the ClassesRoot
			// branch corresponding to the file extension
			// Maybe there's a Win32 API for this?
			return new DesktopApplication ();
		}
		
		protected override string OnGetIconForFile (string filename)
		{
			//TODO: use the registry to look up the icon from the app that handles the icon
			return base.OnGetIconForFile (filename);
		}

		public override string Name {
			get { return "Windows"; }
		}
		
		public override void ShowUrl (string url)
		{
			Process.Start (url);
		}
		
		protected override string OnGetMimeTypeForUri (string uri)
		{
			if (!String.IsNullOrEmpty (uri)) {
				FileInfo file = new FileInfo (uri);
				string ext = Path.GetExtension (file.Name);
				if (String.IsNullOrEmpty (ext))
					return base.OnGetMimeTypeForUri (uri);
				
				RegistryKey key = Registry.ClassesRoot.OpenSubKey (); 
				if (key != null) {
					object val = key.GetValue ("Content Type");
					if (val != null)
						return val.ToString ();
				}
			}
			
			return base.OnGetMimeTypeForUri (uri);
		}
	}
}
