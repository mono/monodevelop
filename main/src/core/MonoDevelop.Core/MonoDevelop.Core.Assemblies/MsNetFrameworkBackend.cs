// 
// MsNetFrameworkBackend.cs
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
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;

namespace MonoDevelop.Core.Assemblies
{
	public class MsNetFrameworkBackend: TargetFrameworkBackend<MsNetTargetRuntime>
	{
		string ref_assemblies_folder;

		string GetReferenceAssembliesFolder ()
		{
			if (ref_assemblies_folder != null)
				return ref_assemblies_folder;
			
			var fxDir = framework.Id.GetAssemblyDirectoryName ();
			foreach (var rootDir in runtime.GetReferenceFrameworkDirectories ()) {
				var dir = rootDir.Combine (fxDir);
				var frameworkList = dir.Combine ("RedistList", "FrameworkList.xml");
				if (File.Exists (frameworkList))
					return ref_assemblies_folder = dir;
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
			
			switch (framework.Id.Version) {
			case "1.1":
			case "2.0":
				yield return targetRuntime.RootDirectory.Combine (GetClrVersion (framework.ClrVersion));
				break;
			case "4.0":
			case "4.5":
				var fx40dir = targetRuntime.RootDirectory.Combine (GetClrVersion (framework.ClrVersion));
				yield return fx40dir;
				yield return fx40dir.Combine ("WPF");
				break;
			case "3.0":
			case "3.5":
				RegistryKey fxFolderKey = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\.NETFramework\AssemblyFolders\v" + framework.Id.Version, false);
				if (fxFolderKey != null) {
					string folder = fxFolderKey.GetValue ("All Assemblies In") as string;
					fxFolderKey.Close ();
					yield return folder;
				}
				break;
			}
		}
		
		public override Dictionary<string, string> GetToolsEnvironmentVariables ()
		{
			var vars = new Dictionary<string, string> ();
			var path = new System.Text.StringBuilder ();
			foreach (var s in GetFrameworkToolsPaths ()) {
				path.Append (s);
				path.Append (Path.PathSeparator);
			}
			path.Append (Environment.GetEnvironmentVariable ("PATH"));
			vars["PATH"] = path.ToString ();
			return vars;
		}

		public override IEnumerable<string> GetToolsPaths ()
		{
			foreach (string s in GetFrameworkToolsPaths ())
				yield return s;
			foreach (string s in BaseGetToolsPaths ())
				yield return s;
			yield return PropertyService.EntryAssemblyPath;
		}

		// ProgramFilesX86 is broken on 32-bit WinXP, this is a workaround
		static string GetProgramFilesX86 ()
		{
			return Environment.GetFolderPath (IntPtr.Size == 8?
				Environment.SpecialFolder.ProgramFilesX86 : Environment.SpecialFolder.ProgramFiles);
		}
		
		IEnumerable<string> GetFrameworkToolsPaths ()
		{
			//FIXME: use the toolversion from the project file
			TargetFrameworkToolsVersion toolsVersion = framework.GetToolsVersion ();

			string programFilesX86 = GetProgramFilesX86 ();
			string sdkPath = Path.Combine (programFilesX86, "Microsoft SDKs", "Windows");
			
			switch (toolsVersion) {
			case TargetFrameworkToolsVersion.V1_1:
				yield return Path.Combine (sdkPath, "v6.0A\\bin");
				yield return targetRuntime.RootDirectory.Combine (GetClrVersion (ClrVersion.Net_1_1));
				break;
			case TargetFrameworkToolsVersion.V2_0:
				yield return Path.Combine (sdkPath, "v6.0A\\bin");
				yield return targetRuntime.RootDirectory.Combine (GetClrVersion (ClrVersion.Net_2_0));
				break;
			case TargetFrameworkToolsVersion.V3_5:
				yield return Path.Combine (sdkPath, "v7.0A\\bin");
				yield return targetRuntime.RootDirectory.Combine ("v3.5");
				goto case TargetFrameworkToolsVersion.V2_0;
			case TargetFrameworkToolsVersion.V4_0:
				yield return Path.Combine (sdkPath, "v7.0A\\bin\\NETFX 4.0 Tools");
				yield return targetRuntime.RootDirectory.Combine (GetClrVersion (ClrVersion.Net_4_0));
				break;
			default:
				throw new Exception ("Unknown ToolsVersion");
			}
		}

		//base isn't verifiably accessible from the enumerator so use this private helper
		IEnumerable<string> BaseGetToolsPaths ()
		{
			return base.GetToolsPaths ();
		}
		
		internal static string GetClrVersion (ClrVersion v)
		{
			switch (v) {
				case ClrVersion.Net_1_1: return "v1.1.4322";
				case ClrVersion.Net_2_0: return "v2.0.50727";
				case ClrVersion.Net_4_5: // The 4_5 binaries have the same version as the NET_4_0 binaries
				case ClrVersion.Net_4_0: return "v4.0.30319";
			}
			return null;
		}
	}
}
