using System;
using MonoDevelop.Core.Gui;
using System.Runtime.InteropServices;
using System.Collections;

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
	}
}
