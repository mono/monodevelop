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
using MonoDevelop.Ide.Projects;
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
			SolutionProject project = ProjectService.ActiveProject;
			if (project == null)
				return false;
			if (lang == "*")
				return (project.Project is MSBuildProject);
				
			BackendBindingCodon codon = BackendBindingService.GetBackendBindingCodonByGuid (project.TypeGuid);
			if (codon != null)
				//foreach (string suppLang in project.SupportedLanguages)
					if (codon.Id == lang) 
						return true;
						
			return false;
		}
	}

}
