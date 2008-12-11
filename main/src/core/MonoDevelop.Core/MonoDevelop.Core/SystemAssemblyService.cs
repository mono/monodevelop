//
// SystemAssemblyService.cs
//
// Author:
//   Todd Berman <tberman@sevenl.net>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (C) 2004 Todd Berman
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Core
{
	public class SystemAssemblyService
	{
		Dictionary<string, SystemPackage> assemblyPathToPackage = new Dictionary<string, SystemPackage> ();
		Dictionary<string, string> assemblyFullNameToPath = new Dictionary<string, string> ();
		Dictionary<string, SystemPackage> packagesHash = new Dictionary<string, SystemPackage> ();
		List<SystemPackage> packages = new List<SystemPackage> ();
		List<TargetFramework> frameworks;
		HashSet<string> corePackages = new HashSet<string> ();
		
		object initLock = new object ();
		bool initialized;
		bool reportedPkgConfigNotFound = false;
		
		public event EventHandler PackagesChanged;

		internal SystemAssemblyService ()
		{
			// Initialize the service in a background thread.
			Thread t = new Thread (new ThreadStart (BackgroundInitialize));
			t.IsBackground = true;
			t.Start ();
		}
		
		public IEnumerable<TargetFramework> GetTargetFrameworks ()
		{
			return frameworks;
		}

		public TargetFramework GetTargetFramework (string id)
		{
			foreach (TargetFramework fx in frameworks)
				if (fx.Id == id)
					return fx;
			TargetFramework f = new TargetFramework (id);
			frameworks.Add (f);
			return f;
		}

		public ICollection<string> GetAssemblyFullNames ()
		{
			return assemblyFullNameToPath.Keys;
		}
		
		public SystemPackage RegisterPackage (string name, string version, string description, string targetVersion, string gacRoot, params string[] assemblyFiles)
		{
			SystemPackage p = new SystemPackage ();
			foreach (string asm in assemblyFiles)
				AddAssembly (asm, p);
			p.Initialize (name, version, description, assemblyFiles, targetVersion, gacRoot, true);
			packages.Add (p);
			
			if (PackagesChanged != null)
				PackagesChanged (this, EventArgs.Empty);
			
			return p;
		}
		
		public void UnregisterPackage (string name, string version)
		{
			SystemPackage p = GetPackage (name, version);
			if (!p.IsInternalPackage)
				throw new InvalidOperationException ("Only internal packages can be unregistered");
			
			packages.Remove (p);
			packagesHash.Remove (name);

			if (PackagesChanged != null)
				PackagesChanged (this, EventArgs.Empty);
		}
		
		public IEnumerable<SystemPackage> GetPackages (TargetFramework fx)
		{
			foreach (SystemPackage pkg in packages) {
				if (pkg.IsFrameworkPackage) {
					if (pkg.TargetFramework == fx.Id)
						yield return pkg;
				}
				else if (fx.IsCompatibleWithFramework (pkg.TargetFramework))
					yield return pkg;
			}
		}

		public ICollection GetAssemblyPaths ()
		{
			return GetAssemblyPaths (null);
		}
		
		// Returns the list of installed assemblies for the given runtime version.
		public ICollection GetAssemblyPaths (TargetFramework fx)
		{
			Initialize ();
			
			List<string> list = new List<string> ();
			if (fx != null && !fx.IsSupported)
				return list;
			
			foreach (KeyValuePair<string, SystemPackage> e in assemblyPathToPackage) {
				SystemPackage pkg = e.Value;
				if (pkg.IsFrameworkPackage) {
					if (fx != null && fx.IsExtensionOfFramework (pkg.TargetFramework))
						list.Add (e.Key);
				} else if (fx == null || fx.IsCompatibleWithFramework (pkg.TargetFramework))
					list.Add (e.Key);
			}
			return list;
		}

		public SystemPackage GetPackageFromFullName (string fullname)
		{
			Initialize ();
			
			fullname = NormalizeAsmName (fullname);
			string path;
			if (!assemblyFullNameToPath.TryGetValue (fullname, out path))
				return null;

			return assemblyPathToPackage.ContainsKey (path) ? assemblyPathToPackage [path] : null;
		}

		public SystemPackage GetPackage (string name)
		{
			return packagesHash.ContainsKey (name) ? packagesHash [name] : null;
		}

		public SystemPackage GetPackage (string name, string version)
		{
			foreach (SystemPackage p in packages)
				if (p.Name == name && p.Version == version)
					return p;
			return null;
		}

		public SystemPackage GetPackageFromPath (string path)
		{
			return assemblyPathToPackage.ContainsKey (path) ? assemblyPathToPackage [path] : null;
		}
		
		string NormalizeAsmName (string name)
		{
			int i = name.IndexOf (", PublicKeyToken=null");
			if (i != -1)
				return name.Substring (0, i).Trim ();
			return name;
		}
	
		// Returns the installed version of the given assembly name
		// (it returns the full name of the installed assembly).
		public string FindInstalledAssembly (string fullname)
		{
			Initialize ();
			fullname = NormalizeAsmName (fullname);
			if (assemblyFullNameToPath.ContainsKey (fullname))
				return fullname;
			
			// Try to find a newer version of the same assembly.
			AssemblyName reqName = ParseAssemblyName (fullname);
			foreach (string asm in assemblyFullNameToPath.Keys) {
				AssemblyName foundName = ParseAssemblyName (asm);
				if (reqName.Name == foundName.Name && (reqName.Version == null || reqName.Version.CompareTo (foundName.Version) < 0))
					return asm;
			}
			
			return null;
		}
	
		public string GetAssemblyLocation (string assemblyName)
		{
			Initialize ();
			
			assemblyName = NormalizeAsmName (assemblyName); 
			
			string path;
			if (assemblyFullNameToPath.TryGetValue (assemblyName, out path))
				return path;

			if (assemblyName == "mscorlib")
				return typeof(object).Assembly.Location;
			
			path = FindAssembly (assemblyName, AppDomain.CurrentDomain.BaseDirectory);
			if (path != null)
				return path;

			AssemblyLocator locator = (AssemblyLocator) Runtime.ProcessService.CreateExternalProcessObject (typeof(AssemblyLocator), true);
			using (locator) {
				return locator.Locate (assemblyName);
			}
		}

		public bool AssemblyIsInGac (string aname)
		{
			string gf = GetGacFile (aname);
			return gf != null && File.Exists (gf);
		}
		
		string FindAssembly (string aname, string baseDirectory)
		{
			// A fast but hacky way of location an assembly.
			
			int i = aname.IndexOf (",");
			if (i == -1) return null;

			string name = aname.Substring (0, i).Trim ();
			string file = Path.Combine (baseDirectory, name + ".dll");
			
			if (File.Exists (file))
				return file;
				
			file = Path.Combine (baseDirectory, name + ".exe");
			if (File.Exists (file))
				return file;
			
			string gf = GetGacFile (aname);
			if (File.Exists (gf))
				return gf;
			return null;
		}
		
		string GetGacFile (string aname)
		{
			// Look for the assembly in the GAC.
			// WARNING: this is a hack, but there isn't right now a better
			// way of doing it
			
			string gacDir = typeof(Uri).Assembly.Location;
			gacDir = Path.GetDirectoryName (gacDir);
			gacDir = Path.GetDirectoryName (gacDir);
			gacDir = Path.GetDirectoryName (gacDir);
			
			string[] parts = aname.Split (',');
			if (parts.Length != 4) return null;
			string name = parts[0].Trim ();
			
			int i = parts[1].IndexOf ('=');
			string version = i != -1 ? parts[1].Substring (i+1).Trim () : parts[1].Trim ();
			
			i = parts[2].IndexOf ('=');
			string culture = i != -1 ? parts[2].Substring (i+1).Trim () : parts[2].Trim ();
			if (culture == "neutral") culture = "";
			
			i = parts[3].IndexOf ('=');
			string token = i != -1 ? parts[3].Substring (i+1).Trim () : parts[3].Trim ();
			
			string file = Path.Combine (gacDir, name);
			file = Path.Combine (file, version + "_" + culture + "_" + token);
			file = Path.Combine (file, name + ".dll");
			return file;
		}
		
		// Given the full name of an assembly, returns the corresponding full assembly name
		// in the specified target CLR version, or null if it doesn't exist in that version.
		public string GetAssemblyNameForVersion (string fullName, TargetFramework fx)
		{
			Initialize ();

			fullName = NormalizeAsmName (fullName);
			SystemPackage package = GetPackageFromFullName (fullName);
			
			if (package == null)
				return fullName;
			
			if (!package.IsFrameworkPackage) {
				// Return null if the package is not compatible with the requested version
				if (fx.IsCompatibleWithFramework (package.TargetFramework))
					return fullName;
				else
					return null;
			}
			if (fx.IsExtensionOfFramework (package.TargetFramework))
				return fullName;

			// We have to find a core package which contains whits assembly
			string fname = Path.GetFileName ((string) assemblyFullNameToPath [fullName]);
			
			foreach (KeyValuePair<string, string> pair in assemblyFullNameToPath) {
				SystemPackage rpack = (SystemPackage) assemblyPathToPackage [pair.Value];
				if (rpack.IsFrameworkPackage && fx.IsExtensionOfFramework (rpack.TargetFramework) && Path.GetFileName (pair.Value) == fname)
					return pair.Key;
			}
			return null;
		}
		
		public string GetAssemblyFullName (string assemblyName)
		{
			Initialize ();
			
			assemblyName = NormalizeAsmName (assemblyName);
			
			// Fast path for known assemblies.
			if (assemblyFullNameToPath.ContainsKey (assemblyName))
				return assemblyName;

			if (File.Exists (assemblyName)) {
				return AssemblyName.GetAssemblyName (assemblyName).FullName;
			}
			AssemblyLocator locator = (AssemblyLocator) Runtime.ProcessService.CreateExternalProcessObject (typeof(AssemblyLocator), true);
			using (locator) {
				return locator.GetFullName (assemblyName);
			}
		}
		
		new void Initialize ()
		{
			lock (initLock) {
				while (!initialized)
					Monitor.Wait (initLock);
			}
		}
		
		void BackgroundInitialize ()
		{
			lock (initLock) {
				try {
					RunInitialization ();
				} catch (Exception ex) {
					LoggingService.LogFatalError ("Unhandled exception in SystemAssemblyService background initialisation thread.", ex);
				} finally {
					Monitor.PulseAll (initLock);
					initialized = true;
				}
			}
		}
		
		void RunInitialization ()
		{
			string versionDir;
			
			if (Environment.Version.Major == 1) {
				versionDir = "1.0";
			} else {
				versionDir = "2.0";
			}

			//Pull up assemblies from the installed mono system.
			string prefix = Path.GetDirectoryName (typeof (int).Assembly.Location);

			if (prefix.IndexOf (Path.Combine("mono", versionDir)) == -1)
				prefix = Path.Combine (prefix, "mono");
			else
				prefix = Path.GetDirectoryName (prefix);
			
			CreateFrameworks (prefix);
			
			string search_dirs = Environment.GetEnvironmentVariable ("PKG_CONFIG_PATH");
			string libpath = Environment.GetEnvironmentVariable ("PKG_CONFIG_LIBPATH");

			if (String.IsNullOrEmpty (libpath)) {
				string path_dirs = Environment.GetEnvironmentVariable ("PATH");
				foreach (string pathdir in path_dirs.Split (Path.PathSeparator)) {
					if (pathdir == null)
						continue;
					if (File.Exists (pathdir + Path.DirectorySeparatorChar + "pkg-config")) {
						libpath = Path.Combine(pathdir,"..");
						libpath = Path.Combine(libpath,"lib");
						libpath = Path.Combine(libpath,"pkgconfig");
						break;
					}
				}
			}
			search_dirs += Path.PathSeparator + libpath;
			if (search_dirs != null && search_dirs.Length > 0) {
				List<string> scanDirs = new List<string> ();
				foreach (string potentialDir in search_dirs.Split (Path.PathSeparator)) {
					try {
						string absPotentialDir = Path.GetFullPath (potentialDir);
						if (!scanDirs.Contains (absPotentialDir))
							scanDirs.Add (absPotentialDir);
					} catch {}
				}
				foreach (string pcdir in scanDirs) {
					if (pcdir == null)
						continue;
	
					if (Directory.Exists (pcdir)) {
						foreach (string pcfile in Directory.GetFiles (pcdir, "*.pc")) {
							try {
								ParsePCFile (pcfile);
							}
							catch (Exception ex) {
								LoggingService.LogError ("Could not parse file '" + pcfile + "'", ex);
							}
						}
					}
				}
			}
			
			// Get assemblies registered using the extension point
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Core/SupportPackages", OnPackagesChanged);
		}
		
		void OnPackagesChanged (object s, ExtensionNodeEventArgs args)
		{
			PackageExtensionNode node = (PackageExtensionNode) args.ExtensionNode;
			if (args.Change == ExtensionChange.Add) {
				if (GetPackage (node.Name, node.Version) == null)
					RegisterPackage (node.Name, node.Version, node.Name, node.TargetFrameworkVersion, node.GacRoot, node.Assemblies);
			}
			else {
				SystemPackage p = GetPackage (node.Name, node.Version);
				if (p.IsInternalPackage)
					UnregisterPackage (node.Name, node.Version);
			}
		}

		void CreateFrameworks (string prefix)
		{
			using (Stream s = AddinManager.CurrentAddin.GetResource ("frameworks.xml")) {
				XmlTextReader reader = new XmlTextReader (s);
				XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
				frameworks = (List<TargetFramework>) ser.Deserialize (reader, typeof(List<TargetFramework>));
			}

			// Find framework realtions
			foreach (TargetFramework fx in frameworks)
				BuildFrameworkRelations (fx);
			
			foreach (TargetFramework fx in frameworks) {
				// A framework is installed if the assemblies directory exists and the first
				// assembly of the list exists.
				string dir = Path.Combine (prefix, fx.AssembliesDir);
				if (Directory.Exists (dir)) {
					string firstAsm = Path.Combine (dir, fx.Assemblies [0]) + ".dll";
					if (File.Exists (firstAsm)) {
						fx.IsSupported = true;
						RegisterSystemAssemblies (prefix, fx);
					}
				}
				if (!string.IsNullOrEmpty (fx.Package))
					corePackages.Add (fx.Package);
			}
		}

		void BuildFrameworkRelations (TargetFramework fx)
		{
			if (fx.RelationsBuilt)
				return;
			
			fx.ExtendedFrameworks.Add (fx.Id);
			fx.CompatibleFrameworks.Add (fx.Id);
			
			if (!string.IsNullOrEmpty (fx.CompatibleWithFramework)) {
				TargetFramework compatFx = GetTargetFramework (fx.CompatibleWithFramework);
				BuildFrameworkRelations (compatFx);
				List<string> allAsm = new List<string> (fx.Assemblies);
				allAsm.AddRange (compatFx.Assemblies);
				fx.Assemblies = allAsm.ToArray ();
				fx.CompatibleFrameworks.AddRange (compatFx.CompatibleFrameworks);
			}
			else if (!string.IsNullOrEmpty (fx.ExtendsFramework)) {
				TargetFramework compatFx = GetTargetFramework (fx.ExtendsFramework);
				BuildFrameworkRelations (compatFx);
				fx.CompatibleFrameworks.AddRange (compatFx.CompatibleFrameworks);
				fx.ExtendedFrameworks.AddRange (compatFx.ExtendedFrameworks);
			}
			
			// Find subsets of this framework
			foreach (TargetFramework sfx in frameworks) {
				if (sfx.SubsetOfFramework == fx.Id)
					fx.CompatibleFrameworks.Add (sfx.Id);
			}
			
			fx.RelationsBuilt = true;
		}

		void RegisterSystemAssemblies (string prefix, TargetFramework fx)
		{
			SystemPackage package = new SystemPackage ();
			List<string> list = new List<string> ();
			
			string dir = Path.Combine (prefix, fx.AssembliesDir);
			if (!Directory.Exists(dir))
				return;

			foreach (string assembly in fx.Assemblies) {
				string file = Path.Combine (dir, assembly) + ".dll";
				if (File.Exists (file))
					AddAssembly (file, package);
			}

			// Include files from extended frameworks but don't register them,
			// since they belong to another package
			foreach (string fxid in fx.ExtendedFrameworks) {
				TargetFramework compFx = GetTargetFramework (fxid);
				dir = Path.Combine (prefix, compFx.AssembliesDir);
				if (!Directory.Exists(dir))
					continue;
				foreach (string assembly in compFx.Assemblies) {
					string file = Path.Combine (dir, assembly) + ".dll";
					if (File.Exists (file))
						list.Add (file);
				}
			}

			package.Initialize (fx.Package ?? "mono", fx.Id, fx.Name, list.ToArray (), fx.Id, null, false);
			package.IsFrameworkPackage = true;
			package.IsCorePackage = string.IsNullOrEmpty (fx.Package);
			packages.Add (package);
		}

		private void ParsePCFile (string pcfile)
		{
			// Don't register the package twice
			string pname = Path.GetFileNameWithoutExtension (pcfile);
			if (packagesHash.ContainsKey (pname) || corePackages.Contains (pname))
				return;

			List<string> fullassemblies = null;
			string version = "";
			string desc = "";
			
			SystemPackage package = new SystemPackage ();
			
			using (StreamReader reader = new StreamReader (pcfile)) {
				string line;
				while ((line = reader.ReadLine ()) != null) {
					string lowerLine = line.ToLower ();
					if (lowerLine.StartsWith ("libs:") && lowerLine.IndexOf (".dll") != -1) {
						string choppedLine = line.Substring (5).Trim ();
						if (choppedLine.IndexOf ("-lib:") != -1 || choppedLine.IndexOf ("/lib:") != -1) {
							fullassemblies = GetAssembliesWithLibInfo (choppedLine, pcfile);
						} else {
							fullassemblies = GetAssembliesWithoutLibInfo (choppedLine, pcfile);
						}
					}
					else if (lowerLine.StartsWith ("version:")) {
						version = line.Substring (8).Trim ();
					}
					else if (lowerLine.StartsWith ("description:")) {
						desc = line.Substring (12).Trim ();
					}
				}
			}
	
			if (fullassemblies == null)
				return;

			foreach (string assembly in fullassemblies) {
				AddAssembly (assembly, package);
			}

			package.Initialize (pname, version, desc, fullassemblies.ToArray (), null, null, false);
			packages.Add (package);
			packagesHash [pname] = package;
		}

		private void AddAssembly (string assemblyfile, SystemPackage package)
		{
			if (!File.Exists (assemblyfile))
				return;

			try {
				System.Reflection.AssemblyName an = System.Reflection.AssemblyName.GetAssemblyName (assemblyfile);
				assemblyFullNameToPath[NormalizeAsmName (an.FullName)] = assemblyfile;
				assemblyPathToPackage[assemblyfile] = package;
			} catch {
			}
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
	
		private string GetVariableFromPkgConfig (string var, string pcfile)
		{
			if (reportedPkgConfigNotFound)
				return string.Empty;
			ProcessStartInfo psi = new ProcessStartInfo ("pkg-config");
			psi.RedirectStandardOutput = true;
			psi.UseShellExecute = false;
			psi.Arguments = String.Format ("--variable={0} {1}", var, pcfile);
			Process p = new Process ();
			p.StartInfo = psi;
			string ret = string.Empty;
			try {
				p.Start ();
				ret = p.StandardOutput.ReadToEnd ().Trim ();
				p.WaitForExit ();
			} catch (System.ComponentModel.Win32Exception) {
				LoggingService.LogError ("Could not run pkg-config to locate system assemblies.");
				reportedPkgConfigNotFound = true;
			} catch (Exception ex) {
				LoggingService.LogError ("Could not run pkg-config to locate system assemblies.", ex);
				reportedPkgConfigNotFound = true;
			}
			
			return ret ?? string.Empty;
		}
		
		public AssemblyName ParseAssemblyName (string fullname)
		{
			AssemblyName aname = new AssemblyName ();
			int i = fullname.IndexOf (',');
			if (i == -1) {
				aname.Name = fullname.Trim ();
				return aname;
			}
			
			aname.Name = fullname.Substring (0, i).Trim ();
			i = fullname.IndexOf ("Version", i+1);
			if (i == -1)
				return aname;
			i = fullname.IndexOf ('=', i);
			if (i == -1) 
				return aname;
			int j = fullname.IndexOf (',', i);
			if (j == -1)
				aname.Version = new Version (fullname.Substring (i+1).Trim ());
			else
				aname.Version = new Version (fullname.Substring (i+1, j - i - 1).Trim ());
			return aname;
		}
	}
	
	internal class AssemblyLocator: RemoteProcessObject
	{
		public string Locate (string assemblyName)
		{
			Assembly asm = null;
			try {
				asm = Assembly.Load (assemblyName);
			} catch {
			}
			if (asm == null) {
				try {
					asm = Assembly.LoadWithPartialName (assemblyName);
				} catch {}
			}
			if (asm == null)
				return null;
			return asm.Location;
		}
		
		public string GetFullName (string assemblyName)
		{
			Assembly asm = null;
			try {
				asm = Assembly.Load (assemblyName);
			} catch {
			}
			if (asm == null) {
				try {
					asm = Assembly.LoadWithPartialName (assemblyName);
				} catch {}
			}
			
			if (asm == null)
				return null;
			
			return asm.FullName;
		}
	}
}
