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
			Project project = Runtime.ProjectService.CurrentSelectedProject;
			if (activeproject == "*") {
				return project != null;
			}
			return project != null && project.ProjectType == activeproject;
		}
	}

}
