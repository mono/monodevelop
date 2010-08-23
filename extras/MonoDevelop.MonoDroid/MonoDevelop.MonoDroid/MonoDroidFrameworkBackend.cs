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
	public class MonoDroidFrameworkBackend : MonoFrameworkBackend
	{
		string sdkDir;
		string sdkBin;
		
		public MonoDroidFrameworkBackend ()
		{
			var sdkRoot = MonoDroidFramework.MonoDroidSdkPath;
			if (Directory.Exists (sdkRoot)) {
				try {
					sdkDir = sdkRoot + "/usr/lib/mono/2.1";
					sdkBin = sdkRoot + "/usr/bin";
					if (!File.Exists (Path.Combine (sdkDir, "mscorlib.dll"))) {
						sdkDir = null;
					    throw new Exception ("Missing mscorlib in MonoDroid SDK " + sdkRoot);
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Unexpected error finding MonoDroid SDK directory", ex);
				}
			}
		}
		
		public override IEnumerable<string> GetToolsPaths ()
		{
			yield return sdkBin;
			yield return sdkDir;
			foreach (string path in base.GetToolsPaths ())
				yield return path;
		}
		
		public override IEnumerable<string> GetFrameworkFolders ()
		{
			yield return sdkDir;
		}
		
		public override SystemPackageInfo GetFrameworkPackageInfo (string packageName)
		{
			SystemPackageInfo info = base.GetFrameworkPackageInfo ("monodroid");
			info.Name = "monodroid";
			return info;
		}
		
		public override bool IsInstalled {
			get { return sdkDir != null; }
		}
	}
}
