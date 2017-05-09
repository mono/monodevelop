using System;
using MonoDevelop.Ide.GettingStarted;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class GettingStartedNode
	{
		internal GettingStartedNode (GettingStartedProjectExtension extension)
		{
			Extension = extension;
			Project = extension.Project;
			Provider = Project.GetGettingStartedProvider ();
		}

		public Project Project { get; private set; }

		public GettingStartedProvider Provider { get; private set; }

		internal GettingStartedProjectExtension Extension { get; private set; }

		public void Remove ()
		{
			if (Removed != null)
				Removed (this, null);
		}

		public event EventHandler Removed;
	}
}

