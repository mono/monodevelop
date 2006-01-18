//
// Runtime.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Setup;

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
		static AddInService addInService;
		static SetupService setupService;
		static bool initialized;
		
		private Runtime ()
		{
		}
		
		public static void Initialize ()
		{
			if (initialized)
				throw new InvalidOperationException ("Runtime already initialized.");
			initialized = true;
			AddInService.Initialize ();
		}
	
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
	
		public static AddInService AddInService {
			get {
				if (addInService == null)
					addInService = new AddInService();
				
				return addInService;
			}
		}
	
		public static SetupService SetupService {
			get {
				if (setupService == null)
					setupService = new SetupService();
				
				return setupService;
			}
		}
	}
}
