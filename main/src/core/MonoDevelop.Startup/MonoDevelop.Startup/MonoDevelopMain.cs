
using System;
using System.IO;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using Mono.Addins;

namespace MonoDevelop.Startup
{
	public class MonoDevelopMain
	{
		public static int Main (string[] args)
		{
			bool retry = false;

			EnableFileLogging ();
			
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

			if (logFile != null)
				logFile.Close ();

			return -1;
		}

		static StreamWriter logFile;

		static void EnableFileLogging ( )
		{
			if (Path.DirectorySeparatorChar != '\\')
				return;

			// On Windows log all output to a log file

			string configPath = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			configPath = Path.Combine (configPath, "MonoDevelop");
			if (!Directory.Exists (configPath))
				Directory.CreateDirectory (configPath);

			string file = Path.Combine (configPath, "log.txt");
			try {
				logFile = new StreamWriter (file);
				logFile.AutoFlush = true;

				LogTextWriter tw = new LogTextWriter ();
				tw.ChainWriter (logFile);
				tw.ChainWriter (Console.Out);
				Console.SetOut (tw);

				tw = new LogTextWriter ();
				tw.ChainWriter (logFile);
				tw.ChainWriter (Console.Error);
				Console.SetError (tw);
			}
			catch {
			}
		}
	}
}
