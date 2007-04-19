
using System;
using System.Collections;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Deployment
{
	[CodonNameAttribute("DeployDirectory")]
	internal class DeployDirectoryNodeType: AbstractCodon
	{
		[XmlMemberAttribute ("_label")]
		string description;
		
		public DeployDirectoryInfo GetDeployDirectoryInfo ()
		{
			return new DeployDirectoryInfo (ID, description);
		}
		
		public override object BuildItem (object owner, ArrayList subItems, ConditionCollection conditions)
		{
			return this;
		}
	}
}
