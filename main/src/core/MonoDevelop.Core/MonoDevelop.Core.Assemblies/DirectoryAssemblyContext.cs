// 
// DirectoryAssemblyContext.cs
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
using Mono.PkgConfig;

namespace MonoDevelop.Core.Assemblies
{
	public class DirectoryAssemblyContext: AssemblyContext
	{
		List<SystemPackage> packages = new List<SystemPackage> ();
		List<string> directories = new List<string> ();
		object updatesLock = new object ();
		bool pendingUpdate;
		
		public IEnumerable<string> Directories {
			get {
				return directories;
			}
			set {
				lock (updatesLock) {
					directories = new List<string> (value);
					if (!pendingUpdate) {
						pendingUpdate = true;
						Runtime.SystemAssemblyService.CurrentRuntime.Initialized += OnUpdatePackages;
					}
					NotifyChanged ();
				}
			}
		}
		
		void OnUpdatePackages (object o, EventArgs a)
		{
			lock (updatesLock) {
				Runtime.SystemAssemblyService.CurrentRuntime.Initialized -= OnUpdatePackages;
			
				foreach (SystemPackage p in packages)
					UnregisterPackage (p);
				packages.Clear ();
				
				foreach (string dir in directories) {
					if (Directory.Exists (dir)) {
						try {
							foreach (string file in Directory.GetFiles (dir)) {
								string ext = Path.GetExtension (file);
								if (ext == ".dll")
									RegisterAssembly (file);
								else if (ext == ".pc")
									RegisterPcFile (file);
							}
						} catch (Exception ex) {
							LoggingService.LogError ("Error while updating assemblies from directory: " + dir, ex);
						}
					} else {
						LoggingService.LogWarning ("User defined assembly forlder doesn't exist: " + dir);
					}
				}
				pendingUpdate = false;
			}
		}
		
		void RegisterAssembly (string file)
		{
			SystemPackageInfo spi = new SystemPackageInfo ();
			spi.Name = file;
			spi.Description = Path.GetDirectoryName (file);
			spi.Version = "";
			spi.IsGacPackage = false;
			spi.TargetFramework = Runtime.SystemAssemblyService.GetTargetFrameworkForAssembly (Runtime.SystemAssemblyService.CurrentRuntime, file);
			
			SystemPackage sp = RegisterPackage (spi, true, file);
			packages.Add (sp);
		}
		
		void RegisterPcFile (string file)
		{
			LibraryPackageInfo pinfo = MonoTargetRuntime.PcFileCache.GetPackageInfo (file);
			if (pinfo.IsValidPackage) {
				SystemPackage sp = RegisterPackage (pinfo, true);
				packages.Add (sp);
			}
		}
	}
}
