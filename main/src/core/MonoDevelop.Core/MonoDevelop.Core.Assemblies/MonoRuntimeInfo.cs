// 
// MonoRuntimeInfo.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Core.Assemblies
{
	public class MonoRuntimeInfo
	{
		[ItemProperty]
		string prefix;
		
		string monoVersion = "Unknown";
		Dictionary<string,string> envVars = new Dictionary<string, string> ();
		bool initialized;
		bool isValidRuntime;
		
		internal MonoRuntimeInfo ()
		{
		}
		
		public MonoRuntimeInfo (string prefix)
		{
			this.prefix = prefix;
			Initialize ();
		}
		
		public string Prefix {
			get { return prefix; }
		}
		
		/// <summary>
		/// This string is strictly for displaying to the user or logging. It should never be used for version checks.
		/// </summary>
		public string MonoVersion {
			get {
				Initialize ();
				return monoVersion;
			}
		}
		
		public string DisplayName {
			get { return "Mono " + MonoVersion + " (" + prefix + ")"; }
		}
		
		public bool IsValidRuntime {
			get {
				Initialize ();
				return isValidRuntime; 
			}
		}
		
		void Initialize ()
		{
			if (!initialized) {
				initialized = true;
				isValidRuntime = InternalInitialize ();
			}
		}
		
		bool InternalInitialize ()
		{
			string libDir = Path.Combine (prefix, "lib");
			if (!Directory.Exists (Path.Combine (libDir, "mono")))
				return false;
			string binDir = Path.Combine (prefix, "bin");
			envVars ["PATH"] = libDir + Path.PathSeparator + binDir + Path.PathSeparator + Environment.GetEnvironmentVariable ("PATH");
			envVars ["LD_LIBRARY_PATH"] = libDir + Path.PathSeparator + Environment.GetEnvironmentVariable ("LD_LIBRARY_PATH");
			
			// Avoid inheriting this var from the current process environment
			envVars ["MONO_PATH"] = string.Empty;
			
			StringWriter output = new StringWriter ();
			try {
				string monoPath = Path.Combine (prefix, "bin");
				monoPath = Path.Combine (monoPath, "mono");
				ProcessStartInfo pi = new ProcessStartInfo (monoPath, "--version");
				pi.UseShellExecute = false;
				pi.RedirectStandardOutput = true;
				foreach (KeyValuePair<string,string> var in envVars) {
					pi.EnvironmentVariables [var.Key] = var.Value;
				}
				ProcessWrapper p = Runtime.ProcessService.StartProcess (pi, output, null, null);
				p.WaitForOutput ();
			} catch {
				return false;
			}
			
			//set up paths using the prefix
			SetupPkgconfigPaths (null, null);
			
			string ver = output.ToString ();
			int i = ver.IndexOf ("version");
			if (i == -1)
				return false;
			i += 8;
			int j = ver.IndexOf (' ', i);
			if (j == -1)
				return false;
			
			monoVersion = ver.Substring (i, j - i);
			
			return true;
		}
		
		internal Dictionary<string,string> GetEnvironmentVariables ()
		{
			Initialize ();
			return envVars;
		}
		
		internal bool IsRunning { get; private set; }
		
		public static MonoRuntimeInfo FromCurrentRuntime ()
		{
			// Get the current mono version
			Type t = Type.GetType ("Mono.Runtime");
			if (t == null)
				return null;
			
			MonoRuntimeInfo rt = new MonoRuntimeInfo ();
			
			// Since Mono 3.0.5/ee035e3eb463816a9590b09f65842503640c6337 GetDisplayName is public
			string ver = (string) t.InvokeMember ("GetDisplayName", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, null, null);
			
			//not sure which version this old scheme applies to
			int i = ver.IndexOf ("/branches/mono-");
			if (i != -1) {
				i += 15;
				int j = ver.IndexOf ('/', i);
				if (j != -1)
					rt.monoVersion = ver.Substring (i, j - i).Replace ('-','.');
			} else if (ver.StartsWith ("/trunk/mono ")) {
				rt.monoVersion = "Trunk";
			}
			
			if (rt.monoVersion == "Unknown") {
				i = ver.IndexOf (' ');
				//this schme applies to mono 2.6.3+
				if (ver.Length > i && ver[i+1] == '(')
					rt.monoVersion = ver.Substring (0, i);
				//not sure how old this scheme is
				else
					rt.monoVersion = ver.Substring (i+1);
			}

			//Pull up assemblies from the installed mono system.
			rt.prefix = PathUp (typeof (int).Assembly.Location, 4);
			if (rt.prefix == null)
				throw new SystemException ("Could not detect Mono prefix");
			
			rt.SetupPkgconfigPaths (Environment.GetEnvironmentVariable ("PKG_CONFIG_PATH"),
			                        Environment.GetEnvironmentVariable ("PKG_CONFIG_LIBDIR"));
			
			foreach (string varName in new [] { "PATH", "MONO_GAC_PREFIX", "XBUILD_FRAMEWORK_FOLDERS_PATH" }) {
				rt.envVars [varName] = Environment.GetEnvironmentVariable (varName);
			}
			
			rt.IsRunning = true;
			rt.initialized = true;
			rt.isValidRuntime = true;
			
			return rt;
		}
		
		void SetupPkgconfigPaths (string pkgConfigPath, string pkgConfigLibdir)
		{
			IEnumerable<string> paths = MonoTargetRuntime.PcFileCache.GetPkgconfigPaths (prefix, pkgConfigPath, pkgConfigLibdir);
			
			//NOTE: we set PKG_CONFIG_LIBDIR so that pkgconfig has the exact set of paths MD is using
			envVars ["PKG_CONFIG_PATH"] = String.Join (Path.PathSeparator.ToString (), System.Linq.Enumerable.ToArray (paths));
			envVars ["PKG_CONFIG_LIBDIR"] = "";
		}
		
		#region Path utilities
		
		static string PathUp (string path, int up)
		{
			if (up == 0)
				return path;
			for (int i = path.Length -1; i >= 0; i--) {
				if (path[i] == Path.DirectorySeparatorChar) {
					up--;
					if (up == 0)
						return path.Substring (0, i);
				}
			}
			return null;
		}
		
		#endregion
	}
}
