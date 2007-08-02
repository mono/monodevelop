using System;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.SourceEditor
{
	class Services
	{
		static IDebuggingService debuggingService;
		static MessageService messageService;
		static ResourceService resourceService;
		static LanguageBindingService languageBindingService;

		public static MessageService MessageService {
			get {
				if (messageService == null)
					messageService = (MessageService) ServiceManager.GetService (typeof(MessageService));
				return messageService;
			}
		}
	
		public static ResourceService Resources {
			get {
				if (resourceService == null)
					resourceService = (ResourceService) ServiceManager.GetService (typeof(ResourceService));
				return resourceService;
			}
		}
	
		public static IDebuggingService DebuggingService {
			get {
				if (debuggingService == null)
					debuggingService = (IDebuggingService) ServiceManager.GetService (typeof(IDebuggingService));
				return debuggingService;
			}
		}
	
		public static LanguageBindingService Languages {
			get {
				if (languageBindingService == null)
					languageBindingService = (LanguageBindingService) ServiceManager.GetService (typeof(LanguageBindingService));
				return languageBindingService;
			}
		}
	}
}

