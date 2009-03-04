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
using Mono.Debugging.Backend;

namespace MonoDevelop.Debugger.Gdb
{
	public class GdbSessionFactory: IDebuggerEngine
	{
		struct FileData {
			public DateTime LastCheck;
			public bool IsExe;
		}
		
		Dictionary<string,FileData> fileCheckCache = new Dictionary<string, FileData> ();
		
		public string Name {
			get { return "GNU Debugger (GDB)"; }
		}
		
		public bool CanDebugPlatform (string platformId)
		{
			return platformId == "Native";
		}
		
		public bool CanDebugFile (string file)
		{
			string ext = System.IO.Path.GetExtension (file).ToLower ();
			if (ext == ".exe" || ext == ".dll")
				return false;
			file = FindFile (file);
			if (!File.Exists (file)) {
				// The provided file is not guaranteed to exist. If it doesn't
				// we assume we can execute it because otherwise the run command
				// in the IDE will be disabled, and that's not good because that
				// command will build the project if the exec doesn't yet exist.
				return true;
			}
			
			file = Path.GetFullPath (file);
			DateTime currentTime = File.GetLastWriteTime (file);
				
			FileData data;
			if (fileCheckCache.TryGetValue (file, out data)) {
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
		
		public bool IsExecutable (string file)
		{
			// HACK: this is a quick but not very reliable way of checking if a file
			// is a native executable. Actually, we are interested in checking that
			// the file is not a script.
			using (StreamReader sr = new StreamReader (file)) {
				char[] chars = new char[3];
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

		public DebuggerFeatures SupportedFeatures {
			get {
				return DebuggerFeatures.All & ~DebuggerFeatures.Catchpoints;
			}
		}
		
		public DebuggerSession CreateSession ()
		{
			GdbSession ds = new GdbSession ();
			return ds;
		}
		
		public ProcessInfo[] GetAttachablePocesses ()
		{
			List<ProcessInfo> procs = new List<ProcessInfo> ();
			foreach (string dir in Directory.GetDirectories ("/proc")) {
				int id;
				if (!int.TryParse (Path.GetFileName (dir), out id))
					continue;
				try {
					File.ReadAllText (Path.Combine (dir, "sessionid"));
				} catch {
					continue;
				}
				string cmdline = File.ReadAllText (Path.Combine (dir, "cmdline"));
				cmdline = cmdline.Replace ('\0',' ');
				ProcessInfo pi = new ProcessInfo (id, cmdline);
				procs.Add (pi);
			}
			return procs.ToArray ();
		}
		
		string FindFile (string cmd)
		{
			if (Path.IsPathRooted (cmd))
				return cmd;
			string pathVar = Environment.GetEnvironmentVariable ("PATH");
			string[] paths = pathVar.Split (Path.PathSeparator);
			foreach (string path in paths) {
				string file = Path.Combine (path, cmd);
				if (File.Exists (file))
					return file;
			}
			return cmd;
		}
	}
}
