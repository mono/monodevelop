
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Documentation;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Gui;
using MonoDevelop.Projects.Documentation;

namespace MonoDevelop.Ide
{
	internal class Services
	{
		static MessageService messageService;
		static DisplayBindingService displayBindingService;
		static ResourceService resourceService;
		static IStatusBarService statusBarService;
		static IconService icons;
		static IDocumentationService documentationService;
		static IDebuggingService debuggingService;
		static TaskService taskService;
//		static IParserService parserService;
		static DispatchService dispatchService;
	
		public static IStatusBarService StatusBar {
			get {
				if (statusBarService == null)
					statusBarService = (IStatusBarService) ServiceManager.GetService (typeof(IStatusBarService));
				return statusBarService;
			}
		}

		public static MessageService MessageService {
			get {
				if (messageService == null)
					messageService = (MessageService) ServiceManager.GetService (typeof(MessageService));
				return messageService;
			}
		}

		internal static DisplayBindingService DisplayBindings {
			get {
				if (displayBindingService == null)
					displayBindingService = (DisplayBindingService) ServiceManager.GetService (typeof(DisplayBindingService));
				return displayBindingService;
			}
		}
	
		public static ResourceService Resources {
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
	
		public static IDocumentationService DocumentationService {
			get {
				if (documentationService == null)
					documentationService = (IDocumentationService) ServiceManager.GetService (typeof(IDocumentationService));
				return documentationService;
			}
		}
	
		public static IDebuggingService DebuggingService {
			get {
				if (debuggingService == null)
					debuggingService = (IDebuggingService) ServiceManager.GetService (typeof(IDebuggingService));
				return debuggingService;
			}
		}
	
		public static TaskService TaskService {
			get {
				if (taskService == null)
					taskService = (TaskService) ServiceManager.GetService (typeof(TaskService));
				return taskService;
			}
		}
	
/*		public static IParserService ParserService {
			get {
				if (parserService == null)
					parserService = (IParserService) ServiceManager.GetService (typeof(IParserService));
				return parserService;
			}
		}*/
	
		public static DispatchService DispatchService {
			get {
				if (dispatchService == null)
					dispatchService = (DispatchService) ServiceManager.GetService (typeof(DispatchService));
				return dispatchService;
			}
		}
	}
}

