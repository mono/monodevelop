// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

namespace MonoDevelop.Projects
{
	public interface IDeploymentStrategy
	{
		void DeployProject(Project project);
	}
	
}
