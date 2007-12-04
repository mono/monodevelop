//
// AddinManager.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Reflection;
using System.Collections;

using Mono.Addins.Localization;

namespace Mono.Addins
{
	public class AddinManager
	{
		static AddinSessionService sessionService;
		static AddinRegistry registry;
		
		static string startupDirectory;
		static bool initialized;
		static IAddinInstaller installer;

		public static event AddinErrorEventHandler AddinLoadError;
		public static event AddinEventHandler AddinLoaded;
		public static event AddinEventHandler AddinUnloaded;

		private AddinManager ()
		{
		}
		
		public static void Initialize ()
		{
			Initialize (null);
		}
		
		public static void Initialize (string configDir)
		{
			if (initialized)
				return;
			
			Assembly asm = Assembly.GetEntryAssembly ();
			if (asm == null) asm = Assembly.GetCallingAssembly ();
			string asmFile = new Uri (asm.CodeBase).LocalPath;
			
			startupDirectory = Path.GetDirectoryName (asmFile);
			
			string customDir = Environment.GetEnvironmentVariable ("MONO_ADDINS_REGISTRY");
			if (customDir != null && customDir.Length > 0)
				configDir = customDir;

			if (configDir == null || configDir.Length == 0)
				registry = AddinRegistry.GetGlobalRegistry (startupDirectory);
			else
				registry = new AddinRegistry (configDir, startupDirectory);

			if (registry.CreateHostAddinsFile (asmFile))
				registry.Update (new ConsoleProgressStatus (false));
			
			initialized = true;
			
			SessionService.Initialize ();
		}
		
		public static void Shutdown ()
		{
			SessionService.Shutdown ();
			registry.Dispose ();
			registry = null;
			startupDirectory = null;
			initialized = false;
		}
		
		public static void InitializeDefaultLocalizer (IAddinLocalizer localizer)
		{
			CheckInitialized ();
			SessionService.InitializeDefaultLocalizer (localizer);
		}
		
		internal static string StartupDirectory {
			get { return startupDirectory; }
		}
		
		public static bool IsInitialized {
			get { return initialized; }
		}
		
		public static IAddinInstaller DefaultInstaller {
			get { return installer; }
			set { installer = value; }
		}
		
		public static AddinLocalizer DefaultLocalizer {
			get {
				CheckInitialized ();
				return SessionService.DefaultLocalizer;
			}
		}
		
		public static AddinLocalizer CurrentLocalizer {
			get {
				CheckInitialized ();
				RuntimeAddin addin = SessionService.GetAddinForAssembly (Assembly.GetCallingAssembly ());
				if (addin != null)
					return addin.Localizer;
				else
					return SessionService.DefaultLocalizer;
			}
		}
		
		public static RuntimeAddin CurrentAddin {
			get {
				CheckInitialized ();
				return SessionService.GetAddinForAssembly (Assembly.GetCallingAssembly ());
			}
		}
		
		internal static AddinSessionService SessionService {
			get {
				if (sessionService == null)
					sessionService = new AddinSessionService();
				
				return sessionService;
			}
		}
	
		public static AddinRegistry Registry {
			get {
				CheckInitialized ();
				return registry;
			}
		}
		
		// This method checks if the specified add-ins are installed.
		// If some of the add-ins are not installed, it will use
		// the installer assigned to the DefaultAddinInstaller property
		// to install them. If the installation fails, or if DefaultAddinInstaller
		// is not set, an exception will be thrown.
		public static void CheckInstalled (string message, params string[] addinIds)
		{
			ArrayList notInstalled = new ArrayList ();
			foreach (string id in addinIds) {
				Addin addin = Registry.GetAddin (id, false);
				if (addin != null) {
					// The add-in is already installed
					// If the add-in is disabled, enable it now
					if (!addin.Enabled)
						addin.Enabled = true;
				} else {
					notInstalled.Add (id);
				}
			}
			if (notInstalled.Count == 0)
				return;
			if (installer == null)
				throw new InvalidOperationException ("Add-in installer not set");
			
			// Install the add-ins
			installer.InstallAddins (Registry, message, (string[]) notInstalled.ToArray (typeof(string)));
		}
	
