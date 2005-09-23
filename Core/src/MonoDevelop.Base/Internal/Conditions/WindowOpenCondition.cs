// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;


using MonoDevelop.Core.AddIns.Conditions;

using MonoDevelop.Gui;

namespace MonoDevelop.Core.AddIns
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
			if (WorkbenchSingleton.Workbench == null) {
				return false;
			}
			
			if (openwindow == "*") {
				return WorkbenchSingleton.Workbench.ActiveWorkbenchWindow != null;
			}
			foreach (IViewContent view in WorkbenchSingleton.Workbench.ViewContentCollection) {
				Type currentType = view.GetType();
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
