
using System;

namespace Stetic.Editor
{
	public class ProjectIconSelectorItem: IconSelectorItem
	{
		IProject project;

		public ProjectIconSelectorItem (IProject project): base ("Project Icons")
		{
			this.project = project;
		}
		
		protected override void CreateIcons ()
		{
			foreach (ProjectIconSet icon in project.IconFactory.Icons)
				AddIcon (icon.Name, icon.Sources [0].Image.GetScaledImage (project, Gtk.IconSize.Menu), icon.Name);
		}
	}
}
