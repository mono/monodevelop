//
// Repository.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

namespace Mono.Addins.Setup
{
	internal class Repository
	{
		RepositoryEntryCollection repositories;
		RepositoryEntryCollection addins;
		string name;
		internal string url;
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public string Url {
			get { return url; }
			set { url = value; }
		}
	
		[XmlElement ("Repository", Type = typeof(ReferenceRepositoryEntry))]
		public RepositoryEntryCollection Repositories {
			get {
				if (repositories == null)	
					repositories = new RepositoryEntryCollection (this);
				return repositories;
			}
		}
	
		[XmlElement ("Addin", Type = typeof(PackageRepositoryEntry))]
		public RepositoryEntryCollection Addins {
			get {
				if (addins == null)
					addins = new RepositoryEntryCollection (this);
				return addins;
			}
		}
		
		public RepositoryEntry FindEntry (string url)
		{
			if (Repositories != null) {
				foreach (RepositoryEntry e in Repositories)
					if (e.Url == url) return e;
			}
			if (Addins != null) {
				foreach (RepositoryEntry e in Addins)
					if (e.Url == url) return e;
			}
			return null;
		}
		
		public void AddEntry (RepositoryEntry entry)
		{
			entry.owner = this;
			if (entry is ReferenceRepositoryEntry) {
				Repositories.Add (entry);
			} else {
				Addins.Add (entry);
			}
		}
		
		public void RemoveEntry (RepositoryEntry entry)
		{
			if (entry is PackageRepositoryEntry)
				Addins.Remove (entry);
			else
				Repositories.Remove (entry);
		}
	}
}
