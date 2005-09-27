using System;
using System.Xml;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Codons
{
	[ConditionAttribute()]
	internal class WorkbenchContextCondition : AbstractCondition
	{
		[XmlMemberAttribute("context", IsRequired = true)]
			string context;

		public string CurrentContext 
		{
			get 
			{
				return context;
			}
			set
			{
				context = value;
			}
		}

		public override bool IsValid (object owner)
		{
			if (context == "*")
				return true;

			if (context == IdeApp.Workbench.Context.Id)
				return true;

			return false;
		}
	}
}