		public static bool IsAddinLoaded (string id)
		{
			CheckInitialized ();
			return SessionService.IsAddinLoaded (id);
		}
		
		public static void LoadAddin (IProgressStatus statusMonitor, string id)
		{
			CheckInitialized ();
			SessionService.LoadAddin (statusMonitor, id, true);
		}
		
		public static ExtensionContext CreateExtensionContext ()
		{
			CheckInitialized ();
			return SessionService.DefaultContext.CreateChildContext ();
		}
		
		public static ExtensionNode GetExtensionNode (string path)
		{
			CheckInitialized ();
			return SessionService.DefaultContext.GetExtensionNode (path);
		}
		
		public static ExtensionNodeList GetExtensionNodes (string path)
		{
			CheckInitialized ();
			return SessionService.DefaultContext.GetExtensionNodes (path);
		}
		
		public static ExtensionNodeList GetExtensionNodes (string path, Type type)
		{
			CheckInitialized ();
			return SessionService.DefaultContext.GetExtensionNodes (path, type);
		}
		
		public static object[] GetExtensionObjects (Type instanceType)
		{
			CheckInitialized ();
			return SessionService.DefaultContext.GetExtensionObjects (instanceType);
		}
		
		public static object[] GetExtensionObjects (Type instanceType, bool reuseCachedInstance)
		{
			CheckInitialized ();
			return SessionService.DefaultContext.GetExtensionObjects (instanceType, reuseCachedInstance);
		}
		
		public static object[] GetExtensionObjects (string path)
		{
			CheckInitialized ();
			return SessionService.DefaultContext.GetExtensionObjects (path);
		}
		
		public static object[] GetExtensionObjects (string path, bool reuseCachedInstance)
		{
			CheckInitialized ();
			return SessionService.DefaultContext.GetExtensionObjects (path, reuseCachedInstance);
		}
		
		public static object[] GetExtensionObjects (string path, Type arrayElementType)
		{
			CheckInitialized ();
			return SessionService.DefaultContext.GetExtensionObjects (path, arrayElementType);
		}
		
		public static object[] GetExtensionObjects (string path, Type arrayElementType, bool reuseCachedInstance)
		{
			CheckInitialized ();
			return SessionService.DefaultContext.GetExtensionObjects (path, arrayElementType, reuseCachedInstance);
		}
		
		public static event ExtensionEventHandler ExtensionChanged {
			add { CheckInitialized(); SessionService.DefaultContext.ExtensionChanged += value; }
			remove { CheckInitialized(); SessionService.DefaultContext.ExtensionChanged -= value; }
		}
		
		public static void AddExtensionNodeHandler (string path, ExtensionNodeEventHandler handler)
		{
			CheckInitialized ();
			SessionService.DefaultContext.AddExtensionNodeHandler (path, handler);
		}
		
		public static void RemoveExtensionNodeHandler (string path, ExtensionNodeEventHandler handler)
		{
			CheckInitialized ();
			SessionService.DefaultContext.RemoveExtensionNodeHandler (path, handler);
		}
		
		static void CheckInitialized ()
		{
			if (!initialized)
				throw new InvalidOperationException ("Add-in manager not initialized.");
		}
		
		internal static void ReportError (string message, string addinId, Exception exception, bool fatal)
		{
			if (AddinLoadError != null)
				AddinLoadError (null, new AddinErrorEventArgs (message, addinId, exception));
			else {
				Console.WriteLine (message);
				if (exception != null)
					Console.WriteLine (exception);
			}
		}
		
		internal static void ReportAddinLoad (string id)
		{
			if (AddinLoaded != null) {
				try {
					AddinLoaded (null, new AddinEventArgs (id));
				} catch {
					// Ignore subscriber exceptions
				}
			}
		}
		
		internal static void ReportAddinUnload (string id)
		{
			if (AddinUnloaded != null) {
				try {
					AddinUnloaded (null, new AddinEventArgs (id));
				} catch {
					// Ignore subscriber exceptions
				}
			}
		}
	}

}
