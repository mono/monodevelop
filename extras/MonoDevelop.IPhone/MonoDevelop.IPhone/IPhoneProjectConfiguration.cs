// 
// IPhoneProjectConfiguration.cs
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
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using System.IO;



namespace MonoDevelop.IPhone
{
	
	
	public class IPhoneProjectConfiguration : DotNetProjectConfiguration
	{
		public IPhoneProjectConfiguration () : base ()
		{
		}
		
		public IPhoneProjectConfiguration (string name) : base (name)
		{
		}
		
		public FilePath AppDirectory {
			get {
				IPhoneProject proj = ParentItem as IPhoneProject;
				if (proj != null)
					return OutputDirectory.Combine (proj.Name + ".app");
				return FilePath.Null;
			}
		}
		
		public FilePath NativeExe {
			get {
				IPhoneProject proj = ParentItem as IPhoneProject;
				if (proj != null)
					return OutputDirectory.Combine (proj.Name + ".app")
						.Combine (Path.GetFileNameWithoutExtension (OutputAssembly));
				return null;
			}
		}
		
		[ItemProperty ("ExtraMtouchArgs")]
		public string ExtraMtouchArgs { get; set; }
		
		public override void CopyFrom (ItemConfiguration configuration)
		{
			var cfg = (IPhoneProjectConfiguration) configuration;
			base.CopyFrom (configuration);
			ExtraMtouchArgs = cfg.ExtraMtouchArgs;
		}
		
	}
}
