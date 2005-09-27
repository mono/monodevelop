// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Xml;


using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Codons
{
	[ConditionAttribute()]
	internal class ProjectActiveCondition : AbstractCondition
	{
		[XmlMemberAttribute("activeproject", IsRequired = true)]
		string activeproject;
		
		public string ActiveProject {
			get {
				return activeproject;
			}
			set {
				activeproject = value;
			}
		}
		
		public override bool IsValid(object owner)
		{
			Project project = IdeApp.ProjectOperations.CurrentSelectedProject;
			if (activeproject == "*") {
				return project != null;
			}
			return project != null && project.ProjectType == activeproject;
		}
	}

}
