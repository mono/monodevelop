// 
// AssemblyContext.cs
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
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using Mono.PkgConfig;

namespace MonoDevelop.Core.Assemblies
{
	public class AssemblyContext: IAssemblyContext
	{
		ImmutableDictionary<string, SystemPackage> assemblyPathToPackage = ImmutableDictionary<string, SystemPackage>.Empty;
		ImmutableDictionary<string, SystemAssembly> assemblyFullNameToAsm = ImmutableDictionary<string, SystemAssembly>.Empty;
		ImmutableDictionary<string, SystemPackage> packagesHash = ImmutableDictionary<string, SystemPackage>.Empty;
		ImmutableList<SystemPackage> packages = ImmutableList<SystemPackage>.Empty;
		
		public event EventHandler Changed;

		public IEnumerable<string> GetAssemblyFullNames ()
		{
			return assemblyFullNameToAsm.Keys;
		}
		
		internal SystemPackage RegisterPackage (LibraryPackageInfo pinfo, bool isInternal)
		{
			return RegisterPackage (new SystemPackageInfo (pinfo), isInternal, pinfo.Assemblies.ToArray ());
		}
		
		internal protected SystemPackage RegisterPackage (SystemPackageInfo pinfo, bool isInternal, params string[] assemblyFiles)
		{
			List<PackageAssemblyInfo> pinfos = new List<PackageAssemblyInfo> (assemblyFiles.Length);
			foreach (string afile in assemblyFiles) {
				if (!SystemAssemblyService.IsManagedAssembly (afile))
					continue;

				try {
					PackageAssemblyInfo pi = new PackageAssemblyInfo ();
					pi.File = afile;
					pi.Update (SystemAssemblyService.GetAssemblyNameObj (pi.File));
					pinfos.Add (pi);
				}
				catch {
					// Ignore
				}
			}
			return RegisterPackage (pinfo, isInternal, pinfos.ToArray ());
		}
		
		SystemPackage RegisterPackage (SystemPackageInfo pinfo, bool isInternal, PackageAssemblyInfo[] assemblyFiles)
		{
			//don't allow packages to duplicate framework package names
			//but multiple framework packages (from different versions) may have the same name
			SystemPackage oldPackage;
			if (packagesHash.TryGetValue (pinfo.Name, out oldPackage)) {
				if (pinfo.IsFrameworkPackage) {
					if (!oldPackage.IsFrameworkPackage)
						ForceUnregisterPackage (oldPackage);
				} else if (oldPackage.IsFrameworkPackage) {
					return oldPackage;
				}
			}
			
			SystemPackage p = new SystemPackage ();
			List<SystemAssembly> asms = new List<SystemAssembly> ();
			foreach (PackageAssemblyInfo asm in assemblyFiles) {
				if (pinfo.IsFrameworkPackage || !GetAssembliesFromFullNameInternal (asm.FullName, false).Any (a => a.Package != null && a.Package.IsFrameworkPackage))
					asms.Add (AddAssembly (asm.File, new AssemblyInfo (asm), p));
			}
			p.Initialize (pinfo, asms, isInternal);
			packages = packages.Add (p);
			packagesHash = packagesHash.SetItem (pinfo.Name, p);
			
			NotifyChanged ();
			
			return p;
		}
		
		internal protected void UnregisterPackage (string name, string version)
		{
			SystemPackage p = GetPackage (name, version);
			UnregisterPackage (p);
		}
		
		internal protected void UnregisterPackage (SystemPackage p)
		{
			if (!p.IsInternalPackage)
				throw new InvalidOperationException ("Only internal packages can be unregistered");
			
			ForceUnregisterPackage (p);
		}
		
		void ForceUnregisterPackage (SystemPackage p)
		{
			foreach (SystemAssembly asm in p.Assemblies)
				RemoveAssembly (asm);
			
			packages = packages.Remove (p);
			packagesHash = packagesHash.Remove (p.Name);
			NotifyChanged ();
		}
		
		protected void NotifyChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public IEnumerable<SystemPackage> GetPackages ()
		{
			return packages; 
		}

		[Obsolete ("Cannot de-duplicate framework assemblies")]
		public IEnumerable<SystemPackage> GetPackages (TargetFramework fx)
		{
			return GetPackagesInternal (fx);
		}

