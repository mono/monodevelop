using System;
using System.Xml;

using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Gui;

namespace MonoDevelop.Core.AddIns
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
			if (WorkbenchSingleton.Workbench == null)
				return false;

			if (context == "*")
				return true;

			if (context == WorkbenchSingleton.Workbench.Context.Id)
				return true;

			return false;
		}
	}
}
