// GdbSessionFactory.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Debugging.Client;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Debugger.Gdb
{
	class GdbDebuggerBackend : DebuggerEngineBackend
	{
		struct FileData
		{
			public DateTime LastCheck;
			public bool IsExe;
		}

		Dictionary<string, FileData> fileCheckCache = new Dictionary<string, FileData> ();

		public override bool CanDebugCommand (ExecutionCommand cmd)
		{
			if (!(cmd is NativeExecutionCommand nativeCmd))
				return false;

			string file = FindFile (nativeCmd.Command);
			if (!File.Exists (file)) {
				// The provided file is not guaranteed to exist. If it doesn't
				// we assume we can execute it because otherwise the run command
				// in the IDE will be disabled, and that's not good because that
				// command will build the project if the exec doesn't yet exist.
				return true;
			}

			file = Path.GetFullPath (file);
			DateTime currentTime = File.GetLastWriteTime (file);

			if (fileCheckCache.TryGetValue (file, out var data)) {
				if (data.LastCheck == currentTime)
					return data.IsExe;
			}
			data.LastCheck = currentTime;
			try {
				data.IsExe = IsExecutable (file);
			} catch {
				data.IsExe = false;
			}
			fileCheckCache [file] = data;
			return data.IsExe;
		}

		public override DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand cmd)
		{
			var nativeCmd = (NativeExecutionCommand)cmd;
			var startInfo = new DebuggerStartInfo {
				Command = nativeCmd.Command,
				Arguments = nativeCmd.Arguments,
				WorkingDirectory = nativeCmd.WorkingDirectory
			};
			if (nativeCmd.EnvironmentVariables.Count > 0) {
				foreach (KeyValuePair<string, string> val in nativeCmd.EnvironmentVariables)
					startInfo.EnvironmentVariables [val.Key] = val.Value;
			}
			return startInfo;
		}

		internal static bool IsExecutable (string file)
		{
			// HACK: this is a quick but not very reliable way of checking if a file
			// is a native executable. Actually, we are interested in checking that
			// the file is not a script.
			using (var sr = new StreamReader (file)) {
				char [] chars = new char [3];
				int n = 0, nr = 0;
				while (n < chars.Length && (nr = sr.ReadBlock (chars, n, chars.Length - n)) != 0)
					n += nr;
				if (nr != chars.Length)
					return true;
				if (chars [0] == '#' && chars [1] == '!')
					return false;
			}
			return true;
		}

		public override DebuggerSession CreateSession ()
		{
			return new GdbSession ();
		}

		public override ProcessInfo [] GetAttachableProcesses ()
		{
			var procs = new List<ProcessInfo> ();
			foreach (string dir in Directory.GetDirectories ("/proc")) {
				if (!int.TryParse (Path.GetFileName (dir), out var id))
					continue;
				try {
					File.ReadAllText (Path.Combine (dir, "sessionid"));
				} catch {
					continue;
				}
				string cmdline = File.ReadAllText (Path.Combine (dir, "cmdline"));
				cmdline = cmdline.Replace ('\0', ' ');
				procs.Add (new ProcessInfo (id, cmdline));
			}
			return procs.ToArray ();
		}

		static string FindFile (string cmd)
		{
			if (Path.IsPathRooted (cmd))
				return cmd;
			string pathVar = Environment.GetEnvironmentVariable ("PATH");
			string [] paths = pathVar.Split (Path.PathSeparator);
			foreach (string path in paths) {
				string file = Path.Combine (path, cmd);
				if (File.Exists (file))
					return file;
			}
			return cmd;
		}
	}

	[Obsolete ("Should not have been public")]
	public class GdbSessionFactory: IDebuggerEngine
	{
		readonly GdbDebuggerBackend backend = new GdbDebuggerBackend ();

		public bool CanDebugCommand (ExecutionCommand command)
		{
			return backend.CanDebugCommand (command);
		}
		
		public DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand command)
		{
			return backend.CreateDebuggerStartInfo (command);
		}
		
		public bool IsExecutable (string file)
		{
			return GdbDebuggerBackend.IsExecutable (file);
		}

		public DebuggerSession CreateSession ()
		{
			return backend.CreateSession ();
		}
		
		public ProcessInfo[] GetAttachableProcesses ()
		{
			return backend.GetAttachableProcesses ();
		}
	}
}
