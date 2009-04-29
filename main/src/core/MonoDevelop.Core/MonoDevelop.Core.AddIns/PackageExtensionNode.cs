// PackageExtensionNode.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using System.IO;
using System.Collections;
using Mono.Addins;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Core.AddIns
{
	[ExtensionNode ("Package")]
	[ExtensionNodeChild (typeof(AssemblyExtensionNode))]
	internal class PackageExtensionNode: TypeExtensionNode
	{
		[NodeAttribute ("name", Required=true)]
		string name;
		
		[NodeAttribute ("version", Required=true)]
		string version;
		
		[NodeAttribute ("targetFramework")]
		string fxVersion;
		
		[NodeAttribute("clrVersion")]
		ClrVersion clrVersion = ClrVersion.Default;
		
		[NodeAttribute ("gacRoot")]
		bool hasGacRoot;
		
		string[] assemblies;
		
		public string[] Assemblies {
			get {
				lock (this) {
					if (assemblies == null) {
						assemblies = new string [ChildNodes.Count];
						for (int n=0; n<ChildNodes.Count; n++) {
							string file = ((AssemblyExtensionNode)ChildNodes [n]).FileName;
							file = base.Addin.GetFilePath (file);
							assemblies [n] = file;
						}
					}
				}
				return assemblies; 
			}
		}
		
		public SystemPackageInfo GetPackageInfo ()
		{
			SystemPackageInfo pi = new SystemPackageInfo ();
			pi.Name = name;
			pi.Version = version;
			
			if (fxVersion != null)
				pi.TargetFramework = fxVersion;
			else if (clrVersion == ClrVersion.Net_1_1)
				pi.TargetFramework = "1.1";
			else if (clrVersion == ClrVersion.Net_2_0)
				pi.TargetFramework = "2.0";
			
			if (hasGacRoot)
				pi.GacRoot = Addin.GetFilePath (".");
			
			return pi;
		}
	}
}