		IEnumerable<SystemPackage> GetPackagesInternal (TargetFramework fx)
		{
			foreach (SystemPackage pkg in packages) {
				if (pkg.IsFrameworkPackage) {
					if (fx.IncludesFramework (pkg.TargetFramework))
						yield return pkg;
				} else {
					if (fx.CanReferenceAssembliesTargetingFramework (pkg.TargetFramework))
						yield return pkg;
				}		
			}
		}

		public SystemAssembly[] GetAssembliesFromFullName (string fullname)
		{
			List<SystemAssembly> asms = new List<SystemAssembly> (GetAssembliesFromFullNameInternal (fullname));
			return asms.ToArray ();
		}
		
		IEnumerable<SystemAssembly> GetAssembliesFromFullNameInternal (string fullname)
		{
			return GetAssembliesFromFullNameInternal (fullname, true);
		}
		
		IEnumerable<SystemAssembly> GetAssembliesFromFullNameInternal (string fullname, bool initialize)
		{
			if (initialize)
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
			
			if (fx == null) {
				foreach (SystemPackage pkg in packages) {
					if (pkg.IsFrameworkPackage)
						continue;
					foreach (SystemAssembly asm in pkg.Assemblies)
						yield return asm;
				}
				yield break;
			}

			var fxGroups = new Dictionary<string, List<SystemAssembly>> ();

			foreach (SystemPackage pkg in GetPackagesInternal (fx)) {
				if (pkg.IsFrameworkPackage) {
					foreach (var asm in pkg.Assemblies) {
						List<SystemAssembly> list;
						if (!fxGroups.TryGetValue (asm.FullName, out list))
							fxGroups [asm.FullName] = list = new List<SystemAssembly> ();
						list.Add (asm);
					}
				} else {
					foreach (var asm in pkg.Assemblies)
						yield return asm;
				}
			}

			foreach (var g in fxGroups) {
				var a = BestFrameworkAssembly (g.Value);
				if (a != null)
					yield return a;
			}
		}
		
