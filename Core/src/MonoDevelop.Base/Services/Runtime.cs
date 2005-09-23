
using MonoDevelop.Core.Services;
using MonoDevelop.Gui;

namespace MonoDevelop.Services
{
	public class Runtime
	{
		static ProcessService processService;
		static PropertyService propertyService;
		static AmbienceService ambienceService;
		static DispatchService dispatchService;
		static IFileService fileService;
		static IParserService parserService;
		static IProjectService projectService;
		static TaskService taskService;
		static IDebuggingService debuggingService;
		static MessageService messageService;
		static MonodocService monodocService;
		static StringParserService stringParserService;
		static GuiService guiService;
		static LanguageBindingService languageBindingService;
		static SystemAssemblyService systemAssemblyService;
		static FileUtilityService fileUtilityService;
		static ILoggingService loggingService;

		public static ProcessService ProcessService {
			get {
				if (processService == null)
					processService = (ProcessService) ServiceManager.GetService (typeof(ProcessService));
				return processService;
			}
		}
	
		public static PropertyService Properties {
			get {
				if (propertyService == null)
					propertyService = (PropertyService) ServiceManager.GetService (typeof(PropertyService));
				return propertyService ;
			}
		}
	
		public static AmbienceService Ambience {
			get {
				if (ambienceService == null)
					ambienceService = (AmbienceService) ServiceManager.GetService (typeof(AmbienceService));
				return ambienceService;
			}
		}
	
		public static LanguageBindingService Languages {
			get {
				if (languageBindingService == null)
					languageBindingService = (LanguageBindingService) ServiceManager.GetService (typeof(LanguageBindingService));
				return languageBindingService;
			}
		}
	
		public static MonodocService Documentation {
			get {
				if (monodocService == null)
					monodocService = (MonodocService) ServiceManager.GetService (typeof(MonodocService));
				return monodocService;
			}
		}
	
		public static DispatchService DispatchService {
			get {
				if (dispatchService == null)
					dispatchService = (DispatchService) ServiceManager.GetService (typeof(DispatchService));
				return dispatchService;
			}
		}
	
	
		public static IFileService FileService {
			get {
				if (fileService == null)
					fileService = (IFileService) ServiceManager.GetService (typeof(IFileService));
				return fileService;
			}
		}
	
		public static IParserService ParserService {
			get {
				if (parserService == null)
					parserService = (IParserService) ServiceManager.GetService (typeof(IParserService));
				return parserService;
			}
		}
	
		public static IProjectService ProjectService {
			get {
				if (projectService == null)
					projectService = (IProjectService) ServiceManager.GetService (typeof(IProjectService));
				return projectService;
			}
		}
	
		public static TaskService TaskService {
			get {
				if (taskService == null)
					taskService = (TaskService) ServiceManager.GetService (typeof(TaskService));
				return taskService;
			}
		}
	
		public static IDebuggingService DebuggingService {
			get {
				if (debuggingService == null)
					debuggingService = (IDebuggingService) ServiceManager.GetService (typeof(IDebuggingService));
				return debuggingService;
			}
		}
	
		public static MessageService MessageService {
			get {
				if (messageService == null)
					messageService = (MessageService) ServiceManager.GetService (typeof(MessageService));
				return messageService;
			}
		}
	
		public static FileUtilityService FileUtilityService {
			get {
				if (fileUtilityService == null)
					fileUtilityService = (FileUtilityService) ServiceManager.GetService (typeof(FileUtilityService));
				return fileUtilityService; 
			}
		}
		
		public static StringParserService StringParserService {
			get {
				if (stringParserService == null)
					stringParserService = (StringParserService) ServiceManager.GetService (typeof(StringParserService));
				return stringParserService; 
			}
		}
		
		public static SystemAssemblyService SystemAssemblyService {
			get {
				if (systemAssemblyService == null)
					systemAssemblyService = (SystemAssemblyService) ServiceManager.GetService (typeof(SystemAssemblyService));
				return systemAssemblyService;
			}
		}
	
		public static GuiService Gui {
			get {
				if (guiService == null)
					guiService = new GuiService ();
				return guiService;
			}
		}
		
		public static ILoggingService LoggingService {
			get {
				if (loggingService == null)
					loggingService = new DefaultLoggingService();
				
				return loggingService;
			}
		}
	}
}
