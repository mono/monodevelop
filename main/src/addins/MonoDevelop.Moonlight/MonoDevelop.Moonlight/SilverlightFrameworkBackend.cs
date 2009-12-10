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
			base.Initialize (runtime, framework);
			switch (framework.Id) {
			case "SL2.0":
				fxVersion = "2.0";
				break;
			case "SL3.0":
				fxVersion = "3.0";
				break;
			default:
				throw new InvalidOperationException ("Cannot handle unknown target framework '" + framework.Id +"'");
			}
			
			foreach (var dir in GetMoonDirectories ()) {
				var fxdir = dir.Combine (fxVersion);
				var buildVersion = fxdir.Combine ("buildversion");
				if (Directory.Exists (fxdir) && Directory.Exists (fxdir + "-redist") && File.Exists (buildVersion)) {
					if (LoadVersionString (buildVersion) && RegisterRedistAssemblies (fxdir))
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
				IsGacPackage = true,
				IsCorePackage = true,
				TargetFramework = framework.Id,
			};
			var dir = location.Combine (fxVersion, "-redist");
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
			yield return ((FilePath)base.targetRuntime.Prefix).Combine ("lib", "moonlight");
		}
		
		public override bool IsInstalled {
			get { return !location.IsNullOrEmpty; }
		}
		
		public override IEnumerable<string> GetToolsPaths ()
		{
			foreach (var f in GetFrameworkFolders ())
				yield return f;
			foreach (var f in base.GetToolsPaths ())
				yield return f;
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
	}
}
