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
using System.Collections.Specialized;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Serialization;
using Mono.Addins;
using Mono.PkgConfig;
using MonoDevelop.Core.Instrumentation;
using System.Linq;

namespace MonoDevelop.Core.Assemblies
{
	public abstract class TargetRuntime
	{
		HashSet<string> corePackages = new HashSet<string> ();
		
		object initLock = new object ();
		object initEventLock = new object ();
		bool initialized;
		bool frameworksInitialized;
		bool initializing;
		bool backgroundInitialize;
		bool extensionInitialized;
		
		Dictionary<TargetFrameworkMoniker,TargetFrameworkBackend> frameworkBackends
			= new Dictionary<TargetFrameworkMoniker, TargetFrameworkBackend> ();
		
		RuntimeAssemblyContext assemblyContext;
		ComposedAssemblyContext composedAssemblyContext;
		ITimeTracker timer;
		TargetFramework[] customFrameworks = new TargetFramework[0];

		static int internalIdCounter;

		protected bool ShuttingDown { get; private set; }
		
		public TargetRuntime ()
		{
			assemblyContext = new RuntimeAssemblyContext (this);
			composedAssemblyContext = new ComposedAssemblyContext ();
			composedAssemblyContext.Add (Runtime.SystemAssemblyService.UserAssemblyContext);
			composedAssemblyContext.Add (assemblyContext);

			InternalId = Interlocked.Increment (ref internalIdCounter);
			
			Runtime.ShuttingDown += delegate {
				ShuttingDown = true;
			};
		}
		
		public bool IsInitialized {
			get { return initialized; }
		}
		
		protected object InitializationLock {
			get { return initLock; }
		}
		
		internal void StartInitialization ()
		{
			backgroundInitialize = true;
			initializing = true;
			ThreadPool.QueueUserWorkItem (BackgroundInitialize);
		}
		
		/// <summary>
		/// Display name of the runtime. For example "The Mono Runtime 2.6"
		/// </summary>
		public virtual string DisplayName {
			get {
				if (string.IsNullOrEmpty (Version))
					return DisplayRuntimeName;
				else
					return DisplayRuntimeName + " " + Version;
			}
		}
		
		/// <summary>
		/// Unique identifier of this runtime. For example "Mono 2.6".
		/// </summary>
		public string Id {
			get {
				if (string.IsNullOrEmpty (Version))
					return RuntimeId;
				else
					return RuntimeId + " " + Version;
			}
		}

		/// <summary>
		/// Core display name of the runtime. For example "The Mono Runtime"
		/// </summary>
		public virtual string DisplayRuntimeName {
			get { return RuntimeId; }
		}
		
		/// <summary>
		/// Core identifier the runtime. For example, if there are several
		/// versions of Mono installed, each of them will have "Mono" as RuntimeId
		/// </summary>
		public abstract string RuntimeId { get; }
		
		/// <summary>
		/// Version of the runtime.
		/// This string is strictly for displaying to the user or logging. It should never be used for version checks.
		/// </summary>
		public abstract string Version { get; }
		
		/// <summary>
		/// Returns 'true' if this runtime is the one currently running MonoDevelop.
		/// </summary>
		public abstract bool IsRunning { get; }

		/// <summary>
		/// Internal id, to be used at run time
		/// </summary>
		/// <value>The internal identifier.</value>
		internal int InternalId { get; private set; }
		
		public virtual IEnumerable<FilePath> GetReferenceFrameworkDirectories ()
		{
			yield break;
		}
		
		public IEnumerable<TargetFramework> CustomFrameworks {
			get { return customFrameworks; }
		}
		
		protected abstract void OnInitialize ();
		
		/// <summary>
		/// Returns an IExecutionHandler which can be used to execute commands in this runtime.
		/// </summary>
		public abstract IExecutionHandler GetExecutionHandler ();
		
		/// <summary>
		/// Returns an IAssemblyContext which can be used to discover assemblies through this runtime.
		/// It includes assemblies from directories manually registered by the user.
		/// </summary>
		public IAssemblyContext AssemblyContext {
			get {
				EnsureInitialized ();
				return composedAssemblyContext;
			}
		}
		
		/// <summary>
		/// Returns an IAssemblyContext which can be used to discover assemblies provided by this runtime
		/// </summary>
		public RuntimeAssemblyContext RuntimeAssemblyContext {
			get {
				EnsureInitialized ();
				return assemblyContext;
			}
		}

