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
		static string configFile = Path.Combine (PropertyService.ConfigPath, "mono-runtimes.xml");
		
		static MonoTargetRuntimeFactory ()
		{
			LoadRuntimes ();
		}
		
		public IEnumerable<TargetRuntime> CreateRuntimes ()
		{
			MonoRuntimeInfo currentRuntime = MonoRuntimeInfo.FromCurrentRuntime ();
			if (currentRuntime != null) {
				yield return new MonoTargetRuntime (currentRuntime);
			}
			if (PropertyService.IsWindows) {
				string progs = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles);
				foreach (string dir in Directory.GetDirectories (progs, "Mono-")) {
					MonoRuntimeInfo info = new MonoRuntimeInfo (dir);
					if (info.IsValidRuntime)
						yield return new MonoTargetRuntime (info);
				}
			} else if (PropertyService.IsMac) {
				foreach (string dir in Directory.GetDirectories ("/Library/Frameworks/Mono.framework/Versions")) {
					if (dir.EndsWith ("/Current") || currentRuntime.Prefix == dir)
						continue;
					MonoRuntimeInfo info = new MonoRuntimeInfo (dir);
					if (info.IsValidRuntime)
						yield return new MonoTargetRuntime (info);
				}
			} else {
				foreach (MonoRuntimeInfo info in customRuntimes)
					yield return new MonoTargetRuntime (info);
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
