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
		
		public static ResourceService Resources {
			get { return MonoDevelop.Core.Gui.Services.Resources; }
		}
	
		public static IDebuggingService DebuggingService {
			get {
				if (debuggingService == null)
					debuggingService = (IDebuggingService) ServiceManager.GetService (typeof(IDebuggingService));
				return debuggingService;
			}
		}
	
		public static LanguageBindingService Languages {
			get { return MonoDevelop.Projects.Services.Languages; }
		}
	}
}

