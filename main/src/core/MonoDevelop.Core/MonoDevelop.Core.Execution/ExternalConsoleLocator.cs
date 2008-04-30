//
// ExternalConsoleLocator.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Diagnostics;

namespace MonoDevelop.Core.Execution
{
	public class ExternalConsoleLocator
	{
		private delegate string TerminalRunnerHandler (string command, string args, string dir, string title, bool pause);
	
		private static string terminal_command;
		private static bool terminal_probed;
		private static TerminalRunnerHandler runner;
		
		public static ProcessStartInfo GetConsoleProcess (string command, string commandArguments, 
			string workingDirectory, string title, bool pauseWhenFinished)
		{
			ProbeTerminal ();
			
			string exec = runner (command, commandArguments, workingDirectory, title, pauseWhenFinished);
			Console.WriteLine ("{0} {1}", terminal_command, exec);
			
			ProcessStartInfo psi = new ProcessStartInfo (terminal_command, exec);
			psi.WorkingDirectory = workingDirectory;
			psi.UseShellExecute = false;
			
			return psi;
		}
		
#region Terminal runner implementations
		
		private static string GnomeTerminalRunner (string command, string args, string dir, string title, bool pause)
		{
			string extra_commands = pause 
				? BashPause.Replace ("'", "\\\"")
				: String.Empty;
			
			return String.Format (@" --title ""{4}"" -e ""bash -c 'cd {3} ; {0} {1} ; {2}'""",
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
			
			return String.Format (@" -title ""{4}"" -e ""cd {3} ; '{0}' {1} ; {2}""",
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

		private static void ProbeTerminal ()
		{
			if (terminal_probed) {
				return;
			}
			
			terminal_probed = true;
			
			string fallback_terminal = "xterm";
			string preferred_terminal;
			TerminalRunnerHandler preferred_runner = null;
			TerminalRunnerHandler fallback_runner = XtermRunner;

			switch (FindDesktopEnvironment ()) {
			   case DesktopEnvironment.Gnome: 
					preferred_terminal = "gnome-terminal";
					preferred_runner = GnomeTerminalRunner;
					break;
				/*case DesktopEnvironment.Kde: 
					preferred_terminal = "konsole"; 
					preferred_runner = KonsoleRunner;
					break;*/
				case DesktopEnvironment.Unknown: 
				default:
					preferred_terminal = fallback_terminal;
					preferred_runner = fallback_runner;
					break;
			}

			terminal_command = FindExec (preferred_terminal);
			if (terminal_command != null) {
				runner = preferred_runner;
				return;
			}
			
			FindExec (fallback_terminal);
			runner = fallback_runner;
		}

		private static string FindExec (string command)
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

		private static string [] GetExecPaths ()
		{
			string path = Environment.GetEnvironmentVariable ("PATH");
			if (String.IsNullOrEmpty (path)) {
				return new string [] { "/bin", "/usr/bin", "/usr/local/bin" };
			}

			// this is super lame, should handle quoting/escaping
			return path.Split (':');
		}

		private enum DesktopEnvironment 
		{
			Unknown,
			Gnome,
			Kde,
			Xfce
		}

		private static DesktopEnvironment FindDesktopEnvironment ()
		{
			// Logic from XDG Utils
			if (Environment.GetEnvironmentVariable ("KDE_FULL_SESSION") == "true") {
				return DesktopEnvironment.Kde;
			} else if (!String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("GNOME_DESKTOP_SESSION_ID"))) {
				return DesktopEnvironment.Gnome;
			}

			// xprop -root _DT_SAVE_MODE | grep ' = \"xfce4\"$'
			// Will detect XFCE - should add that and probably
			// OS X desktop environment detection

			return DesktopEnvironment.Unknown;
		}

#endregion
		
	}
}
