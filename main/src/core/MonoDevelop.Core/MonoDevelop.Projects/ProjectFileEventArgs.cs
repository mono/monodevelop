// ProjectFileEventArgs.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public delegate void ProjectFileEventHandler(object sender, ProjectFileEventArgs e);
	
	public class ProjectFileEventArgs : EventArgs
	{
		Project project;
		ProjectFile file;
		
		public Project Project {
			get {
				return project;
			}
		}
		
		public ProjectFile ProjectFile {
			get {
				return file;
			}
		}
		
		public ProjectFileEventArgs (Project project, ProjectFile file)
		{
			this.project = project;
			this.file = file;
		}
	}
	
	public delegate void ProjectFileRenamedEventHandler(object sender, ProjectFileRenamedEventArgs e);
	
	public class ProjectFileRenamedEventArgs : ProjectFileEventArgs
	{
		FilePath oldName;
	
		public FilePath OldName {
			get { return oldName; }
		}
		
		public FilePath NewName {
			get { return ProjectFile.FilePath; }
		}
		
		public ProjectFileRenamedEventArgs (Project project, ProjectFile file, FilePath oldName)
		: base (project, file)
		{
			this.oldName = oldName;
		}
	}
}
