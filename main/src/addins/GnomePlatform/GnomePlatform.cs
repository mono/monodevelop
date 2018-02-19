//
// GnomePlatform.cs
//
// Author:
//   Geoff Norton  <gnorton@novell.com>
//   Matthias Gliwka <hello@gliwka.eu>
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

using MonoDevelop.Ide.Desktop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core;

namespace MonoDevelop.Platform
{
	public class GnomePlatform : PlatformService
	{
		static GnomePlatform ()
		{
		}
		
		public override IEnumerable<DesktopApplication> GetApplications (string filename)
		{
			var mimeType = GetMimeTypeForUri (filename);
			return GetApplicationsForMimeType (mimeType);
		}

		IEnumerable<DesktopApplication> GetApplicationsForMimeType (string mimeType)
		{
			return Gio.GetAllForType (mimeType);
		}
		
		struct GnomeVfsApp {
			public string Id, DisplayName, Command;
		}

		protected override string OnGetMimeTypeDescription (string mt)
		{
			return Gio.GetMimeTypeDescription (mt);
		}

		protected override string OnGetMimeTypeForUri (string uri)
		{
			if (uri == null)
				return null;
			
			return Gio.GetMimeTypeForUri (uri);
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
			Runtime.ProcessService.StartProcess ("xdg-open", url, null, null);
		}
		
		public override string DefaultMonospaceFont {
			get {
				try {
					return (string) (Gio.GetGSettingsString ("org.gnome.desktop.interface","monospace-font-name"));
				} catch (Exception) {
					return "Monospace 11";
				}
			}
		}
		
		public override string Name {
			get { return "Gnome"; }
		}

