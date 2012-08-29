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
	
	public class ProjectFileEventArgs : EventArgsChain<ProjectFileEventInfo>
	{
		FilePath rootDir;
		FilePath virtualRootDir;
		bool singleDir;
		bool singleVirtualDir;
		Project commonProject;
		
		public ProjectFileEventArgs (Project project, ProjectFile file)
		{
			Add (new ProjectFileEventInfo (project, file));
		}
		
		public ProjectFileEventArgs ()
		{
		}
		
		public Project CommonProject {
			get {
				if (rootDir.IsNull)
					CalcRootDir (true, out rootDir, out singleDir);
				return commonProject;
			}
		}
		
		public FilePath CommonRootDirectory {
			get {
				if (rootDir.IsNull)
					CalcRootDir (false, out rootDir, out singleDir);
				return rootDir;
			}
		}
		
		public bool SingleDirectory {
			get {
				if (rootDir.IsNull)
					CalcRootDir (false, out rootDir, out singleDir);
				return singleDir;
			}
		}
		
		public FilePath CommonVirtualRootDirectory {
			get {
				if (virtualRootDir.IsNull)
					CalcRootDir (true, out virtualRootDir, out singleVirtualDir);
				return virtualRootDir;
			}
		}
		
		public bool SingleVirtualDirectory {
			get {
				if (virtualRootDir.IsNull)
					CalcRootDir (true, out virtualRootDir, out singleVirtualDir);
				return singleVirtualDir;
			}
		}
		
		void CalcRootDir (bool calcVirtual, out FilePath baseDir, out bool sameDir)
		{
			baseDir = FilePath.Null;
			sameDir = true;
			commonProject = null;
			
			foreach (ProjectFileEventInfo f in this) {
				FilePath parentDir;
				if (calcVirtual && f.ProjectFile.IsLink)
					parentDir = f.Project.BaseDirectory.Combine (f.ProjectFile.ProjectVirtualPath).ParentDirectory;
				else
					parentDir = f.ProjectFile.FilePath.ParentDirectory;
				
				if (baseDir.IsNull) {
					commonProject = f.Project;
					baseDir = parentDir;
					continue;
				}
				
				if (f.Project != commonProject)
					commonProject = null;
				
				if (parentDir == baseDir)
					continue;
				else if (baseDir.IsChildPathOf (parentDir)) {
					sameDir = false;
					baseDir = parentDir;
				} else {
					sameDir = false;
					while (!parentDir.IsChildPathOf (baseDir))
						baseDir = baseDir.ParentDirectory;
				}
			}
		}
	}
	
	public class ProjectFileEventInfo
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
		
		public ProjectFileEventInfo (Project project, ProjectFile file)
		{
			this.project = project;
			this.file = file;
		}
	}
	
	public delegate void ProjectFileRenamedEventHandler(object sender, ProjectFileRenamedEventArgs e);
	
	public class ProjectFileRenamedEventArgs : EventArgsChain<ProjectFileRenamedEventInfo>
	{
		public ProjectFileRenamedEventArgs (Project project, ProjectFile file, FilePath oldName)
		{
			Add (new ProjectFileRenamedEventInfo (project, file, oldName));
		}
	}
	
	public class ProjectFileRenamedEventInfo : ProjectFileEventInfo
	{
		FilePath oldName;
	
		public FilePath OldName {
			get { return oldName; }
		}
		
		public FilePath NewName {
			get { return ProjectFile.FilePath; }
		}
		
		public ProjectFileRenamedEventInfo (Project project, ProjectFile file, FilePath oldName)
		: base (project, file)
		{
			this.oldName = oldName;
		}
	}
}
