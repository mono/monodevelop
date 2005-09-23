// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Diagnostics;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;

namespace MonoDevelop.Internal.Project
{
	public class ScriptDeploy : IDeploymentStrategy
	{
		public void DeployProject(Project project)
		{
			if (project.DeployInformation.DeployScript.Length == 0) {
				Runtime.MessageService.ShowError(GettextCatalog.GetString ("Can't deploy: you forgot to specify a deployment script"));
				return;
			}
			try {
				FileUtilityService fileUtilityService = Runtime.FileUtilityService;
				if (fileUtilityService.TestFileExists(project.DeployInformation.DeployScript)) {
					ProcessStartInfo pInfo = new ProcessStartInfo(project.DeployInformation.DeployScript);
					pInfo.WorkingDirectory = Path.GetDirectoryName(project.DeployInformation.DeployScript);
					Process.Start(pInfo);
				}
			} catch (Exception e) {
				Runtime.MessageService.ShowError(e, GettextCatalog.GetString ("Error while executing deploy script"));
			}
		}
	}
}
