// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;


using Mono.Addins;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Codons
{
	internal class CombineOpenCondition : ConditionType
	{
		public CombineOpenCondition ()
		{
			ProjectService.SolutionOpened += delegate { NotifyChanged(); };
			ProjectService.SolutionClosed += delegate { NotifyChanged(); };
		}
		
		public override bool Evaluate (NodeElement condition)
		{
			return ProjectService.Solution != null;
		}
	}
}
