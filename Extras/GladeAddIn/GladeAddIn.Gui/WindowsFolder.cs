
using MonoDevelop.Projects;

namespace GladeAddIn.Gui
{
	class WindowsFolder
	{
		Project project;
		
		public WindowsFolder (Project project)
		{
			this.project = project;
		}
		
		public Project Project {
			get { return project; }
		}
	}
}
