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
using System.Runtime.InteropServices;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Execution;
using Mono.Addins;
using Mono.Addins.Setup;
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.Core
{
	public static class Runtime
	{
		static ProcessService processService;
		static SystemAssemblyService systemAssemblyService;
		static SetupService setupService;
		static ApplicationService applicationService;
		static bool initialized;
		
		public static void Initialize (bool updateAddinRegistry)
		{
			if (initialized)
				return;
			Counters.RuntimeInitialization.BeginTiming ();
			SetupInstrumentation ();
			
			AddinManager.AddinLoadError += OnLoadError;
			AddinManager.AddinLoaded += OnLoad;
			AddinManager.AddinUnloaded += OnUnload;
			
			try {
				Counters.RuntimeInitialization.Trace ("Initializing Addin Manager");
				AddinManager.Initialize (MonoDevelop.Core.PropertyService.ConfigPath);
				AddinManager.InitializeDefaultLocalizer (new DefaultAddinLocalizer ());
				
				if (updateAddinRegistry)
					AddinManager.Registry.Update (null);
				setupService = new SetupService (AddinManager.Registry);
				Counters.RuntimeInitialization.Trace ("Initialized Addin Manager");
				
				RegisterAddinRepositories ();
				
				Counters.RuntimeInitialization.Trace ("Initializing Assembly Service");
				systemAssemblyService = new SystemAssemblyService ();
				systemAssemblyService.Initialize ();
				
				initialized = true;
				
			} catch (Exception ex) {
				Console.WriteLine (ex);
				AddinManager.AddinLoadError -= OnLoadError;
				AddinManager.AddinLoaded -= OnLoad;
				AddinManager.AddinUnloaded -= OnUnload;
			} finally {
				Counters.RuntimeInitialization.EndTiming ();
			}
		}
		
		static void RegisterAddinRepositories ()
		{
			string stableUrl = GetRepoUrl ("Stable");
			string betaUrl = GetRepoUrl ("Beta");
			string alphaUrl = GetRepoUrl ("Alpha");
			
			IList validUrls = new string[] { stableUrl, betaUrl, alphaUrl };
			
			// Remove old repositories
			
			var reps = setupService.Repositories;
			
			foreach (AddinRepository rep in reps.GetRepositories ()) {
				if (rep.Url.StartsWith ("http://go-mono.com/md/") || 
					(rep.Url.StartsWith ("http://monodevelop.com/files/addins/")) ||
					(rep.Url.StartsWith ("http://addins.monodevelop.com/") && !validUrls.Contains (rep.Url)))
					reps.RemoveRepository (rep.Url);
			}
			
			if (!reps.ContainsRepository (stableUrl)) {
				// Add the stable and beta channels. Don't add alpha.
				reps.RegisterRepository (null, stableUrl, false);
				reps.RegisterRepository (null, betaUrl, false);
			}
		}
		
		internal static string GetRepoUrl (string quality)
		{
			string platform;
			if (PropertyService.IsWindows)
				platform = "Windows";
			else if (PropertyService.IsMac)
				platform = "Mac";
			else
				platform = "Linux";
			
			return "http://addins.monodevelop.com/" + quality + "/" + platform + "/" + AddinManager.CurrentAddin.Version + "/main.mrep";
		}
		
		static void SetupInstrumentation ()
		{
			InstrumentationService.Enabled = PropertyService.Get ("MonoDevelop.EnableInstrumentation", false);
			if (InstrumentationService.Enabled) {
				LoggingService.LogInfo ("Instrumentation Service started");
				try {
					int port = InstrumentationService.PublishService (0);
					LoggingService.LogInfo ("Instrumentation available at port " + port);
				} catch (Exception ex) {
					LoggingService.LogError ("Instrumentation service could not be published", ex);
				}
			}
			PropertyService.AddPropertyHandler ("MonoDevelop.EnableInstrumentation", delegate {
				InstrumentationService.Enabled = PropertyService.Get ("MonoDevelop.EnableInstrumentation", false);
			});
		}
		
		static void OnLoadError (object s, AddinErrorEventArgs args)
		{
			string msg = "Add-in error (" + args.AddinId + "): " + args.Message;
			LoggingService.LogError (msg, args.Exception);
		}
		
		static void OnLoad (object s, AddinEventArgs args)
		{
			Counters.AddinsLoaded.Inc ("Add-in loaded: " + args.AddinId);
		}
		
		static void OnUnload (object s, AddinEventArgs args)
		{
			Counters.AddinsLoaded.Dec ("Add-in unloaded: " + args.AddinId);
		}
		
		internal static bool Initialized {
			get { return initialized; }
		}
		
		public static void Shutdown ()
		{
			if (!initialized)
				return;
			
			if (ShuttingDown != null)
				ShuttingDown (null, EventArgs.Empty);
			
			PropertyService.SaveProperties ();
			
			if (processService != null) {
				processService.Dispose ();
				processService = null;
			}
			initialized = false;
		}
		
		public static ProcessService ProcessService {
			get {
				if (processService == null)
					processService = new ProcessService ();
				return processService;
			}
		}
		
		public static SystemAssemblyService SystemAssemblyService {
			get {
				return systemAssemblyService;
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
		
		public static void SetProcessName (string name)
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix) {
				try {
					unixSetProcessName (name);
				} catch (Exception e) {
					LoggingService.LogError ("Error setting process name", e);
				}
			}
		}
		
		[DllImport ("libc")] // Linux
		private static extern int prctl (int option, byte [] arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);
		
		[DllImport ("libc")] // BSD
		private static extern void setproctitle (byte [] fmt, byte [] str_arg);
		
		//this is from http://abock.org/2006/02/09/changing-process-name-in-mono/
		static void unixSetProcessName (string name)
		{
			try {
				if (prctl (15 /* PR_SET_NAME */, Encoding.ASCII.GetBytes (name + "\0"), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != 0) {
					throw new ApplicationException ("Error setting process name: " + Mono.Unix.Native.Stdlib.GetLastError ());
				}
			} catch (EntryPointNotFoundException) {
				// Not every BSD has setproctitle
				try {
					setproctitle (Encoding.ASCII.GetBytes ("%s\0"), Encoding.ASCII.GetBytes (name + "\0"));
				} catch (EntryPointNotFoundException) {}
			}
		}
		
		public static event EventHandler ShuttingDown;
	}
	
	public class ApplicationService
	{
		public int StartApplication (string appId, string[] parameters)
		{
			ExtensionNode node = AddinManager.GetExtensionNode ("/MonoDevelop/Core/Applications/" + appId);
			if (node == null)
				throw new InstallException ("Application not found: " + appId);
			
			ApplicationExtensionNode apnode = node as ApplicationExtensionNode;
			if (apnode == null)
				throw new Exception ("Invalid node type");
			
			IApplication app = (IApplication) apnode.CreateInstance ();

			try {
				return app.Run (parameters);
			} catch (Exception ex) {
				Console.WriteLine (ex.Message);
				LoggingService.LogFatalError (ex.ToString ());
				return -1;
			}
		}
		
		public IApplicationInfo[] GetApplications ()
		{
			ExtensionNodeList nodes = AddinManager.GetExtensionNodes ("/MonoDevelop/Core/Applications");
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
	
	internal static class Counters
	{
		public static TimerCounter RuntimeInitialization = InstrumentationService.CreateTimerCounter ("Runtime initialization", "Runtime");
		public static TimerCounter PropertyServiceInitialization = InstrumentationService.CreateTimerCounter ("Property Service initialization", "Runtime");
		
		public static Counter AddinsLoaded = InstrumentationService.CreateCounter ("Add-ins loaded", "Add-in Engine", true);
		
		public static Counter ProcessesStarted = InstrumentationService.CreateCounter ("Processes started", "Process Service");
		public static Counter ExternalObjects = InstrumentationService.CreateCounter ("External objects", "Process Service");
		public static Counter ExternalHostProcesses = InstrumentationService.CreateCounter ("External processes hosting objects", "Process Service");
		
		public static TimerCounter TargetRuntimesLoading = InstrumentationService.CreateTimerCounter ("Target runtimes loaded", "Assembly Service", 0, true);
		public static Counter PcFilesParsed = InstrumentationService.CreateCounter (".pc Files parsed", "Assembly Service");
		
		public static Counter FileChangeNotifications = InstrumentationService.CreateCounter ("File change notifications", "File Service");
		public static Counter FilesRemoved = InstrumentationService.CreateCounter ("Files removed", "File Service");
		public static Counter FilesCreated = InstrumentationService.CreateCounter ("Files created", "File Service");
		public static Counter FilesRenamed = InstrumentationService.CreateCounter ("Files renamed", "File Service");
		public static Counter DirectoriesRemoved = InstrumentationService.CreateCounter ("Directories removed", "File Service");
		public static Counter DirectoriesCreated = InstrumentationService.CreateCounter ("Directories created", "File Service");
		public static Counter DirectoriesRenamed = InstrumentationService.CreateCounter ("Directories renamed", "File Service");
		
		public static Counter LogErrors = InstrumentationService.CreateCounter ("Errors", "Log");
		public static Counter LogWarnings = InstrumentationService.CreateCounter ("Warnings", "Log");
		public static Counter LogMessages = InstrumentationService.CreateCounter ("Information messages", "Log");
		public static Counter LogFatalErrors = InstrumentationService.CreateCounter ("Fatal errors", "Log");
		public static Counter LogDebug = InstrumentationService.CreateCounter ("Debug messages", "Log");
	}
}
