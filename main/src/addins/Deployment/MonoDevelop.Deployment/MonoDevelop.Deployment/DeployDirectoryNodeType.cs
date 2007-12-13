
using System;
using System.Collections;
using Mono.Addins;

namespace MonoDevelop.Deployment
{
	[ExtensionNode ("DeployDirectory")]
	internal class DeployDirectoryNodeType: ExtensionNode
	{
		[NodeAttribute ("_label", Localizable=true)]
		string description = null;
		
		public DeployDirectoryInfo GetDeployDirectoryInfo ()
		{
			return new DeployDirectoryInfo (Id, description);
		}
	}
}
