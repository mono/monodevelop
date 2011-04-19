// 
// SilverlightFrameworkBackend.cs
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
using System.Collections.Generic;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core;

namespace MonoDevelop.Moonlight
{
	class MoonlightFrameworkBackend: MonoFrameworkBackend
	{
		string fxVersion;
		FilePath location;
		string pluginVersion;
		
		protected override void Initialize (TargetRuntime runtime, TargetFramework framework)
		{
			if (framework.Id.Identifier != "Silverlight")
				throw new InvalidOperationException (string.Format ("Cannot handle unknown framework {0}", framework.Id));
			
			base.Initialize (runtime, framework);
			fxVersion = framework.Id.Version;
			
			foreach (var dir in GetMoonDirectories ()) {
				var fxdir = dir.Combine (fxVersion);
				var buildVersion = fxdir.Combine ("buildversion");
				if (Directory.Exists (fxdir) && Directory.Exists (fxdir + "-redist") && File.Exists (buildVersion)) {
					if (LoadVersionString (buildVersion) && RegisterRedistAssemblies (dir))
						this.location = dir;
					break;
				}
			}
		}
		
		bool LoadVersionString (FilePath buildVersion)
		{
			try {
				using (var reader = File.OpenText (buildVersion)) {
					var line = reader.ReadLine ();
					if (!string.IsNullOrEmpty (line)) {
						this.pluginVersion = line;
						return true;
					} else {
						LoggingService.LogError ("Could not get SL build version from file '" + buildVersion + "'");
					}
				}
			} catch (IOException ex) {
				LoggingService.LogError ("Could not get SL build version from file '" + buildVersion + "'", ex);
			}
			return false;
		}

		bool RegisterRedistAssemblies (FilePath location)
		{
			var info = new SystemPackageInfo () {
				Name = "moonlight-web-" + fxVersion + "-redist",
				Description = "Moonlight " + fxVersion + " Redistributable Assemblies",
				Version = pluginVersion,
				IsFrameworkPackage = true,
				IsGacPackage = false,
				IsCorePackage = false,
				TargetFramework = framework.Id,
			};
			var dir = location.Combine (fxVersion + "-redist");
			try {
				var files = Directory.GetFiles (dir, "*.dll");
				runtime.RegisterPackage (info, files);
				return true;
			} catch (IOException ex) {
				LoggingService.LogError ("Could not enumerate redist assemblies from directory '" + dir + "'", ex);
				return false;
			}
		}
		
		IEnumerable<FilePath> GetMoonDirectories ()
		{
			string path;
			if (targetRuntime.EnvironmentVariables.TryGetValue ("MOONLIGHT_SDK_PATH", out path))
				yield return (FilePath) path;
			yield return ((FilePath)targetRuntime.Prefix).Combine ("lib", "moonlight");
			var env = System.Environment.GetEnvironmentVariable ("MOONLIGHT_SDK_PATH");
			if (!string.IsNullOrEmpty (env))
				yield return (FilePath) env;
		}
		
		public override bool IsInstalled {
			get { return !location.IsNullOrEmpty; }
		}
		
		public override IEnumerable<string> GetToolsPaths ()
		{
			yield return location;
			yield return location.Combine (fxVersion);
			foreach (var f in BaseGetToolsPaths ())
				yield return f;
		}
		
		//WORKAROUND for gmcs code generation bug - base not properly accessible from generators.
		//Should be fixed in Mono 2.8 final. 
		IEnumerable<string> BaseGetToolsPaths ()
		{
			return base.GetToolsPaths ();
		}
		
		Dictionary<string, string> envVars;
		
		public override Dictionary<string, string> GetToolsEnvironmentVariables ()
		{
			if (envVars == null) {
				envVars = base.GetToolsEnvironmentVariables ();
				if (envVars.ContainsKey ("MONO_PATH")) {
					envVars = new Dictionary<string,string> (envVars);
					envVars.Remove ("MONO_PATH");
				}
			}
			return envVars;
		}
		
		public override IEnumerable<string> GetFrameworkFolders ()
		{
			yield return location.Combine (fxVersion);
			yield return location.Combine (fxVersion + "-redist");
		}
		
		public override SystemPackageInfo GetFrameworkPackageInfo (string packageName)
		{
			SystemPackageInfo info = base.GetFrameworkPackageInfo (packageName);
			info.Name = "moonlight-web-" + fxVersion;
			info.Description = "Moonlight " + fxVersion;
			info.Version = pluginVersion;
			return info;
		}
		
		public override string GetToolPath (string toolName)
		{
			// never run smcs.exe directly. 
			// base method also checks for the exe in each path, but don't want that
			// it needs magic mono runtime args and maybe a special runtime.
			if (toolName == "csc" || toolName == "mcs" || toolName == "smcs")
				return GetToolsPaths ().Select (p => Path.Combine (p, "smcs")).Where (File.Exists).FirstOrDefault ();
			
			return base.GetToolPath (toolName);
		}
	}
}
