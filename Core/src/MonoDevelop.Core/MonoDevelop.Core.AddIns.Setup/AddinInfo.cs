//
// AddinInfo.cs
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
using System.IO;
using System.Collections;
using System.Xml;

namespace MonoDevelop.Core.AddIns.Setup
{
	public class AddinInfo
	{
		string id;
		string version;
		string author;
		string copyright;
		string url;
		string description;
		PackageDependencyCollection dependencies;
		
		public AddinInfo ()
		{
			dependencies = new PackageDependencyCollection ();
		}
		
		public string Id {
			get { return id; }
			set { id = value; }
		}
		
		public string Version {
			get { return version; }
			set { version = value; }
		}
		
		public string Author {
			get { return author; }
			set { author = value; }
		}
		
		public string Copyright {
			get { return copyright; }
			set { copyright = value; }
		}
		
		public string Url {
			get { return url; }
			set { url = value; }
		}
		
		public string Description {
			get { return description; }
			set { description = value; }
		}
		
		public PackageDependencyCollection Dependencies {
			get { return dependencies; }
		}
		
		public static AddinInfo ReadFromAddinFile (StreamReader r)
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load (r);
			r.Close ();
			
			AddinInfo info = new AddinInfo ();
			info.id = doc.DocumentElement.GetAttribute ("name");
			info.version = doc.DocumentElement.GetAttribute ("version");
			info.author = doc.DocumentElement.GetAttribute ("author");
			info.copyright = doc.DocumentElement.GetAttribute ("copyright");
			info.url = doc.DocumentElement.GetAttribute ("url");
			info.description = doc.DocumentElement.GetAttribute ("description");
			
			foreach (XmlElement dep in doc.SelectNodes ("AddIn/Dependencies/AddIn")) {
				AddinDependency adep = new AddinDependency ();
				adep.AddinId = dep.GetAttribute ("name");
				string v = dep.GetAttribute ("version");
				if (v.Length != 0)
					adep.Version = v;
				info.Dependencies.Add (adep);
			}
			
			return info;
		}
	}
}
