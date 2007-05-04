// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;


using Mono.Addins;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Codons
{
	internal class WindowActiveCondition : ConditionType
	{
		public WindowActiveCondition ()
		{
			IdeApp.Workbench.ActiveDocumentChanged += delegate { NotifyChanged (); };
		}
		
		public override bool Evaluate (NodeElement condition)
		{
			string activewindow = condition.GetAttribute ("value");
			
			if (activewindow == "*") {
				return IdeApp.Workbench.ActiveDocument != null;
			}
			if (IdeApp.Workbench.ActiveDocument == null) {
				return false;
			}
			Type currentType = IdeApp.Workbench.ActiveDocument.GetContent<IBaseViewContent> ().GetType ();
			if (currentType.ToString() == activewindow) {
				return true;
			}
			foreach (Type i in currentType.GetInterfaces()) {
				if (i.ToString() == activewindow) {
					return true;
				}
			}
			return false;
		}
	}
}
