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
		public override string GetFrameworkFolder ()
		{
			string subdir;
			switch (framework.Id) {
				case "1.1":
					subdir = "1.0"; break;
				case "3.5":
					subdir = "2.0"; break;
				default:
					subdir = framework.Id; break;
			}
			return Path.Combine (targetRuntime.MonoDirectory, subdir);
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
			}
			else if (toolName == "resgen") {
				if (framework.ClrVersion == ClrVersion.Net_1_1)
					toolName = "resgen1";
				else if (framework.ClrVersion == ClrVersion.Net_2_0)
					toolName = "resgen2";
			}
			return base.GetToolPath (toolName);
		}
		
		public override SystemPackageInfo GetFrameworkPackageInfo ()
		{
			SystemPackageInfo info = base.GetFrameworkPackageInfo ();
			if (framework.Id == "3.0") {
				info.Name = "olive";
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
