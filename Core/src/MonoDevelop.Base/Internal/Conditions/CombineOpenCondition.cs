// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;


using MonoDevelop.Core.AddIns;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Codons
{
	[ConditionAttribute()]
	internal class CombineOpenCondition : AbstractCondition
	{
		[XmlMemberAttribute("iscombineopen", IsRequired = true)]
		bool isCombineOpen;
		
		public bool IsCombineOpen {
			get {
				return isCombineOpen;
			}
			set {
				isCombineOpen = value;
			}
		}
		
		public override bool IsValid(object owner)
		{
			return IdeApp.ProjectOperations.CurrentOpenCombine != null || !isCombineOpen;
		}
	}
}
