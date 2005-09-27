// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Diagnostics;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class ScriptDeploy : IDeploymentStrategy
	{
		public void DeployProject(Project project)
		{
			if (project.DeployInformation.DeployScript.Length == 0) {
				throw new Exception (GettextCatalog.GetString ("Can't deploy: you forgot to specify a deployment script"));
			}
			try {
				if (File.Exists (project.DeployInformation.DeployScript)) {
					ProcessStartInfo pInfo = new ProcessStartInfo(project.DeployInformation.DeployScript);
					pInfo.WorkingDirectory = Path.GetDirectoryName(project.DeployInformation.DeployScript);
					Process.Start(pInfo);
				}
			} catch (Exception e) {
				throw new Exception (GettextCatalog.GetString ("Error while executing deploy script"), e);
			}
		}
	}
}
