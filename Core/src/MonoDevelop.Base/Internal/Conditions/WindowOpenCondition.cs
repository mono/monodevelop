// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;


using MonoDevelop.Core.AddIns;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Codons
{
	[ConditionAttribute()]
	internal class WindowOpenCondition : AbstractCondition
	{
		[XmlMemberAttribute("openwindow", IsRequired = true)]
		string openwindow;
		
		public string ActiveWindow {
			get {
				return openwindow;
			}
			set {
				openwindow = value;
			}
		}
		
		public override bool IsValid(object owner)
		{
			if (openwindow == "*") {
				return IdeApp.Workbench.ActiveDocument != null;
			}
			foreach (Document doc in IdeApp.Workbench.Documents) {
				Type currentType = doc.Window.ViewContent.GetType();
				if (currentType.ToString() == openwindow) {
					return true;
				}
				foreach (Type i in currentType.GetInterfaces()) {
					if (i.ToString() == openwindow) {
						return true;
					}
				}
			}
			return false;
		}
	}
}
