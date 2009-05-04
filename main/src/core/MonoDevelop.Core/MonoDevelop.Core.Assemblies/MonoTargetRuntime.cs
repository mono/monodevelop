// 
// MonoTargetRuntime.cs
//  
// Author:
//   Todd Berman <tberman@sevenl.net>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (C) 2004 Todd Berman
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Threading;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Serialization;
using Mono.Addins;

namespace MonoDevelop.Core.Assemblies
{
	public class MonoTargetRuntime: TargetRuntime
	{
		bool reportedPkgConfigNotFound = false;
		string monoVersion;
		string monoDir;
		MonoPlatformExecutionHandler execHandler;
		Dictionary<string,string> environmentVariables;
		
		static PcFileCache pcFileCache = new PcFileCache ();
		
		MonoRuntimeInfo monoRuntimeInfo;
		
		internal MonoTargetRuntime (MonoRuntimeInfo info)
		{
			this.monoVersion = info.MonoVersion;
			this.monoDir = Path.Combine (Path.Combine (info.Prefix, "lib"), "mono");
			environmentVariables = info.GetEnvironmentVariables ();
			monoRuntimeInfo = info;
		}
		
		internal MonoRuntimeInfo MonoRuntimeInfo {
			get { return monoRuntimeInfo; }
		}
		
		public string Prefix {
			get { return monoRuntimeInfo.Prefix; }
		}
		
		public override bool IsRunning {
			get {
				return monoRuntimeInfo.IsRunning;
			}
		}
		
		public override string RuntimeId {
			get {
				return "Mono";
			}
		}
		
		public override string Version {
			get {
				return monoVersion;
			}
		}
		
		public override IExecutionHandler GetExecutionHandler ()
		{
			if (execHandler == null) {
				string monoPath = Path.Combine (Path.Combine (MonoRuntimeInfo.Prefix, "bin"), "mono");
				execHandler = new MonoPlatformExecutionHandler (monoPath, environmentVariables);
			}
			return execHandler;
		}

		public override IEnumerable<string> GetToolsPaths (TargetFramework fx)
		{
			yield return Path.Combine (MonoRuntimeInfo.Prefix, "bin");
		}
		
