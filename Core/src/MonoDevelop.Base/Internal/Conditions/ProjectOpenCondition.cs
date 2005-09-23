// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Xml;


using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Core.Services;

using MonoDevelop.Gui;
using MonoDevelop.Services;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.Core.AddIns
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
			Project project = Runtime.ProjectService.CurrentSelectedProject;
			
			if (project == null && Runtime.ProjectService.CurrentOpenCombine != null) {
				CombineEntryCollection projects = Runtime.ProjectService.CurrentOpenCombine.GetAllProjects();
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
