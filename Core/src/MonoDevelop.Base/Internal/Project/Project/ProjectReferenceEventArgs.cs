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
	public delegate void ProjectReferenceEventHandler(object sender, ProjectReferenceEventArgs e);
	
	public class ProjectReferenceEventArgs : EventArgs
	{
		Project project;
		ProjectReference reference;
		
		public Project Project {
			get {
				return project;
			}
		}
		
		public ProjectReference ProjectReference {
			get {
				return reference;
			}
		}
		
		public ProjectReferenceEventArgs (Project project, ProjectReference reference)
		{
			this.project = project;
			this.reference = reference;
		}
	}
}