		[Obsolete ("Use DotNetProject.GetAssemblyDebugInfoFile()")]
		public abstract string GetAssemblyDebugInfoFile (string assemblyPath);
		
		/// <summary>
		/// Executes an assembly using this runtime
		/// </summary>
		public Process ExecuteAssembly (string file, string arguments)
		{
			return ExecuteAssembly (file, arguments, null);
		}
		
		/// <summary>
		/// Executes an assembly using this runtime and the specified framework.
		/// </summary>
		public Process ExecuteAssembly (string file, string arguments, TargetFramework fx)
		{
			ProcessStartInfo pi = new ProcessStartInfo (file, arguments);
			pi.UseShellExecute = false;
			return ExecuteAssembly (pi, fx);
		}

		/// <summary>
		/// Executes an assembly using this runtime.
		/// </summary>
		/// <param name="pinfo">
		/// Information of the process to execute
		/// </param>
		/// <returns>
		/// The started process.
		/// </returns>
		public Process ExecuteAssembly (ProcessStartInfo pinfo)
		{
			return ExecuteAssembly (pinfo, null);
		}

		/// <summary>
		/// Executes an assembly using this runtime and the specified framework.
		/// </summary>
		/// <param name="pinfo">
		/// Information of the process to execute
		/// </param>
		/// <param name="fx">
		/// Framework on which the assembly has to be executed.
		/// </param>
		/// <returns>
		/// The started process.
		/// </returns>
		public virtual Process ExecuteAssembly (ProcessStartInfo pinfo, TargetFramework fx)
		{
			ConvertAssemblyProcessStartInfo (pinfo);
			return Process.Start (pinfo);
		}
		
		protected virtual void ConvertAssemblyProcessStartInfo (ProcessStartInfo pinfo)
		{
		}
		
		protected TargetFrameworkBackend GetBackend (TargetFramework fx)
		{
			EnsureInitialized ();
			lock (frameworkBackends) {
				TargetFrameworkBackend backend;
				if (frameworkBackends.TryGetValue (fx.Id, out backend))
					return backend;
				backend = CreateBackend (fx) ?? new NotSupportedFrameworkBackend ();
				backend.Initialize (this, fx);
				frameworkBackends[fx.Id] = backend;
				return backend;
			}
		}
		
		protected virtual TargetFrameworkBackend CreateBackend (TargetFramework fx)
		{
			return null;
		}
		
		internal protected virtual IEnumerable<string> GetFrameworkFolders (TargetFramework fx)
		{
			return GetBackend (fx).GetFrameworkFolders ();
		}

		public IEnumerable<string> FindFacadeAssembliesForPCL (TargetFramework tx)
		{
			foreach (var folder in GetFrameworkFolders (tx)) {
				var facades = Path.Combine (folder, "Facades");
				if (!Directory.Exists (facades))
					continue;

				return Directory.EnumerateFiles (facades, "*.dll");
			}

			//MonoDroid is special case because it's keeping Fascades in v1.0 folder
			if (tx.Id.Identifier == TargetFrameworkMoniker.ID_MONODROID) {
				var frameworkFolder = GetFrameworkFolders (tx).FirstOrDefault ();
				if (frameworkFolder != null) {
					var facades = Path.Combine (Path.Combine (Path.GetDirectoryName (frameworkFolder), "v1.0"), "Facades");
					if (Directory.Exists (facades))
						return Directory.EnumerateFiles (facades, "*.dll");
				}
			}

			return new string[0];
		}

		/// <summary>
		/// Returns a list of environment variables that should be set when running tools using this runtime
		/// </summary>
		public virtual ExecutionEnvironment GetToolsExecutionEnvironment (TargetFramework fx)
		{
			return new ExecutionEnvironment (GetBackend (fx).GetToolsEnvironmentVariables ());
		}

		public virtual ExecutionEnvironment GetToolsExecutionEnvironment ()
		{
			return new ExecutionEnvironment ();
		}
		
		/// <summary>
		/// Looks for the specified tool in this runtime. The name can be a script or a .exe.
		/// </summary>
		public virtual string GetToolPath (TargetFramework fx, string toolName)
		{
			return GetBackend (fx).GetToolPath (toolName);
		}
		
