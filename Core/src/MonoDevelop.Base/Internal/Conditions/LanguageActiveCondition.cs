// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Lluis Sanchez" email="lluis@novell.com"/>
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
			DotNetProject project = Runtime.ProjectService.CurrentSelectedProject as DotNetProject;
			
			if (activelanguage == "*") {
				return project != null;
			}
			return project != null && project.LanguageName == activelanguage;
		}
	}

}
