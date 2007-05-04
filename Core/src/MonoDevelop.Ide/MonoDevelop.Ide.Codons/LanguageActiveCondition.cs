// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Lluis Sanchez" email="lluis@novell.com"/>
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
	internal class LanguageActiveCondition : ConditionType
	{
		public LanguageActiveCondition ()
		{
			IdeApp.ProjectOperations.CurrentProjectChanged += delegate { NotifyChanged (); };
		}
		
		public override bool Evaluate (NodeElement condition)
		{
			string lang = condition.GetAttribute ("value");
			DotNetProject project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			
			if (lang == "*") {
				return project != null;
			}
			return project != null && project.LanguageName == lang;
		}
	}

}
