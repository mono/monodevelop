
using System;
using System.Collections;
using Mono.Addins;

namespace MonoDevelop.Deployment
{
	[ExtensionNode ("DeployDirectory")]
	internal class DeployDirectoryNodeType: ExtensionNode
	{
		[NodeAttribute ("_label")]
		string description;
		
		public DeployDirectoryInfo GetDeployDirectoryInfo ()
		{
			return new DeployDirectoryInfo (Id, description);
		}
	}
}
