using System;
using MonoDevelop.Ide.GettingStarted;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class GettingStartedNode
	{
		public GettingStartedNode (Project project)
		{
			Project = project;
			Provider = project.GetGettingStartedProvider ();
		}

		public Project Project { get; private set; }

		public IGettingStartedProvider Provider { get; private set; }

		public void Remove ()
		{
			if (Removed != null)
				Removed (this, null);
		}

		public event EventHandler Removed;
	}
}

