using System;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.SourceEditor
{
	class Services
	{		
		public static ResourceService Resources {
			get { return MonoDevelop.Core.Gui.Services.Resources; }
		}
	
		public static LanguageBindingService Languages {
			get { return MonoDevelop.Projects.Services.Languages; }
		}
	}
}

