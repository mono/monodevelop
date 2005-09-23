//
// NamespaceEntry.cs
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
using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Parser;

namespace MonoDevelop.Services
{
	[Serializable]
	internal class NamespaceEntry
	{
		Hashtable contents = new Hashtable ();
		
		// This is the case insensitive version of the hashtable.
		// It is constructed only when needed.
		[NonSerialized] Hashtable contents_ci;
		
		// All methods with the caseSensitive parameter, first check for an
		// exact match, and if not found, they try with the case insensitive table.
		
		public NamespaceEntry GetNamespace (string ns, bool caseSensitive)
		{
			NamespaceEntry ne = contents[ns] as NamespaceEntry;
			if (ne != null || caseSensitive) return ne;
			
			if (contents_ci == null) BuildCaseInsensitiveTable ();
			return contents_ci[ns] as NamespaceEntry;
		}
		
		public ClassEntry GetClass (string name, bool caseSensitive)
		{
			ClassEntry ne = contents[name] as ClassEntry;
			if (ne != null || caseSensitive) return ne;
			
			if (contents_ci == null) BuildCaseInsensitiveTable ();
			return contents_ci[name] as ClassEntry;
		}
		
		public void Add (string name, object value)
		{
			contents [name] = value;
			if (contents_ci != null)
				contents_ci [name] = value;
		}
		
		public void Remove (string name)
		{
			contents.Remove (name);
			contents_ci = null;
		}
		
		public ICollection Contents
		{
			get { return contents; }
		}
		
		public int ContentCount
		{
			get { return contents.Count; }
		}
		
		public void Clean ()
		{
			ArrayList todel = new ArrayList ();
			foreach (DictionaryEntry en in contents)
			{
				NamespaceEntry h = en.Value as NamespaceEntry;
				if (h != null) {
					h.Clean ();
					if (h.ContentCount == 0) todel.Add (en.Key);
				}
			}
			
			if (todel.Count > 0)
			{
				contents_ci = null;
				foreach (string key in todel)
					contents.Remove (key);
			}
		}
		
		void BuildCaseInsensitiveTable ()
		{
			contents_ci = new Hashtable (CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default);
			foreach (DictionaryEntry en in contents)
				contents_ci.Add (en.Key, en.Value);
		}
	}
}
