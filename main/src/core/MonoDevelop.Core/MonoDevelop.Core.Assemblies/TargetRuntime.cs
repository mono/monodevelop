// 
// TargetRuntime.cs
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
	public abstract class TargetRuntime
	{
		Dictionary<string, SystemPackage> assemblyPathToPackage = new Dictionary<string, SystemPackage> ();
		Dictionary<string, SystemAssembly> assemblyFullNameToAsm = new Dictionary<string, SystemAssembly> ();
		Dictionary<string, SystemPackage> packagesHash = new Dictionary<string, SystemPackage> ();
		List<SystemPackage> packages = new List<SystemPackage> ();
		HashSet<string> corePackages = new HashSet<string> ();
		
		object initLock = new object ();
		bool initialized;
		
		public event EventHandler PackagesChanged;

		internal void StartInitialization ()
		{
			// Initialize the service in a background thread.
			Thread t = new Thread (new ThreadStart (BackgroundInitialize));
			t.IsBackground = true;
			t.Start ();
		}
		
		public virtual string DisplayName {
			get {
				if (string.IsNullOrEmpty (Version))
					return DisplayRuntimeName;
				else
					return DisplayRuntimeName + " " + Version;
			}
		}
		
		public string Id {
			get {
				if (string.IsNullOrEmpty (Version))
					return RuntimeId;
				else
					return RuntimeId + " " + Version;
			}
		}
		
		public virtual string DisplayRuntimeName {
			get { return RuntimeId; }
		}
		
		public abstract string RuntimeId { get; }
		
		public abstract string Version { get; }
		
		public abstract bool IsRunning { get; }
		
		protected abstract void OnInitialize ();
		
		protected abstract string GetFrameworkFolder (TargetFramework fx);
		
		public abstract IExecutionHandler GetExecutionHandler ();
		
		public ICollection<string> GetAssemblyFullNames ()
		{
			return assemblyFullNameToAsm.Keys;
		}
		
		public SystemPackage RegisterPackage (SystemPackageInfo pinfo, params string[] assemblyFiles)
		{
			return RegisterPackage (pinfo, true, assemblyFiles);
		}
		
		public SystemPackage RegisterPackage (SystemPackageInfo pinfo, bool isInternal, params string[] assemblyFiles)
		{
			SystemPackage p = new SystemPackage (this);
			List<SystemAssembly> asms = new List<SystemAssembly> ();
			foreach (string asm in assemblyFiles)
				asms.Add (AddAssembly (asm, p));
			p.Initialize (pinfo, asms, isInternal);
			packages.Add (p);
			packagesHash [pinfo.Name] = p;
			
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
			Initialize ();
			foreach (SystemPackage pkg in packages) {
				foreach (SystemAssembly asm in pkg.Assemblies)
					yield return asm;
			}
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
			if (package == null) {
				SystemAssembly found = null;
				foreach (SystemAssembly asm in GetAssembliesFromFullNameInternal (fullname)) {
					found = asm;
					if (asm.Package.IsGacPackage)
						return asm;
				}
				return found;
			}
			
			foreach (SystemAssembly asm in GetAssembliesFromFullNameInternal (fullname)) {
				if (package == asm.Package.Name)
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
			SystemPackage res;
			packagesHash.TryGetValue (name, out res);
			return res;
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
			int i = name.ToLower ().IndexOf (", publickeytoken=null");
			if (i != -1)
				name = name.Substring (0, i).Trim ();
			i = name.ToLower ().IndexOf (", processorarchitecture=");
			if (i != -1)
				name = name.Substring (0, i).Trim ();
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
		
		protected abstract string GetGacDirectory ();
		
		string GetGacFile (string aname)
		{
			// Look for the assembly in the GAC.
			// WARNING: this is a hack, but there isn't right now a better
			// way of doing it
			
			string gacDir = GetGacDirectory ();
			
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
		
		void RunInitialization ()
		{
			CreateFrameworks ();
			OnInitialize ();
			
			// Get assemblies registered using the extension point
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Core/SupportPackages", OnPackagesChanged);
		}
		
		void OnPackagesChanged (object s, ExtensionNodeEventArgs args)
		{
			PackageExtensionNode node = (PackageExtensionNode) args.ExtensionNode;
			SystemPackageInfo pi = node.GetPackageInfo ();
			
			if (args.Change == ExtensionChange.Add) {
				if (GetPackage (pi.Name, pi.Version) == null)
					RegisterPackage (pi, node.Assemblies);
			}
			else {
				SystemPackage p = GetPackage (pi.Name, pi.Version);
				if (p.IsInternalPackage)
					UnregisterPackage (pi.Name, pi.Version);
			}
		}

		void CreateFrameworks ()
		{
			foreach (TargetFramework fx in Runtime.SystemAssemblyService.GetTargetFrameworks ()) {
				// A framework is installed if the assemblies directory exists and the first
				// assembly of the list exists.
				string dir = GetFrameworkFolder (fx);
				if (Directory.Exists (dir)) {
					string firstAsm = Path.Combine (dir, fx.Assemblies [0]) + ".dll";
					if (File.Exists (firstAsm)) {
						fx.IsSupported = true;
						RegisterSystemAssemblies (fx);
					}
				}
			}
		}
		
		protected bool IsCorePackage (string pname)
		{
			return corePackages.Contains (pname);
		}

		void RegisterSystemAssemblies (TargetFramework fx)
		{
			SystemPackage package = new SystemPackage (this);
			List<SystemAssembly> list = new List<SystemAssembly> ();
			
			string dir = GetFrameworkFolder (fx);
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
				TargetFramework compFx = Runtime.SystemAssemblyService.GetTargetFramework (fxid);
				dir = GetFrameworkFolder (compFx);
				if (!Directory.Exists(dir))
					continue;
				foreach (string assembly in compFx.Assemblies) {
					string file = Path.Combine (dir, assembly) + ".dll";
					if (File.Exists (file))
						list.Add (SystemAssembly.FromFile (file));
				}
			}

			SystemPackageInfo info = GetFrameworkPackageInfo (fx);
			package.Initialize (info, list.ToArray (), false);
			packages.Add (package);
			
			if (info.IsCorePackage)
				corePackages.Add (info.Name);
		}
		
		protected virtual SystemPackageInfo GetFrameworkPackageInfo (TargetFramework fx)
		{
			SystemPackageInfo info = new SystemPackageInfo ();
			info.Name = "system";
			info.Description = fx.Name;
			info.IsFrameworkPackage = true;
			info.IsCorePackage = true;
			info.IsGacPackage = true;
			info.Version = fx.Id;
			info.TargetFramework = fx.Id;
			return info;
		}


		protected SystemAssembly AddAssembly (string assemblyfile, SystemPackage package)
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
}