		/// <summary>
		/// Returns a list of paths which can contain tools for this runtime.
		/// </summary>
		public virtual IEnumerable<string> GetToolsPaths (TargetFramework fx)
		{
			return GetBackend (fx).GetToolsPaths ();
		}

		/// <summary>
		/// Returns the MSBuild bin path for this runtime.
		/// </summary>
		public abstract string GetMSBuildBinPath (string toolsVersion);

		/// <summary>
		/// Returns the MSBuild bin path for this runtime.
		/// </summary>
		public abstract string GetMSBuildToolsPath (string toolsVersion);

		/// <summary>
		/// Returns the MSBuild extensions path.
		/// </summary>
		public abstract string GetMSBuildExtensionsPath ();

		/// <summary>
		/// Returns all GAC locations for this runtime.
		/// </summary>
		internal protected abstract IEnumerable<string> GetGacDirectories ();
		
		EventHandler initializedEvent;
		EventHandler frameworksInitializedEvent;

		/// <summary>
		/// This event is fired when the runtime has finished initializing. Runtimes are initialized
		/// in a background thread, so they are not guaranteed to be ready just after the IDE has
		/// finished loading. If the runtime is already initialized when the event is subscribed, then the
		/// subscribed handler will be automatically invoked.
		/// </summary>
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
		
		internal event EventHandler FrameworksInitialized {
			add {
				lock (initEventLock) {
					if (frameworksInitialized) {
						if (!ShuttingDown)
							value (this, EventArgs.Empty);
					}
					else
						frameworksInitializedEvent += value;
				}
			}
			remove {
				lock (initEventLock) {
					frameworksInitializedEvent -= value;
				}
			}
		}
		
		internal void EnsureInitialized ()
		{
			lock (initLock) {
				if (!initialized && !initializing) {
					if (!backgroundInitialize) {
						initializing = true;
						BackgroundInitialize (null);
					}
					else
						// If we are here, that's because 1) the runtime has been initialized, or 2) the runtime is being initialized by *this* thread
						throw new InvalidOperationException ("Runtime intialization not started");
				}
				if (!extensionInitialized && !initializing) {
					// Get assemblies registered using the extension point.
					// This is not done in BackgroundInitialize because the add-in manager is not thread safe
					extensionInitialized = true;
					AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Core/SupportPackages", OnPackagesChanged);
				}
			}
		}
		
		void BackgroundInitialize (object state)
		{
			timer = Counters.TargetRuntimesLoading.BeginTiming ("Initializing Runtime " + Id);
			lock (initLock) {
				try {
					RunInitialization ();
				} catch (Exception ex) {
					LoggingService.LogInternalError ("Unhandled exception in SystemAssemblyService background initialisation thread.", ex);
				} finally {
					lock (initEventLock) {
						initializing = false;
						initialized = true;
						try {
							if (initializedEvent != null && !ShuttingDown)
								initializedEvent (this, EventArgs.Empty);
						} catch (Exception ex) {
							LoggingService.LogError ("Error while initializing the runtime: " + Id, ex);
						}
					}
					timer.End ();
				}
			}
		}
		
