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
		public MonoDroidFrameworkBackend ()
		{
		}
			/*
			
			try {
				var sdkRoot = MonoDroidFramework.MonoDroidSdkLocation;
				
				sdkBin = sdkRoot.Combine ("bin");
				javaBinDir = MonoDroidFramework.JavaSdkLocation.Combine ("bin");
				androidBinDir = MonoDroidFramework.AndroidSdkLocation.Combine ("tools");
				
				
				if (!File.Exists (Path.Combine (sdkDir, "mscorlib.dll"))) {
					sdkDir = null;
				    throw new Exception ("Missing mscorlib in MonoDroid SDK " + sdkRoot);
				}
				
					
					
				}
				
			} catch (Exception ex) {
				LoggingService.LogError ("Unexpected error finding MonoDroid SDK directory", ex);
			}
		}
		*/
		
		public override IEnumerable<string> GetToolsPaths ()
		{
			yield return MonoDroidFramework.FrameworkDir;
			yield return MonoDroidFramework.BinDir;
			yield return MonoDroidFramework.AndroidBinDir;
			yield return MonoDroidFramework.JavaBinDir;
			foreach (var path in BaseGetToolsPaths ())
				yield return path;
		}
		
		IEnumerable<string> BaseGetToolsPaths ()
		{
			return base.GetToolsPaths ();
		}
		
		public override Dictionary<string, string> GetToolsEnvironmentVariables ()
		{
			return MonoDroidFramework.EnvironmentOverrides;
		}
		
		public override IEnumerable<string> GetFrameworkFolders ()
		{
			yield return MonoDroidFramework.FrameworkDir;
		}
		
		public override SystemPackageInfo GetFrameworkPackageInfo (string packageName)
		{
			SystemPackageInfo info = base.GetFrameworkPackageInfo ("monodroid");
			info.Name = "monodroid";
			return info;
		}
		
		public override bool IsInstalled {
			get { return MonoDroidFramework.IsInstalled; }
		}
	}
}
