//
// PackageRepositoryEntry.cs
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

namespace Mono.Addins.Setup
{
	internal class PackageRepositoryEntry: RepositoryEntry, AddinRepositoryEntry, IComparable
	{
		AddinInfo addin;
		
		public AddinInfo Addin {
			get { return addin; }
			set { addin = value; }
		}
		
		AddinHeader AddinRepositoryEntry.Addin {
			get { return addin; }
		}
		
		public string RepositoryUrl {
			get { return Repository.Url; }
		}
		
		public string RepositoryName {
			get { return Repository.Name; }
		}
		
		public int CompareTo (object other)
		{
			PackageRepositoryEntry rep = (PackageRepositoryEntry) other;
			string n1 = Mono.Addins.Addin.GetIdName (Addin.Id);
			string n2 = Mono.Addins.Addin.GetIdName (rep.Addin.Id);
			if (n1 != n2)
				return n1.CompareTo (n2);
			else
				return Mono.Addins.Addin.CompareVersions (rep.Addin.Version, Addin.Version);
		}
	}
	
	public interface AddinRepositoryEntry
	{
		AddinHeader Addin {
			get;
		}
		
		string Url {
			get;
		}
		
		string RepositoryUrl {
			get;
		}
		
		string RepositoryName {
			get;
		}
	}
}
