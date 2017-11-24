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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Mono.Addins;
using Mono.Addins.Setup;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Setup;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace MonoDevelop.Core
{
	public static class Runtime
	{
		static ProcessService processService;
		static SystemAssemblyService systemAssemblyService;
		static AddinSetupService setupService;
		static ApplicationService applicationService;
		static bool initialized;
		static SynchronizationContext mainSynchronizationContext;
		static SynchronizationContext defaultSynchronizationContext;
		static RuntimePreferences preferences = new RuntimePreferences ();
		static Thread mainThread;

		public static void GetAddinRegistryLocation (out string configDir, out string addinsDir, out string databaseDir)
		{
			//provides a development-time way to load addins that are being developed in a asperate solution
			var devConfigDir = Environment.GetEnvironmentVariable ("MONODEVELOP_DEV_CONFIG");
			if (devConfigDir != null && devConfigDir.Length == 0)
				devConfigDir = null;

			var devAddinDir = Environment.GetEnvironmentVariable ("MONODEVELOP_DEV_ADDINS");
			if (devAddinDir != null && devAddinDir.Length == 0)
				devAddinDir = null;

			configDir = devConfigDir ?? UserProfile.Current.ConfigDir;
			addinsDir = devAddinDir ?? UserProfile.Current.LocalInstallDir.Combine ("Addins");
			databaseDir = devAddinDir ?? UserProfile.Current.CacheDir;
		}

		public static void Initialize (bool updateAddinRegistry)
		{
			if (initialized)
				return;

			Counters.RuntimeInitialization.BeginTiming ();
			SetupInstrumentation ();

			Platform.Initialize ();

			mainThread = Thread.CurrentThread;
			// Set a default sync context
			if (SynchronizationContext.Current == null) {
				defaultSynchronizationContext = new SynchronizationContext ();
				SynchronizationContext.SetSynchronizationContext (defaultSynchronizationContext);
			} else
				defaultSynchronizationContext = SynchronizationContext.Current;


			// Hook up the SSL certificate validation codepath
			ServicePointManager.ServerCertificateValidationCallback += delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
				if (sslPolicyErrors == SslPolicyErrors.None)
					return true;
				
				if (sender is WebRequest)
					sender = ((WebRequest)sender).RequestUri.Host;
				return WebCertificateService.GetIsCertificateTrusted (sender as string, certificate.GetPublicKeyString ());
			};
			
			AddinManager.AddinLoadError += OnLoadError;
			AddinManager.AddinLoaded += OnLoad;
			AddinManager.AddinUnloaded += OnUnload;

			try {
				Counters.RuntimeInitialization.Trace ("Initializing Addin Manager");

				string configDir, addinsDir, databaseDir;
				GetAddinRegistryLocation (out configDir, out addinsDir, out databaseDir);
				AddinManager.Initialize (configDir, addinsDir, databaseDir);
				AddinManager.InitializeDefaultLocalizer (new DefaultAddinLocalizer ());
				
				if (updateAddinRegistry)
					AddinManager.Registry.Update (null);
				setupService = new AddinSetupService (AddinManager.Registry);
				Counters.RuntimeInitialization.Trace ("Initialized Addin Manager");
				
				PropertyService.Initialize ();

				WebRequestHelper.Initialize ();
				Mono.Addins.Setup.WebRequestHelper.SetRequestHandler (WebRequestHelper.GetResponse);
				
				//have to do this after the addin service and property service have initialized
				if (UserDataMigrationService.HasSource) {
					Counters.RuntimeInitialization.Trace ("Migrating User Data from MD " + UserDataMigrationService.SourceVersion);
					UserDataMigrationService.StartMigration ();
				}
				
				RegisterAddinRepositories ();

				Counters.RuntimeInitialization.Trace ("Initializing Assembly Service");
				systemAssemblyService = new SystemAssemblyService ();
				systemAssemblyService.Initialize ();
				LoadMSBuildLibraries ();
				
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
			var validUrls = Enum.GetValues (typeof(UpdateLevel)).Cast<UpdateLevel> ().Select (v => setupService.GetMainRepositoryUrl (v)).ToList ();
			
			// Remove old repositories
			
			var reps = setupService.Repositories;
			
			foreach (AddinRepository rep in reps.GetRepositories ()) {
				if (rep.Url.StartsWith ("http://go-mono.com/md/") || 
					(rep.Url.StartsWith ("http://monodevelop.com/files/addins/")) ||
					(rep.Url.StartsWith ("http://addins.monodevelop.com/") && !validUrls.Contains (rep.Url)))
					reps.RemoveRepository (rep.Url);
			}
			
			if (!setupService.IsMainRepositoryRegistered (UpdateLevel.Stable)) {
				setupService.RegisterMainRepository (UpdateLevel.Stable, true);
				setupService.RegisterMainRepository (UpdateLevel.Beta, true);
			}
			if (!setupService.IsMainRepositoryRegistered (UpdateLevel.Beta))
				setupService.RegisterMainRepository (UpdateLevel.Beta, false);

			if (!setupService.IsMainRepositoryRegistered (UpdateLevel.Alpha))
				setupService.RegisterMainRepository (UpdateLevel.Alpha, false);
		}
		
		internal static string GetRepoUrl (string quality)
		{
			string platform;
			if (Platform.IsWindows)
				platform = "Win32";
			else if (Platform.IsMac)
				platform = "Mac";
			else
				platform = "Linux";
			
			return "http://addins.monodevelop.com/" + quality + "/" + platform + "/" + AddinManager.CurrentAddin.Version + "/main.mrep";
		}
		
		static void SetupInstrumentation ()
		{
			InstrumentationService.Enabled = Runtime.Preferences.EnableInstrumentation;
			if (InstrumentationService.Enabled) {
				LoggingService.LogInfo ("Instrumentation Service started");
				try {
					int port = InstrumentationService.PublishService ();
					LoggingService.LogInfo ("Instrumentation available at port " + port);
				} catch (Exception ex) {
					LoggingService.LogError ("Instrumentation service could not be published", ex);
				}
			}
			Runtime.Preferences.EnableInstrumentation.Changed += (s,e) => InstrumentationService.Enabled = Runtime.Preferences.EnableInstrumentation;
		}
		
		static void OnLoadError (object s, AddinErrorEventArgs args)
		{
			string msg = "Add-in error (" + args.AddinId + "): " + args.Message;
			LoggingService.LogError (msg, args.Exception);
		}
		
		static void OnLoad (object s, AddinEventArgs args)
		{
			Counters.AddinsLoaded.Inc ("Add-in loaded: " + args.AddinId, new Dictionary<string,string> { { "AddinId", args.AddinId } });
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
	
		public static AddinSetupService AddinSetupService {
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

		public static RuntimePreferences Preferences {
			get { return preferences; }
		}
		
		static Version version;

		public static Version Version {
			get {
				if (version == null) {
					version = new Version (BuildInfo.Version);
					var relId = SystemInformation.GetReleaseId ();
					if (relId != null && relId.Length >= 9) {
						int rev;
						int.TryParse (relId.Substring (relId.Length - 4), out rev);
						version = new Version (Math.Max (version.Major, 0), Math.Max (version.Minor, 0), Math.Max (version.Build, 0), Math.Max (rev, 0));
					}
				}
				return version;
			}
		}

		public static SynchronizationContext MainSynchronizationContext {
			get {
				return mainSynchronizationContext ?? defaultSynchronizationContext;
			}
			set {
				if (mainSynchronizationContext != null && value != null)
					throw new InvalidOperationException ("The main synchronization context has already been set");
				mainSynchronizationContext = value;
				taskScheduler = null;
			}
		}


		static TaskScheduler taskScheduler;
		public static TaskScheduler MainTaskScheduler {
			get {
				if (taskScheduler == null)
					RunInMainThread (() => taskScheduler = TaskScheduler.FromCurrentSynchronizationContext ()).Wait ();
				return taskScheduler;
			}
		}

		/// <summary>
		/// Runs an action in the main thread (usually the UI thread). The method returns a task, so it can be awaited.
		/// </summary>
		public static Task RunInMainThread (Action action)
		{
			var ts = new TaskCompletionSource<int> ();
			if (IsMainThread) {
				try {
					action ();
					ts.SetResult (0);
				} catch (Exception ex) {
					ts.SetException (ex);
				}
			} else {
				MainSynchronizationContext.Post (delegate {
					try {
						action ();
						ts.SetResult (0);
					} catch (Exception ex) {
						ts.SetException (ex);
					}
				}, null);
			}
			return ts.Task;
		}

		/// <summary>
		/// Runs a function in the main thread (usually the UI thread). The method returns a task, so it can be awaited.
		/// </summary>
		public static Task<T> RunInMainThread<T> (Func<T> func)
		{
			var ts = new TaskCompletionSource<T> ();
			if (IsMainThread) {
				try {
					ts.SetResult (func ());
				} catch (Exception ex) {
					ts.SetException (ex);
				}
			} else {
				MainSynchronizationContext.Post (delegate {
					try {
						ts.SetResult (func ());
					} catch (Exception ex) {
						ts.SetException (ex);
					}
				}, null);
			}
			return ts.Task;
		}

		/// <summary>
		/// Runs an action in the main thread (usually the UI thread). The method returns a task, so it can be awaited.
		/// </summary>
		/// <remarks>This version of the method is useful when the operation to be executed in the main
		/// thread is asynchronous.</remarks>
		public static Task<T> RunInMainThread<T> (Func<Task<T>> func)
		{
			if (IsMainThread) {
				return func ();
			} else {
				var ts = new TaskCompletionSource<T> ();
				MainSynchronizationContext.Post (async state => {
					try {
						ts.SetResult (await func ());
					} catch (Exception ex) {
						ts.SetException (ex);
					}
				}, null);
				return ts.Task;
			}
		}

		/// <summary>
		/// Runs an action in the main thread (usually the UI thread). The method returns a task, so it can be awaited.
		/// </summary>
		/// <remarks>This version of the method is useful when the operation to be executed in the main
		/// thread is asynchronous.</remarks>
		public static Task RunInMainThread (Func<Task> func)
		{
			if (IsMainThread) {
				return func ();
			} else {
				var ts = new TaskCompletionSource<int> ();
				MainSynchronizationContext.Post (async state => {
					try {
						await func ();
						ts.SetResult (0);
					} catch (Exception ex) {
						ts.SetException (ex);
					}
				}, null);
				return ts.Task;
			}
		}

		/// <summary>
		/// Returns true if current thread is GUI thread.
		/// </summary>
		public static bool IsMainThread
		{
			// TODO: This logic should probably change to:
			// Compare types, because instances can change (using SynchronizationContext.CreateCopy).
			// if (SynchronizationContext.Current.GetType () != MainSynchronizationContext.GetType ())
			// once https://bugzilla.xamarin.com/show_bug.cgi?id=35530 is resolved
			get { return mainThread == Thread.CurrentThread; }
		}

		/// <summary>
		/// Asserts that the current thread is the main thread. It will throw an exception if it isn't.
		/// </summary>
		public static void AssertMainThread ()
		{
			if (!IsMainThread)
				throw new InvalidOperationException ("Operation not supported in background thread");
		}

		/// <summary>
		/// Asserts that the current thread is the main thread. It will log a warning if it isn't.
		/// </summary>
		public static void CheckMainThread ()
		{
			if (IsMainThread) {
				return;
			}

			if (System.Diagnostics.Debugger.IsAttached) {
				System.Diagnostics.Debugger.Break ();
			}

			LoggingService.LogWarning ("Operation not supported in background thread. Location: " + Environment.StackTrace);
		}

		public static void SetProcessName (string name)
		{
			if (!Platform.IsMac && !Platform.IsWindows) {
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

		static void LoadMSBuildLibraries ()
		{
			// Explicitly load the msbuild libraries since they are not installed in the GAC
			var path = systemAssemblyService.CurrentRuntime.GetMSBuildBinPath ("15.0");
			SystemAssemblyService.LoadAssemblyFrom (System.IO.Path.Combine (path, "Microsoft.Build.dll"));
			SystemAssemblyService.LoadAssemblyFrom (System.IO.Path.Combine (path, "Microsoft.Build.Framework.dll"));
			SystemAssemblyService.LoadAssemblyFrom (System.IO.Path.Combine (path, "Microsoft.Build.Utilities.Core.dll"));

			if (Type.GetType ("Mono.Runtime") == null) {
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
			}
		}

		/// <summary>
		/// This is necessary on Windows because without it, even though the assemblies are already loaded
		/// the CLR will still throw FileNotFoundException (unable to load assembly).
		/// </summary>
		static System.Reflection.Assembly CurrentDomain_AssemblyResolve (object sender, ResolveEventArgs args)
		{
			if (args.Name != null && args.Name.StartsWith ("Microsoft.Build", StringComparison.OrdinalIgnoreCase)) {
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies ()) {
					if (string.Equals (assembly.FullName, args.Name, StringComparison.OrdinalIgnoreCase)) {
						return assembly;
					}
				}
			}

			return null;
		}
	}
	
	internal static class Counters
	{
		public static TimerCounter RuntimeInitialization = InstrumentationService.CreateTimerCounter ("Runtime initialization", "Runtime", id:"Core.RuntimeInitialization");
		public static TimerCounter PropertyServiceInitialization = InstrumentationService.CreateTimerCounter ("Property Service initialization", "Runtime");
		
		public static Counter AddinsLoaded = InstrumentationService.CreateCounter ("Add-ins loaded", "Add-in Engine", true, id:"Core.AddinsLoaded");
		
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

	public class RuntimePreferences
	{
		internal RuntimePreferences () { }

		public readonly ConfigurationProperty<bool> EnableInstrumentation = ConfigurationProperty.Create ("MonoDevelop.EnableInstrumentation", false);
		public readonly ConfigurationProperty<bool> EnableAutomatedTesting = ConfigurationProperty.Create ("MonoDevelop.EnableAutomatedTesting", false);
		public readonly ConfigurationProperty<string> UserInterfaceLanguage = ConfigurationProperty.Create ("MonoDevelop.Ide.UserInterfaceLanguage", "");
		public readonly ConfigurationProperty<MonoDevelop.Projects.MSBuild.MSBuildVerbosity> MSBuildVerbosity = ConfigurationProperty.Create ("MonoDevelop.Ide.MSBuildVerbosity", MonoDevelop.Projects.MSBuild.MSBuildVerbosity.Normal);
		public readonly ConfigurationProperty<bool> BuildWithMSBuild = ConfigurationProperty.Create ("MonoDevelop.Ide.BuildWithMSBuild", true);
		public readonly ConfigurationProperty<bool> ParallelBuild = ConfigurationProperty.Create ("MonoDevelop.ParallelBuild", true);

		public readonly ConfigurationProperty<string> AuthorName = ConfigurationProperty.Create ("Author.Name", Environment.UserName, oldName:"ChangeLogAddIn.Name");
		public readonly ConfigurationProperty<string> AuthorEmail = ConfigurationProperty.Create ("Author.Email", "", oldName:"ChangeLogAddIn.Email");
		public readonly ConfigurationProperty<string> AuthorCopyright = ConfigurationProperty.Create ("Author.Copyright", (string) null);
		public readonly ConfigurationProperty<string> AuthorCompany = ConfigurationProperty.Create ("Author.Company", "");
		public readonly ConfigurationProperty<string> AuthorTrademark = ConfigurationProperty.Create ("Author.Trademark", "");

		/// <summary>
		/// Gets or sets a value indicating whether the updater should be enabled for the current session.
		/// This value won't be stored in the user preferences.
		/// </summary>
		public bool EnableUpdaterForCurrentSession { get; set; } = true;
	}
}
