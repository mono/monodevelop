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
using Mono.PkgConfig;

namespace MonoDevelop.Core.Assemblies
{
	public class MonoTargetRuntime: TargetRuntime
	{
		string monoVersion;
		string monoDir;
		MonoPlatformExecutionHandler execHandler;
		Dictionary<string,string> environmentVariables;
		
		internal static LibraryPcFileCache PcFileCache = new LibraryPcFileCache (new PcFileCacheContext ());
		
		MonoRuntimeInfo monoRuntimeInfo;
		
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
			//during initializion, only return the global directory once (for the running runtime) so that it doesn't
			//get scanned multiple times
			return GetReferenceFrameworkDirectories (IsInitialized || IsRunning);
		}

		internal IEnumerable<FilePath> GetReferenceFrameworkDirectories (bool includeMacGlobalDir)
		{
			//duplicate xbuild's framework folders path logic
			//see xbuild man page
			string env;
			if (environmentVariables.TryGetValue ("XBUILD_FRAMEWORK_FOLDERS_PATH", out env) && !string.IsNullOrEmpty (env)) {
				foreach (var dir in env.Split (new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
					yield return (FilePath) dir;
			}

			if (Platform.IsMac && true) {
				yield return "/Library/Frameworks/Mono.framework/External/xbuild-frameworks";
			}

			//can't return $(TargetFrameworkRoot) MSBuild var, since that's per-project
			yield return Path.Combine (monoDir, "xbuild-frameworks");
		}

		public bool UserDefined { get; internal set; }
		
		public override string GetAssemblyDebugInfoFile (string assemblyPath)
		{
			return assemblyPath + ".mdb";
		}
		
		protected override TargetFrameworkBackend CreateBackend (TargetFramework fx)
		{
			return new MonoFrameworkBackend ();
		}

		
		public override IExecutionHandler GetExecutionHandler ()
		{
			if (execHandler == null) {
				string monoPath = Path.Combine (Path.Combine (MonoRuntimeInfo.Prefix, "bin"), "mono");
				execHandler = new MonoPlatformExecutionHandler (monoPath, environmentVariables);
			}
			return execHandler;
		}
		
		protected override void ConvertAssemblyProcessStartInfo (System.Diagnostics.ProcessStartInfo pinfo)
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
		
		public override string GetMSBuildBinPath (TargetFramework fx)
		{
			return Path.Combine (monoDir, "2.0");
		}
		
		public override string GetMSBuildExtensionsPath ()
		{
			return Path.Combine (monoDir, "xbuild");
		}
		
		public IEnumerable<string> PkgConfigDirs {
			get { return PkgConfigPath.Split (Path.PathSeparator); }
		}
		
		public string PkgConfigPath {
			get { return environmentVariables ["PKG_CONFIG_PATH"]; }
		}
		
		public IEnumerable<string> GetAllPkgConfigFiles ()
		{
			HashSet<string> packageNames = new HashSet<string> ();
			foreach (string pcdir in PkgConfigDirs) {
				string[] files;
				try {
					files = Directory.GetFiles (pcdir, "*.pc");
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
					ParsePCFile (FileService.ResolveFullPath (pcfile));
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
					commonFramework = Runtime.SystemAssemblyService.GetCoreFramework (targetFramework);
					if (commonFramework == null)
						inconsistentFrameworks = true;
				}
				else if (targetFramework != null) {
					TargetFramework newfx = Runtime.SystemAssemblyService.GetCoreFramework (targetFramework);
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
