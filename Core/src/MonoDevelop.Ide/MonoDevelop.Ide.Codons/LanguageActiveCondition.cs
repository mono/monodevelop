// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Lluis Sanchez" email="lluis@novell.com"/>
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
	internal class LanguageActiveCondition : AbstractCondition
	{
		[XmlMemberAttribute("activelanguage", IsRequired = true)]
		string activelanguage;
		
		public string ActiveLanguage {
			get {
				return activelanguage;
			}
			set {
				activelanguage = value;
			}
		}
		
		public override bool IsValid(object owner)
		{
			DotNetProject project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			
			if (activelanguage == "*") {
				return project != null;
			}
			return project != null && project.LanguageName == activelanguage;
		}
	}

}
