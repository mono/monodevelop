using System;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects.Gui;
using MonoDevelop.Projects.Ambience;

namespace MonoDevelop.Debugger
{
	public class Services
	{
		static IDebuggingService debuggingService;
		static IconService icons;
		
		public static IconService Icons {
			get {
				if (icons == null)
					icons = (IconService) ServiceManager.GetService (typeof(IconService));
				return icons;
			}
		}
	
		public static IDebuggingService DebuggingService {
			get {
				if (debuggingService == null)
					debuggingService = (IDebuggingService) ServiceManager.GetService (typeof(IDebuggingService));
				return debuggingService;
			}
		}
	
		internal static ResourceService Resources {
			get { return MonoDevelop.Core.Gui.Services.Resources; }
		}

		public static AmbienceService Ambience {
			get { return MonoDevelop.Projects.Services.Ambience; }
		}
	}
}