		void RunInitialization ()
		{
			if (ShuttingDown)
				return;
			
			timer.Trace ("Finding custom frameworks");
			var customFrameworksList = new List<TargetFramework> ();
			try {
				foreach (var dir in GetReferenceFrameworkDirectories ()) {
					if (!string.IsNullOrEmpty (dir) && Directory.Exists (dir)) {
						customFrameworksList.AddRange (FindTargetFrameworks (dir));
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error finding custom frameworks", ex);
			}
			customFrameworks = customFrameworksList.ToArray ();
			
			timer.Trace ("Creating frameworks");
			CreateFrameworks ();
			
			if (ShuttingDown)
				return;
			
			lock (initEventLock) {
				frameworksInitialized = true;
				try {
					if (frameworksInitializedEvent != null && !ShuttingDown)
						frameworksInitializedEvent (this, EventArgs.Empty);
				} catch (Exception ex) {
					LoggingService.LogError ("Error while initializing the runtime: " + Id, ex);
				}
			}

			timer.Trace ("Initializing frameworks");
			OnInitialize ();
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

		/// <summary>
		/// Registers a package. It can be used by add-ins to register a package for a set of assemblies
		/// they provide.
		/// </summary>
		/// <param name="pinfo">
		/// Information about the package.
		/// </param>
		/// <param name="assemblyFiles">
		/// Assemblies that belong to the package
		/// </param>
		/// <returns>
		/// The registered package
		/// </returns>
		public SystemPackage RegisterPackage (SystemPackageInfo pinfo, params string[] assemblyFiles)
		{
			return RegisterPackage (pinfo, true, assemblyFiles);
		}

		/// <summary>
		/// Registers a package.
		/// </summary>
		/// <param name="pinfo">
		/// Information about the package.
		/// </param>
		/// <param name="isInternal">
		/// Set to true if this package is provided by an add-in and is not installed in the system.
		/// </param>
		/// <param name="assemblyFiles">
		/// The assemblies of the package.
		/// </param>
		/// <returns>
		/// A <see cref="SystemPackage"/>
		/// </returns>
		public SystemPackage RegisterPackage (SystemPackageInfo pinfo, bool isInternal, params string[] assemblyFiles)
		{
			EnsureInitialized ();
			return assemblyContext.RegisterPackage (pinfo, isInternal, assemblyFiles);
		}
		
		/// <summary>
		/// Checks if a framework is installed in this runtime.
		/// </summary>
		/// <param name="fx">
		/// The runtime to check.
		/// </param>
		/// <returns>
		/// True if the framework is installed
		/// </returns>
		public bool IsInstalled (TargetFramework fx)
		{
			return GetBackend (fx).IsInstalled;
		}

		void CreateFrameworks ()
		{
			var frameworks = new HashSet<TargetFrameworkMoniker> ();
			
			foreach (TargetFramework fx in Runtime.SystemAssemblyService.GetKnownFrameworks ()) {
				// A framework is installed if the assemblies directory exists and the first
				// assembly of the list exists.
				if (frameworks.Add (fx.Id) && IsInstalled (fx)) {
					timer.Trace ("Registering assemblies for framework " + fx.Id);
					RegisterSystemAssemblies (fx);
				}
			}
			
			foreach (TargetFramework fx in CustomFrameworks) {
				if (frameworks.Add (fx.Id) && IsInstalled (fx)) {
					timer.Trace ("Registering assemblies for framework " + fx.Id);
					RegisterSystemAssemblies (fx);
				}
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
						if (assembly.Version == null && IsRunning) {
							try {
								AssemblyName aname = SystemAssemblyService.GetAssemblyNameObj (file);
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

		protected static IEnumerable<TargetFramework> FindTargetFrameworks (FilePath frameworksDirectory)
		{
			return FindTargetFrameworks (frameworksDirectory, false);
		}

		protected static IEnumerable<TargetFramework> FindTargetFrameworks (FilePath frameworksDirectory, bool rescanKnownFrameworks)
		{
			foreach (FilePath idDir in Directory.EnumerateDirectories(frameworksDirectory)) {
				var id = idDir.FileName;
				foreach (FilePath versionDir in Directory.EnumerateDirectories (idDir)) {
					var version = versionDir.FileName;
					var moniker = new TargetFrameworkMoniker (id, version);
					if (rescanKnownFrameworks || !Runtime.SystemAssemblyService.IsKnownFramework (moniker)) {
						var fx = ReadTargetFramework (moniker, versionDir);
						if (fx != null)
							yield return (fx);
					}
					var profileListDir = versionDir.Combine ("Profile");
					if (!Directory.Exists (profileListDir))
						continue;
					foreach (FilePath profileDir in Directory.EnumerateDirectories (profileListDir)) {
						var profile = profileDir.FileName;
						moniker = new TargetFrameworkMoniker (id, version, profile);
						if (rescanKnownFrameworks || !Runtime.SystemAssemblyService.IsKnownFramework (moniker)) {
							var fx = ReadTargetFramework (moniker, profileDir);
							if (fx != null)
								yield return (fx);
						}
					}
				}
			}
		}
		
		static TargetFramework ReadTargetFramework (TargetFrameworkMoniker moniker, FilePath directory)
		{
			try {
				return TargetFramework.FromFrameworkDirectory (moniker, directory);
			} catch (Exception ex) {
				LoggingService.LogError ("Error reading framework definition '" + directory + "'", ex);
			}
			return null;
		}
	}
}
