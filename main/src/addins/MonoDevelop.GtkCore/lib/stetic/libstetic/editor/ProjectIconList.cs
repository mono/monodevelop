
using System;
using System.Collections;
using Gtk;

namespace Stetic.Editor
{
	class ProjectIconList : IconList 
	{
		IProject project;
		ProjectIconFactory icons;
		
		public ProjectIconList (IProject project, ProjectIconFactory icons)
		{
			this.project = project;
			this.icons = icons;
			Refresh ();
		}
		
		public void Refresh ()
		{
			Clear ();
			foreach (ProjectIconSet icon in icons.Icons)
				AddIcon (icon.Name, icon.Sources [0].Image.GetThumbnail (project, 16), icon.Name);
		}
	}		
}


