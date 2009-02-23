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
using System.IO;
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProjectFolder: IDisposable, IFolderItem
	{
		string absolutePath;
		Project project;
		object parent;
		bool trackChanges;
		
		public ProjectFolder (string absolutePath, Project project): this (absolutePath, project, null)
		{
		}
		
		public ProjectFolder (string absolutePath, Project project, object parent)
		{
			this.parent = parent;
			this.project = project;
			this.absolutePath = absolutePath;
		}
		
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
		
		string IFolderItem.BaseDirectory {
			get { return Path; }
		}
		
		public string Path {
			get { return absolutePath; }
		}
		
		public string Name {
			get { return System.IO.Path.GetFileName (absolutePath); }
		}
		
		public Project Project {
			get { return project; }
		}
		
		public object Parent {
			get {
				if (parent != null)
					return parent; 
				if (project == null)
					return null;

				string dir = System.IO.Path.GetDirectoryName (absolutePath);
				if (dir == project.BaseDirectory)
					return project;
				else
					return new ProjectFolder (dir, project, null);
			}
		}
		
		public override bool Equals (object other)
		{
			ProjectFolder f = other as ProjectFolder;
			return f != null && absolutePath == f.absolutePath && project == f.project;
		}
		
		public override int GetHashCode ()
		{
			if (project != null)
				return (absolutePath + project.Name).GetHashCode ();
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
			if (!e.IsDirectory || e.SourceFile != absolutePath) return;

			// The folder path can't be updated because we would be changing
			// the identity of the object. Another folder object will need
			// to be created by updating the tree.
			
			if (FolderRenamed != null) 
				FolderRenamed (this, e);
		}

		public event EventHandler<FileCopyEventArgs> FolderRenamed;
		public event EventHandler<FileEventArgs> FolderRemoved;
	}
}
