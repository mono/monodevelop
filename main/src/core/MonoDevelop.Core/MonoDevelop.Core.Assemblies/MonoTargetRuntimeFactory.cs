// 
// MonoTargetRuntimeFactory.cs
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
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Core.Assemblies
{
	class MonoTargetRuntimeFactory: ITargetRuntimeFactory
	{
		static RuntimeCollection customRuntimes = new RuntimeCollection ();
		static string configFile = UserProfile.Current.ConfigDir.Combine ("mono-runtimes.xml");
		static string[] commonLinuxPrefixes = new string[] { "/usr", "/usr/local", "/app" };
		
		static MonoTargetRuntimeFactory ()
		{
			LoadRuntimes ();
		}
		
		const string MAC_FRAMEWORK_DIR = "/Library/Frameworks/Mono.framework/Versions";

		public IEnumerable<TargetRuntime> CreateRuntimes ()
		{
			MonoRuntimeInfo currentRuntime = MonoRuntimeInfo.FromCurrentRuntime ();
			if (currentRuntime != null) {
				yield return new MonoTargetRuntime (currentRuntime);
			}
			if (Platform.IsWindows) {
				string progs = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles);
				foreach (string dir in Directory.EnumerateDirectories (progs, "Mono*")) {
					MonoRuntimeInfo info = new MonoRuntimeInfo (dir);
					if (info.IsValidRuntime)
						yield return new MonoTargetRuntime (info);
				}
			} else if (Platform.IsMac) {
				if (!Directory.Exists (MAC_FRAMEWORK_DIR))
					yield break;
				foreach (string dir in Directory.EnumerateDirectories (MAC_FRAMEWORK_DIR)) {
					if (dir.EndsWith ("/Current", StringComparison.Ordinal) || currentRuntime.Prefix == dir)
						continue;
					MonoRuntimeInfo info = new MonoRuntimeInfo (dir);
					if (info.IsValidRuntime)
						yield return new MonoTargetRuntime (info);
				}
			} else {
				foreach (string pref in commonLinuxPrefixes) {
					if (currentRuntime != null && currentRuntime.Prefix == pref)
						continue;
					MonoRuntimeInfo info = new MonoRuntimeInfo (pref);
					if (info.IsValidRuntime) {
						// Clean up old registered runtimes
						foreach (MonoRuntimeInfo ei in customRuntimes) {
							if (ei.Prefix == info.Prefix) {
								customRuntimes.Remove (ei);
								break;
							}
						}
						yield return new MonoTargetRuntime (info);
					}
				}
			}
			foreach (MonoRuntimeInfo info in customRuntimes) {
				MonoTargetRuntime rt = new MonoTargetRuntime (info);
				rt.UserDefined = true;
				yield return rt;
			}
		}
		
		public static TargetRuntime RegisterRuntime (MonoRuntimeInfo info)
		{
			MonoTargetRuntime tr = new MonoTargetRuntime (info);
			Runtime.SystemAssemblyService.RegisterRuntime (tr);
			customRuntimes.Add (info);
			SaveRuntimes ();
			return tr;
		}
		
		public static void UnregisterRuntime (MonoTargetRuntime runtime)
		{
			Runtime.SystemAssemblyService.UnregisterRuntime (runtime);
			foreach (MonoRuntimeInfo ri in customRuntimes) {
				if (ri.Prefix == runtime.MonoRuntimeInfo.Prefix) {
					customRuntimes.Remove (ri);
					break;
				}
			}
			SaveRuntimes ();
		}
		
		static void LoadRuntimes ()
		{
			if (!File.Exists (configFile))
				return;
			try {
				XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
				using (StreamReader sr = new StreamReader (configFile)) {
					customRuntimes = (RuntimeCollection) ser.Deserialize (sr, typeof(RuntimeCollection));
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error while loading mono-runtimes.xml.", ex);
			}
		}
		
		static void SaveRuntimes ()
		{
			try {
				XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
				using (StreamWriter sw = new StreamWriter (configFile)) {
					ser.Serialize (sw, customRuntimes);
				}
			} catch {
			}
		}
	}
	
	class RuntimeCollection: List<MonoRuntimeInfo>
	{
	}
}
