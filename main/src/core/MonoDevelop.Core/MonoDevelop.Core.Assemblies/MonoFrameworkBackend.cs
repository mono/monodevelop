// 
// MonoFrameworkBackend.cs
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

namespace MonoDevelop.Core.Assemblies
{
	public class MonoFrameworkBackend: TargetFrameworkBackend<MonoTargetRuntime>
	{
		public override IEnumerable<string> GetFrameworkFolders ()
		{
			string subdir;
			switch (framework.Id) {
				case "1.1":
					subdir = "1.0"; break;
				case "3.0":
					// WFC is installed in the 2.0 directory. Others (olive) in 3.0.
					yield return Path.Combine (targetRuntime.MonoDirectory, "2.0");
					yield return Path.Combine (targetRuntime.MonoDirectory, "3.0");
					yield break;
				case "3.5":
					subdir = "2.0"; break;
				default:
					subdir = framework.Id; break;
			}
			yield return Path.Combine (targetRuntime.MonoDirectory, subdir);
		}
		
		public override bool IsInstalled {
			get {
				if (framework.Id == "3.0") {
					// This is a special case. The WCF assemblies are installed in the 2.0 directory.
					// There are other 3.0 assemblies which belong to the olive package (WCF doesn't)
					// and which are installed in the 3.0 directory. We consider 3.0 to be installed
					// if any of those assemblies are installed.
					if (base.IsInstalled)
						return true;
					string dir = Path.Combine (targetRuntime.MonoDirectory, "2.0");
					if (Directory.Exists (dir)) {
						string firstAsm = Path.Combine (dir, "System.ServiceModel.dll");
						return File.Exists (firstAsm);
					}
					return false;
				}
				return base.IsInstalled;
			}
		}
		 
		
		public override string GetToolPath (string toolName)
		{
			if (toolName == "csc" || toolName == "mcs") {
				if (framework.ClrVersion == ClrVersion.Net_1_1)
					toolName = "mcs";
				else if (framework.ClrVersion == ClrVersion.Net_2_0)
					toolName = "gmcs";
				else if (framework.ClrVersion == ClrVersion.Clr_2_1)
					toolName = "smcs";
				else if (framework.ClrVersion == ClrVersion.Net_4_0)
					toolName = "dmcs";
			}
			else if (toolName == "resgen") {
				if (framework.ClrVersion == ClrVersion.Net_1_1)
					toolName = "resgen1";
				else if (framework.ClrVersion == ClrVersion.Net_2_0)
					toolName = "resgen2";
			}
			else if (toolName == "msbuild")
				toolName = "xbuild";
			
			return base.GetToolPath (toolName);
		}
		
		public override SystemPackageInfo GetFrameworkPackageInfo (string packageName)
		{
			SystemPackageInfo info = base.GetFrameworkPackageInfo (packageName);
			if (framework.Id == "3.0" && packageName == "olive") {
				info.IsCorePackage = false;
			} else {
				info.Name = "mono";
			}
			return info;
		}
		
		public override IEnumerable<string> GetToolsPaths ()
		{
			yield return Path.Combine (targetRuntime.MonoRuntimeInfo.Prefix, "bin");
		}
		
		//NOTE: mcs, etc need to use the env vars too 	 
		public override Dictionary<string, string> GetToolsEnvironmentVariables ()
		{
			return targetRuntime.EnvironmentVariables;
		}
	}
}
