
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
		static DispatchService dispatchService;
//		static IProjectService projectService;
		static LanguageBindingService languageBindingService;
		static MessageService messageService;
		static ResourceService resourceService;
		static IconService icons;

		internal static AmbienceService Ambience {
			get {
				if (ambienceService == null)
					ambienceService = (AmbienceService) ServiceManager.GetService (typeof(AmbienceService));
				return ambienceService;
			}
		}
	
		internal static LanguageBindingService Languages {
			get {
				if (languageBindingService == null)
					languageBindingService = (LanguageBindingService) ServiceManager.GetService (typeof(LanguageBindingService));
				return languageBindingService;
			}
		}
	
		internal static DispatchService DispatchService {
			get {
				if (dispatchService == null)
					dispatchService = (DispatchService) ServiceManager.GetService (typeof(DispatchService));
				return dispatchService;
			}
		}
	
	
//		internal static IProjectService ProjectService {
//			get {
//				if (projectService == null)
//					projectService = (IProjectService) ServiceManager.GetService (typeof(IProjectService));
//				return projectService;
//			}
//		}
	
		internal static MessageService MessageService {
			get {
				if (messageService == null)
					messageService = (MessageService) ServiceManager.GetService (typeof(MessageService));
				return messageService;
			}
		}
	
		internal static ResourceService Resources {
			get {
				if (resourceService == null)
					resourceService = (ResourceService) ServiceManager.GetService (typeof(ResourceService));
				return resourceService;
			}
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
