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
		protected override string OnGetReferenceAssembliesFolder ()
		{
			FilePath dir = base.OnGetReferenceAssembliesFolder ();
			if (dir != null) {
				var frameworkList = dir.Combine ("RedistList", "FrameworkList.xml");
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
				return dir;
			}
			return null;
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
