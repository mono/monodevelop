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
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	/// <summary>
	/// A list of files to be deployed together with the project output binary
	/// </summary>
	public class FileCopySet : IEnumerable<FileCopySet.Item>
	{
		Dictionary<FilePath, Item> files = new Dictionary<FilePath, Item> ();
		
		public FileCopySet ()
		{
		}
		
		/// <summary>
		/// Add a file
		/// </summary>
		/// <param name='sourcePath'>
		/// File path.
		/// </param>
		public void Add (FilePath sourcePath)
		{
			Add (sourcePath, false);
		}
		
		/// <summary>
		/// Add a file
		/// </summary>
		/// <param name='sourcePath'>
		/// File path.
		/// </param>
		/// <param name='copyOnlyIfNewer'>
		/// Copy to otuput dir only if the file has changed.
		/// </param>
		public void Add (FilePath sourcePath, bool copyOnlyIfNewer)
		{
			Add (sourcePath, copyOnlyIfNewer, sourcePath.FileName);
		}
		
		/// <summary>
		/// Add a file
		/// </summary>
		/// <param name='sourcePath'>
		/// File path.
		/// </param>
		/// <param name='copyOnlyIfNewer'>
		/// Copy to otuput dir only if the file has changed.
		/// </param>
		/// <param name='targetRelativePath'>
		/// Directory (relative to the output directory) where the file has to be copied.
		/// </param>
		public bool Add (FilePath sourcePath, bool copyOnlyIfNewer, FilePath targetRelativePath)
		{
			//don't add duplicates
			if (files.ContainsKey (targetRelativePath))
				return false;
			
			files.Add (targetRelativePath, new Item (sourcePath, copyOnlyIfNewer, targetRelativePath));
			return true;
		}

		/// <summary>
		/// Remove a file
		/// </summary>
		/// <param name='fileName'>
		/// File name.
		/// </param>
		public Item Remove (FilePath fileName)
		{
			string key = fileName.FileName;
			Item f;
			if (files.TryGetValue (key, out f)) {
				files.Remove (key);
				return f;
			} else {
				return null;
			}
		}

		public Dictionary<FilePath, Item>.ValueCollection.Enumerator GetEnumerator ()
		{
			return files.Values.GetEnumerator ();
		}
		
		IEnumerator<Item> IEnumerable<Item>.GetEnumerator ()
		{
			return files.Values.GetEnumerator ();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return files.Values.GetEnumerator ();
		}
		
		/// <summary>
		/// A file
		/// </summary>
		public class Item
		{
			public Item (FilePath src, bool copyOnlyIfNewer, FilePath target)
			{
				this.Src = src;
				this.Target = target;
				this.CopyOnlyIfNewer = copyOnlyIfNewer;
			}
			
			/// <summary>
			/// Gets or sets a value indicating whether the file has to be copied only if it has changed.
			/// </summary>
			public bool CopyOnlyIfNewer { get; private set; }

			/// <summary>
			/// Gets or sets the target directory (must be a relative path)
			/// </summary>
			public FilePath Target { get; private set; }
			
			/// <summary>
			/// Gets or sets the source path
			/// </summary>
			public FilePath Src { get; private set; }
		}
	}
	
	
}
