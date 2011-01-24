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
using Mono.Addins;
using MonoDevelop.Core.AddIns;
using Mono.PkgConfig;

namespace MonoDevelop.Core.Assemblies
{
	public class TargetFramework
	{
		[ItemProperty(SerializationDataType=typeof(TargetFrameworkMonikerDataType))]
		TargetFrameworkMoniker id;
		
		[ItemProperty ("_name")]
		string name;
		
#pragma warning disable 0649
		[ItemProperty]
		bool hidden;
#pragma warning restore 0649
		
		[ItemProperty]
		ClrVersion clrVersion;

		List<TargetFrameworkMoniker> includedFrameworks = new List<TargetFrameworkMoniker> ();

		internal bool RelationsBuilt;
		
		internal static int FrameworkCount;
		internal int Index;
		string corlibVersion;

		public static TargetFramework Default {
			get { return Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.Default); }
		}

		internal TargetFramework ()
		{
			Index = FrameworkCount++;
		}

		internal TargetFramework (TargetFrameworkMoniker id)
		{
			Index = FrameworkCount++;
			this.id = id;
			this.name = id.Profile == null
				? string.Format ("{0} {1}", id.Identifier, id.Version)
				: string.Format ("{0} {1} {2} Profile", id.Identifier, id.Version, id.Profile);
			clrVersion = ClrVersion.Default;
			Assemblies = new AssemblyInfo[0];
		}
		
		public bool Hidden {
			get { return hidden; }
		}
		
		public string Name {
			get {
				return name;
			}
		}
		
		public TargetFrameworkMoniker Id {
			get {
				return id;
			}
		}
		
		public ClrVersion ClrVersion {
			get {
				return clrVersion;
			}
		}

		public bool IsCompatibleWithFramework (TargetFrameworkMoniker fxId)
		{
			return fxId.Identifier == this.id.Identifier
				&& new Version (fxId.Version).CompareTo (new Version (this.id.Version)) <= 0;
		}
		
		internal string GetCorlibVersion ()
		{
			if (corlibVersion != null)
				return corlibVersion;
			
			foreach (AssemblyInfo asm in Assemblies) {
				if (asm.Name == "mscorlib")
					return corlibVersion = asm.Version;
			}
			return corlibVersion = string.Empty;
		}

		internal TargetFrameworkNode FrameworkNode { get; set; }
		
		internal TargetFrameworkBackend CreateBackendForRuntime (TargetRuntime runtime)
		{
			if (FrameworkNode == null || FrameworkNode.ChildNodes == null)
				return null;
			foreach (TypeExtensionNode node in FrameworkNode.ChildNodes) {
				TargetFrameworkBackend backend = (TargetFrameworkBackend) node.CreateInstance (typeof (TargetFrameworkBackend));
				if (backend.SupportsRuntime (runtime))
					return backend;
			}
			return null;
		}
		
		public bool IncludesFramework (TargetFrameworkMoniker id)
		{
			return id == this.id || includedFrameworks.Contains (id);
		}

		internal List<TargetFrameworkMoniker> IncludedFrameworks {
			get { return includedFrameworks; }
		}
				
		[ItemProperty (Name="IncludesFramework")]
		string includesFramework;
		
		internal TargetFrameworkMoniker GetIncludesFramework ()
		{
			if (string.IsNullOrEmpty (includesFramework))
				return null;
			string version = includesFramework[0] == 'v'?
				includesFramework.Substring (1) : includesFramework;
			if (version.Length == 0)
				throw new InvalidOperationException ("Invalid include version in framework " + id);
			
			return new TargetFrameworkMoniker (id.Identifier, version);	
		}
		
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
		
		public override string ToString ()
		{
			return string.Format ("[TargetFramework: Hidden={0}, Name={1}, Id={2}, ClrVersion={3}]",
				Hidden, Name, Id, ClrVersion);
		}
	}
	
	class AssemblyInfo
	{
		[ItemProperty ("name")]
		public string Name = null;
		
		[ItemProperty ("version")]
		public string Version = null;
		
		[ItemProperty ("publicKeyToken", DefaultValue="null")]
		public string PublicKeyToken = null;
		
		[ItemProperty ("package")]
		public string Package = null;
		
		public AssemblyInfo ()
		{
		}
		
		public AssemblyInfo (PackageAssemblyInfo info)
		{
			Name = info.Name;
			Version = info.Version;
			PublicKeyToken = info.PublicKeyToken;
		}
		
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
