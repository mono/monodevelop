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
		string ref_assemblies_folder;

		string GetReferenceAssembliesFolder ()
		{
			if (ref_assemblies_folder != null)
				return ref_assemblies_folder;
			var fxDir = framework.Id.GetAssemblyDirectoryName ();
			foreach (var rootDir in ((MonoTargetRuntime)runtime).GetReferenceFrameworkDirectories ()) {
				var dir = rootDir.Combine (fxDir);
				var frameworkList = dir.Combine ("RedistList", "FrameworkList.xml");
				if (!File.Exists (frameworkList))
					continue;
				//check for the Mono-specific TargetFrameworkDirectory extension
				using (var reader = System.Xml.XmlReader.Create (frameworkList)) {
					if (reader.ReadToDescendant ("FileList") && reader.MoveToAttribute ("TargetFrameworkDirectory") && reader.ReadAttributeValue ()) {
						string targetDir = reader.ReadContentAsString ();
						if (!string.IsNullOrEmpty (targetDir)) {
							targetDir = targetDir.Replace ('\\', System.IO.Path.DirectorySeparatorChar);
							dir = frameworkList.ParentDirectory.Combine (targetDir).FullPath;
						}
					}
				}
				ref_assemblies_folder = dir;
				return dir;
			}
			return null;
		}
		
		public override IEnumerable<string> GetFrameworkFolders ()
		{
			var dir = GetReferenceAssembliesFolder ();
			if (dir != null)
				yield return dir;
			
			if (framework.Id.Identifier != TargetFrameworkMoniker.ID_NET_FRAMEWORK)
				yield break;
			
			string subdir;
			switch (framework.Id.Version) {
				case "1.1":
					subdir = "1.0"; break;
				case "3.0":
					// WCF is installed in the 2.0 directory. Others (olive) in 3.0.
					yield return Path.Combine (targetRuntime.MonoDirectory, "2.0");
					yield return Path.Combine (targetRuntime.MonoDirectory, "3.0");
					yield break;
				case "3.5":
					yield return Path.Combine (targetRuntime.MonoDirectory, "3.5");
					subdir = "2.0"; break;
				default:
					subdir = framework.Id.Version; break;
			}
			yield return Path.Combine (targetRuntime.MonoDirectory, subdir);
		}
		
		public override bool IsInstalled {
			get {
				if (framework.Id.Identifier == TargetFrameworkMoniker.ID_NET_FRAMEWORK &&
					framework.Id.Version == "3.0")
				{
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
		
		
		string GetOldMcsName (TargetFrameworkMoniker fx)
		{
			//old compilers for specific frameworks
			switch (fx.Identifier) {
			case TargetFrameworkMoniker.ID_NET_FRAMEWORK: {
					switch (fx.Version) {
					case "1.1":
						return "mcs";
					case "2.0":
					case "3.0":
					case "3.5":
						return "gmcs";
					case "4.0":
						return "dmcs";
					}
				}
				break;
			case TargetFrameworkMoniker.ID_MONODROID:
			case TargetFrameworkMoniker.ID_MONOTOUCH:
			case TargetFrameworkMoniker.ID_SILVERLIGHT:
				return "smcs";
			}

			return "mcs";
		}
		
		public override string GetToolPath (string toolName)
		{
			if (toolName == "csc" || toolName == "mcs") {
				if (targetRuntime.HasMultitargetingMcs) {
					toolName = "mcs";
				} else {
					toolName = GetOldMcsName (framework.Id);
				}
			}
			else if (toolName == "vbc")
				toolName = "vbnc";
			else if (toolName == "resgen") {
#pragma warning disable CS0618 // Type or member is obsolete
				if (framework.ClrVersion == ClrVersion.Net_1_1)
#pragma warning restore CS0618 // Type or member is obsolete
					toolName = "resgen1";
#pragma warning disable CS0618 // Type or member is obsolete
				else if (framework.ClrVersion == ClrVersion.Net_2_0)
#pragma warning restore CS0618 // Type or member is obsolete
					toolName = "resgen2";
			}
			else if (toolName == "msbuild")
				toolName = "xbuild";
			
			return base.GetToolPath (toolName);
		}
		
		public override SystemPackageInfo GetFrameworkPackageInfo (string packageName)
		{
			SystemPackageInfo info = base.GetFrameworkPackageInfo (packageName);
			if (framework.Id.Version == "3.0" && packageName == "olive") {
				info.IsCorePackage = false;
			}
			if (String.IsNullOrEmpty (info.Name)) {
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
