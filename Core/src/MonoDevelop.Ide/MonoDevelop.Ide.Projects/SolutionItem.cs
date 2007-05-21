//
// SolutionItem.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.Diagnostics;
using System.IO;

namespace MonoDevelop.Ide.Projects
{
	public abstract class SolutionItem
	{
		string guid;
		string name;
		string location;
		
		SolutionItem parent = null;
		
		public abstract string TypeGuid {
			get;
		}
		
		public SolutionItem Parent {
			get { return parent; }
			set { parent = value; }
		}
		
		public string Guid {
			get { return guid; }
			set { this.guid = value; }
		}
		
		public string Name {
			get { return name; }
			set { this.name = value; }
		}
		
		public string Location {
			get { return location; }
			set { this.location = value; }
		}
		
		public SolutionItem (string guid, string name, string location)
		{
			this.guid     = guid;
			this.name     = name;
			this.location = location;
		}
		
		protected void ReadContents (TextReader reader)
		{
			Debug.Assert (reader != null);
			SolutionReadHelper.ReadSection (reader, delegate(string curLine, SolutionReadHelper.ReadLineData data) {
				data.ContinueRead = curLine != "EndProject"; 
			});
		}
		
		public virtual void Write (TextWriter writer)
		{
			Debug.Assert (writer != null);
			writer.WriteLine ("Project(\"" + TypeGuid + "\") = \"" + Name + "\", \"" + Location + "\", \"" + Guid + "\"");
			writer.WriteLine ("EndProject");
		}
	}
}
