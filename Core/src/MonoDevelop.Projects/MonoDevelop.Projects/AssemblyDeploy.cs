// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;

using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class AssemblyDeploy  : IDeploymentStrategy
	{
/*		static string[] extensions = {
			"",
			".exe",
			".dll"
		};
*/		
		public void DeployProject(Project project)
		{
			if (project.DeployInformation.DeployTarget.Length == 0) {
				throw new Exception (GettextCatalog.GetString ("Can't deploy: no deployment target set"));
			}
			if (File.Exists (project.GetOutputFileName ()))
				File.Copy (project.GetOutputFileName (), Path.GetFileName (project.GetOutputFileName ()), true);
			else
				throw new Exception("Assembly not found.");
		}
	}
}
