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

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Gui;

namespace MonoDevelop
{
	/// <summary>
	/// This Class is the Core main class, it starts the program.
	/// </summary>
	public class SharpDevelopMain
	{
		static string[] commandLineArgs = null;
		static Socket listen_socket = null;
		
		public static string[] CommandLineArgs {
			get {
				return commandLineArgs;
			}
		}

/* unused code

		static void ShowErrorBox(object sender, ThreadExceptionEventArgs eargs)
		{
			ExceptionDialog ed;

			ed = new ExceptionDialog(eargs.Exception);
			ed.AddButtonHandler(new ButtonHandler(DialogResultHandler));
			ed.ShowAll();
		}

		static void DialogResultHandler(ExceptionDialog ed, DialogResult dr) {
			ed.Destroy();			
		}
*/

		/// <summary>
		/// Starts the core of MonoDevelop.
		/// </summary>
		public static int Main (string[] args)
		{
			MonoDevelopOptions options = new MonoDevelopOptions ();
			options.ProcessArgs (args);
			string[] remainingArgs = options.RemainingArguments;

			string socket_filename = "/tmp/md-" + Environment.GetEnvironmentVariable ("USER") + "-socket";
			listen_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
			EndPoint ep = new UnixEndPoint (socket_filename);

			// only reuse if we are being passed in a file
			if (remainingArgs.Length > 0 && File.Exists (socket_filename))
			{
				try
				{
					listen_socket.Connect (ep);
					listen_socket.Send (Encoding.UTF8.GetBytes (String.Join ("\n", remainingArgs)));
					return 0;
				}
				catch
				{
				}
			}

			// why was this here
			// File.Delete (socket_filename);
			
			string name = Assembly.GetEntryAssembly ().GetName ().Name;
			string version = Assembly.GetEntryAssembly ().GetName ().Version.Major + "." + Assembly.GetEntryAssembly ().GetName ().Version.Minor;
			if (Assembly.GetEntryAssembly ().GetName ().Version.Build != 0)
				version += "." + Assembly.GetEntryAssembly ().GetName ().Version.Build;
			if (Assembly.GetEntryAssembly ().GetName ().Version.Revision != 0)
				version += "." + Assembly.GetEntryAssembly ().GetName ().Version.Revision;

			new Gnome.Program (name, version, Gnome.Modules.UI, remainingArgs);
			Gdk.Threads.Init();
			commandLineArgs = remainingArgs;

			//remoting check
			try {
				Dns.GetHostByName (Dns.GetHostName ());
			} catch {
				ErrorDialog dialog = new ErrorDialog (null);
				dialog.Message = "MonoDevelop failed to start. Local hostname cannot be resolved.";
				dialog.AddDetails ("Your network may be misconfigured. Make sure the hostname of your system is added to the /etc/hosts file.", true);
				dialog.Run ();
				return 1;
			} 
		
			SplashScreenForm.SetCommandLineArgs(remainingArgs);
			
			if (!options.nologo) {
				SplashScreenForm.SplashScreen.ShowAll ();
				RunMainLoop ();
			}

			SetSplashInfo(0.05, "Initializing Addins ...");
			bool ignoreDefaultPath = false;
			string [] addInDirs = MonoDevelop.AddInSettingsHandler.GetAddInDirectories(out ignoreDefaultPath);
			AddInTreeSingleton.SetAddInDirectories(addInDirs, ignoreDefaultPath);
			RunMainLoop ();

			ArrayList commands = null;
			Exception error = null;
			
			try {
				SetSplashInfo(0.1, "Initializing Icon Service ...");
				ServiceManager.AddService(new IconService());
				SetSplashInfo(0.2, "Initializing Message Service ...");
				ServiceManager.AddService(new MessageService());
				SetSplashInfo(0.4, "Initializing Resource Service ...");
				ServiceManager.AddService(new ResourceService());
				SetSplashInfo(0.6, "Initializing Addin Services ...");
				
				AddinError[] errors = AddInTreeSingleton.InitializeAddins ();
				if (errors != null && errors.Length > 0) {
					SplashScreenForm.SplashScreen.Hide ();
					AddinLoadErrorDialog dlg = new AddinLoadErrorDialog (errors);
					if (!dlg.Run ())
						return 1;
					SplashScreenForm.SplashScreen.Show ();
					RunMainLoop ();
				}
				ServiceManager.InitializeServicesSubsystem("/Workspace/Services");

				SetSplashInfo(0.8, "Initializing Autostart Addins ...");
				commands = AddInTreeSingleton.AddInTree.GetTreeNode("/Workspace/Autostart").BuildChildItems(null);
				SetSplashInfo(1, "Loading MonoDevelop Workbench ...");
				RunMainLoop ();
				for (int i = 0; i < commands.Count - 1; ++i) {
					((ICommand)commands[i]).Run();
					RunMainLoop ();
				}

				// no alternative for Application.ThreadException?
				// Application.ThreadException += new ThreadExceptionEventHandler(ShowErrorBox);

			} catch (Exception e) {
				error = e;
			} finally {
				if (SplashScreenForm.SplashScreen != null) {
					SplashScreenForm.SplashScreen.Hide();
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
			try
			{
				listen_socket.Bind (ep);
				listen_socket.Listen (5);
				listen_socket.BeginAccept (new AsyncCallback (ListenCallback), listen_socket);
			}
			catch
			{
				Console.WriteLine ("Socket already in use");
			}

			// run the last autostart command, this must be the workbench starting command
			if (commands.Count > 0) {
				RunMainLoop ();
				((ICommand)commands[commands.Count - 1]).Run();
			}

			// unloading services
			File.Delete (socket_filename);
			ServiceManager.UnloadAllServices();
			System.Environment.Exit (0);
			return 0;
		}

		static void SetSplashInfo(double Percentage, string Message)
		{
			SplashScreenForm.SetProgress(Percentage);
			SplashScreenForm.SetMessage(Message);
			RunMainLoop();
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
				if (Runtime.ProjectService.IsCombineEntryFile (file)) {
					try {
						IProjectService projectService = (IProjectService)ServiceManager.GetService (typeof (IProjectService));
						projectService.OpenCombine(file);
					} catch (Exception e) {
					}
				} else {
					try {
						IFileService fileService = (IFileService)MonoDevelop.Core.Services.ServiceManager.GetService(typeof(IFileService));
						fileService.OpenFile(file);
					} catch (Exception e) {
					}
				}
				((Gtk.Window)WorkbenchSingleton.Workbench).Present ();
				return false;
			}
		}
	}
}
