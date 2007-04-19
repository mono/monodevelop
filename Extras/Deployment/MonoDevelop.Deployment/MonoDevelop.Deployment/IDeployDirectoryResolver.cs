
using System;
using MonoDevelop.Deployment.Gui;

namespace MonoDevelop.Deployment
{
	public interface IDirectoryResolver
	{
		string GetDirectory (DeployContext context, string folderId);
	}
}
