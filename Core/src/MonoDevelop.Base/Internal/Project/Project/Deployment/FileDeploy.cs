// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.CodeDom.Compiler;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;

namespace MonoDevelop.Internal.Project
{
	public class FileDeploy : IDeploymentStrategy
	{
		public void DeployProject(Project project)
		{
			if (project.DeployInformation.DeployTarget.Length == 0) {
				Runtime.MessageService.ShowError(GettextCatalog.GetString ("Can't deploy: you forgot to specify a deployment script"));
				return;
			}
			
			try {
				FileUtilityService fileUtilityService = Runtime.FileUtilityService;
				foreach (ProjectFile fInfo in project.ProjectFiles) {
					try { 
						if (!project.DeployInformation.IsFileExcluded(fInfo.Name)) {
							string newFileName = fileUtilityService.GetDirectoryNameWithSeparator(project.DeployInformation.DeployTarget) + fileUtilityService.AbsoluteToRelativePath(project.BaseDirectory, fInfo.Name);
							if (!Directory.Exists(Path.GetDirectoryName(newFileName))) {
								Directory.CreateDirectory(Path.GetDirectoryName(newFileName));
							}
							File.Copy(fInfo.Name, newFileName, true);
						}
					} catch (Exception e) {
						throw new ApplicationException("Error while copying '" + fInfo.Name + "' to '" + fileUtilityService.GetDirectoryNameWithSeparator(project.DeployInformation.DeployTarget) + fileUtilityService.AbsoluteToRelativePath(project.BaseDirectory, fInfo.Name) + "'.\nException thrown was :\n" + e.ToString());
					}
				}
			} catch (Exception e) {
				Runtime.MessageService.ShowError(e);
			}
		}
	}
}
