//
// SystemFile.cs
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

using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class SystemFile: IFileItem
	{
		FilePath absolutePath;
		IWorkspaceObject parent;
		bool showTransparent;
		
		public SystemFile (FilePath absolutePath, IWorkspaceObject parent): this (absolutePath, parent, true)
		{
		}

		public SystemFile (FilePath absolutePath, IWorkspaceObject parent, bool showTransparent)
		{
			this.parent = parent;
			this.absolutePath = absolutePath;
			this.showTransparent = showTransparent;
		}
		
		public FilePath Path {
			get { return absolutePath; }
		}
		
		FilePath IFileItem.FileName {
			get { return Path; }
		}
		
		public string Name {
			get { return System.IO.Path.GetFileName (absolutePath); }
		}

		public IWorkspaceObject ParentWorkspaceObject {
			get { return parent; }
		}
		
		public bool ShowTransparent {
			get { return showTransparent; }
			set { showTransparent = value; }
		}

		public override bool Equals (object other)
		{
			SystemFile f = other as SystemFile;
			return f != null && absolutePath == f.absolutePath && parent == f.parent;
		}
		
		public override int GetHashCode ()
		{
			if (parent != null)
				return (absolutePath + parent.Name).GetHashCode ();
			else
				return absolutePath.GetHashCode ();
		}
	}
}
