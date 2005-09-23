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
	public delegate void ProjectEventHandler(object sender, ProjectEventArgs e);
	
	public class ProjectEventArgs : EventArgs
	{
		Project project;
		
		public Project Project {
			get {
				return project;
			}
		}
		
		public ProjectEventArgs(Project project)
		{
			this.project = project;
		}
	}
}
