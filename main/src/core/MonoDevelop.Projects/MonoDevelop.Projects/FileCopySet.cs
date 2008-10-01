// 
// FileCopySet.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	
	public class FileCopySet : IEnumerable<FileCopySet.Item>
	{
		Dictionary<string, Item> files = new Dictionary<string, Item> ();
		
		public FileCopySet ()
		{
		}
		
		public bool Contains (string fileName)
		{
			return files.ContainsKey (Path.GetFileName (fileName));
		}
		
		public void Add (string sourcePath)
		{
			Add (sourcePath, false);
		}
		
		public void Add (string sourcePath, bool copyOnlyIfNewer)
		{
			Add (sourcePath, copyOnlyIfNewer, Path.GetFileName (sourcePath));
		}
		
		public bool Add (string sourcePath, bool copyOnlyIfNewer, string targetName)
		{
			if (Path.GetFileName (targetName) != targetName)
				throw new ArgumentException ("The target name must not contain a path");
			
			//don't add duplicates
			if (files.ContainsKey (targetName))
				return false;
			
			files.Add (targetName, new Item (sourcePath, copyOnlyIfNewer, targetName));
			return true;
		}
		
		public Item Remove (string fileName)
		{
			string key = Path.GetFileName (fileName);
			Item f;
			if (files.TryGetValue (key, out f)) {
				files.Remove (key);
				return f;
			} else {
				return null;
			}
		}
		
		IEnumerator<Item> IEnumerable<Item>.GetEnumerator ()
		{
			return files.Values.GetEnumerator ();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return files.Values.GetEnumerator ();
		}
		
		public class Item
		{
			public Item (string src, bool copyOnlyIfNewer, string target)
			{
				this.Src = src;
				this.Target = target;
				this.CopyOnlyIfNewer = copyOnlyIfNewer;
			}
			
			public bool CopyOnlyIfNewer { get; private set; }
			public string Target { get; private set; }
			public string Src { get; private set; }
		}
	}
	
	
}
