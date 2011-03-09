// 
// MonoDroidProjectConfiguration.cs
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
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using System.IO;
using System.Text;
using System.Linq;

namespace MonoDevelop.MonoDroid
{
	public class MonoDroidProjectConfiguration : DotNetProjectConfiguration
	{
		public MonoDroidProjectConfiguration () : base ()
		{
		}
		
		public MonoDroidProjectConfiguration (string name) : base (name)
		{
		}
		
		public new MonoDroidProject ParentItem {
			get { return (MonoDroidProject) base.ParentItem; }
		}
		
		[ItemProperty ("MandroidExtraArgs")]
		string monoDroidExtraArgs;
		
		public string MonoDroidExtraArgs {
			get { return monoDroidExtraArgs; }
			set {
				if (value != null && value.Length == 0)
					value = null;
				monoDroidExtraArgs = value;
			}
		}
		
		[ProjectPathItemProperty ("AndroidManifest")]
		string androidManifest;
		
		/// <summary>
		/// Only for supporting advanced use. Overrides project.AndroidManifest. 
		/// </summary>
		public FilePath AndroidManifest {
			get { return androidManifest; }
			set {
				if (!value.IsNullOrEmpty && !value.IsAbsolute)
					value = value.ToAbsolute (ParentItem.BaseDirectory);
				androidManifest = value;
			}
		}
		
		[ItemProperty ("AndroidLinkMode", DefaultValue=MonoDroidLinkMode.Full)]
		[MonoDevelop.Projects.Formats.MSBuild.MergeToProject]
		MonoDroidLinkMode monoDroidLinkMode = MonoDroidLinkMode.Full;
		
		public MonoDroidLinkMode MonoDroidLinkMode {
			get { return monoDroidLinkMode; }
			set { monoDroidLinkMode = value; }
		}
		
		[ItemProperty ("AndroidUseSharedRuntime", DefaultValue=true)]
		[MonoDevelop.Projects.Formats.MSBuild.MergeToProject]
		bool androidUseSharedRuntime = true;
		
		public bool AndroidUseSharedRuntime {
			get { return androidUseSharedRuntime; }
			set { androidUseSharedRuntime = value; }
		}
		
		public string PackageName {
			get {
				return ParentItem.GetPackageName (this);
			}
		}
		
		public string ApkPath {
			get {
				string packageName = ParentItem.GetPackageName (this);
				if (packageName == null)
					return null;
				else
					return OutputDirectory.Combine (packageName) + ".apk";
			}
		}
		
		public string ApkSignedPath {
			get {
				string packageName = ParentItem.GetPackageName (this);
				if (packageName == null)
					return null;
				else
					return OutputDirectory.Combine (packageName) + "-Signed.apk";
			}
		}
		
		public FilePath ObjDir {
			get {
				return ParentItem.BaseDirectory.Combine ("obj", this.Name);
			}
		}
		
		public override void CopyFrom (ItemConfiguration configuration)
		{
			base.CopyFrom (configuration);
			var cfg = configuration as MonoDroidProjectConfiguration;
			if (cfg == null)
				return;
			
			monoDroidExtraArgs = cfg.monoDroidExtraArgs;
			androidManifest = cfg.androidManifest;
			monoDroidLinkMode = cfg.monoDroidLinkMode;
			androidUseSharedRuntime = cfg.androidUseSharedRuntime;
		}
	}
	
	public enum MonoDroidLinkMode
	{
		None,
		SdkOnly,
		Full
	}
}
