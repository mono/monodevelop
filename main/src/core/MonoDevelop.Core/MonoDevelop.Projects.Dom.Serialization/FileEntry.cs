//
// FileEntry.cs
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
using System.Collections.Generic;

namespace MonoDevelop.Projects.Dom.Serialization
{	
	[Serializable]
	class FileEntry
	{
		string filePath;
		DateTime parseTime;
		int parseErrorRetries;
		IList<Tag> commentTasks;
		
		// When the file contains only one class, this field has a reference to
		// the only ClassEntry. If it has more than one class, it has a ClassEntry[]
		// with references to all classes.
		object classes;
		
		[NonSerialized]
		bool disableParse;
		
		[NonSerialized]
		bool inParseQueue;
		
		public FileEntry (string path)
		{
			filePath = path;
			parseTime = DateTime.MinValue;
		}
		
		public string FileName
		{
			get { return filePath; }
		}
		
		public bool DisableParse {
			get { return disableParse; }
			set { disableParse = value; }
		}
		
		public bool InParseQueue {
			get { return inParseQueue; }
			set { inParseQueue = value; }
		}
		
		public DateTime LastParseTime
		{
			get { return parseTime; }
			set { parseTime = value; }
		}
		
		public IEnumerable<ClassEntry> ClassEntries {
			get {
				if (classes == null)
					yield break;
				else if (classes is ClassEntry)
					yield return (ClassEntry) classes;
				else {
					foreach (ClassEntry ce in (ClassEntry[]) classes)
						yield return ce;
				}
			}
		}

		public bool IsModified {
			get {
				if (!System.IO.File.Exists (FileName))
					return false;
				return ((System.IO.File.GetLastWriteTime (FileName) > LastParseTime || ParseErrorRetries > 0) && !DisableParse);
			}
		}
		
		public int ParseErrorRetries
		{
			get { return parseErrorRetries; }
			set { parseErrorRetries = value; }
		}
		
		public void SetClasses (ArrayList list)
		{
			classes = null;
			foreach (ClassEntry ce in list)
				AddClass (ce);
		}
		
		public void AddClass (ClassEntry ce)
		{
			if (classes == null)
				// No classes so far in the file. Add the first one.
				classes = ce;
			else if (classes is ClassEntry) {
				// There is already one class. Create an array to hold the old and new reference.
				ClassEntry[] list = new ClassEntry [] { (ClassEntry) classes, ce };
				classes = list;
			} else {
				// It's already an array of class entries. Extend the array.
				ClassEntry[] list = (ClassEntry[]) classes;
				ClassEntry[] newList = new ClassEntry [list.Length + 1];
				Array.Copy (list, newList, list.Length);
				newList [newList.Length - 1] = ce;
				classes = newList;
			}
		}
		
		public void RemoveClass (ClassEntry ce)
		{
			if (classes == null)
				return;
			if ((classes is ClassEntry) && ((ClassEntry)classes == ce))
				classes = null;
			else {
				ClassEntry[] list = (ClassEntry[]) classes;
				ClassEntry[] newList = new ClassEntry [list.Length - 1];
				int i = 0;
				for (int n=0; n<list.Length; n++) {
					if (list [n] == ce)
						continue;
					if (i >= newList.Length)	// Element to remove not found
						return;
					newList [i] = list [n];
					i++;
				}
				classes = newList;
			}
		}
		
		public bool IsAssembly
		{
			get {
				string ext = System.IO.Path.GetExtension (filePath).ToLower ();
				return ext == ".dll" || ext == ".exe";
			}
		}
		
		public IList<Tag> CommentTasks
		{
			get { return commentTasks; }
			set { commentTasks = value; }
		}
	}
}
