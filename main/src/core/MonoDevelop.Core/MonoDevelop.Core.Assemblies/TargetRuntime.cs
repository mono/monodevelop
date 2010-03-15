// 
// TargetRuntime.cs
//  
// Author:
//   Todd Berman <tberman@sevenl.net>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (C) 2004 Todd Berman
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Threading;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Serialization;
using Mono.Addins;
using Mono.PkgConfig;
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.Core.Assemblies
{
	public abstract class TargetRuntime
	{
		HashSet<string> corePackages = new HashSet<string> ();
		
		object initLock = new object ();
		object initEventLock = new object ();
		bool initialized;
		TargetFrameworkBackend[] frameworkBackends;
		
		RuntimeAssemblyContext assemblyContext;
		ComposedAssemblyContext composedAssemblyContext;
		ITimeTracker timer;
		
		protected bool ShuttingDown { get; private set; }
		
		public TargetRuntime ()
		{
			assemblyContext = new RuntimeAssemblyContext (this);
			composedAssemblyContext = new ComposedAssemblyContext ();
			composedAssemblyContext.Add (Runtime.SystemAssemblyService.UserAssemblyContext);
			composedAssemblyContext.Add (assemblyContext);
			
			Runtime.ShuttingDown += delegate {
				ShuttingDown = true;
			};
		}
		
		internal void StartInitialization ()
		{
			// Initialize the service in a background thread.
			Thread t = new Thread (new ThreadStart (BackgroundInitialize)) {
				Name = "Assembly service initialization",
				IsBackground = true,
			};
			t.Start ();
		}
		
		public virtual string DisplayName {
			get {
				if (string.IsNullOrEmpty (Version))
					return DisplayRuntimeName;
				else
					return DisplayRuntimeName + " " + Version;
			}
		}
		
		public string Id {
			get {
				if (string.IsNullOrEmpty (Version))
					return RuntimeId;
				else
					return RuntimeId + " " + Version;
			}
		}
		
		public virtual string DisplayRuntimeName {
			get { return RuntimeId; }
		}
		
		public abstract string RuntimeId { get; }
		
		/// <summary>
		/// This string is strictly for displaying to the user or logging. It should never be used for version checks.
		/// </summary>
		public abstract string Version { get; }
		
		public abstract bool IsRunning { get; }
		
		protected abstract void OnInitialize ();
		
		public abstract IExecutionHandler GetExecutionHandler ();
		
		public IAssemblyContext AssemblyContext {
			get { return composedAssemblyContext; }
		}
		
		public RuntimeAssemblyContext RuntimeAssemblyContext {
			get { return assemblyContext; }
		}
		
		public abstract string GetAssemblyDebugInfoFile (string assemblyPath);
		
		public virtual Process ExecuteAssembly (ProcessStartInfo pinfo, TargetFramework fx)
		{
			// Make a copy of the ProcessStartInfo because we are going to modify it
			
			ProcessStartInfo cp = new ProcessStartInfo ();
			cp.Arguments = pinfo.Arguments;
			cp.CreateNoWindow = pinfo.CreateNoWindow;
			cp.Domain = pinfo.Domain;
			cp.ErrorDialog = pinfo.ErrorDialog;
			cp.ErrorDialogParentHandle = pinfo.ErrorDialogParentHandle;
			cp.FileName = pinfo.FileName;
			cp.LoadUserProfile = pinfo.LoadUserProfile;
			cp.Password = pinfo.Password;
			cp.UseShellExecute = pinfo.UseShellExecute;
			cp.RedirectStandardError = pinfo.RedirectStandardError;
			cp.RedirectStandardInput = pinfo.RedirectStandardInput;
			cp.RedirectStandardOutput = pinfo.RedirectStandardOutput;
			cp.StandardErrorEncoding = pinfo.StandardErrorEncoding;
			cp.StandardOutputEncoding = pinfo.StandardOutputEncoding;
			cp.UserName = pinfo.UserName;
			cp.Verb = pinfo.Verb;
			cp.WindowStyle = pinfo.WindowStyle;
			cp.WorkingDirectory = pinfo.WorkingDirectory;
			
			foreach (string key in pinfo.EnvironmentVariables.Keys)
				cp.EnvironmentVariables [key] = pinfo.EnvironmentVariables [key];
			
			// Set the runtime env vars
			
			foreach (KeyValuePair<string,string> evar in GetToolsEnvironmentVariables (fx))
				cp.EnvironmentVariables [evar.Key] = evar.Value;
			
			ConvertAssemblyProcessStartInfo (pinfo);
			return Process.Start (pinfo);
		}
		
		protected virtual void ConvertAssemblyProcessStartInfo (ProcessStartInfo pinfo)
		{
		}
		
		protected TargetFrameworkBackend GetBackend (TargetFramework fx)
		{
			if (frameworkBackends == null)
				frameworkBackends = new TargetFrameworkBackend [TargetFramework.FrameworkCount];
			else if (fx.Index >= frameworkBackends.Length)
				Array.Resize (ref frameworkBackends, TargetFramework.FrameworkCount);
			
			TargetFrameworkBackend backend = frameworkBackends [fx.Index];
			if (backend == null) {
				backend = fx.CreateBackendForRuntime (this);
				if (backend == null) {
					backend = CreateBackend (fx);
					if (backend == null)
						backend = new NotSupportedFrameworkBackend ();
				}
				backend.Initialize (this, fx);
				frameworkBackends [fx.Index] = backend;
			}
			return backend;
		}
		
		protected virtual TargetFrameworkBackend CreateBackend (TargetFramework fx)
		{
			return null;
		}
		
		internal protected virtual IEnumerable<string> GetFrameworkFolders (TargetFramework fx)
		{
			return GetBackend (fx).GetFrameworkFolders ();
		}
		
		//environment variables that should be set when running tools in this environment
		public virtual Dictionary<string, string> GetToolsEnvironmentVariables (TargetFramework fx)
		{
			return GetBackend (fx).GetToolsEnvironmentVariables ();
		}
		
		public virtual string GetToolPath (TargetFramework fx, string toolName)
		{
			return GetBackend (fx).GetToolPath (toolName);
		}
		
		public virtual IEnumerable<string> GetToolsPaths (TargetFramework fx)
		{
			return GetBackend (fx).GetToolsPaths ();
		}
		
		public abstract string GetMSBuildBinPath (TargetFramework fx);
		
		internal protected abstract IEnumerable<string> GetGacDirectories ();
		
		EventHandler initializedEvent;
			
		public event EventHandler Initialized {
			add {
				lock (initEventLock) {
					if (initialized) {
						if (!ShuttingDown)
							value (this, EventArgs.Empty);
					}
					else
						initializedEvent += value;
				}
			}
			remove {
				lock (initEventLock) {
					initializedEvent -= value;
				}
			}
		}
		
		internal void Initialize ()
		{
			lock (initLock) {
				while (!initialized) {
					Monitor.Wait (initLock);
				}
			}
		}
		
		void BackgroundInitialize ()
		{
			timer = Counters.TargetRuntimesLoading.BeginTiming ("Initializing Runtime " + Id);
			lock (initLock) {
				try {
					RunInitialization ();
				} catch (Exception ex) {
					LoggingService.LogFatalError ("Unhandled exception in SystemAssemblyService background initialisation thread.", ex);
				} finally {
					Monitor.PulseAll (initLock);
					lock (initEventLock) {
						initialized = true;
						if (initializedEvent != null && !ShuttingDown)
							initializedEvent (this, EventArgs.Empty);
					}
					timer.End ();
				}
			}
		}
		
		void RunInitialization ()
		{
			if (ShuttingDown)
				return;
			
			timer.Trace ("Creating frameworks");
			CreateFrameworks ();
			
			if (ShuttingDown)
				return;
			
			timer.Trace ("Initializing frameworks");
			OnInitialize ();
			
			if (ShuttingDown)
				return;
			
			timer.Trace ("Registering support packages");
			
			// Get assemblies registered using the extension point
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Core/SupportPackages", OnPackagesChanged);
		}
		
		void OnPackagesChanged (object s, ExtensionNodeEventArgs args)
		{
			PackageExtensionNode node = (PackageExtensionNode) args.ExtensionNode;
			SystemPackageInfo pi = node.GetPackageInfo ();
			
			if (args.Change == ExtensionChange.Add) {
				var existing = assemblyContext.GetPackageInternal (pi.Name);
				if (existing == null || (!existing.IsFrameworkPackage || pi.IsFrameworkPackage))
					RegisterPackage (pi, node.Assemblies);
			}
			else {
				SystemPackage p = assemblyContext.GetPackage (pi.Name, pi.Version);
				if (p.IsInternalPackage)
					assemblyContext.UnregisterPackage (pi.Name, pi.Version);
			}
		}
		
		public SystemPackage RegisterPackage (SystemPackageInfo pinfo, params string[] assemblyFiles)
		{
			return RegisterPackage (pinfo, true, assemblyFiles);
		}
		
		public SystemPackage RegisterPackage (SystemPackageInfo pinfo, bool isInternal, params string[] assemblyFiles)
		{
			return assemblyContext.RegisterPackage (pinfo, isInternal, assemblyFiles);
		}
		
		public bool IsInstalled (TargetFramework fx)
		{
			return GetBackend (fx).IsInstalled;
		}

		void CreateFrameworks ()
		{
			if ((SystemAssemblyService.UpdateExpandedFrameworksFile || !SystemAssemblyService.UseExpandedFrameworksFile)) {
				// Read the assembly versions
				foreach (TargetFramework fx in Runtime.SystemAssemblyService.GetTargetFrameworks ()) {
					if (IsInstalled (fx)) {
						IEnumerable<string> dirs = GetFrameworkFolders (fx);
						foreach (AssemblyInfo assembly in fx.Assemblies) {
							foreach (string dir in dirs) {
								string file = Path.Combine (dir, assembly.Name) + ".dll";
								if (File.Exists (file)) {
									if ((assembly.Version == null || SystemAssemblyService.UpdateExpandedFrameworksFile) && IsRunning) {
										System.Reflection.AssemblyName aname = SystemAssemblyService.GetAssemblyNameObj (file);
										assembly.Update (aname);
									}
								}
							}
						}
					}
				}
			}
			
			foreach (TargetFramework fx in Runtime.SystemAssemblyService.GetTargetFrameworks ()) {
				// A framework is installed if the assemblies directory exists and the first
				// assembly of the list exists.
				if (IsInstalled (fx)) {
					timer.Trace ("Registering assemblies for framework " + fx.Id);
					RegisterSystemAssemblies (fx);
				}
			}
			
			if (SystemAssemblyService.UpdateExpandedFrameworksFile && IsRunning) {
				Runtime.SystemAssemblyService.SaveGeneratedFrameworkInfo ();
			}
		}
		
		protected bool IsCorePackage (string pname)
		{
			return corePackages.Contains (pname);
		}

		void RegisterSystemAssemblies (TargetFramework fx)
		{
			Dictionary<string,List<SystemAssembly>> assemblies = new Dictionary<string, List<SystemAssembly>> ();
			Dictionary<string,SystemPackage> packs = new Dictionary<string, SystemPackage> ();
			
			IEnumerable<string> dirs = GetFrameworkFolders (fx);

			foreach (AssemblyInfo assembly in fx.Assemblies) {
				foreach (string dir in dirs) {
					string file = Path.Combine (dir, assembly.Name) + ".dll";
					if (File.Exists (file)) {
						if ((assembly.Version == null || SystemAssemblyService.UpdateExpandedFrameworksFile) && IsRunning) {
							try {
								System.Reflection.AssemblyName aname = SystemAssemblyService.GetAssemblyNameObj (file);
								assembly.Update (aname);
							} catch {
								// If something goes wrong when getting the name, just ignore the assembly
							}
						}
						string pkg = assembly.Package ?? string.Empty;
						SystemPackage package;
						if (!packs.TryGetValue (pkg, out package)) {
							packs [pkg] = package = new SystemPackage ();
							assemblies [pkg] = new List<SystemAssembly> ();
						}
						List<SystemAssembly> list = assemblies [pkg];
						list.Add (assemblyContext.AddAssembly (file, assembly, package));
						break;
					}
				}
			}
			
			foreach (string pkg in packs.Keys) {
				SystemPackage package = packs [pkg];
				List<SystemAssembly> list = assemblies [pkg];
				SystemPackageInfo info = GetFrameworkPackageInfo (fx, pkg);
				if (!info.IsCorePackage)
					corePackages.Add (info.Name);
				package.Initialize (info, list.ToArray (), false);
				assemblyContext.InternalAddPackage (package);
			}
		}
		
		protected virtual SystemPackageInfo GetFrameworkPackageInfo (TargetFramework fx, string packageName)
		{
			return GetBackend (fx).GetFrameworkPackageInfo (packageName);
		}
	}
}
