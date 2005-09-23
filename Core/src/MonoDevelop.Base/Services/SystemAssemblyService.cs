using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

using MonoDevelop.Core.Services;

namespace MonoDevelop.Services
{
	public class SystemAssemblyService : AbstractService
	{
		Hashtable assemblyPathToPackage = new Hashtable ();
		Hashtable assemblyFullNameToPath = new Hashtable ();
		bool initialized;

		public ICollection AssemblyPaths
		{
			get {
				if (!initialized)
					Initialize ();
					
				return assemblyPathToPackage.Keys;
			}
		}

		public string GetPackageFromFullName (string fullname)
		{
			if (!initialized)
				Initialize ();
					
			if (!assemblyFullNameToPath.Contains (fullname))
				return String.Empty;
			
			string path = (string)assemblyFullNameToPath[fullname];
			if (!assemblyPathToPackage.Contains (path))
				return String.Empty;
			
			return (string)assemblyPathToPackage[path];
		}
	
		public string GetAssemblyLocation (string assemblyName)
		{
			if (assemblyName == "mscorlib")
				return typeof(object).Assembly.Location;

			AssemblyLocator locator = (AssemblyLocator) Runtime.ProcessService.CreateExternalProcessObject (typeof(AssemblyLocator), true);
			using (locator) {
				return locator.Locate (assemblyName);
			}
		}
		
		new void Initialize ()
		{
			initialized = true;

			//Pull up assemblies from the installed mono system.
			string prefix = Path.GetDirectoryName (typeof (int).Assembly.Location);
			if (prefix.IndexOf ("mono/1.0") == -1) {
				prefix = Path.Combine (Path.Combine (prefix, "mono"), "1.0");
			}
			foreach (string assembly in Directory.GetFiles (prefix, "*.dll")) {
				AddAssembly (assembly, "MONO-SYSTEM");
			}
	
			string search_dirs = Environment.GetEnvironmentVariable ("PKG_CONFIG_PATH");
			string libpath = Environment.GetEnvironmentVariable ("PKG_CONFIG_LIBPATH");
			if (libpath == null || libpath.Length == 0) {
				string path_dirs = Environment.GetEnvironmentVariable ("PATH");
				foreach (string pathdir in path_dirs.Split (':')) {
					if (pathdir == null)
						continue;
					if (File.Exists (pathdir + Path.DirectorySeparatorChar + "pkg-config")) {
						libpath = pathdir + Path.DirectorySeparatorChar + "../lib/pkgconfig/";
						break;
					}
				}
			}
			search_dirs += ":" + libpath;
			if (search_dirs != null && search_dirs.Length > 0) {
				ArrayList scanDirs = new ArrayList ();
				foreach (string potentialDir in search_dirs.Split (':')) {
					if (!scanDirs.Contains (potentialDir))
						scanDirs.Add (potentialDir);
				}
				foreach (string pcdir in scanDirs) {
					if (pcdir == null)
						continue;
	
					if (Directory.Exists (pcdir)) {
						//try  {
							foreach (string pcfile in Directory.GetFiles (pcdir, "*.pc")) {
								ParsePCFile (pcfile);
							}
						//} catch { }
					}
				}
			}
		}
	
		private void ParsePCFile (string pcfile)
		{
			ArrayList fullassemblies = null;
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
				}
			}
	
			if (fullassemblies == null)
				return;
	
			foreach (string assembly in fullassemblies) {
				AddAssembly (assembly, pcfile);
			}
		}

		private void AddAssembly (string assemblyfile, string pcfile)
		{
			if (!File.Exists (assemblyfile))
				return;

			try {
				System.Reflection.AssemblyName an = System.Reflection.AssemblyName.GetAssemblyName (assemblyfile);
				assemblyFullNameToPath[an.FullName] = assemblyfile;
				assemblyPathToPackage[assemblyfile] = Path.GetFileNameWithoutExtension (pcfile);
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
			p.WaitForExit ();
			string ret = p.StandardOutput.ReadToEnd ().Trim ();
			if (ret == null || ret.Length == 0)
				return String.Empty;
			return ret;
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
	}
}
