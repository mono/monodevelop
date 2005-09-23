
using MonoDevelop.Core.Services;
using MonoDevelop.Services;

namespace MonoDevelop.Gui
{
	public class GuiService
	{
		DisplayBindingService displayBindingService;
		IconService icons;
		ResourceService resourceService;
		IStatusBarService statusBarService;
		CommandService commandService;
		
		public IWorkbench Workbench {
			get { return MonoDevelop.Gui.WorkbenchSingleton.Workbench; }
		}
	
		public DisplayBindingService DisplayBindings {
			get {
				if (displayBindingService == null)
					displayBindingService = (DisplayBindingService) ServiceManager.GetService (typeof(DisplayBindingService));
				return displayBindingService;
			}
		}
	
		public IStatusBarService StatusBar {
			get {
				if (statusBarService == null)
					statusBarService = (IStatusBarService) ServiceManager.GetService (typeof(IStatusBarService));
				return statusBarService;
			}
		}
	
		public ResourceService Resources {
			get {
				if (resourceService == null)
					resourceService = (ResourceService) ServiceManager.GetService (typeof(ResourceService));
				return resourceService;
			}
		}
	
		public IconService Icons {
			get {
				if (icons == null)
					icons = (IconService) ServiceManager.GetService (typeof(IconService));
				return icons;
			}
		}
	
		public CommandService CommandService {
			get {
				if (commandService == null)
					commandService = (CommandService) ServiceManager.GetService (typeof(CommandService));
				return commandService;
			}
		}
	}
}
