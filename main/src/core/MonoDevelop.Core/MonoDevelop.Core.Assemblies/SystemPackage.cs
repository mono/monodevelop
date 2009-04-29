//
// SystemPackage.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Core.Assemblies
{
	public class SystemPackage
	{
		string name;
		string version;
		string description;
		SystemAssembly assemblies;
		bool isInternal;
		string targetFramework;
		string gacRoot;
		bool gacPackage;
		TargetRuntime targetRuntime;
		
		internal SystemPackage (TargetRuntime targetRuntime)
		{
			this.targetRuntime = targetRuntime;
		}
		
		internal void Initialize (SystemPackageInfo info, IEnumerable<SystemAssembly> assemblies, bool isInternal)
		{
			this.isInternal = isInternal;
			this.name = info.Name ?? string.Empty;
			this.version = info.Version ?? string.Empty;
			this.description = info.Description ?? string.Empty;
			this.targetFramework = info.TargetFramework;
			this.gacRoot = info.GacRoot;
			this.gacPackage = info.IsGacPackage;
			IsFrameworkPackage = info.IsFrameworkPackage;
			IsCorePackage = info.IsCorePackage;

			SystemAssembly last = null;
			foreach (SystemAssembly asm in assemblies) {
				if (asm == null)
					continue;
				asm.Package = this;
				if (this.assemblies == null)
					this.assemblies = asm;
				else
					last.NextSamePackage = asm;
				last = asm;
			}
		}
		
		public TargetRuntime TargetRuntime {
			get { return targetRuntime; }
		}
		
		public string Name {
			get { return name; }
		}
		
		public string GacRoot {
			get { return gacRoot; }
		}
		
		public bool IsGacPackage {
			get { return gacPackage; }
		}
		
		public string Version {
			get { return version; }
		}
		
		public string Description {
			get { return description; }
		}
		
		public string TargetFramework {
			get { return targetFramework ?? "1.1"; }
		}
		
		// The package is part of the core mono SDK
		public bool IsCorePackage {
			get;
			internal set;
		}
		
		// The package has been registered by an add-in, and is not installed
		// in the system.
		public bool IsInternalPackage {
			get { return isInternal; }
		}
		
		// The package is part of the mono SDK (unlike IsCorePackage, it may be provided by a non-core package)
		public bool IsFrameworkPackage {
			get;
			internal set;
		}
		
		public IEnumerable<SystemAssembly> Assemblies {	
			get {
				SystemAssembly asm = assemblies;
				while (asm != null) {
					yield return asm;
					asm = asm.NextSamePackage;
				}
			}
		}
	}
	
	public class SystemPackageInfo
	{
		public SystemPackageInfo ()
		{
			IsGacPackage = true;
		}

		[ItemProperty]
		public string Name { get; set; }
		
		[ItemProperty (DefaultValue=null)]
		public string GacRoot { get; set; }
		
		[ItemProperty (DefaultValue=true)]
		public bool IsGacPackage { get; set; }
		
		[ItemProperty]
		public string Version { get; set; }
		
		[ItemProperty (DefaultValue=null)]
		public string Description { get; set; }
		
		public string TargetFramework { get; set; }
		
		// The package is part of the core mono SDK
		[ItemProperty (DefaultValue=false)]
		public bool IsCorePackage { get; set; }
		
		// The package is part of the mono SDK (unlike IsCorePackage, it may be provided by a non-core package)
		[ItemProperty (DefaultValue=false)]
		public bool IsFrameworkPackage { get; set; }
		
		[ExpandedCollection]
		[ItemProperty ("Assembly")]
		internal List<PackageAssemblyInfo> Assemblies { get; set; }
		
		[ItemProperty]
		internal DateTime LastWriteTime { get; set; }
		
		internal bool IsValidPackage {
			get { return Assemblies != null && Assemblies.Count > 0; }
		}
	}
	
	class PackageAssemblyInfo: AssemblyInfo
	{
		[ItemProperty]
		public string File { get; set; }
	}
}
