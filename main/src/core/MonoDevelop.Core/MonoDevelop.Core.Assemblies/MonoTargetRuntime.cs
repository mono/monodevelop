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
		
		internal static PcFileCache PcFileCache = new PcFileCache (new PcFileCacheContext ());
		
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

		public bool UserDefined { get; internal set; }
		
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

		
		internal protected override IEnumerable<string> GetGacDirectories ()
		{
			yield return Path.Combine (monoDir, "gac");
			
			string gacs;
			if (environmentVariables.TryGetValue ("MONO_GAC_PREFIX", out gacs)) {
				foreach (string path in gacs.Split (new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
					yield return path;
			}
		}
		
		public override string GetMSBuildBinPath (TargetFramework fx)
		{
			return Path.Combine (monoDir, "2.0");
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
			foreach (string pcdir in PkgConfigDirs)
				foreach (string pcfile in Directory.GetFiles (pcdir, "*.pc"))
					if (packageNames.Add (Path.GetFileNameWithoutExtension (pcfile)))
						yield return pcfile;
		}
		
		protected override void OnInitialize ()
		{
			foreach (string pcfile in GetAllPkgConfigFiles ()) {
				try {
					ParsePCFile (pcfile);
				}
				catch (Exception ex) {
					LoggingService.LogError ("Could not parse file '" + pcfile + "'", ex);
				}
			}
			PcFileCache.Save ();
		}

		private void ParsePCFile (string pcfile)
		{
			// Don't register the package twice
			string pname = Path.GetFileNameWithoutExtension (pcfile);
			if (RuntimeAssemblyContext.GetPackageInternal (pname) != null || IsCorePackage (pname))
				return;

			PackageInfo pinfo = PcFileCache.GetPackageInfo (pcfile);
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
	
	class PcFileCacheContext: Mono.PkgConfig.IPcFileCacheContext
	{
		public void ReportError (string message, System.Exception ex)
		{
			LoggingService.LogError (message, ex);
		}
		
		public bool IsCustomDataComplete (string pcfile, PackageInfo pkg)
		{
			return pkg.GetData ("targetFramework") != null;
		}
		
		public void StoreCustomData (PcFile pcfile, PackageInfo pinfo)
		{
			TargetFramework commonFramework = null;
			bool inconsistentFrameworks = false;
			
			foreach (PackageAssemblyInfo pi in pinfo.Assemblies) {
				string targetFramework = Runtime.SystemAssemblyService.GetTargetFrameworkForAssembly (Runtime.SystemAssemblyService.CurrentRuntime, pi.File);
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
						if (newfx.IsCompatibleWithFramework (commonFramework.Id))
							commonFramework = newfx;
						else if (!commonFramework.IsCompatibleWithFramework (newfx.Id))
							inconsistentFrameworks = true;
					}
				}
				if (inconsistentFrameworks)
					break;
			}
			if (inconsistentFrameworks)
				LoggingService.LogError ("Inconsistent target frameworks found in " + pcfile);
			if (commonFramework != null)
				pinfo.SetData ("targetFramework", commonFramework.Id);
			else
				pinfo.SetData ("targetFramework", "Unknown");
		}
	}
}