		protected override IEnumerable<string> GetGacDirectories ()
		{
			yield return Path.Combine (monoDir, "gac");
			
			string gacs;
			if (environmentVariables.TryGetValue ("MONO_GAC_PREFIX", out gacs)) {
				foreach (string path in gacs.Split (new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
					yield return path;
			}
		}
		
		//NOTE: mcs, etc need to use the env vars too 	 
		public override Dictionary<string, string> GetToolsEnvironmentVariables ()
		{
			return environmentVariables;
		}
		
		protected override string GetFrameworkFolder (TargetFramework fx)
		{
			string subdir;
			switch (fx.Id) {
				case "1.1":
					subdir = "1.0"; break;
				case "3.5":
					subdir = "2.0"; break;
				default:
					subdir = fx.Id; break;
			}
			return Path.Combine (monoDir, subdir);
		}
		
		protected override SystemPackageInfo GetFrameworkPackageInfo (TargetFramework fx)
		{
			SystemPackageInfo info = base.GetFrameworkPackageInfo (fx);
			if (fx.Id == "3.0") {
				info.Name = "olive";
				info.IsCorePackage = false;
			} else
				info.Name = "mono";
			return info;
		}
		
		public IEnumerable<string> PkgConfigDirs {
			get { return PkgConfigPath.Split (Path.PathSeparator); }
		}
		
		public string PkgConfigPath {
			get { return environmentVariables ["PKG_CONFIG_LIBDIR"]; }
		}
		
		public IEnumerable<string> GetAllPkgConfigFiles ()
		{
			HashSet<string> packageNames = new HashSet<string> ();
			foreach (string pcdir in PkgConfigDirs)
				foreach (string pcfile in Directory.GetFiles (pcdir, "*.pc"))
					if (packageNames.Add (Path.GetFileNameWithoutExtension (pcfile)))
						yield return pcfile;
		}
		
		protected override void OnInitialize ()
		{
			foreach (string pcdir in PkgConfigDirs) {
				foreach (string pcfile in Directory.GetFiles (pcdir, "*.pc")) {
					try {
						ParsePCFile (pcfile);
					}
					catch (Exception ex) {
						LoggingService.LogError ("Could not parse file '" + pcfile + "'", ex);
					}
				}
			}
			pcFileCache.Save ();
		}

		private void ParsePCFile (string pcfile)
		{
			// Don't register the package twice
			string pname = Path.GetFileNameWithoutExtension (pcfile);
			if (GetPackage (pname) != null || IsCorePackage (pname))
				return;

			SystemPackageInfo pinfo;
			lock (pcFileCache.SyncRoot) {
				pinfo = pcFileCache.GetPackageInfo (pcfile);
				if (pinfo == null) {
					pinfo = GetPackageInfo (pcfile, pname);
					pcFileCache.StorePackageInfo (pcfile, pinfo);
				}
			}
			if (pinfo.IsValidPackage)
				RegisterPackage (pinfo, false, pinfo.Assemblies.ToArray ());
		}
		
		SystemPackageInfo GetPackageInfo (string pcfile, string pname)
		{
			SystemPackageInfo pinfo = new SystemPackageInfo ();
			pinfo.Name = pname;
			List<string> fullassemblies = null;
			bool gacPackageSet = false;
			
			using (StreamReader reader = new StreamReader (pcfile)) {
				string line;
				while ((line = reader.ReadLine ()) != null) {
					int i = line.IndexOf (':');
					int j = line.IndexOf ('=');
					i = Math.Min (i != -1 ? i : int.MaxValue, j != -1 ? j : int.MaxValue);
					if (i == int.MaxValue)
						continue;
					string var = line.ToLower ().Substring (0, i).Trim ();
					string value = line.Substring (i+1).Trim ();
					if (var == "libs" && value.IndexOf (".dll") != -1 && fullassemblies == null) {
						if (value.IndexOf ("-lib:") != -1 || value.IndexOf ("/lib:") != -1) {
							fullassemblies = GetAssembliesWithLibInfo (value, pcfile);
						} else {
							fullassemblies = GetAssembliesWithoutLibInfo (value, pcfile);
						}
					}
					else if (var == "libraries") {
						fullassemblies = GetAssembliesFromLibrariesVar (value, pcfile);
					}
					else if (var == "version") {
						pinfo.Version = value;
					}
					else if (var == "description") {
						pinfo.Description = value;
					}
					else if (var == "gacpackage") {
						value = value.ToLower ();
						pinfo.IsGacPackage = value == "yes" || value == "true";
						gacPackageSet = true;
					}
				}
			}
	
			if (fullassemblies == null)
				return pinfo;
			
			string pcDir = Path.GetDirectoryName (pcfile);
			string monoPrefix = Path.GetDirectoryName (Path.GetDirectoryName (pcDir));
			monoPrefix = Path.GetFullPath (monoPrefix + Path.DirectorySeparatorChar + "lib" + Path.DirectorySeparatorChar + "mono" + Path.DirectorySeparatorChar);

			List<PackageAssemblyInfo> list = new List<PackageAssemblyInfo> ();
			foreach (string assembly in fullassemblies) {
				string asm;
				if (Path.IsPathRooted (assembly))
					asm = Path.GetFullPath (assembly);
				else {
					if (Path.GetDirectoryName (assembly).Length == 0) {
						asm = assembly;
					} else {
						asm = Path.GetFullPath (Path.Combine (pcDir, assembly));
					}
				}
				if (File.Exists (asm)) {
					PackageAssemblyInfo pi = new PackageAssemblyInfo ();
					pi.File = asm;
					pi.UpdateFromFile (pi.File);
					list.Add (pi);
					if (!gacPackageSet && !asm.StartsWith (monoPrefix) && Path.IsPathRooted (asm)) {
						// Assembly installed outside $(prefix)/lib/mono. It is most likely not a gac package.
						gacPackageSet = true;
						pinfo.IsGacPackage = false;
					}
				}
			}
			pinfo.Assemblies = list;
			return pinfo;
		}
		
	
		private List<string> GetAssembliesWithLibInfo (string line, string file)
		{
			List<string> references = new List<string> ();
			List<string> libdirs = new List<string> ();
			List<string> retval = new List<string> ();
			foreach (string piece in line.Split (' ')) {
				if (piece.ToLower ().Trim ().StartsWith ("/r:") || piece.ToLower ().Trim ().StartsWith ("-r:")) {
					references.Add (ProcessPiece (piece.Substring (3).Trim (), file));
				} else if (piece.ToLower ().Trim ().StartsWith ("/lib:") || piece.ToLower ().Trim ().StartsWith ("-lib:")) {
					libdirs.Add (ProcessPiece (piece.Substring (5).Trim (), file));
				}
			}
	
			foreach (string refrnc in references) {
				foreach (string libdir in libdirs) {
					if (File.Exists (libdir + Path.DirectorySeparatorChar + refrnc)) {
						retval.Add (libdir + Path.DirectorySeparatorChar + refrnc);
					}
				}
			}
	
			return retval;
		}
		
		List<string> GetAssembliesFromLibrariesVar (string line, string file)
		{
			List<string> references = new List<string> ();
			foreach (string reference in line.Split (' ')) {
				if (!string.IsNullOrEmpty (reference))
					references.Add (ProcessPiece (reference, file));
			}
			return references;
		}
	
		private List<string> GetAssembliesWithoutLibInfo (string line, string file)
		{
			List<string> references = new List<string> ();
			foreach (string reference in line.Split (' ')) {
				if (reference.ToLower ().Trim ().StartsWith ("/r:") || reference.ToLower ().Trim ().StartsWith ("-r:")) {
					string final_ref = reference.Substring (3).Trim ();
					references.Add (ProcessPiece (final_ref, file));
				}
			}
			return references;
		}
	
		private string ProcessPiece (string piece, string pcfile)
		{
			int start = piece.IndexOf ("${");
			if (start == -1)
				return piece;
	
			int end = piece.IndexOf ("}");
			if (end == -1)
				return piece;
	
			string variable = piece.Substring (start + 2, end - start - 2);
			string interp = GetVariableFromPkgConfig (variable, Path.GetFileNameWithoutExtension (pcfile));
			return ProcessPiece (piece.Replace ("${" + variable + "}", interp), pcfile);
		}
		
		public string RunPkgConfigCommand (string arguments)
		{
			if (reportedPkgConfigNotFound)
				return string.Empty;
			ProcessStartInfo psi = new ProcessStartInfo ("pkg-config");
			psi.RedirectStandardOutput = true;
			psi.UseShellExecute = false;
			psi.EnvironmentVariables["PKG_CONFIG_PATH"] = PkgConfigPath;
			
			psi.Arguments = arguments;
			Process p = new Process ();
			p.StartInfo = psi;
			string ret = string.Empty;
			try {
				p.Start ();
				ret = p.StandardOutput.ReadToEnd ().Trim ();
				p.WaitForExit ();
			} catch (System.ComponentModel.Win32Exception) {
				LoggingService.LogError ("Could not run pkg-config to locate system assemblies and development packages.");
				reportedPkgConfigNotFound = true;
			} catch (Exception ex) {
				LoggingService.LogError ("Could not run pkg-config to locate system assemblies and development packages.", ex);
				reportedPkgConfigNotFound = true;
			}
			
			return ret ?? string.Empty;
		}
	
		public string GetVariableFromPkgConfig (string var, string pcfile)
		{
			return RunPkgConfigCommand (String.Format ("--variable={0} {1}", var, pcfile));
		}
		
		public string GetLibsFromPkgConfig (string pcfile)
		{
			return RunPkgConfigCommand (String.Format ("--libs {0}", pcfile));
		}
		
		public static TargetRuntime RegisterRuntime (MonoRuntimeInfo info)
		{
			return MonoTargetRuntimeFactory.RegisterRuntime (info);
		}
		
		public static void UnregisterRuntime (MonoTargetRuntime runtime)
		{
			MonoTargetRuntimeFactory.UnregisterRuntime (runtime);
		}
	}
}
