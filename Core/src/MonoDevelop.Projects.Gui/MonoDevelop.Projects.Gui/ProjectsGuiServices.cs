
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Ambience;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.Projects.Gui
{
	internal class Services
	{
		static AmbienceService ambienceService;
		static IProjectService projectService;
		static IconService icons;

		internal static AmbienceService Ambience {
			get { return MonoDevelop.Projects.Services.Ambience; }
		}
	
		internal static LanguageBindingService Languages {
			get { return MonoDevelop.Projects.Services.Languages; }
		}
	
		internal static IProjectService ProjectService {
			get { return MonoDevelop.Projects.Services.ProjectService; }
		}
	
		internal static MessageService MessageService {
			get { return MonoDevelop.Core.Gui.Services.MessageService; }
		}
	
		internal static ResourceService Resources {
			get { return MonoDevelop.Core.Gui.Services.Resources; }
		}
	
		public static IconService Icons {
			get {
				if (icons == null)
					icons = (IconService) ServiceManager.GetService (typeof(IconService));
				return icons;
			}
		}
	}
}
