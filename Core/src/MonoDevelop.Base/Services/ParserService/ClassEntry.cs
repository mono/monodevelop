//
// ClassEntry.cs
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
	class ClassEntry
	{
		long position;
		NamespaceEntry namespaceRef;
		string name;
		FileEntry fileEntry;
		ClassEntry nextInFile;
		
		[NonSerialized]
		int lastGetTime;
		
		[NonSerialized]
		public IClass cls;
		
		public ClassEntry (IClass cls, FileEntry fileEntry, NamespaceEntry namespaceRef)
		{
			this.cls = cls;
			this.fileEntry = fileEntry;
			this.namespaceRef = namespaceRef;
			this.name = cls.Name;
			position = -1;
		}
		
		public long Position
		{
			get { return position; }
			set { position = value; }
		}
		
		public IClass Class
		{
			get { 
				return cls; 
			}
			set {
				cls = value; 
				if (cls != null) {
					name = cls.Name; 
					position = -1; 
				}
			}
		}
		
		public string Name
		{
			get { return name; }
		}
		
		public NamespaceEntry NamespaceRef
		{
			get { return namespaceRef; }
		}
		
		public FileEntry FileEntry
		{
			get { return fileEntry; }
			set { fileEntry = value; }
		}
		
		public int LastGetTime
		{
			get { return lastGetTime; }
			set { lastGetTime = value; }
		}
		
		public ClassEntry NextInFile
		{
			get { return nextInFile; }
			set { nextInFile = value; }
		}
	}
}
