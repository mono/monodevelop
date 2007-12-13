
using System;
using System.Collections;
using Mono.Addins;

namespace MonoDevelop.Deployment
{
	[ExtensionNode("DeployPlatform")]
	internal class DeployPlatformNodeType: ExtensionNode
	{
		[NodeAttribute ("_label", Localizable=true)]
		string description = null;
		
		public DeployPlatformInfo GetDeployPlatformInfo ()
		{
			return new DeployPlatformInfo (Id, description);
		}
	}
}
