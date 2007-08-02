using MonoDevelop.Core;
using MonoDevelop.Projects.Ambience;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Documentation;

namespace MonoDevelop.Projects
{
	public class Services
	{
		static AmbienceService ambienceService;
		static ProjectService projectService;
		static LanguageBindingService languageBindingService;
		static IParserService parserService;
		static IDocumentationService documentationService;
	
		public static AmbienceService Ambience {
			get {
				if (ambienceService == null)
					ambienceService = new AmbienceService ();
				return ambienceService;
			}
		}
	
		public static LanguageBindingService Languages {
			get {
				if (languageBindingService == null)
					languageBindingService = new LanguageBindingService ();
				return languageBindingService;
			}
		}
	
		public static ProjectService ProjectService {
			get {
				if (projectService == null)
					projectService = new ProjectService ();
				return projectService;
			}
		}
	
		public static IParserService ParserService {
			get {
				if (parserService == null)
					parserService = new DefaultParserService ();
				return parserService;
			}
		}
	
		public static IDocumentationService DocumentationService {
			get {
				if (documentationService == null)
					documentationService = (IDocumentationService) ServiceManager.GetService (typeof(IDocumentationService));
				return documentationService;
			}
		}
	}
}
