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
using System.Diagnostics;

using Mono.Unix;
using Mono.GetOptions;

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
		static string fileToOpen = String.Empty;
		ArrayList errorsList = new ArrayList ();
		bool initialized;
		
		public int Run (string[] args)
		{
			//touch Win32 before GTK, to make sure that theme API is inited
			//see http://lists.ximian.com/pipermail/gtk-sharp-list/2009-March/009507.html
			if (PropertyService.IsWindows) {
				var a = Assembly.Load ("System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				if (a != null) {
					var t = a.GetType ("System.Windows.Forms.Application");
					if (t != null) {
						var m = t.GetMethod ("DoEvents");
						if (m != null)
							m.Invoke (null, null);
					}
				}
			}
			
			SetupExceptionManager ();
			
			try {
				MonoDevelop.Core.Gui.GLibLogging.Enabled = true;
			} catch (Exception ex) {
				LoggingService.LogError ("Error initialising GLib logging.", ex);
			}
			
			//OSXFIXME
			Gtk.Application.Init ();
			if (!GLib.Thread.Supported)
				GLib.Thread.Init ();
			InternalLog.Initialize ();
			MonoDevelopOptions options = new MonoDevelopOptions ();
			options.ProcessArgs (args);
			string[] remainingArgs = options.RemainingArguments;
			string socket_filename = null;
			EndPoint ep = null;
			
			AddinManager.AddinLoadError += OnAddinError;
			
			StartupInfo.SetCommandLineArgs (remainingArgs);
			
			// If a combine was specified, force --newwindow.
			
			if(!options.newwindow && StartupInfo.HasFiles) {
				Runtime.Initialize (true);
				foreach (string file in StartupInfo.GetRequestedFileList ()) {
					if (MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile (file))
					{
						options.newwindow = true;
						break;
					}
				}
			}
			
			//don't show the splash screen on the Mac, so instead we get the expected "Dock bounce" effect
			//this also enables the Mac platform service to subscribe to open document events before the GUI loop starts.
			if (PropertyService.IsMac)
				options.nologo = true;
			
			IProgressMonitor monitor;
			if (options.nologo) {
				monitor = new MonoDevelop.Core.ProgressMonitoring.ConsoleProgressMonitor ();
			} else {
				monitor = SplashScreenForm.SplashScreen;
				SplashScreenForm.SplashScreen.ShowAll ();
			}
			
			monitor.BeginTask (GettextCatalog.GetString ("Starting MonoDevelop"), 2);
			
			monitor.BeginTask (GettextCatalog.GetString ("Starting MonoDevelop"), 2);
			monitor.Step (1);
			Runtime.Initialize (true);
			//make sure that the platform service is initialised so that the Mac platform can subscribe to open-document events
			MonoDevelop.Core.Gui.Services.PlatformService.ToString ();
			monitor.Step (1);
			monitor.EndTask ();
			
			monitor.Step (1);

			if(!options.ipc_tcp){
				socket_filename = "/tmp/md-" + Environment.GetEnvironmentVariable ("USER") + "-socket";
				listen_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
				ep = new UnixEndPoint (socket_filename);
				
				// If not opening a combine, connect to existing monodevelop and pass filename(s) and exit
				if (!options.newwindow && StartupInfo.GetRequestedFileList ().Length > 0 && File.Exists (socket_filename)) {
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
			
			string version = Assembly.GetEntryAssembly ().GetName ().Version.Major + "." + Assembly.GetEntryAssembly ().GetName ().Version.Minor;
			
			if (Assembly.GetEntryAssembly ().GetName ().Version.Build != 0)
				version += "." + Assembly.GetEntryAssembly ().GetName ().Version.Build;
			if (Assembly.GetEntryAssembly ().GetName ().Version.Revision != 0)
				version += "." + Assembly.GetEntryAssembly ().GetName ().Version.Revision;
			
			// System checks
			if (!CheckBug77135 ())
				return 1;

			CheckFileWatcher ();
			
			if (options.ipc_tcp) {
				Runtime.ProcessService.ExternalProcessRemotingChannel = "tcp";
				// Remoting check
				try {
					Dns.GetHostEntry (Dns.GetHostName ());
				} catch {
					using (ErrorDialog dialog = new ErrorDialog (null)) {
						if (monitor is SplashScreenForm)
							SplashScreenForm.SplashScreen.Hide ();
						dialog.Message = GettextCatalog.GetString ("MonoDevelop failed to start. Local hostname cannot be resolved.");
						dialog.AddDetails (GettextCatalog.GetString ("Your network may be misconfigured. Make sure the hostname of your system is added to the /etc/hosts file."), true);
						dialog.Run ();
					}
					return 1;
				}
			}

			Exception error = null;
			int reportedFailures = 0;
			
			try {
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
			if (!options.ipc_tcp) {
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
				if (MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile (file)) {
					try {
						IdeApp.Workspace.OpenWorkspaceItem (file);
					} catch {
					}
				} else {
					try {
						IdeApp.Workbench.OpenDocument (file);
					} catch {
					}
				}
				IdeApp.Workbench.RootWindow.Present ();
				return false;
			}
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
			Type t = typeof(GLib.Object).Assembly.GetType ("GLib.ExceptionManager");
			if (t == null)
				return;
			
			EventInfo ev = t.GetEvent ("UnhandledException");
			Type delType = typeof(GLib.Object).Assembly.GetType ("GLib.UnhandledExceptionHandler");
			MethodInfo met = GetType().GetMethod ("OnUnhandledException", BindingFlags.Instance | BindingFlags.NonPublic);
			Delegate del = Delegate.CreateDelegate (delType, this, met);
			ev.AddEventHandler (this, del);
		}
		
		internal void OnUnhandledException (UnhandledExceptionEventArgs args)
		{
			MessageService.ShowException ((Exception) args.ExceptionObject, "Unhandled Exception");
			//Type ueeType = typeof(GLib.Object).Assembly.GetType ("GLib.UnhandledExceptionArgs");
			//PropertyInfo ueeProp = ueeType.GetProperty ("ExitApplication");
			//ueeProp.SetValue (args, (object) false, null);
		}
	}
	
	public class MonoDevelopOptions : Options
	{
		public MonoDevelopOptions ()
		{
			base.ParsingMode = OptionsParsingMode.Both;
		}

		protected override void InitializeOtherDefaults () {
			ipc_tcp = (PlatformID.Unix != Environment.OSVersion.Platform);
		}

		[Option ("Do not display splash screen.")]
		public bool nologo;
		
		[Option ("Use the Tcp channel for inter-process comunication.", "ipc-tcp")]
		public bool ipc_tcp;
		
		[Option ("Do not open in an existing instance of MonoDevelop")]
		public bool newwindow;
	}	
	
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
