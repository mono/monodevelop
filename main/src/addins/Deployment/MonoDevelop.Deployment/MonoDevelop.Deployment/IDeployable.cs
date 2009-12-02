
using System;
using MonoDevelop.Projects;

namespace MonoDevelop.Deployment
{
	public interface IDeployable
	{
		DeployFileCollection GetDeployFiles (ConfigurationSelector configuration);
	}
}
