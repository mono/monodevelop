//
// ProjectFolder.cs
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
using System.Linq;
using System.IO;
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProjectFolder: IDisposable, IFolderItem
	{
		FilePath absolutePath;
		IWorkspaceObject parentWorkspaceObject;
		object parent;
		bool trackChanges;
		
		public ProjectFolder (FilePath absolutePath, IWorkspaceObject parentWorkspaceObject): this (absolutePath, parentWorkspaceObject, null)
		{
		}

		public ProjectFolder (FilePath absolutePath, IWorkspaceObject parentWorkspaceObject, object parent)
		{
			this.parent = parent;
			this.parentWorkspaceObject = parentWorkspaceObject;
			this.absolutePath = absolutePath.CanonicalPath;
		}
		
		//FIXME: we don't track when the folder goes away if it's implicit and all its children are removed
		public bool TrackChanges {
			get { return trackChanges; }
			set {
				if (trackChanges != value) {
					trackChanges = value;
					if (trackChanges)
						FileService.FileRenamed += new EventHandler<FileCopyEventArgs> (OnFileRenamed);
					else
						FileService.FileRenamed -= new EventHandler<FileCopyEventArgs> (OnFileRenamed);
				}
			}
		}
		
		FilePath IFolderItem.BaseDirectory {
			get { return Path; }
		}
		
		public FilePath Path {
			get { return absolutePath; }
		}
		
		public string Name {
			get { return absolutePath.FileName; }
		}
		
		public IWorkspaceObject ParentWorkspaceObject {
			get { return parentWorkspaceObject; }
		}
		
		public Project Project {
			get { return parentWorkspaceObject as Project; }
		}
		
		public object Parent {
			get {
				if (parent != null)
					return parent; 
				if (parentWorkspaceObject == null)
					return null;

				string dir = System.IO.Path.GetDirectoryName (absolutePath);
				if (dir == parentWorkspaceObject.BaseDirectory)
					return parentWorkspaceObject;
				else
					return new ProjectFolder (dir, parentWorkspaceObject, null);
			}
		}
		
		public override bool Equals (object other)
		{
			ProjectFolder f = other as ProjectFolder;
			return f != null && absolutePath == f.absolutePath && parentWorkspaceObject == f.parentWorkspaceObject;
		}
		
		public override int GetHashCode ()
		{
			if (parentWorkspaceObject != null)
				return (absolutePath + parentWorkspaceObject.Name).GetHashCode ();
			else
				return absolutePath.GetHashCode ();
		}
		
		public void Dispose ()
		{
			FileService.FileRenamed -= new EventHandler<FileCopyEventArgs> (OnFileRenamed);
		}
		
		public void Remove ()
		{
			if (FolderRemoved != null) {
				FolderRemoved (this, new FileEventArgs (absolutePath, true));
			}
		}

		void OnFileRenamed (object sender, FileCopyEventArgs e)
		{
			// The folder path can't be updated because we would be changing
			// the identity of the object. Another folder object will need
			// to be created by updating the tree.
			
			var e2 = new FileCopyEventArgs (e.Where (i => i.IsDirectory && i.SourceFile == absolutePath));
			if (e2.Count > 0 && FolderRenamed != null) 
				FolderRenamed (this, e);
		}

		public event EventHandler<FileCopyEventArgs> FolderRenamed;
		public event EventHandler<FileEventArgs> FolderRemoved;
	}
}
