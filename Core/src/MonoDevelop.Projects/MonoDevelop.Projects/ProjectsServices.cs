using MonoDevelop.Core;
using MonoDevelop.Projects.Ambience;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Documentation;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects
{
	public class Services
	{
		static AmbienceService ambienceService;
		static IProjectService projectService;
		static LanguageBindingService languageBindingService;
		static IParserService parserService;
		static IDocumentationService documentationService;
		static DeployService deployService;

		public static DeployService DeployService {
			get {
				if (deployService == null)
					deployService = (DeployService) ServiceManager.GetService (typeof(DeployService));
				return deployService;
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
	
		public static IDocumentationService DocumentationService {
			get {
				if (documentationService == null)
					documentationService = (IDocumentationService) ServiceManager.GetService (typeof(IDocumentationService));
				return documentationService;
			}
		}
	}
}
