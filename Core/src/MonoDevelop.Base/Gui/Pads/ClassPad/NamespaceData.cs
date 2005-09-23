//
// NamespaceData.cs
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

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Internal.Parser;

namespace MonoDevelop.Gui.Pads.ClassPad
{
	public class NamespaceData
	{
		string namesp;
		Project project;
		
		public NamespaceData (Project p, string fullNamespace)
		{
			project = p;
			namesp = fullNamespace;
		}
		
		public string Name {
			get {
				int i = namesp.LastIndexOf (".");
				if (i != -1) return namesp.Substring (i+1);
				else return namesp;
			}
		}
		
		public string FullName {
			get { return namesp; }
		}
		
		public Project Project {
			get { return project; }
		}
		
		public override bool Equals (object ob)
		{
			NamespaceData other = ob as NamespaceData;
			return (namesp == other.namesp &&
					project == other.project);
		}
		
		public override int GetHashCode ()
		{
			if (project != null) return (namesp + project.Name).GetHashCode ();
			else return namesp.GetHashCode ();
		}
		
		public override string ToString ()
		{
			return base.ToString () + " [" + namesp + ", " + (project != null ? project.Name : "no project") + "]";
		}
	}
}
