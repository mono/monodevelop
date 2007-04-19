
using System;
using System.Collections;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Deployment
{
	[CodonNameAttribute("DeployPlatform")]
	internal class DeployPlatformNodeType: AbstractCodon
	{
		[XmlMemberAttribute ("_label")]
		string description;
		
		public DeployPlatformInfo GetDeployPlatformInfo ()
		{
			return new DeployPlatformInfo (ID, description);
		}
		
		public override object BuildItem (object owner, ArrayList subItems, ConditionCollection conditions)
		{
			return this;
		}
	}
}
