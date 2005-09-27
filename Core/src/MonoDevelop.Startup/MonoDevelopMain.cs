// <file>
//	 <copyright see="prj:///doc/copyright.txt"/>
//	 <license see="prj:///doc/license.txt"/>
//	 <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//	 <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Mono.Posix;

using MonoDevelop.Base;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Gui;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Startup
{
	public class SharpDevelopMain
	{
		static string[] commandLineArgs = null;
		static Socket   listen_socket   = null;
		
		public static string[] CommandLineArgs {
			get {
				return commandLineArgs;
			}
		}

		public static int Main (string[] args)
		{
			MonoDevelopOptions options = new MonoDevelopOptions ();
			options.ProcessArgs (args);
			string[] remainingArgs = options.RemainingArguments;
			
			string socket_filename = "/tmp/md-" + Environment.GetEnvironmentVariable ("USER") + "-socket";
			listen_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
			EndPoint ep = new UnixEndPoint (socket_filename);
			
			// Connect to existing monodevelop and pass filename(s) and exit
			if (remainingArgs.Length > 0 && File.Exists (socket_filename)) {
				try {
					listen_socket.Connect (ep);
					listen_socket.Send (Encoding.UTF8.GetBytes (String.Join ("\n", remainingArgs)));
					return 0;
				} catch {}
			}
			
			string name    = Assembly.GetEntryAssembly ().GetName ().Name;
			string version = Assembly.GetEntryAssembly ().GetName ().Version.Major + "." + Assembly.GetEntryAssembly ().GetName ().Version.Minor;
			
			if (Assembly.GetEntryAssembly ().GetName ().Version.Build != 0)
				version += "." + Assembly.GetEntryAssembly ().GetName ().Version.Build;
			if (Assembly.GetEntryAssembly ().GetName ().Version.Revision != 0)
				version += "." + Assembly.GetEntryAssembly ().GetName ().Version.Revision;

			new Gnome.Program (name, version, Gnome.Modules.UI, remainingArgs);
			Gdk.Threads.Init();
			commandLineArgs = remainingArgs;

			// Remoting check
			try {
				Dns.GetHostByName (Dns.GetHostName ());
			} catch {
				using (ErrorDialog dialog = new ErrorDialog (null)) {
					dialog.Message = "MonoDevelop failed to start. Local hostname cannot be resolved.";
					dialog.AddDetails ("Your network may be misconfigured. Make sure the hostname of your system is added to the /etc/hosts file.", true);
					dialog.Run ();
				}
				return 1;
			}
		
			StartupInfo.SetCommandLineArgs (remainingArgs);
			
			if (!options.nologo) {
				SplashScreenForm.SplashScreen.ShowAll ();
				RunMainLoop ();
			}

			SetSplashInfo (0.1, "Initializing Addins ...");
			
			bool ignoreDefaultPath = false;
			string [] addInDirs = MonoDevelop.Startup.AddInSettingsHandler.GetAddInDirectories (out ignoreDefaultPath);
			AddInTreeSingleton.SetAddInDirectories (addInDirs, ignoreDefaultPath);
			RunMainLoop ();

			Exception error    = null;
			
			try {
				SetSplashInfo(0.1, "Initializing Icon Service ...");
				ServiceManager.AddService(new IconService());
				
				SetSplashInfo(0.2, "Initializing Message Service ...");
				ServiceManager.AddService(new MessageService());
				
				SetSplashInfo(0.4, "Initializing Resource Service ...");
				ServiceManager.AddService(new ResourceService());
				
				SetSplashInfo(0.5, "Initializing Addin Services ...");
				AddinError[] errors = AddInTreeSingleton.InitializeAddins ();
				if (errors != null && errors.Length > 0) {
					SplashScreenForm.SplashScreen.Hide ();
					AddinLoadErrorDialog dlg = new AddinLoadErrorDialog (errors);
					if (!dlg.Run ())
						return 1;
					SplashScreenForm.SplashScreen.Show ();
					RunMainLoop ();
				}
				
				ServiceManager.ServiceLoadCallback = new ServiceLoadCallback (OnServiceLoad);
				ServiceManager.InitializeServicesSubsystem("/Workspace/Services");

				SetSplashInfo(0.8, "Loading MonoDevelop Workbench ...");

				// no alternative for Application.ThreadException?
				// Application.ThreadException += new ThreadExceptionEventHandler(ShowErrorBox);

			} catch (Exception e) {
				error = e;
			} finally {
				if (SplashScreenForm.SplashScreen != null) {
					SplashScreenForm.SplashScreen.Hide ();
				}
			}
			
			if (error != null) {
				ErrorDialog dialog = new ErrorDialog (null);
				dialog.Message = "MonoDevelop failed to start. The following error has been reported: " + error.Message;
				dialog.AddDetails (error.ToString (), false);
				dialog.Run ();
				return 1;
			}

			// FIXME: we should probably track the last 'selected' one
			// and do this more cleanly
			try {
				listen_socket.Bind (ep);
				listen_socket.Listen (5);
				listen_socket.BeginAccept (new AsyncCallback (ListenCallback), listen_socket);
			} catch {
				Console.WriteLine ("Socket already in use");
			}

			IdeApp.Run ();

			// unloading services
			File.Delete (socket_filename);
			ServiceManager.UnloadAllServices ();
			System.Environment.Exit (0);
			return 0;
		}

		static void SetSplashInfo(double Percentage, string Message)
		{
			SplashScreenForm.SetProgress (Percentage);
			SplashScreenForm.SetMessage (Message);
			RunMainLoop();
		}

		static int servicesLoaded = 0;

		static void OnServiceLoad (object o, ServiceLoadArgs args)
		{
			try {
				double   level = 0.5 + ((double)servicesLoaded / (double)args.TotalServices * 0.3);
				string[] parts = args.Service.ToString ().Split ('.');
				string service = parts[parts.Length - 1];
				
				if (args.LoadType == ServiceLoadType.LoadStarted) {
					SetSplashInfo(level, String.Format ("Initializing {0} ...", service));
				} else {
					SetSplashInfo(level, String.Format ("Initialized {0} ...", service));
					servicesLoaded++;
				}
			} catch {}
		}
		
		static string fileToOpen = String.Empty;
		
		static void RunMainLoop ()
		{
			while (Gtk.Application.EventsPending()) {
				Gtk.Application.RunIteration (false);
			}
		}

		static void ListenCallback (IAsyncResult state)
		{
			Socket sock = (Socket)state.AsyncState;

			if (!sock.Connected) {
				return;
			}

			Socket client = sock.EndAccept (state);
			((Socket)state.AsyncState).BeginAccept (new AsyncCallback (ListenCallback), sock);
			byte[] buf = new byte[1024];
			client.Receive (buf);
			foreach (string filename in Encoding.UTF8.GetString (buf).Split ('\n')) {
				string trimmed = filename.Trim ();
				string file = "";
				foreach (char c in trimmed) {
					if (c == 0x0000)
						continue;
					file += c;
				}
				fileToOpen = file;
				GLib.Idle.Add (new GLib.IdleHandler (openFile));
			}
		}

		static bool openFile () 
		{
			lock (fileToOpen) {
				string file = fileToOpen;
				if (file == null || file.Length == 0)
					return false;
				if (MonoDevelop.Projects.Services.ProjectService.IsCombineEntryFile (file)) {
					try {
						IdeApp.ProjectOperations.OpenCombine (file);
					} catch (Exception e) {
					}
				} else {
					try {
						IdeApp.Workbench.OpenDocument (file);
					} catch (Exception e) {
					}
				}
				IdeApp.Workbench.RootWindow.Present ();
				return false;
			}
		}
	}
}
