// 
// MonoDroidFrameworkBackend.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using Mono.Addins;
using MonoDevelop.Ide;
using Gtk;
using MonoDevelop.Core.Serialization;


namespace MonoDevelop.MonoDroid
{
	public class MonoDroidMonoFrameworkBackend : MonoFrameworkBackend
	{
		public override IEnumerable<string> GetToolsPaths ()
		{
			return MonoDroidFramework.GetToolsPaths ().Concat (base.GetToolsPaths ());
		}
		
		public override Dictionary<string, string> GetToolsEnvironmentVariables ()
		{
			return MonoDroidFramework.EnvironmentOverrides;
		}
		
		public override IEnumerable<string> GetFrameworkFolders ()
		{
			string version = framework.Id.Version;
			if (version == "1.0")
				return new string[] { MonoDroidFramework.MonoDroidFrameworkDir };
			
			string apiLevel = GetApiLevelFromVersion (version);
			if (apiLevel == null)
				return new string[0];
			
			return new string[] {  MonoDroidFramework.MonoDroidToolsDir.Combine ("platforms", "android-" + apiLevel) };
		}
		
		static string GetApiLevelFromVersion (string version)
		{
			switch (version) {
			case "1.6":   return "4";
			case "2.0":   return "5";
			case "2.0.1": return "6";
			case "2.1":   return "7";
			case "2.2":   return "8";
			case "2.3":   return "10";
			default:      return null;
			}
		}
		
		public override SystemPackageInfo GetFrameworkPackageInfo (string packageName)
		{
			string version = framework.Id.Version;
			bool isCore = version == "1.0";
			
			SystemPackageInfo info = base.GetFrameworkPackageInfo ("monodroid");
			info.Name = "monodroid";
			info.Version = isCore? "core" : version;
			info.Description = isCore? "MonoDroid Core" : "MonoDroid " + version;
			return info;
		}
		
		public override bool IsInstalled {
			get { return MonoDroidFramework.IsInstalled && base.IsInstalled; }
		}
	}
	
	
	public class MonoDroidMsNetFrameworkBackend : MsNetFrameworkBackend
	{
		public override IEnumerable<string> GetToolsPaths ()
		{
			return MonoDroidFramework.GetToolsPaths ().Concat (base.GetToolsPaths ());
		}
		
		public override Dictionary<string, string> GetToolsEnvironmentVariables ()
		{
			return MonoDroidFramework.EnvironmentOverrides;
		}
		
		public override IEnumerable<string> GetFrameworkFolders ()
		{
			yield return MonoDroidFramework.MonoDroidFrameworkDir.ParentDirectory.Combine ("v" + framework.Id.Version);
		}
		
		public override SystemPackageInfo GetFrameworkPackageInfo (string packageName)
		{
			string version = framework.Id.Version;
			bool isCore = version == "1.0";
			
			SystemPackageInfo info = base.GetFrameworkPackageInfo ("monodroid");
			info.Name = "monodroid";
			info.Version = isCore? "core" : version;
			info.Description = isCore? "MonoDroid Core" : "MonoDroid " + version;
			return info;
		}
		
		public override bool IsInstalled {
			get { return MonoDroidFramework.IsInstalled && base.IsInstalled; }
		}
	}
}