		protected override string OnGetIconIdForFile (string filename)
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
				try {
					icon = Gio.GetIconIdForFile (filename);
				} catch {}
				if (icon != null && icon.Length > 0)
					return icon;
			}			
			return "gnome-fs-regular";
			
		}
		
		protected override Xwt.Drawing.Image OnGetIconForFile (string filename)
		{
			string icon = OnGetIconIdForFile (filename);
			return GetIconForType (icon);
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
		
		
		delegate string TerminalRunnerHandler (string command, string args, string dir, string title, bool pause, Guid applicationId);
		delegate string TerminalOpenFolderRunnerHandler (string dir);

		string terminal_command;
		bool terminal_probed;
		TerminalRunnerHandler runner;
		TerminalOpenFolderRunnerHandler openDirectoryRunner;
		
		public override ProcessAsyncOperation StartConsoleProcess (string command, string arguments, string workingDirectory,
		                                                            IDictionary<string, string> environmentVariables, 
		                                                            string title, bool pauseWhenFinished)
		{
			ProbeTerminal ();
			
			//generate unique guid to derive application id for gnome terminal server
			var consoleGuid = Guid.NewGuid (); 

			string exec = runner (command, arguments, workingDirectory, title, pauseWhenFinished, consoleGuid);

			var psi = new ProcessStartInfo (terminal_command, exec) {
				CreateNoWindow = true,
				UseShellExecute = false,
			};
			foreach (var env in environmentVariables)
				psi.EnvironmentVariables [env.Key] = env.Value;
			
			ProcessWrapper proc = new ProcessWrapper ();
			if (terminal_command.Contains ("gnome-terminal")) {
				var parameter = String.Format ("--app-id {0}", GenerateAppId (consoleGuid));
				var terminalProcessStartInfo = new ProcessStartInfo ("/usr/lib/gnome-terminal/gnome-terminal-server", parameter) {
					CreateNoWindow = true,
					UseShellExecute = false,
				};
				proc.StartInfo = terminalProcessStartInfo;
				proc.Start ();
				proc.WaitForExit (500); //give the terminal server some warm up time

				Process.Start (psi);
			} else {
				proc.StartInfo = psi;
				proc.Start ();
			}
			return proc.ProcessAsyncOperation;
		}
		
#region Terminal runner implementations
		
		private static string GnomeTerminalRunner (string command, string args, string dir, string title, bool pause, Guid applicationId)
		{
			string extra_commands = pause 
				? BashPause.Replace ("'", "\\\"")
				: String.Empty;
			
			return String.Format (@" --app-id {5} --name ""{4}"" -e ""bash -c 'cd {3} ; {0} {1} ; {2}'""",
				command,
				EscapeArgs (args),
				extra_commands,
				EscapeDir (dir),
				title,
				GenerateAppId (applicationId));
		}
		
		private static string MateTerminalRunner (string command, string args, string dir, string title, bool pause, Guid applicationId)
		{
			string extra_commands = pause 
				? BashPause.Replace ("'", "\\\"")
				: String.Empty;
			
			return String.Format (@"--name ""{4}"" -e ""bash -c 'cd {3} ; {0} {1} ; {2}'""",
				command,
				EscapeArgs (args),
				extra_commands,
				EscapeDir (dir),
				title);
		}

		private static string GenerateAppId (Guid applicationId)
 		{
			return String.Format("mono.develop.id{0}", applicationId.ToString ().Replace ("-", ""));
 		}
		
		private static string XtermRunner (string command, string args, string dir, string title, bool pause, Guid applicationId)
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

		private static string KdeTerminalRunner (string command, string args, string dir, string title, bool pause, Guid applicationId)
		{
			string extra_commands = pause 
				? BashPause.Replace ("'", "\"")
					: String.Empty;

			return String.Format (@" --nofork --caption ""{4}"" --workdir=""{3}"" -e ""bash"" -c '{0} {1} ; {2}'",
			                      command,
			                      args,
			                      extra_commands,
			                      EscapeDir (dir),
			                      title);
		}

		private static string GnomeTerminalOpenFolderRunner (string dir) {
			return string.Format(@" --working-directory=""{0}""", EscapeDir(dir));
		}

		private static string XtermOpenFolderRunner (string dir) {
			return string.Format(@" -e bash -c ""cd {0}""", EscapeDir(dir));
		}

		private static string KdeTerminalOpenFolderRunner (string dir) {
			return string.Format(@" --nofork --workdir=""{0}""", EscapeDir(dir));
		}

		private static string EscapeArgs (string args)
		{
			return args.Replace ("\\", "\\\\").Replace ("\"", "\\\"");
		}
		
		private static string EscapeDir (string dir)
		{
			return dir.Replace (" ", "\\ ").Replace (";", "\\;");
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
			
			string fallback_terminal = PropertyService.Get ("MonoDevelop.Shell", "xterm");
			string preferred_terminal;
			TerminalRunnerHandler preferred_runner = null;
			TerminalRunnerHandler fallback_runner = XtermRunner;

			TerminalOpenFolderRunnerHandler preferedOpenFolderRunner = null;
			TerminalOpenFolderRunnerHandler fallbackOpenFolderRunner = XtermOpenFolderRunner;

			if (!String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("GNOME_DESKTOP_SESSION_ID"))) {
				preferred_terminal = "gnome-terminal";
				preferred_runner = GnomeTerminalRunner;
				preferedOpenFolderRunner = GnomeTerminalOpenFolderRunner;
			}
			else if (!String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("MATE_DESKTOP_SESSION_ID"))) {
				preferred_terminal = "mate-terminal";
				preferred_runner = MateTerminalRunner;
				preferedOpenFolderRunner = GnomeTerminalOpenFolderRunner;
			} 
			else if (!String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("KDE_SESSION_VERSION"))) { 
				preferred_terminal = "konsole";
				preferred_runner = KdeTerminalRunner;
				preferedOpenFolderRunner = KdeTerminalOpenFolderRunner;
			}
			else {
				preferred_terminal = fallback_terminal;
				preferred_runner = fallback_runner;
				preferedOpenFolderRunner = fallbackOpenFolderRunner;
			}

			terminal_command = FindExec (preferred_terminal);
			if (terminal_command != null) {
				runner = preferred_runner;
				openDirectoryRunner = preferedOpenFolderRunner;
				return;
			}
			
			terminal_command = FindExec (fallback_terminal);
			runner = fallback_runner;
			openDirectoryRunner = fallbackOpenFolderRunner;
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
				
		public override bool CanOpenTerminal {
			get {
				return true;
			}
		}

		public override void OpenTerminal (FilePath directory, IDictionary<string, string> environmentVariables, string title)
		{
			ProbeTerminal ();
			Runtime.ProcessService.StartProcess (terminal_command, openDirectoryRunner(directory), directory, null);
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
