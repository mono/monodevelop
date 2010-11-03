//
// GnomePlatform.cs
//
// Author:
//   Geoff Norton  <gnorton@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using Gnome;
using MonoDevelop.Ide.Desktop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core;

namespace MonoDevelop.Platform
{
	public class GnomePlatform : PlatformService
	{
		static bool useGio;

		Gnome.ThumbnailFactory thumbnailFactory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Normal);

		static GnomePlatform ()
		{
			try {
				Gio.GetDefaultForType ("text/plain");
				useGio = true;
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
			//apparently Gnome.Icon needs GnomeVFS initialized even when we're using GIO.
			Gnome.Vfs.Vfs.Initialize ();
		}
		
		DesktopApplication GetGnomeVfsDefaultApplication (string mimeType)
		{
			var app = Gnome.Vfs.Mime.GetDefaultApplication (mimeType);
			if (app != null)
				return (DesktopApplication) Marshal.PtrToStructure (app.Handle, typeof(DesktopApplication));
			else
				return null;
		}
		
		IEnumerable<DesktopApplication> GetGnomeVfsApplications (string mimeType)
		{
			var def = GetGnomeVfsDefaultApplication (mimeType);
			var list = new List<DesktopApplication> ();
			var apps = Gnome.Vfs.Mime.GetAllApplications (mimeType);
			foreach (var app in apps) {
				var dap = (GnomeVfsApp) Marshal.PtrToStructure (app.Handle, typeof(GnomeVfsApp));
				if (!string.IsNullOrEmpty (dap.Command) && !string.IsNullOrEmpty (dap.DisplayName) && !dap.Command.Contains ("monodevelop ")) {
					var isDefault = def != null && def.Id == dap.Command;
					list.Add (new GnomeDesktopApplication (dap.Command, dap.DisplayName, isDefault));
				}
			}
			return list;
		}
		
		public override IEnumerable<DesktopApplication> GetApplications (string filename)
		{
			var mimeType = GetMimeTypeForUri (filename);
			return GetApplicationsForMimeType (mimeType);
		}

		IEnumerable<DesktopApplication> GetApplicationsForMimeType (string mimeType)
		{
			if (useGio)
				return Gio.GetAllForType (mimeType);
			else
				return GetGnomeVfsApplications (mimeType);
		}
		
		struct GnomeVfsApp {
			public string Id, DisplayName, Command;
		}

		protected override string OnGetMimeTypeDescription (string mt)
		{
			if (useGio)
				return Gio.GetMimeTypeDescription (mt);
			else
				return Gnome.Vfs.Mime.GetDescription (mt);
		}

		protected override string OnGetMimeTypeForUri (string uri)
		{
			if (uri == null)
				return null;
			
			if (useGio) {
				string mt = Gio.GetMimeTypeForUri (uri);
				if (mt != null)
					return mt;
			}
			return Gnome.Vfs.MimeType.GetMimeTypeForUri (ConvertFileNameToVFS (uri));
		}
		
		protected override bool OnGetMimeTypeIsText (string mimeType)
		{
			// If gedit can open the file, this editor also can do it
			foreach (DesktopApplication app in GetApplicationsForMimeType (mimeType))
				if (app.Id == "gedit")
					return true;
			return base.OnGetMimeTypeIsText (mimeType);
		}


		public override void ShowUrl (string url)
		{
			Gnome.Url.Show (url);
		}
		
		public override string DefaultMonospaceFont {
			get { return (string) (new GConf.Client ().Get ("/desktop/gnome/interface/monospace_font_name")); }
		}
		
		public override string Name {
			get { return "Gnome"; }
		}

		protected override string OnGetIconForFile (string filename)
		{
			if (filename == "Documentation") {
				return "gnome-fs-regular";
			} 
			if (System.IO.Directory.Exists (filename)) {
				return "gnome-fs-directory";
			} else if (System.IO.File.Exists (filename)) {
				filename = EscapeFileName (filename);
				if (filename == null)
					return "gnome-fs-regular";
				
				string icon = null;
				Gnome.IconLookupResultFlags result;
				try {
					icon = Gnome.Icon.LookupSync (IconTheme.Default, thumbnailFactory, filename, null, 
					                              Gnome.IconLookupFlags.None, out result);
				} catch {}
				if (icon != null && icon.Length > 0)
					return icon;
			}			
			return "gnome-fs-regular";
			
		}
		
		protected override Gdk.Pixbuf OnGetPixbufForFile (string filename, Gtk.IconSize size)
		{
			string icon = OnGetIconForFile (filename);
			return GetPixbufForType (icon, size);
		}
		
		string EscapeFileName (string filename)
		{
			foreach (char c in filename) {
				// FIXME: This is a temporary workaround. In some systems, files with
				// accented characters make LookupSync crash. Still trying to find out why.
				if ((int)c < 32 || (int)c > 127)
					return null;
			}
			return ConvertFileNameToVFS (filename);
		}
		
		static string ConvertFileNameToVFS (string fileName)
		{
			string result = fileName;
			result = result.Replace ("%", "%25");
			result = result.Replace ("#", "%23");
			result = result.Replace ("?", "%3F");
			return result;
		}
		
		
		delegate string TerminalRunnerHandler (string command, string args, string dir, string title, bool pause);
	
		string terminal_command;
		bool terminal_probed;
		TerminalRunnerHandler runner;
		
		public override IProcessAsyncOperation StartConsoleProcess (string command, string arguments, string workingDirectory,
		                                                            IDictionary<string, string> environmentVariables, 
		                                                            string title, bool pauseWhenFinished)
		{
			ProbeTerminal ();
			
			string exec = runner (command, arguments, workingDirectory, title, pauseWhenFinished);
			var psi = new ProcessStartInfo (terminal_command, exec) {
				CreateNoWindow = true,
				UseShellExecute = false,
			};
			foreach (var env in environmentVariables)
				psi.EnvironmentVariables [env.Key] = env.Value;
			
			ProcessWrapper proc = new ProcessWrapper ();
			proc.StartInfo = psi;
			proc.Start ();
			return proc;
		}
		
#region Terminal runner implementations
		
		private static string GnomeTerminalRunner (string command, string args, string dir, string title, bool pause)
		{
			string extra_commands = pause 
				? BashPause.Replace ("'", "\\\"")
				: String.Empty;
			
			return String.Format (@" --disable-factory --title ""{4}"" -e ""bash -c 'cd {3} ; {0} {1} ; {2}'""",
				command,
				EscapeArgs (args),
				extra_commands,
				EscapeDir (dir),
				title);
		}
		
		private static string XtermRunner (string command, string args, string dir, string title, bool pause)
		{
			string extra_commands = pause 
				? BashPause
				: String.Empty;
			
			return String.Format (@" -title ""{4}"" -e bash -c ""cd {3} ; '{0}' {1} ; {2}""",
				command,
				EscapeArgs (args),
				extra_commands,
				EscapeDir (dir),
				title);
		}
		
		private static string EscapeArgs (string args)
		{
			return args.Replace ("\\", "\\\\").Replace ("\"", "\\\"");
		}
		
		private static string EscapeDir (string dir)
		{
			return dir.Replace (" ", "\\ ");
		}
		
		private static string BashPause {
			get { return @"echo; read -p 'Press any key to continue...' -n1;"; }
		}

#endregion

#region Probing for preferred terminal

		private void ProbeTerminal ()
		{
			if (terminal_probed) {
				return;
			}
			
			terminal_probed = true;
			
			string fallback_terminal = "xterm";
			string preferred_terminal;
			TerminalRunnerHandler preferred_runner = null;
			TerminalRunnerHandler fallback_runner = XtermRunner;

			if (!String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("GNOME_DESKTOP_SESSION_ID"))) {
				preferred_terminal = "gnome-terminal";
				preferred_runner = GnomeTerminalRunner;
			}
			else {
				preferred_terminal = fallback_terminal;
				preferred_runner = fallback_runner;
			}

			terminal_command = FindExec (preferred_terminal);
			if (terminal_command != null) {
				runner = preferred_runner;
				return;
			}
			
			FindExec (fallback_terminal);
			runner = fallback_runner;
		}

