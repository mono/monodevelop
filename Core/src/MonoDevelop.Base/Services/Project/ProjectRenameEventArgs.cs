// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.Services 
{
	public delegate void ProjectRenameEventHandler(object sender, ProjectRenameEventArgs e);
	
	public class ProjectRenameEventArgs : EventArgs
	{ 
		Project project;
		string   oldName;
		string   newName;
		
		public Project Project {
			get {
				return project;
			}
		}
		
		public string OldName {
			get {
				return oldName;
			}
		}
		
		public string NewName {
			get {
				return newName;
			}
		}
		
		public ProjectRenameEventArgs(Project project, string oldName, string newName)
		{
			this.project = project;
			this.oldName = oldName;
			this.newName = newName;
		}
	}
}
