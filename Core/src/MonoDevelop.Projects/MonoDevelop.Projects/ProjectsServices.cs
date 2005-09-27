
using MonoDevelop.Core;
using MonoDevelop.Projects.Ambience;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Documentation;

namespace MonoDevelop.Projects
{
	public class Services
	{
		static AmbienceService ambienceService;
		static IProjectService projectService;
		static LanguageBindingService languageBindingService;
		static IParserService parserService;
		static MonodocService monodocService;

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
	
		public static IProjectService ProjectService {
			get {
				if (projectService == null)
					projectService = (IProjectService) ServiceManager.GetService (typeof(IProjectService));
				return projectService;
			}
		}
	
		public static IParserService ParserService {
			get {
				if (parserService == null)
					parserService = (IParserService) ServiceManager.GetService (typeof(IParserService));
				return parserService;
			}
		}
	
		internal static MonodocService Documentation {
			get {
				if (monodocService == null)
					monodocService = (MonodocService) ServiceManager.GetService (typeof(MonodocService));
				return monodocService;
			}
		}
	}
}
