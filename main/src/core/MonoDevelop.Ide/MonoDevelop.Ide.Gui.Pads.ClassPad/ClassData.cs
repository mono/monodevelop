//
// ClassData.cs
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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.Ide.Gui.Pads.ClassPad
{
	public class ClassData
	{
		IType cls;
		Project project;
		
		public ClassData (Project p, IType c)
		{
			cls = c;
			project = p;
		}
		
		public IType Class {
			get { return cls; }
		}
		
		public Project Project {
			get { return project; }
		}
		
		internal void UpdateFrom (ClassData cd)
		{
			cls = cd.cls;
			project = cd.project;
		}
		
		public override bool Equals (object ob)
		{
			ClassData other = ob as ClassData;
			return (other != null && cls.FullName == other.cls.FullName &&
					project == other.project);
		}
		
		public override int GetHashCode ()
		{
			if (project == null)
				return cls.FullName.GetHashCode ();
			return (cls.FullName + project.Name).GetHashCode ();
		}
		
		public override string ToString ()
		{
			return base.ToString () + " [" + cls.FullName + ", " + (project != null ? project.Name : "null")+ "]";
		}
	}
}
