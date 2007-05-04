using System;
using System.Xml;

using Mono.Addins;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Codons
{
	internal class WorkbenchContextCondition : ConditionType
	{
		public override bool Evaluate (NodeElement condition)
		{
			string context = condition.GetAttribute ("value");
			if (context == "*")
				return true;

			if (context == IdeApp.Workbench.Context.Id)
				return true;

			return false;
		}
	}
}
