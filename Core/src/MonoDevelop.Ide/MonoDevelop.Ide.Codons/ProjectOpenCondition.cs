// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Xml;


using Mono.Addins;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Codons
{
	[ConditionAttribute()]
	internal class ProjectOpenCondition : AbstractCondition
	{
		[XmlMemberAttribute("openproject", IsRequired = true)]
		string openproject;
		
		public string OpenProject {
			get {
				return openproject;
			}
			set {
				openproject = value;
			}
		}
		
		public override bool IsValid(object owner)
		{
			Project project = IdeApp.ProjectOperations.CurrentSelectedProject;
			
			if (project == null && IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				CombineEntryCollection projects = IdeApp.ProjectOperations.CurrentOpenCombine.GetAllProjects();
				if (projects.Count > 0) {
					project = (Project)projects[0];
				}
			}
			
			if (openproject == "*") {
				return project != null;
			}
			return project != null && project.ProjectType == openproject;
		}
	}

}
