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
	public class MsNetTargetRuntime: TargetRuntime
	{
		FilePath rootDir;
		FilePath newFxDir;
		bool running;
		MsNetExecutionHandler execHandler;
		string winDir;
		
		public MsNetTargetRuntime (bool running)
		{
			winDir = Path.GetFullPath (Environment.SystemDirectory + "\\..");
			rootDir = winDir + "\\Microsoft.NET\\Framework";
			newFxDir = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles);
			newFxDir = newFxDir + "\\Reference Assemblies\\Microsoft\\Framework";
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
		
		public FilePath RootDirectory {
			get { return rootDir; }
		}
		
		public override string GetAssemblyDebugInfoFile (string assemblyPath)
		{
			return Path.ChangeExtension (assemblyPath, ".pdb");
		}

		
		protected override void OnInitialize ()
		{
			RegistryKey foldersKey = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\.NETFramework\AssemblyFolders", false);
			foreach (string key in foldersKey.GetSubKeyNames ()) {
				if (ShuttingDown)
					return;
				if (key.StartsWith ("Microsoft .NET Framework"))
					continue; // Framework assemblies
				RegistryKey fk = foldersKey.OpenSubKey (key, false);
				string folder = fk.GetValue ("") as string;
				string version = fk.GetValue ("version") as string ?? "";
				if (!string.IsNullOrEmpty (folder))
					AddPackage (key, version, folder, null);
				fk.Close ();
			}
			foldersKey.Close ();

			// Extended assembly folders

			foreach (TargetFramework fx in Runtime.SystemAssemblyService.GetTargetFrameworks ()) {
				if (ShuttingDown)
					return;
				RegistryKey fxKey = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\.NETFramework\v" + fx.Id + @"\AssemblyFoldersEx", false);
				if (fxKey != null) {
					AddPackages (fx, fxKey);
					fxKey.Close ();
				}

				string clrVer = MsNetFrameworkBackend.GetClrVersion (fx.ClrVersion);
				if (clrVer.StartsWith ("v" + fx.Id)) {
					// Several frameworks can share the same clr version. Make sure only one registers the assemblies.
					fxKey = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\.NETFramework\" + clrVer + @"\AssemblyFoldersEx", false);
					if (fxKey != null) {
						AddPackages (fx, fxKey);
						fxKey.Close ();
					}
				}
			}
		}

		void AddPackages (TargetFramework fx, RegistryKey fxKey)
		{
			foreach (string key in fxKey.GetSubKeyNames ()) {
				if (ShuttingDown)
					return;
				RegistryKey fk = fxKey.OpenSubKey (key, false);
				string folder = fk.GetValue ("") as string;
				string version = fk.GetValue ("version") as string ?? "";
				if (!string.IsNullOrEmpty (folder))
					AddPackage (key, version, folder, fx);
				fk.Close ();
			}
		}
		
		public override string GetMSBuildBinPath (TargetFramework fx)
		{
			RegistryKey msb = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\MSBuild\ToolsVersions\" + fx.Id, false);
			if (msb != null) {
				string path = msb.GetValue ("MSBuildToolsPath") as string;
				if (path != null)
					return path;
			}
			return rootDir.Combine ("v2.0.50727");
		}
		
		void AddPackage (string name, string version, string folder, TargetFramework fx)
		{
			SystemPackageInfo pinfo = new SystemPackageInfo ();
			pinfo.Name = name;
			pinfo.Description = name;
			pinfo.Version = version;
			pinfo.TargetFramework = fx != null ? fx.Id : null;
			try {
				if (Directory.Exists (folder))
					RegisterPackage (pinfo, false, Directory.GetFiles (folder, "*.dll"));
			}
			catch (Exception ex) {
				LoggingService.LogError ("Error while scanning assembly folder '" + folder + "'", ex);
			}
		}

		public override bool IsRunning {
			get {
				return running;
			}
		}

		internal protected override IEnumerable<string> GetGacDirectories ()
		{
			FilePath gacDir = winDir + "\\assembly\\GAC";
			if (Directory.Exists (gacDir))
				yield return gacDir;
			if (Directory.Exists (gacDir + "_32"))
				yield return gacDir + "_32";
			if (Directory.Exists (gacDir + "_64"))
				yield return gacDir + "_64";
			if (Directory.Exists (gacDir + "_MSIL"))
				yield return gacDir + "_MSIL";
			
			gacDir = winDir + "\\Microsoft.NET\\assembly\\GAC";
			if (Directory.Exists (gacDir))
				yield return gacDir;
			if (Directory.Exists (gacDir + "_32"))
				yield return gacDir + "_32";
			if (Directory.Exists (gacDir + "_64"))
				yield return gacDir + "_64";
			if (Directory.Exists (gacDir + "_MSIL"))
				yield return gacDir + "_MSIL";
		}

		public override IExecutionHandler GetExecutionHandler ()
		{
			return execHandler;
		}
		
		protected override TargetFrameworkBackend CreateBackend (TargetFramework fx)
		{
			return new MsNetFrameworkBackend ();
		}
	}
}
