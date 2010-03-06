//
// IdeStartup.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Reflection;
using System.Xml;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

using Mono.Unix;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Ide.Gui
{
	public class IdeStartup: IApplication
	{
		Socket listen_socket   = null;
		ArrayList errorsList = new ArrayList ();
		bool initialized;
		internal static string DefaultTheme;
		
		public int Run (string[] args)
		{
			var options = new MonoDevelopOptions ();
			var optionsSet = new Mono.Options.OptionSet () {
				{ "nologo", "Do not display splash screen.", s => options.NoLogo = true },
				{ "ipc-tcp", "Use the Tcp channel for inter-process comunication.", s => options.IpcTcp = true },
				{ "newwindow", "Do not open in an existing instance of MonoDevelop", s => options.NewWindow = true },
				{ "h|?|help", "Show help", s => options.ShowHelp = true },
			};
			var remainingArgs = optionsSet.Parse (args);
			if (options.ShowHelp) {
				Console.WriteLine ("MonoDevelop IDE " + MonoDevelop.Ide.BuildVariables.PackageVersionLabel);
				Console.WriteLine ("Options:");
				optionsSet.WriteOptionDescriptions (Console.Out);
				return 0;
			}
			
			LoggingService.Trace ("IdeStartup", "Initializing GTK");
			Counters.Initialization++;
			SetupExceptionManager ();
			
			try {
				MonoDevelop.Core.Gui.GLibLogging.Enabled = true;
			} catch (Exception ex) {
				LoggingService.LogError ("Error initialising GLib logging.", ex);
			}
			
			//OSXFIXME
			Gtk.Application.Init ("monodevelop", ref args);
			InternalLog.Initialize ();
			string socket_filename = null;
			EndPoint ep = null;
			
			AddinManager.AddinLoadError += OnAddinError;
			
			StartupInfo.SetCommandLineArgs (remainingArgs.ToArray ());
			
			// If a combine was specified, force --newwindow.
			
			if(!options.NewWindow && StartupInfo.HasFiles) {
				LoggingService.Trace ("IdeStartup", "Pre-Initializing Runtime to load files in existing window");
				Runtime.Initialize (true);
				foreach (string file in StartupInfo.GetRequestedFileList ()) {
					if (MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile (file))
					{
						options.NewWindow = true;
						break;
					}
				}
			}
			
			DefaultTheme = Gtk.Settings.Default.ThemeName;
			if (!string.IsNullOrEmpty (IdeApp.Preferences.UserInterfaceTheme))
				Gtk.Settings.Default.ThemeName = IdeApp.Preferences.UserInterfaceTheme;
			
			//don't show the splash screen on the Mac, so instead we get the expected "Dock bounce" effect
			//this also enables the Mac platform service to subscribe to open document events before the GUI loop starts.
			if (PropertyService.IsMac)
				options.NoLogo = true;
			
			IProgressMonitor monitor;
			if (options.NoLogo) {
				monitor = new MonoDevelop.Core.ProgressMonitoring.ConsoleProgressMonitor ();
			} else {
				monitor = SplashScreenForm.SplashScreen;
				SplashScreenForm.SplashScreen.ShowAll ();
			}
			
			LoggingService.Trace ("IdeStartup", "Initializing Runtime");
			monitor.BeginTask (GettextCatalog.GetString ("Starting MonoDevelop"), 2);
			monitor.Step (1);
			Runtime.Initialize (true);
			
			//make sure that the platform service is initialised so that the Mac platform can subscribe to open-document events
			LoggingService.Trace ("IdeStartup", "Initializing Platform Service");
			DesktopService.Initialize ();
			monitor.Step (1);
			monitor.EndTask ();
			
			monitor.Step (1);

			if(!options.IpcTcp){
				socket_filename = "/tmp/md-" + Environment.GetEnvironmentVariable ("USER") + "-socket";
				listen_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
				ep = new UnixEndPoint (socket_filename);
				
				// If not opening a combine, connect to existing monodevelop and pass filename(s) and exit
				if (!options.NewWindow && StartupInfo.GetRequestedFileList ().Length > 0 && File.Exists (socket_filename)) {
					try {
						listen_socket.Connect (ep);
						listen_socket.Send (Encoding.UTF8.GetBytes (String.Join ("\n", StartupInfo.GetRequestedFileList ())));
						return 0;
					} catch {
						// Reset the socket
						File.Delete (socket_filename);
					}
				}
			}
			
			LoggingService.Trace ("IdeStartup", "Checking System");
			string version = Assembly.GetEntryAssembly ().GetName ().Version.Major + "." + Assembly.GetEntryAssembly ().GetName ().Version.Minor;
			
			if (Assembly.GetEntryAssembly ().GetName ().Version.Build != 0)
				version += "." + Assembly.GetEntryAssembly ().GetName ().Version.Build;
			if (Assembly.GetEntryAssembly ().GetName ().Version.Revision != 0)
				version += "." + Assembly.GetEntryAssembly ().GetName ().Version.Revision;
			
			// System checks
			if (!CheckBug77135 ())
				return 1;
			
			if (!CheckQtCurve ())
				return 1;

			CheckFileWatcher ();
			
			Exception error = null;
			int reportedFailures = 0;
			
			try {
				LoggingService.Trace ("IdeStartup", "Loading Icons");
				//force initialisation before the workbench so that it can register stock icons for GTK before they get requested
				MonoDevelop.Core.Gui.ImageService.Initialize ();
				
				if (errorsList.Count > 0) {
					if (monitor is SplashScreenForm)
						SplashScreenForm.SplashScreen.Hide ();
					AddinLoadErrorDialog dlg = new AddinLoadErrorDialog ((AddinError[]) errorsList.ToArray (typeof(AddinError)), false);
					if (!dlg.Run ())
						return 1;
					if (monitor is SplashScreenForm)
						SplashScreenForm.SplashScreen.Show ();
					reportedFailures = errorsList.Count;
				}
				
				// no alternative for Application.ThreadException?
				// Application.ThreadException += new ThreadExceptionEventHandler(ShowErrorBox);

				LoggingService.Trace ("IdeStartup", "Initializing IdeApp");
				IdeApp.Initialize (monitor);
				monitor.Step (1);
			
			} catch (Exception e) {
				error = e;
			} finally {
				monitor.Dispose ();
			}
			
			if (error != null) {
				MessageService.ShowException (error,
				                              GettextCatalog.GetString ("MonoDevelop failed to start. The following error has been reported: ") + error.Message);
				return 1;
			}

			if (errorsList.Count > reportedFailures) {
				AddinLoadErrorDialog dlg = new AddinLoadErrorDialog ((AddinError[]) errorsList.ToArray (typeof(AddinError)), true);
				dlg.Run ();
			}
			
			errorsList = null;
			
			// FIXME: we should probably track the last 'selected' one
			// and do this more cleanly
			if (!options.IpcTcp) {
				try {
					listen_socket.Bind (ep);
					listen_socket.Listen (5);
					listen_socket.BeginAccept (new AsyncCallback (ListenCallback), listen_socket);
				} catch {
					// Socket already in use
				}
			}
			
			initialized = true;
			MessageService.RootWindow = IdeApp.Workbench.RootWindow;
			Counters.Initialization--;
			
			LoggingService.Trace ("IdeStartup", "Running IdeApp");
			IdeApp.Run ();
			
			// unloading services
			if (null != socket_filename)
				File.Delete (socket_filename);
			
			Runtime.Shutdown ();
			System.Environment.Exit (0);
			return 0;
		}
		
		public bool Initialized {
			get { return initialized; }
		}
		
		void OnAddinError (object s, AddinErrorEventArgs args)
		{
			if (errorsList != null)
				errorsList.Add (new AddinError (args.AddinId, args.Message, args.Exception, false));
		}

		void ListenCallback (IAsyncResult state)
		{
			Socket sock = (Socket)state.AsyncState;

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
				GLib.Idle.Add (delegate(){ return openFile (file); });
			}
		}

		bool openFile (string file) 
		{
			if (string.IsNullOrEmpty (file))
				return false;
			
			Match fileMatch = StartupInfo.fileExpression.Match (file);
			if (null == fileMatch || !fileMatch.Success)
				return false;
				
			int line = 1,
			    column = 1;
			
			file = fileMatch.Groups["filename"].Value;
			if (fileMatch.Groups["line"].Success)
				int.TryParse (fileMatch.Groups["line"].Value, out line);
			if (fileMatch.Groups["column"].Success)
				int.TryParse (fileMatch.Groups["column"].Value, out column);
				
			if (MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile (file)) {
				try {
					IdeApp.Workspace.OpenWorkspaceItem (file);
				} catch {
				}
			} else {
				try {
					LoggingService.LogError ("Opening {0} at {1}:{2}", file, line, column);
					IdeApp.Workbench.OpenDocument (file, line, column, true);
				} catch {
				}
			}
			IdeApp.Workbench.Present ();
			return false;
		}
		
		bool CheckQtCurve ()
		{
			if (Gtk.Settings.Default.ThemeName == "QtCurve") {
				string msg = "QtCurve theme not supported";
				string desc = "Your system is using the QtCurve GTK+ theme. This theme is known to cause stability issues in MonoDevelop. Please select another theme in the GTK+ Theme Selector.\n\nIf you click on Proceed, MonoDevelop will switch to the default GTK+ theme.";
				AlertButton res = MessageService.GenericAlert (Gtk.Stock.DialogWarning, msg, desc, AlertButton.Cancel, AlertButton.Proceed);
				if (res == AlertButton.Cancel)
					return false;
				Gtk.Settings.Default.ThemeName = "Gilouche";
			}
			return true;
		}
		
		void CheckFileWatcher ()
		{
			string watchesFile = "/proc/sys/fs/inotify/max_user_watches";
			try {
				if (File.Exists (watchesFile)) {
					string val = File.ReadAllText (watchesFile);
					int n = int.Parse (val);
					if (n <= 9000) {
						string msg = "Inotify watch limit is too low (" + n + ").\n";
						msg += "MonoDevelop will switch to managed file watching.\n";
						msg += "See http://monodevelop.com/Inotify_Watches_Limit for more info.";
						LoggingService.LogWarning (msg);
						Runtime.ProcessService.EnvironmentVariableOverrides["MONO_MANAGED_WATCHER"] = 
							Environment.GetEnvironmentVariable ("MONO_MANAGED_WATCHER");
						Environment.SetEnvironmentVariable ("MONO_MANAGED_WATCHER", "1");
					}
				}
			} catch (Exception e) {
				LoggingService.LogWarning ("There was a problem checking whether to use managed file watching", e);
			}
		}
		
		bool CheckBug77135 ()
		{
			try {
				// Check for bug 77135. Some versions of gnome-vfs2 and libgda
				// make MD crash in the file open dialog or in FileIconLoader.
				// Only in Suse.
				
				string path = "/etc/SuSE-release";
				if (!File.Exists (path))
					return true;
					
				// Only run the check for SUSE 10
				StreamReader sr = File.OpenText (path);
				string txt = sr.ReadToEnd ();
				sr.Close ();
				
				if (txt.IndexOf ("SUSE LINUX 10") == -1)
					return true;
					
				string current_libgda;
				string current_gnomevfs;
				string required_libgda = "1.3.91.5.4";
				string required_gnomevfs = "2.12.0.9.2";
				
				StringWriter sw = new StringWriter ();
				ProcessWrapper pw = Runtime.ProcessService.StartProcess ("rpm", "--qf %{version}.%{release} -q libgda", null, sw, null, null);
				pw.WaitForOutput ();
				current_libgda = sw.ToString ().Trim (' ','\n');
				
				sw = new StringWriter ();
				pw = Runtime.ProcessService.StartProcess ("rpm", "--qf %{version}.%{release} -q gnome-vfs2", null, sw, null, null);
				pw.WaitForOutput ();
				current_gnomevfs = sw.ToString ().Trim (' ','\n');
				
				bool fail1 = Addin.CompareVersions (current_libgda, required_libgda) == 1;
				bool fail2 = Addin.CompareVersions (current_gnomevfs, required_gnomevfs) == 1;
				
				if (fail1 || fail2) {
					string msg = GettextCatalog.GetString ("Some packages installed in your system are not compatible with MonoDevelop:\n");
					if (fail1)
						msg += "\nlibgda " + current_libgda + " ("+ GettextCatalog.GetString ("version required: {0}", required_libgda) + ")";
					if (fail2)
						msg += "\ngnome-vfs2 " + current_gnomevfs + " ("+ GettextCatalog.GetString ("version required: {0}", required_gnomevfs) + ")";
					msg += "\n\n";
					msg += GettextCatalog.GetString ("You need to upgrade the previous packages to start using MonoDevelop.");
					
					SplashScreenForm.SplashScreen.Hide ();
					Gtk.MessageDialog dlg = new Gtk.MessageDialog (null, Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, msg);
					dlg.Run ();
					dlg.Destroy ();
					
					return false;
				} else
					return true;
			}
			catch (Exception ex)
			{
				// Just ignore for now.
				Console.WriteLine (ex);
				return true;
			}
		}
		
		void SetupExceptionManager ()
		{
			GLib.ExceptionManager.UnhandledException += delegate (GLib.UnhandledExceptionArgs args) {
				var ex = (Exception)args.ExceptionObject;
				LoggingService.LogError ("Unhandled Exception", ex);
				MessageService.ShowException (ex, "Unhandled Exception");
			};
			AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args) {
				//FIXME: try to save all open files, since we can't prevent the runtime from terminating
				var ex = (Exception)args.ExceptionObject;
				LoggingService.LogFatalError ("Unhandled Exception", ex);
				MessageService.ShowException (ex, "Unhandled Exception. MonoDevelop will now close.");
			};
		}
	}
	
#pragma warning disable 0618
	public class MonoDevelopOptions
	{
		public MonoDevelopOptions ()
		{
			IpcTcp = (PlatformID.Unix != Environment.OSVersion.Platform);
		}
		
		public bool NoLogo { get; set; }
		public bool IpcTcp { get; set; }
		public bool NewWindow { get; set; }
		public bool ShowHelp { get; set; }
	}
	
#pragma warning restore 0618
	
	public class AddinError
	{
		string addinFile;
		Exception exception;
		bool fatal;
		string message;
		
		public AddinError (string addin, string message, Exception exception, bool fatal)
		{
			this.addinFile = addin;
			this.message = message;
			this.exception = exception;
			this.fatal = fatal;
		}
		
		public string AddinFile {
			get { return addinFile; }
		}
		
		public string Message {
			get { return message; }
		}
		
		public Exception Exception {
			get { return exception; }
		}
		
		public bool Fatal {
			get { return fatal; }
		}
	}
}
