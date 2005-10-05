//
// AddinDependency.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Xml;
using System.Xml.Serialization;

namespace MonoDevelop.Core.AddIns.Setup
{
	[XmlType ("AddInReference")]
	public class AddinDependency: PackageDependency
	{
		string id;
		string version;
		
		public string AddinId {
			get { return id; }
			set { id = value; }
		}
		
		public string Version {
			get { return version; }
			set { version = value; }
		}
		
		public override bool CheckInstalled (SetupService service)
		{
			AddinInfo[] addins = service.GetInstalledAddins ();
			foreach (AddinInfo addin in addins) {
				if (addin.Id == id && addin.Version == version) {
					return true;
				}
			}
			return false;
		}
		
		public override bool Resolve (IProgressMonitor monitor, SetupService service, PackageCollection packages)
		{
			foreach (Package p in packages) {
				AddinPackage ap = p as AddinPackage;
				if (ap != null) {
					if (ap.Addin.Id == id && ap.Addin.Version == version)
						return true;
				} 
			}
			
			AddinInfo[] addins = service.GetInstalledAddins ();
			foreach (AddinInfo addin in addins) {
				if (addin.Id == id && addin.Version == version) {
					return true;
				}
			}
			
			AddinRepositoryEntry[] avaddins = service.GetAvailableAddins ();
			foreach (AddinRepositoryEntry avAddin in avaddins) {
				if (avAddin.Addin.Id == id && avAddin.Addin.Version == version) {
					packages.Add (AddinPackage.FromAddin (avAddin));
					return true;
				}
			}
			return false;
		}
	}
}
