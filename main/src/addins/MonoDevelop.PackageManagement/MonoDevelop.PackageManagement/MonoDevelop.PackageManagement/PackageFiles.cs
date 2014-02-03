// 
// PackageFiles.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class PackageFiles
	{
		IEnumerable<IPackageFile> files;
		
		public PackageFiles(IPackage package)
			: this(package.GetFiles())
		{
		}
		
		public PackageFiles(IEnumerable<IPackageFile> files)
		{
			this.files = files;
		}
		
		public bool HasAnyPackageScripts()
		{
			foreach (string fileName in GetFileNames()) {
				if (IsPackageScriptFile(fileName)) {
					return true;
				}
			}
			return false;
		}
		
		IEnumerable<string> GetFileNames()
		{
			foreach (IPackageFile file in files) {
				string fileName = Path.GetFileName(file.Path);
				yield return fileName;
			}
		}
		
		public bool HasUninstallPackageScript()
		{
			foreach (string fileName in GetFileNames()) {
				if (IsPackageUninstallScriptFile(fileName)) {
					return true;
				}
			}
			return false;
		}
		
		bool IsPackageScriptFile(string fileName)
		{
			return
				IsPackageInitializationScriptFile(fileName) ||
				IsPackageInstallScriptFile(fileName) ||
				IsPackageUninstallScriptFile(fileName);
		}
		
		bool IsPackageInitializationScriptFile(string fileName)
		{
			return IsCaseInsensitiveMatch(fileName, "init.ps1");
		}
		
		bool IsPackageInstallScriptFile(string fileName)
		{
			return IsCaseInsensitiveMatch(fileName, "install.ps1");
		}
		
		bool IsPackageUninstallScriptFile(string fileName)
		{
			return IsCaseInsensitiveMatch(fileName, "uninstall.ps1");
		}
		
		bool IsCaseInsensitiveMatch(string a, string b)
		{
			return String.Equals(a, b, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
