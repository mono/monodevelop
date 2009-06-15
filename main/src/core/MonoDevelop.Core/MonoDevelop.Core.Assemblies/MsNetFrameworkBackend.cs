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
		public override string GetFrameworkFolder ()
		{
			switch (framework.Id) {
				case "1.1":
				case "2.0": return targetRuntime.RootDirectory.Combine (GetClrVersion (framework.ClrVersion));
			}

			RegistryKey fxFolderKey = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\.NETFramework\AssemblyFolders\v" + framework.Id, false);
			if (fxFolderKey != null) {
				string folder = fxFolderKey.GetValue ("All Assemblies In") as string;
				fxFolderKey.Close ();
				return folder;
			}
			return "";
		}
		
		public override Dictionary<string, string> GetToolsEnvironmentVariables ()
		{
			Dictionary<string, string> vars = new Dictionary<string, string> ();
			string path = Environment.GetEnvironmentVariable ("PATH");
			vars["PATH"] = GetFrameworkToolsPath () + Path.PathSeparator + path;
			return vars;
		}

		public override IEnumerable<string> GetToolsPaths ()
		{
			string path = GetFrameworkToolsPath ();
			if (path != null)
				yield return path;

			string sdkPath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles), "Microsoft SDKs");
			sdkPath = Path.Combine (sdkPath, "Windows");
			if (framework.Id == "4.0")
				yield return Path.Combine (sdkPath, "v7.0A\\bin\\NETFX 4.0 Tools");
			else if (framework.Id == "3.5")
				yield return Path.Combine (sdkPath, "v7.0A\\bin");
			else
				yield return Path.Combine (sdkPath, "v6.0A\\bin");

			foreach (string s in base.GetToolsPaths ())
				yield return s;
		}
		
		string GetFrameworkToolsPath ()
		{
			if (framework.Id == "1.1" || framework.Id == "2.0" || framework.Id == "4.0")
				return targetRuntime.RootDirectory.Combine (GetClrVersion (framework.ClrVersion));

			if (framework.Id == "3.0")
				return targetRuntime.RootDirectory.Combine (GetClrVersion (ClrVersion.Net_2_0));
 
			return targetRuntime.RootDirectory.Combine ("v" + framework.Id);
		}
		
		string GetClrVersion (ClrVersion v)
		{
			switch (v) {
				case ClrVersion.Net_1_1: return "v1.1.4322";
				case ClrVersion.Net_2_0: return "v2.0.50727";
				case ClrVersion.Clr_2_1: return "v2.1";
				case ClrVersion.Net_4_0: return "v4.0.20506";
			}
			return "?";
		}
	}
}
