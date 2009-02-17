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
		Dictionary<string, SystemAssembly> assemblyFullNameToAsm = new Dictionary<string, SystemAssembly> ();
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
			return assemblyFullNameToAsm.Keys;
		}
		
		public SystemPackage RegisterPackage (string name, string version, string description, string targetVersion, string gacRoot, params string[] assemblyFiles)
		{
			SystemPackage p = new SystemPackage ();
			List<SystemAssembly> asms = new List<SystemAssembly> ();
			foreach (string asm in assemblyFiles)
				asms.Add (AddAssembly (asm, p));
			p.Initialize (name, version, description, asms, targetVersion, gacRoot, true, true);
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
		
		public IEnumerable<SystemPackage> GetPackages ()
		{
			return packages; 
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

		public SystemAssembly[] GetAssembliesFromFullName (string fullname)
		{
			List<SystemAssembly> asms = new List<SystemAssembly> (GetAssembliesFromFullNameInternal (fullname));
			return asms.ToArray ();
		}
		
		IEnumerable<SystemAssembly> GetAssembliesFromFullNameInternal (string fullname)
		{
			Initialize ();
			
			fullname = NormalizeAsmName (fullname);
			SystemAssembly asm;
			if (!assemblyFullNameToAsm.TryGetValue (fullname, out asm))
				yield break;

			while (asm != null) {
				yield return asm;
				asm = asm.NextSameName;
			}
		}
		
		public IEnumerable<SystemAssembly> GetAssemblies ()
		{
			return GetAssemblies (null);
		}
		
		public IEnumerable<SystemAssembly> GetAssemblies (TargetFramework fx)
		{
			Initialize ();
			
			if (fx != null && !fx.IsSupported)
				yield break;
			
			foreach (SystemPackage pkg in packages) {
				if (pkg.IsFrameworkPackage) {
					if (fx == null || pkg.TargetFramework != fx.Id)
						continue;
				} else if (fx != null && !fx.IsCompatibleWithFramework (pkg.TargetFramework))
					continue;
				
				foreach (SystemAssembly asm in pkg.Assemblies)
					yield return asm;
			}
		}
		
		public SystemAssembly GetAssemblyFromFullName (string fullname, string package)
		{
			foreach (SystemAssembly asm in GetAssembliesFromFullNameInternal (fullname)) {
				if (package == null || package == asm.Package.Name)
					return asm;
			}
			return null;
		}
		
		public SystemPackage[] GetPackagesFromFullName (string fullname)
		{
			List<SystemPackage> packs = new List<SystemPackage> ();
			foreach (SystemAssembly asm in GetAssembliesFromFullNameInternal (fullname))
				packs.Add (asm.Package);
			return packs.ToArray ();
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
		
		internal static string NormalizeAsmName (string name)
		{
			int i = name.IndexOf (", PublicKeyToken=null");
			if (i != -1)
				return name.Substring (0, i).Trim ();
			return name;
		}
	
		// Returns the installed version of the given assembly name
		// (it returns the full name of the installed assembly).
		public string FindInstalledAssembly (string fullname, string package)
		{
			Initialize ();
			fullname = NormalizeAsmName (fullname);
			
			SystemAssembly fasm = GetAssemblyFromFullName (fullname, package);
			if (fasm != null)
				return fullname;
			
			// Try to find a newer version of the same assembly.
			AssemblyName reqName = ParseAssemblyName (fullname);
			foreach (KeyValuePair<string,SystemAssembly> pair in assemblyFullNameToAsm) {
				AssemblyName foundName = ParseAssemblyName (pair.Key);
				if (reqName.Name == foundName.Name && (reqName.Version == null || reqName.Version.CompareTo (foundName.Version) < 0)) {
					SystemAssembly asm = pair.Value;
					while (asm != null) {
						if (package == null || asm.Package.Name == package)
							return asm.FullName;
					}
				}
			}
			
			return null;
		}
	
		public string GetAssemblyLocation (string assemblyName)
		{
			return GetAssemblyLocation (assemblyName, null);
		}
		
		public string GetAssemblyLocation (string assemblyName, string package)
		{
			Initialize ();
			
			assemblyName = NormalizeAsmName (assemblyName); 
			
			SystemAssembly asm = GetAssemblyFromFullName (assemblyName, package);
			if (asm != null)
				return asm.Location;
			
			if (assemblyName == "mscorlib" || assemblyName.StartsWith ("mscorlib,"))
				return typeof(object).Assembly.Location;
			
			string path = FindAssembly (assemblyName, AppDomain.CurrentDomain.BaseDirectory);
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
			return GetAssemblyNameForVersion (fullName, null, fx);
		}
		
		public string GetAssemblyNameForVersion (string fullName, string packageName, TargetFramework fx)
		{
			SystemAssembly asm = GetAssemblyForVersion (fullName, packageName, fx);
			if (asm != null)
				return asm.FullName;
			else
				return null;
		}
		
		// Given the full name of an assembly, returns the corresponding full assembly name
		// in the specified target CLR version, or null if it doesn't exist in that version.
		public SystemAssembly GetAssemblyForVersion (string fullName, string packageName, TargetFramework fx)
		{
			Initialize ();

			fullName = NormalizeAsmName (fullName);
			SystemAssembly asm = GetAssemblyFromFullName (fullName, packageName);

			if (asm == null)
				return null;
			
			if (!asm.Package.IsFrameworkPackage) {
				// Return null if the package is not compatible with the requested version
				if (fx.IsCompatibleWithFramework (asm.Package.TargetFramework))
					return asm;
				else
					return null;
			}
			if (fx.IsExtensionOfFramework (asm.Package.TargetFramework))
				return asm;

			// We have to find a core package which contains whits assembly
			string fname = Path.GetFileName ((string) asm.Location);
			
			foreach (KeyValuePair<string, SystemAssembly> pair in assemblyFullNameToAsm) {
				SystemPackage rpack = pair.Value.Package;
				if (rpack.IsFrameworkPackage && fx.IsExtensionOfFramework (rpack.TargetFramework) && Path.GetFileName (pair.Value.Location) == fname)
					return pair.Value;
			}
			return null;
		}
		
		public string GetAssemblyFullName (string assemblyName)
		{
			Initialize ();
			
			assemblyName = NormalizeAsmName (assemblyName);
			
			// Fast path for known assemblies.
			if (assemblyFullNameToAsm.ContainsKey (assemblyName))
				return assemblyName;

			if (File.Exists (assemblyName)) {
				return AssemblyName.GetAssemblyName (assemblyName).FullName;
			}
			AssemblyLocator locator = (AssemblyLocator) Runtime.ProcessService.CreateExternalProcessObject (typeof(AssemblyLocator), true);
			using (locator) {
				return locator.GetFullName (assemblyName);
			}
		}
		
		void Initialize ()
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
		
		static string GetMDInstallPrefix ()
		{
			string location = System.Reflection.Assembly.GetExecutingAssembly ().Location;
			location = Path.GetDirectoryName (location);
			// MD is located at $prefix/lib/monodevelop/bin
			// adding "../../.." should give us $prefix
			string prefix = Path.Combine (Path.Combine (Path.Combine (location, ".."), ".."), "..");
			//normalise it
			return Path.GetFullPath (prefix);
		}
		
		
		//returns the install prefixes of monodevelop, mono and pkgconfig, and the prefixes /usr and /local
		static IEnumerable<string> GetSystemPkgConfigDirs ()
		{
			yield return GetMDInstallPrefix ();
			
			string[] paths = Environment.GetEnvironmentVariable ("PATH").Split (Path.PathSeparator);
			if (paths != null && paths.Length > 0) {
				foreach (string prog in new string [] { "mono", "pkgconfig" }) {
					foreach (string path in paths) {
						if (path == null)
							continue;
						
						string file = Path.Combine (path, prog);
						try {
							if (!File.Exists (file))
								continue;
						} catch (IOException ex) {
							LoggingService.LogError ("Error checking for file '" + file + "'.", ex);
						}
						yield return Path.GetFullPath (Path.Combine (path, ".."));		             	   
					}
				}
				
			}
		}
		
		static IEnumerable<string> GetUnfilteredPkgConfigDirs ()
		{
			string envDirs = Environment.GetEnvironmentVariable ("PKG_CONFIG_PATH");
			if (!String.IsNullOrEmpty (envDirs))
				foreach (string dir in envDirs.Split (Path.PathSeparator))
					yield return dir;
			
			string[] suffixes = new string [] {
				Path.Combine ("lib", "pkgconfig"),
				Path.Combine ("lib64", "pkgconfig"),
				Path.Combine ("share", "pkgconfig"),
			};
			
			string libDir= Environment.GetEnvironmentVariable ("PKG_CONFIG_LIBDIR");
			if (!String.IsNullOrEmpty (libDir))
				foreach (string dir in libDir.Split (Path.PathSeparator))
					yield return dir;
			else
				foreach (string prefix in GetSystemPkgConfigDirs ())
					foreach (string suffix in suffixes)
						yield return Path.Combine (prefix, suffix);
		}
		
		static IEnumerable<string> GetPkgConfigDirs ()
		{
			HashSet<string> set = new HashSet<string> ();
			foreach (string s in GetUnfilteredPkgConfigDirs ()) {
				if (set.Contains (s))
					continue;
				set.Add (s);
				try {
					if (!Directory.Exists (s))
						continue;
				} catch (IOException ex) {
					LoggingService.LogError ("Error checking for directory '" + s + "'.", ex);
				}
				yield return s;
			}
		}
		
		
		string[] _pkgConfigDirs;
		public IEnumerable<string> PkgConfigDirs {
			get {
				if (_pkgConfigDirs == null)
					_pkgConfigDirs = System.Linq.Enumerable.ToArray (GetPkgConfigDirs ());
				return _pkgConfigDirs;
			}
		}
		
		public string PkgConfigPath {
			get {
				return string.Join (new string (Path.PathSeparator, 1), (string[])PkgConfigDirs);
			}
		}
		
		public IEnumerable<string> GetAllPkgConfigFiles ()
		{
			HashSet<string> packageNames = new HashSet<string> ();
			foreach (string pcdir in PkgConfigDirs)
				foreach (string pcfile in Directory.GetFiles (pcdir, "*.pc"))
					if (packageNames.Add (Path.GetFileNameWithoutExtension (pcfile)))
						yield return pcfile;
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
			List<SystemAssembly> list = new List<SystemAssembly> ();
			
			string dir = Path.Combine (prefix, fx.AssembliesDir);
			if (!Directory.Exists(dir))
				return;

			foreach (string assembly in fx.Assemblies) {
				string file = Path.Combine (dir, assembly) + ".dll";
				if (File.Exists (file))
					list.Add (AddAssembly (file, package));
			}

			// Include files from extended frameworks but don't register them,
			// since they belong to another package
			foreach (string fxid in fx.ExtendedFrameworks) {
				if (fxid == fx.Id)
					continue;
				TargetFramework compFx = GetTargetFramework (fxid);
				dir = Path.Combine (prefix, compFx.AssembliesDir);
				if (!Directory.Exists(dir))
					continue;
				foreach (string assembly in compFx.Assemblies) {
					string file = Path.Combine (dir, assembly) + ".dll";
					if (File.Exists (file))
						list.Add (SystemAssembly.FromFile (file));
				}
			}

			package.Initialize (fx.Package ?? "mono", fx.Id, fx.Name, list.ToArray (), fx.Id, null, false, true);
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
			bool gacPackage = true;
			
			SystemPackage package = new SystemPackage ();
			
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
					if (var == "libs" && value.IndexOf (".dll") != -1) {
						if (value.IndexOf ("-lib:") != -1 || value.IndexOf ("/lib:") != -1) {
							fullassemblies = GetAssembliesWithLibInfo (value, pcfile);
						} else {
							fullassemblies = GetAssembliesWithoutLibInfo (value, pcfile);
						}
					}
					else if (var == "version") {
						version = value;
					}
					else if (var == "description") {
						desc = value;
					}
					else if (var == "gacpackage") {
						value = value.ToLower ();
						gacPackage = value == "yes" || value == "true";
					}
				}
			}
	
			if (fullassemblies == null)
				return;

			List<SystemAssembly> list = new List<SystemAssembly> ();
			foreach (string assembly in fullassemblies) {
				list.Add (AddAssembly (assembly, package));
			}

			package.Initialize (pname, version, desc, list, null, null, false, gacPackage);
			packages.Add (package);
			packagesHash [pname] = package;
		}

		private SystemAssembly AddAssembly (string assemblyfile, SystemPackage package)
		{
			if (!File.Exists (assemblyfile))
				return null;

			try {
				SystemAssembly asm = SystemAssembly.FromFile (assemblyfile);
				SystemAssembly prevAsm;
				if (assemblyFullNameToAsm.TryGetValue (asm.FullName, out prevAsm)) {
					asm.NextSameName = prevAsm.NextSameName;
					prevAsm.NextSameName = asm;
				} else
					assemblyFullNameToAsm [asm.FullName] = asm;
				assemblyPathToPackage [assemblyfile] = package;
				return asm;
			} catch {
				return null;
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
#pragma warning disable 612
					asm = Assembly.LoadWithPartialName (assemblyName);
#pragma warning restore 612
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
#pragma warning disable 612
					asm = Assembly.LoadWithPartialName (assemblyName);
#pragma warning restore 612
				} catch {}
			}
			
			if (asm == null)
				return null;
			
			return asm.FullName;
		}
	}
}
