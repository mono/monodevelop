//
// RemoteBuildEngineManager.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2017 Microsoft
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects.MSBuild
{
	/// <summary>
	/// Manages build engines running in external processes.
	/// </summary>
	static class RemoteBuildEngineManager
	{
		// A RemoteBuildEngine is a process that can build project using MSBuild.
		// A build engine is bound to a solution and it can usually build any
		// project of the solution.

		// Projects are built by using a RemoteProjectBuilder. A RemoteBuildEngine
		// will have a RemoteProjectBuilder intance for each project that
		// has been loaded into the remote engine.

		// Remote engines and builders are created on demand, when
		// GetRemoteProjectBuilder() is called. The manager first checks if
		// there is an engine currently running for the provided solution,
		// and starts a new one if there isn't. Then it checks if there is
		// a project builder for the provided project, and creates a new
		// one if there isn't.

		// It may happen that a builder exists when requested, but it may
		// be busy (executing a build). The requester can decide what to do in
		// that case: either force the creation of a new builder engine so that
		// the request can be immediately fullfilled, or just pick the busy
		// builder, in which case the task won't be executed until the current
		// one is finished.

		// RemoteProjectBuilder instances must be explicitly disposed after
		// use. When all project builders of a build engine are disposed,
		// the engine will be scheduled for disposal, and it will be disposed
		// if not used again after EngineDisposalDelay milliseconds
		// (see constant below). Build engines can also be explicitly
		// disposed by calling UnloadSolution(). That method is always called
		// when a solution is disposed. Also, at least one builder is always
		// kept alive for each solution.

		static bool searchPathConfigNeedsUpdate;
		static AsyncCriticalSection buildersLock = new AsyncCriticalSection ();
		static BuilderCache builders = new BuilderCache ();
		static bool shutDown;

		internal static int EngineDisposalDelay = 60000;

		class SessionInfo
		{
			public TextWriter Writer;
			public MSBuildLogger Logger;
			public MSBuildVerbosity Verbosity;
			public ProjectConfigurationInfo [] Configurations;
		}

		static RemoteBuildEngineManager ()
		{
			CleanCachedMSBuildExes ();
			MSBuildProjectService.GlobalPropertyProvidersChanged += HandleGlobalPropertyProviderChanged;
			MSBuildProjectService.ImportSearchPathsChanged += (s, e) => {
				// Reload all builders since search paths have changed
				searchPathConfigNeedsUpdate = true;
				RecycleAllBuilders ().Ignore ();
			};
			Runtime.ShuttingDown += Shutdown;
		}

		static async void Shutdown (object s, EventArgs a)
		{
			using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
				shutDown = true;
				foreach (var engine in builders.GetAllBuilders ().ToList ()) {
					// Signal all project builder of the engine to stop
					engine.Shutdown ();
					engine.CancelScheduledDisposal ();
					builders.Remove (engine);
					engine.DisposeGracefully ();
				}
			}
		}

		static void CheckShutDown ()
		{
			if (shutDown)
				throw new InvalidOperationException ("Runtime is shut down");
		}

		internal static int ActiveEnginesCount => builders.GetAllBuilders ().Where (b => !b.IsShuttingDown).Count ();

		internal static int EnginesCount => builders.GetAllBuilders ().Count ();

		// Used by unit tests
		internal static async Task<int> CountActiveBuildersForProject (string projectFile)
		{
			using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
				return builders.GetAllBuilders ().Count (b => b.IsProjectLoaded (projectFile));
			}
		}

		/// <summary>
		/// Gets or creates a remote build engine
		/// </summary>
		/// <returns>The build engine.</returns>
		/// <param name="projectFile">The project to build.</param>
		/// <param name="solutionFile">Solution to which the project belongs.</param>
		/// <param name="runtime">.NET Runtime to be used to run the builder</param>
		/// <param name="minToolsVersion">Minimum tools version that it has to support.</param>
		/// <param name="requiresMicrosoftBuild">If true, use msbuild instead of xbuild.</param>
		/// <param name="setBusy">If true, the engine will be set as busy once allocated.</param>
		/// <param name="allowBusy">If true, busy engines can be returned.</param>
		/// <remarks>
		/// After using it, the builder must be disposed.
		/// </remarks>
		public async static Task<IRemoteProjectBuilder> GetRemoteProjectBuilder (string projectFile, string solutionFile, TargetRuntime runtime, string minToolsVersion, bool requiresMicrosoftBuild, object buildSessionId, bool setBusy = false, bool allowBusy = true)
		{
			CheckShutDown ();

			RemoteBuildEngine engine;
			using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
				// Get a builder with the provided requirements
				engine = await GetBuildEngine (runtime, minToolsVersion, solutionFile, requiresMicrosoftBuild, "", buildSessionId, setBusy, allowBusy);

				// Add a reference to make sure the engine is alive while the builder is being used.
				// This reference will be freed when ReleaseReference() is invoked on the builder.
				engine.ReferenceCount++;
				try {
					return new RemoteProjectBuilderProxy (await engine.GetRemoteProjectBuilder (projectFile, true), setBusy);
				} catch {
					// Something went wrong, release the above engine reference, since it won't be possible to free it through the builder
					ReleaseProjectBuilderNoLock (engine).Ignore ();
					throw;
				}
			}
		}

		/// <summary>
		/// Gets or creates a remote build engine
		/// </summary>
		async static Task<RemoteBuildEngine> GetBuildEngine (TargetRuntime runtime, string minToolsVersion, string solutionFile, bool requiresMicrosoftBuild, string group, object buildSessionId, bool setBusy = false, bool allowBusy = true)
		{
			Version mtv = Version.Parse (minToolsVersion);
			if (mtv >= new Version (15, 0))
				requiresMicrosoftBuild = true;

			string binDir;
			var toolsVersion = MSBuildProjectService.GetNewestInstalledToolsVersion (runtime, requiresMicrosoftBuild, out binDir);

			Version tv;
			if (Version.TryParse (toolsVersion, out tv) && Version.TryParse (minToolsVersion, out mtv) && tv < mtv) {
				throw new InvalidOperationException (string.Format (
					"Project requires MSBuild ToolsVersion '{0}' which is not supported by runtime '{1}'",
					toolsVersion, runtime.Id)
				);
			}

			// One builder per solution
			string builderKey = runtime.Id + " # " + solutionFile + " # " + group + " # " + requiresMicrosoftBuild;

			RemoteBuildEngine builder = null;

			// Find builders which are not being shut down

			var candiateBuilders = builders.GetBuilders (builderKey).Where (b => !b.IsShuttingDown && (!b.IsBusy || allowBusy));

			if (buildSessionId != null) {

				// Look for a builder that already started the session.
				// If there isn't one, pick builders which don't have any session assigned, so a new one
				// can be started.

				var sessionBuilders = candiateBuilders.Where (b => b.BuildSessionId == buildSessionId);
				if (!sessionBuilders.Any ())
					sessionBuilders = candiateBuilders.Where (b => b.BuildSessionId == null);
				candiateBuilders = sessionBuilders;
			} else
				// Pick builders which are not bound to any session
				candiateBuilders = candiateBuilders.Where (b => b.BuildSessionId == null);

			// Prefer non-busy builders

			builder = candiateBuilders.FirstOrDefault (b => !b.IsBusy) ?? candiateBuilders.FirstOrDefault ();

			if (builder != null) {
				if (setBusy)
					builder.SetBusy ();
				if (builder.BuildSessionId == null && buildSessionId != null) {
					// If a new session is being assigned, signal the session start
					builder.BuildSessionId = buildSessionId;
					var si = (SessionInfo)buildSessionId;
					await builder.BeginBuildOperation (si.Writer, si.Logger, si.Verbosity, si.Configurations).ConfigureAwait (false);
				}
				builder.CancelScheduledDisposal ();
				return builder;
			}

			// No builder available with the required constraints. Start a new builder

			return await Task.Run (async () => {

				// Always start the remote process explicitly, even if it's using the current runtime and fx
				// else it won't pick up the assembly redirects from the builder exe

				var exe = GetExeLocation (runtime, toolsVersion, requiresMicrosoftBuild);
				RemoteProcessConnection connection = null;

				try {

					connection = new RemoteProcessConnection (exe, runtime.GetExecutionHandler ());
					await connection.Connect ().ConfigureAwait (false);

					var props = GetCoreGlobalProperties (solutionFile, binDir, toolsVersion);
					foreach (var gpp in MSBuildProjectService.GlobalPropertyProviders) {
						foreach (var e in gpp.GetGlobalProperties ())
							props [e.Key] = e.Value;
					}

					await connection.SendMessage (new InitializeRequest {
						IdeProcessId = Process.GetCurrentProcess ().Id,
						BinDir = binDir,
						CultureName = GettextCatalog.UICulture.Name,
						GlobalProperties = props
					}).ConfigureAwait (false);

					builder = new RemoteBuildEngine (connection, solutionFile);

				} catch {
					if (connection != null) {
						try {
							connection.Dispose ();
						} catch {
						}
					}
					throw;
				}

				builders.Add (builderKey, builder);
				builder.ReferenceCount = 0;
				builder.BuildSessionId = buildSessionId;
				builder.Disconnected += async delegate {
					using (await buildersLock.EnterAsync ().ConfigureAwait (false))
						builders.Remove (builder);
				};
				if (setBusy)
					builder.SetBusy ();
				if (buildSessionId != null) {
					var si = (SessionInfo)buildSessionId;
					await builder.BeginBuildOperation (si.Writer, si.Logger, si.Verbosity, si.Configurations);
				}
				return builder;
			});
		}


		/// <summary>
		/// Unloads a project from all engines
		/// </summary>
		public static async Task UnloadProject (string projectFile)
		{
			List<RemoteBuildEngine> projectBuilders;
			using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
				if (shutDown) return;
				projectBuilders = builders.GetAllBuilders ().Where (b => b.IsProjectLoaded (projectFile)).ToList ();
			}
			foreach (var b in projectBuilders)
				(await b.GetRemoteProjectBuilder (projectFile, false)).Shutdown ();
		}

		/// <summary>
		/// Unloads all engines bound to a solution
		/// </summary>
		public static async Task UnloadSolution (string solutionFile)
		{
			using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
				if (shutDown) return;
				foreach (var engine in builders.GetAllBuilders ().Where (b => b.SolutionFile == solutionFile).ToList ())
					ShutdownBuilderNoLock (engine);
			}
		}

		/// <summary>
		/// Reloads a project in all engines that have it loaded
		/// </summary>
		public static async Task RefreshProject (string projectFile)
		{
			List<RemoteBuildEngine> projectBuilders;
			using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
				if (shutDown) return;
				projectBuilders = builders.GetAllBuilders ().Where (b => b.IsProjectLoaded (projectFile)).ToList ();
			}
			foreach (var b in projectBuilders)
				await (await b.GetRemoteProjectBuilder (projectFile, false)).Refresh ();
		}

		/// <summary>
		/// Refreshes the content of a project in all engines that have it loaded
		/// </summary>
		public static async Task RefreshProjectWithContent (string projectFile, string projectContent)
		{
			List<RemoteBuildEngine> projectBuilders;
			using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
				if (shutDown) return;
				projectBuilders = builders.GetAllBuilders ().Where (b => b.IsProjectLoaded (projectFile)).ToList ();
			}
			foreach (var b in projectBuilders)
				await (await b.GetRemoteProjectBuilder (projectFile, false)).RefreshWithContent (projectContent);
		}

		/// <summary>
		/// Forces the reload of all project builders
		/// </summary>
		/// <remarks>
		/// This method can be used to discard all currently active project builders, and force the creation
		/// of new ones. This method is useful when there is a change in the MSBuild options or environment
		/// that has an effect on all builders. If a builder is running a task, it will be discarded when
		/// the task ends.
		/// </remarks>
		public static async Task RecycleAllBuilders ()
		{
			using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
				if (shutDown) return;
				foreach (var b in builders.GetAllBuilders ().ToList ())
					ShutdownBuilderNoLock (b);
			}
		}

		/// <summary>
		/// Starts a build session that can span multiple builders and projects
		/// </summary>
		/// <returns>The build session handle.</returns>
		/// <param name="tw">Log writter</param>
		/// <param name="verbosity">MSBuild verbosity.</param>
		internal static object StartBuildSession (TextWriter tw, MSBuildLogger logger, MSBuildVerbosity verbosity, ProjectConfigurationInfo[] configurations)
		{
			CheckShutDown ();
			return new SessionInfo {
					Writer = tw,
					Verbosity = verbosity,
					Logger = logger,
					Configurations = configurations
				};
		}

		/// <summary>
		/// Ends the build session in all builders that are building projects that belong
		/// to the provided session handle.
		/// </summary>
		/// <param name="session">Session handle.</param>
		internal static async Task EndBuildSession (object session)
		{
			using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
				if (shutDown) return;
				foreach (var b in builders.GetAllBuilders ())
					if (b.BuildSessionId == session) {
						b.BuildSessionId = null;
						await b.EndBuildOperation ();
					}
			}
		}

		static async void HandleGlobalPropertyProviderChanged (object sender, EventArgs e)
		{
			// Update the global properties in all builders

			using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
				if (shutDown) return;
				var gpp = (IMSBuildGlobalPropertyProvider)sender;
				foreach (var builder in builders.GetAllBuilders ())
					await builder.SetGlobalProperties (new Dictionary<string, string> (gpp.GetGlobalProperties ()));
			}
		}

		static Dictionary<string, string> GetCoreGlobalProperties (string slnFile, string binDir, string toolsVersion)
		{
			var dictionary = new Dictionary<string, string> ();

			// This causes build targets to behave how they should inside an IDE, instead of in a command-line process
			dictionary.Add ("BuildingInsideVisualStudio", "true");

			// We don't have host compilers in MD, and this is set to true by some of the MS targets
			// which causes it to always run the CoreCompile task if BuildingInsideVisualStudio is also
			// true, because the VS in-process compiler would take care of the deps tracking
			dictionary.Add ("UseHostCompilerIfAvailable", "false");

			if (string.IsNullOrEmpty (slnFile))
				return dictionary;

			dictionary.Add ("SolutionPath", Path.GetFullPath (slnFile));
			dictionary.Add ("SolutionName", Path.GetFileNameWithoutExtension (slnFile));
			dictionary.Add ("SolutionFilename", Path.GetFileName (slnFile));
			dictionary.Add ("SolutionDir", Path.GetDirectoryName (slnFile) + Path.DirectorySeparatorChar);

			// When running the dev15 MSBuild from commandline or inside MSBuild, it sets "VSToolsPath" correctly. when running from MD, it falls back to a bad default. override it.
			if (Platform.IsWindows) {
				dictionary.Add ("VSToolsPath", Path.GetFullPath (Path.Combine (binDir, "..", "..", "Microsoft", "VisualStudio", "v" + toolsVersion)));
			}

			return dictionary;
		}

		/// <summary>
		/// Gets the project builder exe to be used to for a specific runtime and tools version
		/// </summary>
		static string GetExeLocation (TargetRuntime runtime, string toolsVersion, bool requiresMicrosoftBuild)
		{
			// If the builder for the latest MSBuild tools is being requested, return a local copy of the exe.
			// That local copy is configured to add additional msbuild search paths defined by add-ins.

			var mainExe = GetMSBuildExeLocationInBundle (runtime);
			var exe = GetExeLocationInBundle (runtime, toolsVersion, requiresMicrosoftBuild);
			if (exe == mainExe)
				return GetLocalMSBuildExeLocation (runtime);
			return exe;
		}

		static string GetMSBuildExeLocationInBundle (TargetRuntime runtime)
		{
			return GetExeLocationInBundle (runtime, "15.0", true);
		}

		static string GetExeLocationInBundle (TargetRuntime runtime, string toolsVersion, bool requiresMicrosoftBuild)
		{
			// Locate the project builder exe in the MD directory

			var builderDir = new FilePath (typeof (MSBuildProjectService).Assembly.Location).ParentDirectory.Combine ("MSBuild");

			var version = Version.Parse (toolsVersion);
			bool useMicrosoftBuild =
				requiresMicrosoftBuild ||
				((version >= new Version (15, 0)) && Runtime.Preferences.BuildWithMSBuild) ||
				(version >= new Version (4, 0) && runtime is MsNetTargetRuntime);

			if (useMicrosoftBuild) {
				toolsVersion = "dotnet." + toolsVersion;
			}

			var exe = builderDir.Combine (toolsVersion, "MonoDevelop.Projects.Formats.MSBuild.exe");
			if (File.Exists (exe))
				return exe;

			throw new InvalidOperationException ("Unsupported MSBuild ToolsVersion '" + version + "'");
		}

		static string GetLocalMSBuildExeLocation (TargetRuntime runtime)
		{
			// Gets a path to the local copy of the project builder for the provided runtime.
			// If no local copy exists, create one.

			// Builders are copied to a folder inside the cache folder. This folder is cleaned
			// every time XS is started, removing unused builders. The process id is used
			// as folder name, so it is easy to check if the folder is currently in use or not.

			var dirId = Process.GetCurrentProcess ().Id.ToString () + "_" + runtime.InternalId;
			var exesDir = UserProfile.Current.CacheDir.Combine ("MSBuild").Combine (dirId);
			var originalExe = GetMSBuildExeLocationInBundle (runtime);
			var originalExeConfig = originalExe + ".config";
			var destinationExe = exesDir.Combine (Path.GetFileName (originalExe));
			var destinationExeConfig = destinationExe + ".config";

			var localResolversDir = Path.Combine (exesDir, "SdkResolvers");
			var mdResolverDir = Path.Combine (localResolversDir, "MonoDevelop.MSBuildResolver");
			var mdResolverConfig = Path.Combine (mdResolverDir, "sdks.config");

			string binDir;
			MSBuildProjectService.GetNewestInstalledToolsVersion (runtime, true, out binDir);

			if (Platform.IsWindows) {
				// on Windows copy the official MSBuild.exe.config from the VS 2017 install
				// and use this as the starting point
				originalExeConfig = Path.Combine (binDir, "MSBuild.exe.config");
			}

			if (!Directory.Exists (exesDir)) {
				// Copy the builder to the local dir, including the debug file and config file.
				Directory.CreateDirectory (exesDir);
				File.Copy (originalExe, destinationExe);
				var exeMdb = originalExe + ".mdb";
				if (File.Exists (exeMdb))
					File.Copy (exeMdb, exesDir.Combine (Path.GetFileName (exeMdb)));
				var exePdb = Path.ChangeExtension (originalExe, ".pdb");
				if (File.Exists (exePdb))
					File.Copy (exePdb, exesDir.Combine (Path.GetFileName (exePdb)));

				// Copy the whole MSBuild bin folder and subfolders. We need all support assemblies
				// and files.

				FileService.CopyDirectory (binDir, exesDir);

				// Copy the MonoDevelop resolver, used for sdks registered by add-ins.
				// This resolver will load registered sdks from the file sdks.config

				if (!Directory.Exists (mdResolverDir))
					Directory.CreateDirectory (mdResolverDir);

				var builderDir = new FilePath (typeof (MSBuildProjectService).Assembly.Location).ParentDirectory.Combine ("MSBuild");
				File.Copy (Path.Combine (builderDir, "MonoDevelop.MSBuildResolver.dll"), Path.Combine (mdResolverDir, "MonoDevelop.MSBuildResolver.dll"));

				searchPathConfigNeedsUpdate = true;
			}

			if (searchPathConfigNeedsUpdate) {
				// There is already a local copy of the builder, but the config file needs to be updated.
				searchPathConfigNeedsUpdate = false;
				UpdateMSBuildExeConfigFile (runtime, originalExeConfig, destinationExeConfig, mdResolverConfig, binDir);
			}
			return destinationExe;
		}

		static void UpdateMSBuildExeConfigFile (TargetRuntime runtime, string sourceConfigFile, string destinationConfigFile, string mdResolverConfig, string binDir)
		{
			// Creates an MSBuild config file with the search paths registered by add-ins.

			var doc = XDocument.Load (sourceConfigFile);
			var configuration = doc.Root;

			if (Platform.IsWindows) {
				// we want the config file to have the UseLegacyPathHandling=false switch
				// https://blogs.msdn.microsoft.com/jeremykuhne/2016/06/21/more-on-new-net-path-handling/
				var runtimeElement = configuration.Element ("runtime");
				ConfigFileUtilities.SetOrAppendSubelementAttributeValue (runtimeElement, "AppContextSwitchOverrides", "value", "Switch.System.IO.UseLegacyPathHandling=false");
			}

			var toolset = doc.Root.Elements ("msbuildToolsets").FirstOrDefault ()?.Elements ("toolset")?.FirstOrDefault ();
			if (toolset != null) {

				// This is required for MSBuild to properly load the searchPaths element (@radical knows why)
				SetMSBuildConfigProperty (toolset, "MSBuildBinPath", binDir, append: false, insertBefore: true);

				// this must match MSBuildBinPath w/MSBuild15
				SetMSBuildConfigProperty (toolset, "MSBuildToolsPath", binDir, append: false, insertBefore: true);

				if (Platform.IsWindows) {
					var extensionsPath = Path.GetDirectoryName (Path.GetDirectoryName (binDir));
					SetMSBuildConfigProperty (toolset, "MSBuildExtensionsPath", extensionsPath);
					SetMSBuildConfigProperty (toolset, "MSBuildExtensionsPath32", extensionsPath);
					SetMSBuildConfigProperty (toolset, "MSBuildToolsPath", binDir);
					SetMSBuildConfigProperty (toolset, "MSBuildToolsPath32", binDir);

					var sdksPath = Path.Combine (extensionsPath, "Sdks");
					SetMSBuildConfigProperty (toolset, "MSBuildSDKsPath", sdksPath);

					var roslynTargetsPath = Path.Combine (binDir, "Roslyn");
					SetMSBuildConfigProperty (toolset, "RoslynTargetsPath", roslynTargetsPath);

					var vcTargetsPath = Path.Combine (extensionsPath, "Common7", "IDE", "VC", "VCTargets");
					SetMSBuildConfigProperty (toolset, "VCTargetsPath", vcTargetsPath);
				} else {
					var path = MSBuildProjectService.GetProjectImportSearchPaths (runtime, false).FirstOrDefault (p => p.Property == "MSBuildSDKsPath");
					if (path != null)
						SetMSBuildConfigProperty (toolset, path.Property, path.Path);
				}

				var projectImportSearchPaths = doc.Root.Elements ("msbuildToolsets").FirstOrDefault ()?.Elements ("toolset")?.FirstOrDefault ()?.Element ("projectImportSearchPaths");
				if (projectImportSearchPaths != null) {
					var os = Platform.IsMac ? "osx" : Platform.IsWindows ? "windows" : "unix";
					XElement searchPaths = projectImportSearchPaths.Elements ("searchPaths").FirstOrDefault (sp => sp.Attribute ("os")?.Value == os);
					if (searchPaths == null) {
						searchPaths = new XElement ("searchPaths");
						searchPaths.SetAttributeValue ("os", os);
						projectImportSearchPaths.Add (searchPaths);
					}
					foreach (var path in MSBuildProjectService.GetProjectImportSearchPaths (runtime, false))
						SetMSBuildConfigProperty (searchPaths, path.Property, path.Path, append: true, insertBefore: false);
				}
				doc.Save (destinationConfigFile);
			}

			// Update the sdk list for the MD resolver
			SdkInfo.SaveConfig (mdResolverConfig, MSBuildProjectService.FindRegisteredSdks ());
		}

		static void SetMSBuildConfigProperty (XElement elem, string name, string value, bool append = false, bool insertBefore = false)
		{
			var prop = elem.Elements ("property").FirstOrDefault (p => p.Attribute ("name")?.Value == name);
			if (prop != null) {
				var val = prop.Attribute ("value")?.Value;
				if (append)
					prop.SetAttributeValue ("value", val + ";" + value);
				else
					prop.SetAttributeValue ("value", value);
			} else {
				prop = new XElement ("property");
				prop.SetAttributeValue ("name", name);
				prop.SetAttributeValue ("value", value);
				if (insertBefore)
					elem.AddFirst (prop);
				else
					elem.Add (prop);
			}
		}

		static void CleanCachedMSBuildExes ()
		{
			// Removes local copies of project builders that are not currently being used.

			var exesDir = UserProfile.Current.CacheDir.Combine ("MSBuild");
			if (!Directory.Exists (exesDir))
				return;

			foreach (var dir in Directory.GetDirectories (exesDir)) {
				// The file name has to parts: <process-id>_<runtime-id>
				var spid = Path.GetFileName (dir);
				int i = spid.IndexOf ('_');
				if (i == -1)
					continue;
				spid = spid.Substring (0, i);
				int pid;
				if (int.TryParse (Path.GetFileName (spid), out pid)) {
					try {
						// If there is a process running with this id it means the builder is still being used
						if (Process.GetProcessById (pid) != null)
							continue;
					} catch {
						// Ignore
					}
					// No process for this id, it should be safe to delete the folder
					try {
						Directory.Delete (dir, true);
					} catch (Exception ex) {
						LoggingService.LogError ("Could not delete MSBuild cache folder", ex);
					}
				}
			}
		}

		internal static async Task ReleaseProjectBuilder (RemoteBuildEngine engine)
		{
			using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
				await ReleaseProjectBuilderNoLock (engine);
			}
		}

		static void ShutdownBuilderNoLock (RemoteBuildEngine engine)
		{
			// Signal all project builder of the engine to stop
			engine.Shutdown ();

			// If there are no references it means the engine is not being used, so it can be disposed now.
			// Otherwise it will be disposed when all references are released
			if (engine.ReferenceCount == 0) {
				engine.CancelScheduledDisposal ();
				builders.Remove (engine);
				engine.DisposeGracefully ();
			}
		}

		static Task ReleaseProjectBuilderNoLock (RemoteBuildEngine engine)
		{
			if (shutDown)
				return Task.CompletedTask;
			if (--engine.ReferenceCount != 0)
				return Task.CompletedTask;
			if (engine.IsShuttingDown) {
				// If the engine is being shut down, dispose it now.
				builders.Remove (engine);
				engine.DisposeGracefully ();
			} else {
				// Wait a bit before disposing the engine. We may need to use it again.
				engine.ScheduleForDisposal (EngineDisposalDelay).ContinueWith (async t => {
					using (await buildersLock.EnterAsync ().ConfigureAwait (false)) {
						// If this is the last standing build engine for the solution, don't dispose it.
						// In this way there will always be at least one build engine for each solution,
						// until explicitly unloaded.
						if (!builders.GetAllBuilders ().Where (b => !b.IsShuttingDown && b != engine && b.SolutionFile == engine.SolutionFile).Any ())
							return;
						// If after the wait there are still 0 references, dispose the builder
						if (engine.ReferenceCount == 0) {
							builders.Remove (engine);
							engine.DisposeGracefully ();
						}
					}
				}, TaskContinuationOptions.NotOnCanceled);
			}
			return Task.CompletedTask;
		}
	}
}