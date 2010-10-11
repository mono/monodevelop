// 
// MonoMacProjectConfiguration.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009-2010 Novell, Inc. (http://www.novell.com)
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

namespace MonoDevelop.MonoMac
{
	
	public class MonoMacProjectConfiguration : DotNetProjectConfiguration
	{
		public MonoMacProjectConfiguration () : base ()
		{
		}
		
		public MonoMacProjectConfiguration (string name) : base (name)
		{
		}
		
		public string AppName {
			get {
				return Path.GetFileNameWithoutExtension (OutputAssembly);
			}
		}
		
		public FilePath AppDirectory {
			get {
				return OutputDirectory.Combine (AppName + ".app");
			}
		}
		
		public FilePath AppDirectoryResources {
			get {
				return AppDirectory.Combine ("Contents", "Resources");
			}
		}
		
		public FilePath LaunchScript {
			get {
				return AppDirectory.Combine ("Contents", "MacOS", AppName);
			}
		}
		
		public override void CopyFrom (ItemConfiguration configuration)
		{
			base.CopyFrom (configuration);
			//var cfg = configuration as MonoMacProjectConfiguration;
		}
	}
}
