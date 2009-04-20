
using System;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.Startup
{
	public class MonoDevelopMain
	{
		public static int Main (string[] args)
		{
			bool retry = false;
			
			do {
				try {
					Runtime.SetProcessName ("monodevelop");
					IdeStartup app = new IdeStartup ();
					return app.Run (args);
				} catch (Exception ex) {
					if (!retry) {
						LoggingService.LogWarning ("MonoDevelop failed to start. Rebuilding addins registry.");
						AddinManager.Registry.Rebuild (new Mono.Addins.ConsoleProgressStatus (true));
						LoggingService.LogInfo ("Addin registry rebuilt. Restarting MonoDevelop.");
						retry = true;
					} else {
						LoggingService.LogFatalError ("MonoDevelop failed to start. Some of the assemblies required to run MonoDevelop (for example gtk-sharp, gnome-sharp or gtkhtml-sharp) may not be properly installed in the GAC.", ex);
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
