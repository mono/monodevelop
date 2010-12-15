// 
// MacPackagingSettingsWidget.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.MacDev;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
namespace MonoDevelop.MonoMac.Gui
{
	public class MonoMacPackagingSettings
	{
		public bool IncludeMono { get; set; }
		public bool SignBundle { get; set; }
		public string BundleSigningKey { get; set; }
		public MonoMacLinkerMode LinkerMode { get; set; }
		
		public bool CreatePackage { get; set; }
		public bool SignPackage { get; set; }
		public string PackageSigningKey { get; set; }
		public FilePath ProductDefinition { get; set; }
		
		public static MonoMacPackagingSettings GetAppStoreDefault ()
		{
			return new MonoMacPackagingSettings () {
				IncludeMono = true,
				SignBundle = true,
				LinkerMode = MonoMacLinkerMode.LinkAll,
				CreatePackage = true,
				SignPackage = true,
			};
		}
	}
}
