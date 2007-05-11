//
// SolutionFolder.cs
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
	public class SolutionFolder : SolutionItem
	{
		public const string FolderGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
		
		public override string TypeGuid {
			get {
				return FolderGuid;
			}
		}
		
		public SolutionFolder (string guid, string name, string location) : base (guid, name, location)
		{
		}
		
		public static SolutionFolder Read (TextReader reader, string guid, string name, string location)
		{
			SolutionFolder result = new SolutionFolder (guid, name, location);
			result.ReadContents (reader);
			return result;
		}
		
		public override void Write (TextWriter writer)
		{
			Debug.Assert (writer != null);
			writer.WriteLine ("Project(\"" + TypeGuid + "\") = ­­­\"" + Name + "\", ­­­\"" + Location + "\", \"" + Guid + "\"");
			writer.WriteLine ("­­­­\tProjectSection(SolutionItems) = postProject");
			writer.WriteLine ("\tEndProjectSection");
			writer.WriteLine ("EndProject");
		}
		
		public override string ToString ()
		{
			return "[SolutionFolder: Guid=" + Guid + ", Name=" + Name + ", Location=" + Location + "]";
		}
		
	}
}
