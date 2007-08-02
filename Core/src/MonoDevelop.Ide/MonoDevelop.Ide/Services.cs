
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
		static IconService icons;
		static IDocumentationService documentationService;
		static IDebuggingService debuggingService;
		static TaskService taskService;
	
		public static MessageService MessageService {
			get { return MonoDevelop.Core.Gui.Services.MessageService; }
		}

		public static ResourceService Resources {
			get { return MonoDevelop.Core.Gui.Services.Resources; }
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
					taskService = new TaskService ();
				return taskService;
			}
		}
	
		public static IParserService ParserService {
			get { return MonoDevelop.Projects.Services.ParserService; }
		}
	
		public static IProjectService ProjectService {
			get { return MonoDevelop.Projects.Services.ProjectService; }
		}
	}
}

