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
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Execution;
using Mono.Addins;
using Mono.Addins.Setup;

namespace MonoDevelop.Core
{
	public class Runtime
	{
		static ProcessService processService;
		static PropertyService propertyService;
		static StringParserService stringParserService;
		static SystemAssemblyService systemAssemblyService;
		static FileService fileService;
		static ILoggingService loggingService;
		static SetupService setupService;
		static ApplicationService applicationService;
		static bool initialized;
		
		private Runtime ()
		{
		}
		
		public static void Initialize (bool updateAddinRegistry)
		{
			if (initialized)
				return;
			initialized = true;
			
			AddinManager.AddinLoadError += OnLoadError;
			AddinManager.AddinLoaded += OnLoad;
			AddinManager.AddinUnloaded += OnUnload;
			
			string configDir = System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".config");
			configDir = System.IO.Path.Combine (configDir, "MonoDevelop");
			
			if ("build" == new System.IO.DirectoryInfo (System.AppDomain.CurrentDomain.BaseDirectory).Parent.Name) {
				Console.WriteLine ("Using debug add-in registry: " + System.AppDomain.CurrentDomain.BaseDirectory);
				configDir = ".";
			}
				
			AddinManager.Initialize (configDir);
			if (updateAddinRegistry)
				AddinManager.Registry.Update (null);
			setupService = new SetupService (AddinManager.Registry);
			setupService.Repositories.RegisterRepository (null, "http://go-mono.com/md/main.mrep", false);

			ServiceManager.Initialize ();
		}
		
		static void OnLoadError (object s, AddinErrorEventArgs args)
		{
			object msg = "Add-in error (" + args.AddinId + "): " + args.Message;
			Runtime.LoggingService.Error (msg, args.Exception);
		}
		
		static void OnLoad (object s, AddinEventArgs args)
		{
			Runtime.LoggingService.Info ("Add-in loaded: " + args.AddinId);
		}
		
		static void OnUnload (object s, AddinEventArgs args)
		{
			Runtime.LoggingService.Info ("Add-in unloaded: " + args.AddinId);
		}
		
		internal static bool Initialized {
			get { return initialized; }
		}
		
		public static void Shutdown ()
		{
			ServiceManager.UnloadAllServices ();
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
	
		public static FileService FileService {
			get {
				if (fileService == null)
					fileService = new FileService ();
				return fileService; 
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
	
		public static SetupService AddinSetupService {
			get {
				return setupService;
			}
		}
	
		public static ApplicationService ApplicationService {
			get {
				if (applicationService == null)
					applicationService = new ApplicationService ();
				return applicationService;
			}
		}
	}
	
	public class ApplicationService
	{
		public int StartApplication (string appId, string[] parameters)
		{
			ExtensionNode node = AddinManager.GetExtensionNode ("/System/Applications/" + appId);
			if (node == null)
				throw new InstallException ("Application not found: " + appId);
			
			ApplicationExtensionNode apnode = node as ApplicationExtensionNode;
			if (apnode == null)
				throw new Exception ("Invalid node type");
			
			IApplication app = (IApplication) apnode.CreateInstance ();

			try {
				return app.Run (parameters);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				return -1;
			}
		}
		
		public IApplicationInfo[] GetApplications ()
		{
			ExtensionNodeList nodes = AddinManager.GetExtensionNodes ("/System/Applications");
			IApplicationInfo[] apps = new IApplicationInfo [nodes.Count];
			for (int n=0; n<nodes.Count; n++)
				apps [n] = (ApplicationExtensionNode) nodes [n];
			return apps;
		}
	}
	
	public interface IApplicationInfo
	{
		string Id { get; }
		string Description { get; }
	}
	
	public interface IApplication
	{
		int Run (string[] arguments);
	}
}
