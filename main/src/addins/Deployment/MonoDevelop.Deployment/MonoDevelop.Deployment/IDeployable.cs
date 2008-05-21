
using System;

namespace MonoDevelop.Deployment
{
	public interface IDeployable
	{
		DeployFileCollection GetDeployFiles (string configuration);
	}
}
