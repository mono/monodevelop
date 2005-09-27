
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Core
{
	public class Runtime
	{
		static ProcessService processService;
		static PropertyService propertyService;
		static StringParserService stringParserService;
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
	
		public static ILoggingService LoggingService {
			get {
				if (loggingService == null)
					loggingService = new DefaultLoggingService();
				
				return loggingService;
			}
		}
	}
}
