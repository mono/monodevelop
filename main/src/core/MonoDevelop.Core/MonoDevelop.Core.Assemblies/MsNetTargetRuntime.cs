// 
// MsNetTargetRuntime.cs
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
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Core.Assemblies
{
	class MsNetTargetRuntime: TargetRuntime
	{
		FilePath rootDir;
		FilePath newFxDir;
		FilePath gacDir;
		bool running;
		MsNetExecutionHandler execHandler;
		
		public MsNetTargetRuntime (bool running)
		{
			string winDir = Path.GetFullPath (Environment.SystemDirectory + "\\..");
			rootDir = winDir + "\\Microsoft.NET\\Framework";
			newFxDir = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles);
			newFxDir = newFxDir + "\\Reference Assemblies\\Microsoft\\Framework";
			gacDir = winDir + "\\assembly\\GAC";
			this.running = running;
			execHandler = new MsNetExecutionHandler ();
		}
		
		public override string DisplayRuntimeName {
			get {
				return "Microsoft .NET";
			}
		}

		public override string RuntimeId {
			get {
				return "MS.NET";
			}
		}
		
		public override string Version {
			get {
				return "";
			}
		}
		
		protected override void OnInitialize ()
		{
			RegistryKey foldersKey = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\.NETFramework\AssemblyFolders", false);
			foreach (string key in foldersKey.GetSubKeyNames ()) {
				if (key.StartsWith ("Microsoft .NET Framework"))
					continue; // Framework assemblies
				RegistryKey fk = foldersKey.OpenSubKey (key, false);
				string folder = fk.GetValue ("") as string;
				string version = fk.GetValue ("version") as string ?? "";
				if (!string.IsNullOrEmpty (folder))
					AddPackage (key, version, folder);
				fk.Close ();
			}
			foldersKey.Close ();
		}
		
		void AddPackage (string name, string version, string folder)
		{
			SystemPackageInfo pinfo = new SystemPackageInfo ();
			pinfo.Name = name;
			pinfo.Description = name;
			pinfo.Version = version;
			RegisterPackage (pinfo, false, Directory.GetFiles (folder, "*.dll"));
		}

		public override bool IsRunning {
			get {
				return running;
			}
		}

		public override Dictionary<string, string> GetToolsEnvironmentVariables (TargetFramework fx)
		{
			Dictionary<string, string> vars = new Dictionary<string, string> ();
			string path = Environment.GetEnvironmentVariable ("PATH");
			vars["PATH"] = GetFrameworkToolsPath (fx) + Path.PathSeparator + path;
			return vars;
		}
		
		protected override IEnumerable<string> GetGacDirectories ()
		{
			yield return gacDir;
		}

		string GetClrVersion (ClrVersion v)
		{
			switch (v) {
				case ClrVersion.Net_1_1: return "v1.1.4322";
				case ClrVersion.Net_2_0: return "v2.0.50727";
				case ClrVersion.Clr_2_1: return "v2.1";
				case ClrVersion.Net_4_0: return "v4.0.20506";
			}
			return "?";
		}
		
		protected override string GetFrameworkFolder (TargetFramework fx)
		{
			switch (fx.Id) {
				case "1.1":
				case "2.0": return rootDir.Combine (GetClrVersion (fx.ClrVersion));
			}

			RegistryKey fxFolderKey = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\.NETFramework\AssemblyFolders\v" + fx.Id, false);
			if (fxFolderKey != null) {
				string folder = fxFolderKey.GetValue ("All Assemblies In") as string;
				fxFolderKey.Close ();
				return folder;
			}
			return "";
		}
		
		public override IExecutionHandler GetExecutionHandler ()
		{
			return execHandler;
		}

		string GetFrameworkToolsPath (TargetFramework fx)
		{
			if (fx.Id == "1.1" || fx.Id == "2.0" || fx.Id == "4.0")
				return rootDir.Combine (GetClrVersion (fx.ClrVersion));

			if (fx.Id == "3.0")
				return rootDir.Combine (GetClrVersion (ClrVersion.Net_2_0));
 
			return rootDir.Combine ("v" + fx.Id);
		}

		public override IEnumerable<string> GetToolsPaths (TargetFramework fx)
		{
			string path = GetFrameworkToolsPath (fx);
			if (path != null)
				yield return path;

			string sdkPath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles), "Microsoft SDKs");
			sdkPath = Path.Combine (sdkPath, "Windows");
			if (fx.Id == "4.0")
				yield return Path.Combine (sdkPath, "v7.0A\\bin\\NETFX 4.0 Tools");
			else if (fx.Id == "3.5")
				yield return Path.Combine (sdkPath, "v7.0A\\bin");
			else
				yield return Path.Combine (sdkPath, "v6.0A");

			foreach (string s in base.GetToolsPaths (fx))
				yield return s;
		}
	}
}
