// TargetFramework.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.Core.Serialization;
using System.Reflection;

namespace MonoDevelop.Core.Assemblies
{
	public class TargetFramework
	{
		[ItemProperty]
		string id;
		
		[ItemProperty ("_name")]
		string name;
		
		[ItemProperty]
		ClrVersion clrVersion;

		List<string> compatibleFrameworks = new List<string> ();
		List<string> extendedFrameworks = new List<string> ();

		internal bool RelationsBuilt;

		public static TargetFramework Default {
			get { return Runtime.SystemAssemblyService.GetTargetFramework ("1.1"); }
		}

		internal TargetFramework ()
		{
		}

		internal TargetFramework (string id)
		{
			this.id = id;
			this.name = id;
			clrVersion = ClrVersion.Default;
			Assemblies = new AssemblyInfo[0];
			IsSupported = false;
			compatibleFrameworks.Add (id);
			extendedFrameworks.Add (id);
		}
		
		public string Name {
			get {
				return name;
			}
		}
		
		public string Id {
			get {
				return id;
			}
		}
		
		public ClrVersion ClrVersion {
			get {
				return clrVersion;
			}
		}

		public bool IsCompatibleWithFramework (string fxId)
		{
			return compatibleFrameworks.Contains (fxId);
		}

		internal bool IsExtensionOfFramework (string fxId)
		{
			return extendedFrameworks.Contains (fxId);
		}

		internal List<string> CompatibleFrameworks {
			get { return compatibleFrameworks; }
		}

		internal List<string> ExtendedFrameworks {
			get { return extendedFrameworks; }
		}
		
		internal string BaseCoreFramework { get; set; }

		public bool IsSupported { get; internal set; }
		
		[ItemProperty]
		internal string ExtendsFramework { get; set; }
		
		[ItemProperty]
		internal string CompatibleWithFramework { get; set; }
		
		[ItemProperty]
		internal string SubsetOfFramework { get; set; }
		
		[ItemProperty]
		[ItemProperty ("Assembly", Scope="*")]
		internal AssemblyInfo[] Assemblies {
			get;
			set;
		}
		
		internal AssemblyInfo[] AssembliesExpanded {
			get;
			set;
		}
	}
	
	class AssemblyInfo
	{
		[ItemProperty ("name")]
		public string Name;
		
		[ItemProperty ("version")]
		public string Version;
		
		[ItemProperty ("publicKeyToken", DefaultValue="null")]
		public string PublicKeyToken;
		
		public void UpdateFromFile (string file)
		{
			Update (SystemAssemblyService.GetAssemblyNameObj (file));
		}
		
		public void Update (AssemblyName aname)
		{
			Name = aname.Name;
			Version = aname.Version.ToString ();
			string fn = aname.ToString ();
			string key = "publickeytoken=";
			int i = fn.ToLower().IndexOf (key) + key.Length;
			int j = fn.IndexOf (',', i);
			if (j == -1) j = fn.Length;
			PublicKeyToken = fn.Substring (i, j - i);
		}
		
		public AssemblyInfo Clone ()
		{
			return (AssemblyInfo) MemberwiseClone ();
		}
	}
}
