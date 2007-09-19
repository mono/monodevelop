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
		
		public int Run (string[] args)
		{
			SetupExceptionManager ();
			
			Gnome.Vfs.Vfs.Initialize ();
			InternalLog.Initialize ();
			MonoDevelopOptions options = new MonoDevelopOptions ();
			options.ProcessArgs (args);
			string[] remainingArgs = options.RemainingArguments;
			string socket_filename = null;
			EndPoint ep = null;
			
			AddinManager.AddinLoadError += OnAddinError;
			
			if(!options.ipc_tcp){
				socket_filename = "/tmp/md-" + Environment.GetEnvironmentVariable ("USER") + "-socket";
				listen_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
				ep = new UnixEndPoint (socket_filename);
				
				// Connect to existing monodevelop and pass filename(s) and exit
				if (remainingArgs.Length > 0 && File.Exists (socket_filename)) {
					try {
						listen_socket.Connect (ep);
						listen_socket.Send (Encoding.UTF8.GetBytes (String.Join ("\n", remainingArgs)));
						return 0;
					} catch {
						// Reset the socket
						File.Delete (socket_filename);
					}
				}
			}
			
			string name    = Assembly.GetEntryAssembly ().GetName ().Name;
			string version = Assembly.GetEntryAssembly ().GetName ().Version.Major + "." + Assembly.GetEntryAssembly ().GetName ().Version.Minor;
			
			if (Assembly.GetEntryAssembly ().GetName ().Version.Build != 0)
				version += "." + Assembly.GetEntryAssembly ().GetName ().Version.Build;
			if (Assembly.GetEntryAssembly ().GetName ().Version.Revision != 0)
				version += "." + Assembly.GetEntryAssembly ().GetName ().Version.Revision;

			try {
				new Gnome.Program (name, version, Gnome.Modules.UI, remainingArgs);
			} catch (Exception ex) {
				string msg = GettextCatalog.GetString ("MonoDevelop failed to start.\nIf you installed MonoDevelop using a binary installer, take a look at \nhttp://www.mono-project.com/InstallerInstructions for more info about possible\ncauses of this error.");
				Console.WriteLine (new string ('#',70));
				Console.WriteLine (msg);
				Console.WriteLine (new string ('#',70));
				Console.WriteLine (ex);
				return 1;
			}
			
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
						dialog.Message = GettextCatalog.GetString ("MonoDevelop failed to start. Local hostname cannot be resolved.");
						dialog.AddDetails (GettextCatalog.GetString ("Your network may be misconfigured. Make sure the hostname of your system is added to the /etc/hosts file."), true);
						dialog.Run ();
					}
					return 1;
				}
			}
		
			StartupInfo.SetCommandLineArgs (remainingArgs);
			
			IProgressMonitor monitor = SplashScreenForm.SplashScreen;
			
			if (!options.nologo) {
				SplashScreenForm.SplashScreen.ShowAll ();
			}

			Exception error = null;
			int reportedFailures = 0;
			
			try {
				ServiceManager.AddService(new IconService());

				if (errorsList.Count > 0) {
					SplashScreenForm.SplashScreen.Hide ();
					AddinLoadErrorDialog dlg = new AddinLoadErrorDialog ((AddinError[]) errorsList.ToArray (typeof(AddinError)), false);
					if (!dlg.Run ())
						return 1;
					SplashScreenForm.SplashScreen.Show ();
					reportedFailures = errorsList.Count;
				}
				
				// no alternative for Application.ThreadException?
				// Application.ThreadException += new ThreadExceptionEventHandler(ShowErrorBox);

				IdeApp.Initialize (monitor);
			
			} catch (Exception e) {
				error = e;
			} finally {
				monitor.Dispose ();
			}
			
			if (error != null) {
				ErrorDialog dialog = new ErrorDialog (null);
				dialog.Message = GettextCatalog.GetString ("MonoDevelop failed to start. The following error has been reported: ") + error.Message;
				dialog.AddDetails (error.ToString (), false);
				dialog.Run ();
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

			IdeApp.Run ();

			// unloading services
			if (null != socket_filename)
				File.Delete (socket_filename);

			ServiceManager.UnloadAllServices ();
			System.Environment.Exit (0);
			return 0;
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
				if (MonoDevelop.Projects.Services.ProjectService.IsCombineEntryFile (file)) {
					try {
						IdeApp.ProjectOperations.OpenCombine (file);
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
						msg += "See http://www.monodevelop.com/Inotify_Watches_Limit for more info.";
						Runtime.LoggingService.Warn (msg);
						Environment.SetEnvironmentVariable ("MONO_MANAGED_WATCHER", "1");
					}
				}
			} catch {
				// Ignore
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
		
		void OnUnhandledException (UnhandledExceptionEventArgs args)
		{
			Gtk.Window[] wins = Gtk.Window.ListToplevels ();
			Gtk.Window win = IdeApp.Workbench != null && IdeApp.Workbench.RootWindow != null ? IdeApp.Workbench.RootWindow : null;
			foreach (Gtk.Window w in wins) {
				if (w.IsActive && w.Visible && w.Type == Gtk.WindowType.Toplevel) {
					win = w;
					break;
				}
			}
			Services.MessageService.ShowError ((Exception) args.ExceptionObject, "Unhandled Exception", win, true);
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
