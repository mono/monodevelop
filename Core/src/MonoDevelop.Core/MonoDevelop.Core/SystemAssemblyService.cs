using System;
using System.Threading;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Core
{
	public class SystemAssemblyService : AbstractService
	{
		Hashtable assemblyPathToPackage = new Hashtable ();
		Hashtable assemblyFullNameToPath = new Hashtable ();
		Hashtable packagesHash = new Hashtable ();
		ArrayList packages = new ArrayList ();
		
		object initLock = new object ();
		bool initialized;
		
		ClrVersion currentVersion;
		
		public override void InitializeService ()
		{
			// Initialize the service in a background thread.
			Thread t = new Thread (new ThreadStart (BackgroundInitialize));
			t.IsBackground = true;
			t.Start ();
		}
		
		public ClrVersion[] GetSupportedClrVersions ()
		{
			return new ClrVersion [] { ClrVersion.Net_1_1, ClrVersion.Net_2_0 };
		}
		
		public SystemPackage RegisterPackage (string name, string version, string description, ClrVersion targetVersion, params string[] assemblyFiles)
		{
			SystemPackage p = new SystemPackage ();
			foreach (string asm in assemblyFiles)
				AddAssembly (asm, p);
			p.Initialize (name, version, description, assemblyFiles, targetVersion, true);
			packages.Add (p);
			return p;
		}

		// Returns the list of installed assemblies for the given runtime version.
		public ICollection GetAssemblyPaths (ClrVersion clrVersion)
		{
			Initialize ();
			
			if (clrVersion == ClrVersion.Default)
				clrVersion = currentVersion;

			ArrayList list = new ArrayList ();
			foreach (DictionaryEntry e in assemblyPathToPackage) {
				SystemPackage pkg = (SystemPackage) e.Value;
				if (pkg.TargetVersion != ClrVersion.Default && pkg.TargetVersion != clrVersion)
					continue;
				list.Add (e.Key);
			}
			return list;
		}

		public SystemPackage GetPackageFromFullName (string fullname)
		{
			Initialize ();
			
			fullname = NormalizeAsmName (fullname);
			string path = (string)assemblyFullNameToPath[fullname];
			if (path == null)
				return null;

			return (SystemPackage) assemblyPathToPackage[path];
		}

		public SystemPackage GetPackage (string name)
		{
			return (SystemPackage) packagesHash [name];
		}

		public SystemPackage GetPackageFromPath (string path)
		{
			return (SystemPackage) assemblyPathToPackage [path];
		}
		
		string NormalizeAsmName (string name)
		{
			int i = name.IndexOf (", PublicKeyToken=null");
			if (i != -1)
				return name.Substring (0, i).Trim ();
			else
				return name;
		}
	
		// Returns the installed version of the given assembly name
		// (it returns the full name of the installed assembly).
		public string FindInstalledAssembly (string fullname)
		{
			Initialize ();
			fullname = NormalizeAsmName (fullname);
			if (assemblyFullNameToPath.Contains (fullname))
				return fullname;
			
			// Try to find a newer version of the same assembly.
			AssemblyName reqName = ParseAssemblyName (fullname);
			foreach (string asm in assemblyFullNameToPath.Keys) {
				AssemblyName foundName = ParseAssemblyName (asm);
				if (reqName.Name == foundName.Name && reqName.Version.CompareTo (foundName.Version) < 0)
					return asm;
			}
			
			return null;
		}
	
		public string GetAssemblyLocation (string assemblyName)
		{
			Initialize ();
			
			assemblyName = NormalizeAsmName (assemblyName); 
			
			string path = (string)assemblyFullNameToPath [assemblyName];
			if (path != null)
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
			
			// Look for the assembly in the GAC.
			// WARNING: this is a hack, but there isn't right now a better
			// way of doing it
			
			string gacDir = typeof(Uri).Assembly.Location;
			gacDir = Path.GetDirectoryName (gacDir);
			gacDir = Path.GetDirectoryName (gacDir);
			gacDir = Path.GetDirectoryName (gacDir);
			
			string[] parts = aname.Split (',');
			if (parts.Length != 4) return null;
			name = parts[0].Trim ();
			
			i = parts[1].IndexOf ('=');
			string version = i != -1 ? parts[1].Substring (i+1).Trim () : parts[1].Trim ();
			
			i = parts[2].IndexOf ('=');
			string culture = i != -1 ? parts[2].Substring (i+1).Trim () : parts[2].Trim ();
			if (culture == "neutral") culture = "";
			
			i = parts[3].IndexOf ('=');
			string token = i != -1 ? parts[3].Substring (i+1).Trim () : parts[3].Trim ();
			
			file = Path.Combine (gacDir, name);
			file = Path.Combine (file, version + "_" + culture + "_" + token);
			file = Path.Combine (file, name + ".dll");
			
			if (File.Exists (file))
				return file;
			else
				return null;
		}
		
		// Given the full name of an assembly, returns the corresponding full assembly name
		// in the specified target CLR version, or null if it doesn't exist in that version.
		public string GetAssemblyNameForVersion (string fullName, ClrVersion targetVersion)
		{
			Initialize ();

			fullName = NormalizeAsmName (fullName);
			SystemPackage package = GetPackageFromFullName (fullName);
			if (package == null || !package.IsCorePackage)
				return fullName;
			
			string fname = Path.GetFileName ((string) assemblyFullNameToPath [fullName]);
			foreach (DictionaryEntry e in assemblyFullNameToPath) {
				SystemPackage rpack = (SystemPackage) assemblyPathToPackage [e.Value];
				if (rpack.IsCorePackage && rpack.TargetVersion == targetVersion && Path.GetFileName ((string)e.Value) == fname)
					return (string) e.Key;
			}
			return null;
		}
		
		public string GetAssemblyFullName (string assemblyName)
		{
			Initialize ();
			
			assemblyName = NormalizeAsmName (assemblyName);
			
			// Fast path for known assemblies.
			if (assemblyFullNameToPath.Contains (assemblyName))
				return assemblyName;

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
				currentVersion = ClrVersion.Net_1_1;
			} else {
				versionDir = "2.0";
				currentVersion = ClrVersion.Net_2_0;
			}

			//Pull up assemblies from the installed mono system.
			string prefix = Path.GetDirectoryName (typeof (int).Assembly.Location);

			if (prefix.IndexOf ( Path.Combine("mono", versionDir)) == -1)
				prefix = Path.Combine (prefix, "mono");
			else
				prefix = Path.GetDirectoryName (prefix);
			
			RegisterSystemAssemblies (prefix, "1.0", ClrVersion.Net_1_1);
			RegisterSystemAssemblies (prefix, "2.0", ClrVersion.Net_2_0);

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
				ArrayList scanDirs = new ArrayList ();
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
								ParsePCFile (pcfile);
							}
					}
				}
			}
			
			// Get assemblies registered using the extension point
			foreach (PackageExtensionNode node in Runtime.AddInService.GetTreeItems ("/MonoDevelop/Core/SupportPackages")) {
				RegisterPackage (node.ID, node.Version, node.ID, node.TargetClrVersion, node.Assemblies);
			}
		}
	
		void RegisterSystemAssemblies (string prefix, string version, ClrVersion ver)
		{
			SystemPackage package = new SystemPackage ();
			ArrayList list = new ArrayList ();
			
			string dir = Path.Combine (prefix, version);
			if(!Directory.Exists(dir)) {
				return;
			}

			foreach (string assembly in Directory.GetFiles (dir, "*.dll")) {
				AddAssembly (assembly, package);
				list.Add (assembly);
			}

			package.Initialize ("mono", version, "The Mono runtime", (string[]) list.ToArray (typeof(string)), ver, false);
			packages.Add (package);
		}
		
		private void ParsePCFile (string pcfile)
		{
			// Don't register the package twice
			string pname = Path.GetFileNameWithoutExtension (pcfile);
			if (packagesHash.Contains (pname))
				return;

			ArrayList fullassemblies = null;
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

			package.Initialize (pname, version, desc, (string[]) fullassemblies.ToArray (typeof(string)), ClrVersion.Default, false);
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
	
		private ArrayList GetAssembliesWithLibInfo (string line, string file)
		{
			ArrayList references = new ArrayList ();
			ArrayList libdirs = new ArrayList ();
			ArrayList retval = new ArrayList ();
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
	
		private ArrayList GetAssembliesWithoutLibInfo (string line, string file)
		{
			ArrayList references = new ArrayList ();
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
			ProcessStartInfo psi = new ProcessStartInfo ("pkg-config");
			psi.RedirectStandardOutput = true;
			psi.UseShellExecute = false;
			psi.Arguments = String.Format ("--variable={0} {1}", var, pcfile);
			Process p = new Process ();
			p.StartInfo = psi;
			p.Start ();
			string ret = p.StandardOutput.ReadToEnd ().Trim ();
			p.WaitForExit ();
			if (String.IsNullOrEmpty (ret))
				return String.Empty;
			return ret;
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
			Assembly asm;
			try {
				asm = Assembly.Load (assemblyName);
			}
			catch {
				asm = Assembly.LoadWithPartialName (assemblyName);
			}
			if (asm == null)
				return null;
			
			return asm.Location;
		}
		
		public string GetFullName (string assemblyName)
		{
			Assembly asm;
			try {
				asm = Assembly.Load (assemblyName);
			}
			catch {
				asm = Assembly.LoadWithPartialName (assemblyName);
			}
			if (asm == null)
				return null;
			
			return asm.FullName;
		}
	}
}