		public SystemAssembly GetAssemblyFromFullName (string fullname, string package, TargetFramework fx)
		{
			if (package == null) {
				var asms = GetAssembliesFromFullNameInternal (fullname).ToList ();
				return BestFrameworkAssembly (asms, fx)
					?? asms.FirstOrDefault (a => a.Package.IsGacPackage)
					?? asms.FirstOrDefault ();
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
			Initialize ();
			return GetPackageInternal (name);
		}
		
		//safe to be called from the background initialization thread
		internal protected SystemPackage GetPackageInternal (string name)
		{
			SystemPackage res;
			packagesHash.TryGetValue (name, out res);
			return res;
		}

		public SystemPackage GetPackage (string name, string version)
		{
			Initialize ();
			return GetPackageInternal (name, version);
		}

		internal SystemPackage GetPackageInternal (string name, string version)
		{
			foreach (SystemPackage p in packages)
				if (p.Name == name && p.Version == version)
					return p;
			return null;
		}

		public SystemPackage GetPackageFromPath (string path)
		{
			Initialize ();
			return assemblyPathToPackage.ContainsKey (path) ? assemblyPathToPackage [path] : null;
		}
		
		public static string NormalizeAsmName (string name)
		{
			int i = name.IndexOf (", publickeytoken=null", StringComparison.OrdinalIgnoreCase);
			if (i != -1)
				name = name.Substring (0, i).Trim ();
			i = name.IndexOf (", processorarchitecture=", StringComparison.OrdinalIgnoreCase);
			if (i != -1)
				name = name.Substring (0, i).Trim ();
			return name;
		}
	
		// Returns the installed version of the given assembly name
		// (it returns the full name of the installed assembly).
		public string FindInstalledAssembly (string fullname, string package, TargetFramework fx)
		{
			Initialize ();
			fullname = NormalizeAsmName (fullname);
			
			SystemAssembly fasm = GetAssemblyFromFullName (fullname, package, fx);
			if (fasm != null)
				return fullname;
			
			// Try to find a newer version of the same assembly, preferring framework assemblies
			if (fx == null) {
				string best = null;
				foreach (SystemAssembly asm in FindNewerAssembliesSameName (fullname)) {
					if (package == null || asm.Package.Name == package) {
						if (asm.Package.IsFrameworkPackage)
							return asm.FullName;
						else
							best = asm.FullName;
					}
				}
				return best;
			}
			

			var asms = FindNewerAssembliesSameName (fullname).ToList ();

			if (fx != null) {
				var fxAsm = BestFrameworkAssembly (asms, fx);
				if (fxAsm != null)
					return fxAsm.FullName;
			}

			string bestMatch = null;
			foreach (SystemAssembly asm in asms) {
				if (fx.CanReferenceAssembliesTargetingFramework (asm.Package.TargetFramework)) {
					if (package != null && asm.Package.Name == package)
						return asm.FullName;
					bestMatch = asm.FullName;
				}
			}
			return bestMatch;
		}

		static SystemAssembly BestFrameworkAssembly (IEnumerable<SystemAssembly> assemblies, TargetFramework fx)
		{
			if (fx == null)
				return null;
			return BestFrameworkAssembly (
				assemblies
				.Where (a => a.Package != null && a.Package.IsFrameworkPackage && fx.IncludesFramework (a.Package.TargetFramework))
				.ToList ()
			);
		}

		static SystemAssembly BestFrameworkAssembly (List<SystemAssembly> list)
		{
			if (list.Count == 0)
				return null;

			if (list.Count == 1)
				return list [0];

			SystemAssembly best = list [0];

			for (int i = 1; i < list.Count; i++) {
				var a = list[i];
				var f = Runtime.SystemAssemblyService.GetTargetFramework (a.Package.TargetFramework);
				if (f.IncludesFramework (best.Package.TargetFramework))
					best = a;
			}
			return best;
		}
		
		IEnumerable<SystemAssembly> FindNewerAssembliesSameName (string fullname)
		{
			AssemblyName reqName = ParseAssemblyName (fullname);
			foreach (KeyValuePair<string,SystemAssembly> pair in assemblyFullNameToAsm) {
				AssemblyName foundName = pair.Value.AssemblyName;
				if (reqName.Name == foundName.Name && (reqName.Version == null || reqName.Version.CompareTo (foundName.Version) < 0)) {
					SystemAssembly asm = pair.Value;
					while (asm != null) {
						yield return asm;
						asm = asm.NextSameName;
					}
				}
			}
		}
	
		public string GetAssemblyLocation (string assemblyName, TargetFramework fx)
		{
			return GetAssemblyLocation (assemblyName, null, fx);
		}
		
		public virtual string GetAssemblyLocation (string assemblyName, string package, TargetFramework fx)
		{
			Initialize ();
			
			assemblyName = NormalizeAsmName (assemblyName); 
			
			SystemAssembly asm = GetAssemblyFromFullName (assemblyName, package, fx);
			if (asm != null)
				return asm.Location;
			
			return null;
		}
		
		public virtual bool AssemblyIsInGac (string aname)
		{
			return false;
		}
		
		protected virtual IEnumerable<string> GetAssemblyDirectories ()
		{
			yield break;
		}

		
		public static void ParseAssemblyName (string assemblyName, out string name, out string version, out string culture, out string token)
		{
			name = version = culture = token = null;
			string[] parts = assemblyName.Split (',');
			if (parts.Length < 1)
				return;
			name = parts[0].Trim ();
			
			if (parts.Length < 2)
				return;
			int i = parts[1].IndexOf ('=');
			version = i != -1 ? parts[1].Substring (i+1).Trim () : parts[1].Trim ();
			
			if (parts.Length < 3)
				return;
			i = parts[2].IndexOf ('=');
			culture = i != -1 ? parts[2].Substring (i+1).Trim () : parts[2].Trim ();
			if (culture == "neutral") culture = "";
			
			if (parts.Length < 4)
				return;
			i = parts[3].IndexOf ('=');
			token = i != -1 ? parts[3].Substring (i+1).Trim () : parts[3].Trim ();
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
			
			//get the SystemAssembly for the current fullname, NOT the new target fx
			//in order to be able to check whether it's a framework assembly
			SystemAssembly asm = GetAssemblyFromFullName (fullName, packageName, null);

			if (asm == null)
				return null;
			
			var fxAsms = asm.AllSameName ().Where (a => a.Package.IsFrameworkPackage).ToList ();
			
			//if the asm is not a framework asm, we don't upgrade it automatically
			if (!fxAsms.Any ()) {
				// Return null if the package is not compatible with the requested version
				if (fx.CanReferenceAssembliesTargetingFramework (asm.Package.TargetFramework))
					return asm;
				else
					return null;
			}
			
			var bestFx = BestFrameworkAssembly (fxAsms, fx);
			if (bestFx != null)
				return bestFx;

			// We have to find the assembly with the same name in the target fx
			string fname = Path.GetFileName (fxAsms.First ().Location);

			var possible = packages.Where (p => p.IsFrameworkPackage && fx.IncludesFramework (p.TargetFramework))
				.SelectMany (p => p.Assemblies)
				.Where (a => Path.GetFileName (a.Location) == fname)
				.ToList ();

			return BestFrameworkAssembly (possible);
		}
		
		public string GetAssemblyFullName (string assemblyName, TargetFramework fx)
		{
			Initialize ();
			
			assemblyName = NormalizeAsmName (assemblyName);
			
			// Fast path for known assemblies.
			if (assemblyFullNameToAsm.ContainsKey (assemblyName))
				return assemblyName;

			// Look in assemblies of the framework. Done here since later steps look in the gac
			// without taking into account the framework.
			foreach (SystemAssembly sa in GetAssemblies (fx)) {
				if (sa.Package.IsGacPackage && sa.Name == assemblyName)
					return sa.FullName;
			}

			if (File.Exists (assemblyName))
				return SystemAssemblyService.GetAssemblyName (assemblyName);

			string file = GetAssemblyLocation (assemblyName, fx);
			if (file != null)
				return SystemAssemblyService.GetAssemblyName (file);
			else
				return null;
		}
		
		protected virtual void Initialize ()
		{
		}
		
		internal SystemAssembly AddAssembly (string assemblyfile, AssemblyInfo ainfo, SystemPackage package)
		{
			if (!File.Exists (assemblyfile))
				return null;

			try {
				SystemAssembly asm = SystemAssembly.FromFile (assemblyfile, ainfo);
				SystemAssembly prevAsm;
				if (assemblyFullNameToAsm.TryGetValue (asm.FullName, out prevAsm)) {
					asm.NextSameName = prevAsm.NextSameName;
					prevAsm.NextSameName = asm;
				} else
					assemblyFullNameToAsm = assemblyFullNameToAsm.SetItem (asm.FullName, asm);
				assemblyPathToPackage = assemblyPathToPackage.SetItem (assemblyfile, package);
				return asm;
			} catch {
				return null;
			}
		}
		
		void RemoveAssembly (SystemAssembly asm)
		{
			SystemAssembly ca;
			if (!assemblyFullNameToAsm.TryGetValue (asm.FullName, out ca))
				return;
			
			assemblyPathToPackage = assemblyPathToPackage.Remove (asm.Location);
			
			SystemAssembly prev = null;
			do {
				if (ca == asm) {
					if (prev != null)
						prev.NextSameName = ca.NextSameName;
					else if (ca.NextSameName != null)
						assemblyFullNameToAsm = assemblyFullNameToAsm.SetItem (asm.FullName, ca.NextSameName);
					else
						assemblyFullNameToAsm = assemblyFullNameToAsm.Remove (asm.FullName);
					break;
				} else {
					prev = ca;
					ca = ca.NextSameName;
				}
			} while (ca != null);
		}
		
		internal void InternalAddPackage (SystemPackage package)
		{
			SystemPackage oldPackage;
			if (package.IsFrameworkPackage && !string.IsNullOrEmpty (package.Name)
			    && packagesHash.TryGetValue (package.Name, out oldPackage) && !oldPackage.IsFrameworkPackage) {
				ForceUnregisterPackage (oldPackage);
			}
			packagesHash = packagesHash.SetItem (package.Name, package);
			packages = packages.Add (package);
		}

		public static AssemblyName ParseAssemblyName (string fullname)
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
