using System;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// Defines the Connected Services folder node that is shown to the user in the solution pad
	/// </summary>
	sealed class ConnectedServiceFolderNode 
	{
		public ConnectedServiceFolderNode (DotNetProject project)
		{
			Project = project;
		}

		public DotNetProject Project { get; private set; }
	}
}
