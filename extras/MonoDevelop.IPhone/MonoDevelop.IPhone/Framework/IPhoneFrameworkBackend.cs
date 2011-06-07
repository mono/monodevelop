// 
// IPhoneFrameworkBackend.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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


namespace MonoDevelop.IPhone
{
	public class IPhoneFrameworkBackend : MonoFrameworkBackend
	{
		const string SDK_ROOT = "/Developer/MonoTouch";
		
		string sdkDir;
		string sdkBin;
		
		public IPhoneFrameworkBackend ()
		{
			if (Directory.Exists (SDK_ROOT)) {
				try {
					sdkDir = SDK_ROOT + "/usr/lib/mono/2.1";
					sdkBin = SDK_ROOT + "/usr/bin";
					if (!File.Exists (Path.Combine (sdkDir, "mscorlib.dll"))) {
						sdkDir = null;
					    throw new Exception ("Missing mscorlib in iPhone SDK " + SDK_ROOT);
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Unexpected error finding iPhone SDK directory", ex);
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
			SystemPackageInfo info = base.GetFrameworkPackageInfo ("mono-iphone");
			info.Name = "mono-iphone";
			return info;
		}
		
		public override bool IsInstalled {
			get { return sdkDir != null; }
		}
	}
}
