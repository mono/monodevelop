
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
using Mono.GetOptions;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Gui;
using MonoDevelop.Ide.Gui;


namespace MonoDevelop.Ide.Gui
{
	public class IdeStartup: IApplication
	{
		Socket listen_socket   = null;
		static string fileToOpen = String.Empty;
		
		public int Run (string[] args)
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
			
			IProgressMonitor monitor = SplashScreenForm.SplashScreen;
			
			if (!options.nologo) {
				SplashScreenForm.SplashScreen.ShowAll ();
			}

			monitor.BeginTask ("Initializing MonoDevelop", 2);
			
			Exception error = null;
			int reportedFailures = 0;
			
			try {
				ServiceManager.AddService(new IconService());
				
				Runtime.AddInService.PreloadAddins (monitor,
					"/SharpDevelop/Workbench",
					"/SharpDevelop/Views",
					"/SharpDevelop/Commands",
					"/SharpDevelop/Dialogs"
					);

				monitor.Step (1);

				AddinError[] errors = Runtime.AddInService.AddInLoadErrors;
				if (errors.Length > 0) {
					SplashScreenForm.SplashScreen.Hide ();
					AddinLoadErrorDialog dlg = new AddinLoadErrorDialog (errors, false);
					if (!dlg.Run ())
						return 1;
					SplashScreenForm.SplashScreen.Show ();
					reportedFailures = errors.Length;
				}
				
				// no alternative for Application.ThreadException?
				// Application.ThreadException += new ThreadExceptionEventHandler(ShowErrorBox);

				IdeApp.Initialize (monitor);
				monitor.Step (1);

				monitor.EndTask ();
			
			} catch (Exception e) {
				error = e;
			} finally {
				monitor.Dispose ();
			}
			
			if (error != null) {
				ErrorDialog dialog = new ErrorDialog (null);
				dialog.Message = "MonoDevelop failed to start. The following error has been reported: " + error.Message;
				dialog.AddDetails (error.ToString (), false);
				dialog.Run ();
				return 1;
			}

			AddinError[] errs = Runtime.AddInService.AddInLoadErrors;
			if (errs.Length > reportedFailures) {
				AddinLoadErrorDialog dlg = new AddinLoadErrorDialog (errs, true);
				dlg.Run ();
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

		void ListenCallback (IAsyncResult state)
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

		bool openFile () 
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
	
	public class MonoDevelopOptions : Options
	{
		public MonoDevelopOptions ()
		{
			base.ParsingMode = OptionsParsingMode.Both;
		}

		[Option ("Do not display splash screen.")]
		public bool nologo;
	}	
}
