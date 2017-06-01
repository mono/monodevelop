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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Serialization;
using Mono.Addins;
using Mono.PkgConfig;

namespace MonoDevelop.Core.Assemblies
{
	public class MonoTargetRuntime: TargetRuntime
	{
		readonly string monoVersion,  monoDir;
		MonoPlatformExecutionHandler execHandler;
		readonly Dictionary<string,string> environmentVariables;
		
		internal static LibraryPcFileCache PcFileCache = new LibraryPcFileCache (new PcFileCacheContext ());
		
		readonly MonoRuntimeInfo monoRuntimeInfo;
		
		internal MonoTargetRuntime (MonoRuntimeInfo info)
		{
			this.monoVersion = info.MonoVersion;
			this.monoDir = Path.Combine (Path.Combine (info.Prefix, "lib"), "mono");
			environmentVariables = info.GetEnvironmentVariables ();
			monoRuntimeInfo = info;
		}
		
		public MonoRuntimeInfo MonoRuntimeInfo {
			get { return monoRuntimeInfo; }
		}
		
		public string Prefix {
			get { return monoRuntimeInfo.Prefix; }
		}
		
		public string MonoDirectory {
			get { return monoDir; }
		}
		
		public Dictionary<string,string> EnvironmentVariables {
			get { return environmentVariables; }
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
		
		public override string DisplayName {
			get {
				if (!IsRunning)
					return base.DisplayName + " (" + Prefix + ")";
				else
					return base.DisplayName;
			}
		}

		public bool HasMultitargetingMcs { get; private set; }

		public override IEnumerable<FilePath> GetReferenceFrameworkDirectories ()
		{
			//duplicate xbuild's framework folders path logic
			//see xbuild man page
			string env;
			if (environmentVariables.TryGetValue ("XBUILD_FRAMEWORK_FOLDERS_PATH", out env) && !string.IsNullOrEmpty (env)) {
				foreach (var dir in env.Split (new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
					yield return (FilePath) dir;
			}

			if (Platform.IsMac) {
				yield return "/Library/Frameworks/Mono.framework/External/xbuild-frameworks";
			}

			//can't return $(TargetFrameworkRoot) MSBuild var, since that's per-project
			yield return Path.Combine (monoDir, "xbuild-frameworks");
		}

		public bool UserDefined { get; internal set; }
		
		public override string GetAssemblyDebugInfoFile (string assemblyPath)
		{
			if (monoRuntimeInfo.RuntimeVersion != null && monoRuntimeInfo.RuntimeVersion >= new Version (4,9,0))
				return Path.ChangeExtension (assemblyPath, ".pdb");
			return assemblyPath + ".mdb";
		}
		
		protected override TargetFrameworkBackend CreateBackend (TargetFramework fx)
		{
			return new MonoFrameworkBackend ();
		}

		
		public override IExecutionHandler GetExecutionHandler ()
		{
			if (execHandler == null) {
				execHandler = new MonoPlatformExecutionHandler (this);
			}
			return execHandler;
		}
		
		protected override void ConvertAssemblyProcessStartInfo (ProcessStartInfo pinfo)
		{
			pinfo.Arguments = "\"" + pinfo.FileName + "\" " + pinfo.Arguments;
			pinfo.FileName = Path.Combine (Path.Combine (MonoRuntimeInfo.Prefix, "bin"), "mono");
		}

		public override string GetToolPath (TargetFramework fx, string toolName)
		{
			if (fx.ClrVersion == ClrVersion.Net_2_0 && toolName == "al")
				toolName = "al2";
			return base.GetToolPath (fx, toolName);
		}
		
		internal protected override IEnumerable<string> GetGacDirectories ()
		{
			yield return Path.Combine (monoDir, "gac");
			
			string gacs;
			if (environmentVariables.TryGetValue ("MONO_GAC_PREFIX", out gacs)) {
				if (string.IsNullOrEmpty (gacs))
					yield break;
				foreach (string path in gacs.Split (new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
					yield return path;
			}
		}
		
		public override string GetMSBuildBinPath (string toolsVersion)
		{
			var path = Path.Combine (monoDir, "msbuild", toolsVersion, "bin");
			if (File.Exists (Path.Combine (path, "MSBuild.exe")) ||
			    File.Exists (Path.Combine (path, "MSBuild.dll"))) {
				return path;
			}

			return null;
		}

		public override string GetMSBuildToolsPath (string toolsVersion)
		{
			var path = Path.Combine (monoDir, "msbuild", toolsVersion, "bin");
			if (Directory.Exists (path)) {
				return path;
			}

			return null;
		}

		public override string GetMSBuildExtensionsPath ()
		{
			return Path.Combine (monoDir, "xbuild");
		}
		
		public IEnumerable<string> PkgConfigDirs {
			get { return GetPkgConfigDirs (IsInitialized || IsRunning); }
		}

		IEnumerable<string> GetPkgConfigDirs (bool includeGlobalDirectories)
		{
			foreach (string s in PkgConfigPath.Split (Path.PathSeparator))
				yield return s;
			if (includeGlobalDirectories && Platform.IsMac)
				yield return "/Library/Frameworks/Mono.framework/External/pkgconfig";
		}
		
		public string PkgConfigPath {
			get { return environmentVariables ["PKG_CONFIG_PATH"]; }
		}

		public IEnumerable<string> GetAllPkgConfigFiles ()
		{
			var packageNames = new HashSet<string> ();
			foreach (string pcdir in PkgConfigDirs) {
				IEnumerable<string> files;

				if (!Directory.Exists (pcdir))
					continue;

				try {
					files = Directory.EnumerateFiles (pcdir, "*.pc");
				} catch (Exception ex) {
					LoggingService.LogError (string.Format (
						"Runtime '{0}' error in pc file scan of directory '{1}'", DisplayName, pcdir), ex);
					continue;
				}

				foreach (string pcfile in files)
					if (packageNames.Add (Path.GetFileNameWithoutExtension (pcfile)))
						yield return pcfile;
			}
		}
		
		protected override void OnInitialize ()
		{
			if (!monoRuntimeInfo.IsValidRuntime)
				return;
			foreach (string pcfile in GetAllPkgConfigFiles ()) {
				try {
					var pc = new FilePath (pcfile).ResolveLinks ();
					if (!string.IsNullOrEmpty (pc))
						ParsePCFile (pc);

					if (ShuttingDown)
						return;
				}
				catch (Exception ex) {
					LoggingService.LogError ("Could not parse file '" + pcfile + "'", ex);
				}
			}

			// Mono 2.11 is the first release with either 4.5 framework and multitargeting mcs
			// so by detecting the 4.5 profile, we know we have multitargeting mcs
			HasMultitargetingMcs = File.Exists (Path.Combine (monoDir, "4.5", "mscorlib.dll"));

			PcFileCache.Save ();
		}

		private void ParsePCFile (string pcfile)
		{
			// Don't register the package twice
			string pname = Path.GetFileNameWithoutExtension (pcfile);
			if (RuntimeAssemblyContext.GetPackageInternal (pname) != null || IsCorePackage (pname))
				return;

			LibraryPackageInfo pinfo = PcFileCache.GetPackageInfo (pcfile);
			if (pinfo.IsValidPackage)
				RuntimeAssemblyContext.RegisterPackage (pinfo, false);
		}

		
		public static TargetRuntime RegisterRuntime (MonoRuntimeInfo info)
		{
			return MonoTargetRuntimeFactory.RegisterRuntime (info);
		}
		
		public static void UnregisterRuntime (MonoTargetRuntime runtime)
		{
			MonoTargetRuntimeFactory.UnregisterRuntime (runtime);
		}

		public override ExecutionEnvironment GetToolsExecutionEnvironment ()
		{
			return new ExecutionEnvironment (EnvironmentVariables);
		}

		static bool Is64BitPE (Mono.Cecil.TargetArchitecture machine)
		{
			return machine == Mono.Cecil.TargetArchitecture.AMD64 ||
				   machine == Mono.Cecil.TargetArchitecture.IA64 ||
				   machine == Mono.Cecil.TargetArchitecture.ARM64;
		}

		/// <summary>
		/// Get the Mono executable best matching the assembly architecture flags.
		/// </summary>
		/// <remarks>Returns a fallback Mono executable, if a match cannot be found.</remarks>
		/// <returns>The Mono executable that should be used to execute the assembly.</returns>
		/// <param name="assemblyPath">Assembly path.</param>
		public string GetMonoExecutableForAssembly (string assemblyPath)
		{
			Mono.Cecil.ModuleAttributes peKind;
			Mono.Cecil.TargetArchitecture machine;

			try {
				using (var adef = Mono.Cecil.AssemblyDefinition.ReadAssembly (assemblyPath)) {
					peKind = adef.MainModule.Attributes;
					machine = adef.MainModule.Architecture;
				}
			} catch {
				peKind = Mono.Cecil.ModuleAttributes.ILOnly;
				machine = Mono.Cecil.TargetArchitecture.I386;
			}

			string monoPath;

			if ((peKind & (Mono.Cecil.ModuleAttributes.Required32Bit | Mono.Cecil.ModuleAttributes.Preferred32Bit)) != 0) {
				monoPath = Path.Combine (MonoRuntimeInfo.Prefix, "bin", "mono32");
				if (File.Exists (monoPath))
					return monoPath;
			} else if (Is64BitPE (machine)) {
				monoPath = Path.Combine (MonoRuntimeInfo.Prefix, "bin", "mono64");
				if (File.Exists (monoPath))
					return monoPath;
			}

			return monoPath = Path.Combine (MonoRuntimeInfo.Prefix, "bin", "mono");
		}
	}
	
	class PcFileCacheContext: Mono.PkgConfig.IPcFileCacheContext<LibraryPackageInfo>
	{
		public void ReportError (string message, System.Exception ex)
		{
			LoggingService.LogError (message, ex);
		}
		
		public bool IsCustomDataComplete (string pcfile, LibraryPackageInfo pkg)
		{
			string fx = pkg.GetData ("targetFramework");
			return fx != null && fx != "Unknown";
			// The 'unknown' check was added here to force a re-scan of .pc files
			// which resulted in unknown framework version due to a bug
		}
		
		public void StoreCustomData (PcFile pcfile, LibraryPackageInfo pinfo)
		{
			TargetFramework commonFramework = null;
			bool inconsistentFrameworks = false;
			
			foreach (PackageAssemblyInfo pi in pinfo.Assemblies) {
				TargetFrameworkMoniker targetFramework = Runtime.SystemAssemblyService.GetTargetFrameworkForAssembly (Runtime.SystemAssemblyService.CurrentRuntime, pi.File);
				if (commonFramework == null) {
					commonFramework = Runtime.SystemAssemblyService.GetTargetFramework (targetFramework);
					if (commonFramework == null)
						inconsistentFrameworks = true;
				}
				else if (targetFramework != null) {
					TargetFramework newfx = Runtime.SystemAssemblyService.GetTargetFramework (targetFramework);
					if (newfx == null)
						inconsistentFrameworks = true;
					else {
						if (newfx.CanReferenceAssembliesTargetingFramework (commonFramework))
							commonFramework = newfx;
						else if (!commonFramework.CanReferenceAssembliesTargetingFramework (newfx))
							inconsistentFrameworks = true;
					}
				}
				if (inconsistentFrameworks)
					break;
			}
			if (inconsistentFrameworks)
				LoggingService.LogError ("Inconsistent target frameworks found in " + pcfile);
			if (commonFramework != null)
				pinfo.SetData ("targetFramework", commonFramework.Id.ToString ());
			else
				pinfo.SetData ("targetFramework", "FxUnknown");
		}
	}
}
