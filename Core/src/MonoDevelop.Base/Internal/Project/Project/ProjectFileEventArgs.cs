// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Lluis Sanchez Gual" email="lluis@ximian.com"/>
//     <version value="$version"/>
// </file>

using System;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.Internal.Project
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
		string oldName;
	
		public string OldName {
			get { return oldName; }
		}
		
		public string NewName {
			get { return ProjectFile.Name; }
		}
		
		public ProjectFileRenamedEventArgs (Project project, ProjectFile file, string oldName)
		: base (project, file)
		{
			this.oldName = oldName;
		}
	}
}
