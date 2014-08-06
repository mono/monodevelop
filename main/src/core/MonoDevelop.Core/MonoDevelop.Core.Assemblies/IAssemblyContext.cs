// 
// IAssemblyContext.cs
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
using System.Collections.Generic;

namespace MonoDevelop.Core.Assemblies
{
	public interface IAssemblyContext
	{
		[Obsolete ("Avoid use of SystemPackage")]
		IEnumerable<SystemPackage> GetPackages ();

		[Obsolete ("Avoid use of SystemPackage")]
		IEnumerable<SystemPackage> GetPackages (TargetFramework fx);

		SystemPackage GetPackage (string name);
		SystemPackage GetPackage (string name, string version);
		SystemPackage GetPackageFromPath (string path);
		SystemPackage[] GetPackagesFromFullName (string fullname);
		
		IEnumerable<SystemAssembly> GetAssemblies ();
		IEnumerable<SystemAssembly> GetAssemblies (TargetFramework fx);
		SystemAssembly[] GetAssembliesFromFullName (string fullname);
		SystemAssembly GetAssemblyFromFullName (string fullname, string package, TargetFramework fx);
		
		string FindInstalledAssembly (string fullname, string package, TargetFramework fx);
		string GetAssemblyLocation (string assemblyName, TargetFramework fx);
		string GetAssemblyLocation (string assemblyName, string package, TargetFramework fx);
		string GetAssemblyNameForVersion (string fullName, TargetFramework fx);
		string GetAssemblyNameForVersion (string fullName, string packageName, TargetFramework fx);
		SystemAssembly GetAssemblyForVersion (string fullName, string packageName, TargetFramework fx);
		string GetAssemblyFullName (string assemblyName, TargetFramework fx);
		bool AssemblyIsInGac (string aname);
		
		event EventHandler Changed;
	}
}
