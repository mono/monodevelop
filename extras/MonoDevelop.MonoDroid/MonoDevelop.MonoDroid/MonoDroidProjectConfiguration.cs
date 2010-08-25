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
		
		[ItemProperty ("MonoDroidExtraArgs")]
		public string MonoDroidExtraArgs { get; set; }
		
		[MonoDevelop.Projects.Formats.MSBuild.MergeToProject]
		[ProjectPathItemProperty ("AndroidManifest")]
		public string AndroidManifest { get; set; }
		
		public string ApkPath {
			get {
				throw new NotImplementedException ();
			}
		}
		
		public string ApkSignedPath {
			get {
				throw new NotImplementedException ();
			}
		}
		
		public bool IsApplication {
			get {
				return !string.IsNullOrEmpty (AndroidManifest);
			}
		}
		
		public ProjectFile GetAndroidManifestFile (ConfigurationSelector conf)
		{
			string name = AndroidManifest;
			var pf = ParentItem.Files.GetFileWithVirtualPath (name);
			if (pf != null)
				return pf;
			
			name = ParentItem.BaseDirectory.Combine (name);
			var doc = new AndroidManifest ();
			throw new NotImplementedException ();
			//doc.WriteToFile (name);
			return ParentItem.AddFile (name);
		}
		
		public override void CopyFrom (ItemConfiguration configuration)
		{
			base.CopyFrom (configuration);
			var cfg = configuration as MonoDroidProjectConfiguration;
			if (cfg == null)
				return;
			
			MonoDroidExtraArgs = cfg.MonoDroidExtraArgs;
			AndroidManifest = cfg.AndroidManifest;
		}
	}
}
