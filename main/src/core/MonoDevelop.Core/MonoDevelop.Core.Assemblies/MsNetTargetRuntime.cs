﻿// 
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
using Microsoft.Build.MSBuildLocator;
using Microsoft.Win32;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Core.Assemblies
{
	public class MsNetTargetRuntime: TargetRuntime
	{
		FilePath rootDir;
		FilePath newFxDir;
		FilePath msbuildDir;
		bool running;
		MsNetExecutionHandler execHandler;
		string winDir;

		// ProgramFilesX86 is broken on 32-bit WinXP, this is a workaround
		static string GetProgramFilesX86 ()
		{
			return Environment.GetFolderPath (IntPtr.Size == 8?
				Environment.SpecialFolder.ProgramFilesX86 : Environment.SpecialFolder.ProgramFiles);
		}
		
		public MsNetTargetRuntime (bool running)
		{
			winDir = Path.GetFullPath (Environment.SystemDirectory + "\\..");
			rootDir = winDir + "\\Microsoft.NET\\Framework";
			
			string programFilesX86 = GetProgramFilesX86 ();
			newFxDir = programFilesX86 + "\\Reference Assemblies\\Microsoft\\Framework";
			msbuildDir = GetMSBuildBinPath ("15.0"); // C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\bin
			msbuildDir = Path.GetDirectoryName (Path.GetDirectoryName (msbuildDir)); // C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild
			
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
		
		public override IEnumerable<FilePath> GetReferenceFrameworkDirectories ()
		{
			yield return newFxDir;
		}

		[Obsolete("Use DotNetProject.GetAssemblyDebugInfoFile()")]
		public override string GetAssemblyDebugInfoFile (string assemblyPath)
		{
			return Path.ChangeExtension (assemblyPath, ".pdb");
		}
		
		protected override void OnInitialize ()
		{
			RegistryKey foldersKey = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\.NETFramework\AssemblyFolders", false);
			if (foldersKey != null) {
				foreach (string key in foldersKey.GetSubKeyNames ()) {
					if (ShuttingDown)
						return;
					if (key.StartsWith ("Microsoft .NET Framework", StringComparison.Ordinal))
						continue; // Framework assemblies
					RegistryKey fk = foldersKey.OpenSubKey (key, false);
					string folder = fk.GetValue ("") as string;
					string version = fk.GetValue ("version") as string ?? "";
					if (!string.IsNullOrEmpty (folder))
						AddPackage (key, version, folder, null);
					fk.Close ();
				}
				foldersKey.Close ();
			}

			// Extended assembly folders

			foreach (TargetFramework fx in Runtime.SystemAssemblyService.GetKnownFrameworks ()) {
				if (fx.Id.Identifier != ".NETFramework")
					continue;
				if (ShuttingDown)
					return;
				RegistryKey fxKey = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\.NETFramework\v" + fx.Id.Version + @"\AssemblyFoldersEx", false);
				if (fxKey != null) {
					AddPackages (fx, fxKey);
					fxKey.Close ();
				}

				string clrVer = GetClrVersion (fx.Id);
				if (clrVer != null && clrVer.StartsWith ("v" + fx.Id.Version, StringComparison.Ordinal)) {
					// Several frameworks can share the same clr version. Make sure only one registers the assemblies.
					fxKey = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\.NETFramework\" + clrVer + @"\AssemblyFoldersEx", false);
					if (fxKey != null) {
						AddPackages (fx, fxKey);
						fxKey.Close ();
					}
				}
			}
		}
		
		static string GetClrVersion (TargetFrameworkMoniker id)
		{
			if (id.Identifier != TargetFrameworkMoniker.ID_NET_FRAMEWORK) {
				return null;
			}

			switch (id.Version) {
				case "2.0": return "v2.0.50727";
				// The 4.5 binaries have the same version as the 4.0 binaries
				case "4.0":
				case "4.5": return "v4.0.30319";
				default: return null;
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
		
		public override string GetMSBuildBinPath (string toolsVersion)
		{
			var instance = MSBuildLocator.QueryVisualStudioInstances (new VisualStudioInstanceQueryOptions ()).MaxValueOrDefault (vs => vs.Version);
			if (instance != null)
				return instance.MSBuildPath;

			using (RegistryKey msb = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\MSBuild\ToolsVersions\" + toolsVersion, false)) {
				if (msb != null) {
					if (msb.GetValue ("MSBuildToolsPath") is string path && File.Exists (Path.Combine (path, "MSBuild.exe")))
						return path;
				}
				return null;
			}
		}

		public override string GetMSBuildToolsPath (string toolsVersion)
		{
			return GetMSBuildBinPath (toolsVersion);
		}
		
		public override string GetMSBuildExtensionsPath ()
		{
			return msbuildDir;
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
