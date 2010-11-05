// 
// IPhoneProject.cs
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
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using System.Reflection;
using MonoDevelop.MacDev.Plist;
using System.Text;
namespace MonoDevelop.IPhone
{
	[Mono.Addins.Extension]
	class IPhoneProjectStringTagProvider : MonoDevelop.Core.StringParsing.StringTagProvider<IPhoneProject>  
	{
		public override IEnumerable<MonoDevelop.Core.StringParsing.StringTagDescription> GetTags ()
		{
			yield return new MonoDevelop.Core.StringParsing.StringTagDescription ("BundleIdentifier", GettextCatalog.GetString ("iPhone Bundle Identifier"));
			yield return new MonoDevelop.Core.StringParsing.StringTagDescription ("BundleVersion", GettextCatalog.GetString ("iPhone Bundle Version"));
		}
		
		public override object GetTagValue (IPhoneProject instance, string tag)
		{
			switch (tag) {
			case "BUNDLEIDENTIFIER":
				return instance.BundleIdentifier;
			case "BUNDLEVERSION":
				return instance.BundleVersion;
			}
			throw new NotSupportedException ();
		}
	}
}
