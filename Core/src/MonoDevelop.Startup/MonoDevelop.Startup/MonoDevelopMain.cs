
using System;

using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.Startup
{
	public class SharpDevelopMain
	{
		public static int Main (string[] args)
		{
			Runtime.Initialize (true);
			bool retry = false;
			
			do {
				try {
					return Runtime.ApplicationService.StartApplication ("IDE", args);
				} catch (Exception ex) {
					if (!retry) {
						AddinManager.Registry.Rebuild (new Mono.Addins.ConsoleProgressStatus (true));
						retry = true;
					} else {
						Console.WriteLine (ex);
						Console.WriteLine ("MonoDevelop failed to start. Some of the assemblies required to run MonoDevelop (for example gtk-sharp, gnome-sharp or gtkhtml-sharp) may not be properly installed in the GAC.");
						retry = false;
					}
				} finally {
					Runtime.Shutdown ();
				}
			}
			while (retry);
			return -1;
		}
	}
}