		private string FindExec (string command)
		{
			foreach (string path in GetExecPaths ()) {
				string full_path = Path.Combine (path, command);
				try {
					FileInfo info = new FileInfo (full_path);
					// FIXME: System.IO is super lame, should check for 0755
					if (info.Exists) {
						return full_path;
					}
				} catch {
				}
			}

			return null;
		}

		private string [] GetExecPaths ()
		{
			string path = Environment.GetEnvironmentVariable ("PATH");
			if (String.IsNullOrEmpty (path)) {
				return new string [] { "/bin", "/usr/bin", "/usr/local/bin" };
			}

			// this is super lame, should handle quoting/escaping
			return path.Split (':');
		}

#endregion
		
		//FIXME: probe for terminal
		static string TerminalCommand {
			get {
				return PropertyService.Get ("MonoDevelop.Shell", "gnome-terminal");
			}
		}
		
		public override bool CanOpenTerminal {
			get {
				return true;
			}
		}
		
		public override void OpenInTerminal (FilePath directory)
		{
			Runtime.ProcessService.StartProcess (TerminalCommand, "", directory, null);
		}
	}
	
	class GnomeDesktopApplication : DesktopApplication
	{
		public GnomeDesktopApplication (string command, string displayName, bool isDefault) : base (command, displayName, isDefault)
		{
		}
		
		string Command {
			get { return Id; }
		}
		
		public override void Launch (params string[] files)
		{
			// TODO: implement all other cases
			if (Command.IndexOf ("%f") != -1) {
				foreach (string s in files) {
					string cmd = Command.Replace ("%f", "\"" + s + "\"");
					Process.Start (cmd);
				}
			}
			else if (Command.IndexOf ("%F") != -1) {
				string[] fs = new string [files.Length];
				for (int n=0; n<files.Length; n++) {
					fs [n] = "\"" + files [n] + "\"";
				}
				string cmd = Command.Replace ("%F", string.Join (" ", fs));
				Process.Start (cmd);
			} else {
				foreach (string s in files) {
					Process.Start (Command, "\"" + s + "\"");
				}
			}
		}
	}
}
