//  Project.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//   Viktoria Dudka  <viktoriad@remobjects.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2009 RemObjects Software
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
//
//


using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MonoDevelop;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using System.Threading.Tasks;
using MonoDevelop.Projects.Formats.MSBuild;
using System.Xml;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects.Extensions;
using System.Threading;
using Mono.Addins;


namespace MonoDevelop.Projects
{
	/// <summary>
	/// A project
	/// </summary>
	/// <remarks>
	/// This is the base class for MonoDevelop projects. A project is a solution item which has a list of
	/// source code files and which can be built to generate an output.
	/// </remarks>
	public class Project : SolutionItem
	{
		string[] flavorGuids = new string[0];
		string[] buildActions;
		MSBuildProject sourceProject;

		string productVersion;
		string schemaVersion;
		bool modifiedInMemory;

		List<string> defaultImports;

		protected Project ()
		{
			FileService.FileChanged += OnFileChanged;
			Runtime.SystemAssemblyService.DefaultRuntimeChanged += OnDefaultRuntimeChanged;
			files = new ProjectFileCollection ();
			Items.Bind (files);
			DependencyResolutionEnabled = true;
		}

		protected Project (params string[] flavorGuids): this()
		{
			this.flavorGuids = flavorGuids;
		}

		protected Project (string[] flavorIds, MSBuildProject sourceProject): this(flavorIds)
		{
		}

		protected Project (ProjectCreateInformation projectCreateInfo, XmlElement projectOptions): this()
		{
			var ids = projectOptions != null ? projectOptions.GetAttribute ("flavorIds") : null;
			if (!string.IsNullOrEmpty (ids)) {
				this.flavorGuids = ids.Split (new [] {';'}, StringSplitOptions.RemoveEmptyEntries);
			}
		}

		internal class CreationContext
		{
			static object theLock = new object ();
			public MSBuildProject Project { get; set; }
			public string TypeGuid { get; set; }

			internal static void LockContext (MSBuildProject p, string typeGuid)
			{
				Monitor.Enter (theLock);
				Current = new CreationContext ();
				Current.Project = p;
				Current.TypeGuid = typeGuid;
			}

			internal static void UnlockContext ()
			{
				Current = null;
				Monitor.Exit (theLock);
			}

			public static CreationContext Current { get; private set; }
		}

		protected override void OnInitialize ()
		{
			base.OnInitialize ();

			if (CreationContext.Current != null) {

				if (IsExtensionChainCreated)
					throw new InvalidOperationException ("Extension chain already created for this object");

				TypeGuid = CreationContext.Current.TypeGuid;
				this.sourceProject = CreationContext.Current.Project;

				IMSBuildPropertySet globalGroup = sourceProject.GetGlobalPropertyGroup ();
				string projectTypeGuids = globalGroup.GetValue ("ProjectTypeGuids");

				if (projectTypeGuids != null) {
					var subtypeGuids = new List<string> ();
					foreach (string guid in projectTypeGuids.Split (';')) {
						string sguid = guid.Trim ();
						if (sguid.Length > 0 && string.Compare (sguid, CreationContext.Current.TypeGuid, StringComparison.OrdinalIgnoreCase) != 0)
							subtypeGuids.Add (guid);
					}
					flavorGuids = subtypeGuids.ToArray ();
				}
			}
		}

		protected override void OnExtensionChainInitialized ()
		{
			base.OnExtensionChainInitialized ();
			if (CreationContext.Current != null)
				FileName = CreationContext.Current.Project.FileName;
		}

		void OnDefaultRuntimeChanged (object o, EventArgs args)
		{
			// If the default runtime changes, the project builder for this project may change
			// so it has to be created again.
			CleanupProjectBuilder ();
		}

		public IEnumerable<string> FlavorGuids {
			get { return flavorGuids; }
		}

		public List<string> DefaultImports {
			get {
				if (defaultImports == null) {
					var list = new List<string> ();
					ProjectExtension.OnGetDefaultImports (list);
					defaultImports = list;
				}
				return defaultImports; 
			}
		}

		protected virtual void OnGetDefaultImports (List<string> imports)
		{
		}

		public string ToolsVersion { get; private set; }

		internal bool CheckAllFlavorsSupported ()
		{
			return FlavorGuids.All (g => ProjectExtension.SupportsFlavor (g));
		}

		ProjectExtension projectExtension;
		ProjectExtension ProjectExtension {
			get {
				if (projectExtension == null)
					projectExtension = ExtensionChain.GetExtension<ProjectExtension> ();
				return projectExtension;
			}
		}

		/// <summary>Whether to use the MSBuild engine by default.</summary>
		internal bool UseMSBuildEngineByDefault { get; set; }

		/// <summary>Forces the MSBuild engine to be used.</summary>
		internal bool RequireMSBuildEngine { get; set; }

		protected override void OnModified (SolutionItemModifiedEventArgs args)
		{
			if (!Loading)
				modifiedInMemory = true;
			base.OnModified (args);
		}

		protected override Task OnLoad (ProgressMonitor monitor)
		{
			MSBuildProject p = sourceProject;
			sourceProject = null;

			return Task.Factory.StartNew (delegate {
				if (p == null)
					p = MSBuildProject.LoadAsync (FileName).Result;

				IMSBuildPropertySet globalGroup = p.GetGlobalPropertyGroup ();
				// Avoid crash if there is not global group
				if (globalGroup == null)
					p.AddNewPropertyGroup (false);

				ProjectExtension.OnPrepareForEvaluation (p);

				try {
					ProjectExtensionUtil.BeginLoadOperation ();
					p.Evaluate ();
					ReadProject (monitor, p);
				} finally {
					ProjectExtensionUtil.EndLoadOperation ();
				}
			});
		}

		/// <summary>
		/// Called just after the MSBuild project is loaded but before it is evaluated.
		/// </summary>
		/// <param name="project">The project</param>
		/// <remarks>
		/// Subclasses can override this method to transform the MSBuild project before it is evaluated.
		/// For example, it can be used to add or remove imports, or to set custom values for properties.
		/// Changes done in the MSBuild files are not saved.
		/// </remarks>
		protected virtual void OnPrepareForEvaluation (MSBuildProject project)
		{
		}

		internal protected override Task OnSave (ProgressMonitor monitor)
		{
			modifiedInMemory = false;

			return Task.Factory.StartNew (delegate {
				var msproject = WriteProject (monitor);
				if (msproject == null)
					return;

				// Don't save the file to disk if the content did not change
				msproject.Save (FileName);

				if (projectBuilder != null)
					projectBuilder.Refresh ();
			});
		}

		protected override IEnumerable<WorkspaceObjectExtension> CreateDefaultExtensions ()
		{
			return base.CreateDefaultExtensions ().Concat (Enumerable.Repeat (new DefaultMSBuildProjectExtension (), 1));
		}

		internal protected override IEnumerable<string> GetItemTypeGuids ()
		{
			return base.GetItemTypeGuids ().Concat (flavorGuids);
		}

		/// <summary>
		/// Description of the project.
		/// </summary>
		private string description = "";
		public string Description {
			get { return description ?? ""; }
			set {
				description = value;
				NotifyModified ("Description");
			}
		}
		
		/// <summary>
		/// Determines whether the provided file can be as part of this project
		/// </summary>
		/// <returns>
		/// <c>true</c> if the file can be compiled; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='fileName'>
		/// File name
		/// </param>
		public virtual bool IsCompileable (string fileName)
		{
			return ProjectExtension.OnGetIsCompileable (fileName);
		}

		/// <summary>
		/// Files of the project
		/// </summary>
		public ProjectFileCollection Files {
			get { return files; }
		}
		private ProjectFileCollection files;

		FilePath baseIntermediateOutputPath;

		public virtual FilePath BaseIntermediateOutputPath {
			get {
				if (!baseIntermediateOutputPath.IsNullOrEmpty)
					return baseIntermediateOutputPath;
				return BaseDirectory.Combine ("obj");
			}
			set {
				if (value.IsNullOrEmpty)
					value = FilePath.Null;
				if (baseIntermediateOutputPath == value)
					return;
				NotifyModified ("BaseIntermediateOutputPath");
			}
		}

		/// <summary>
		/// Gets the type of the project.
		/// </summary>
		/// <value>
		/// The type of the project.
		/// </value>
		[Obsolete ("Use GetProjectTypes")]
		public virtual string ProjectType {
			get { return GetProjectTypes ().First (); }
		}

		/// <summary>
		/// Gets the project type and its base types.
		/// </summary>
		public IEnumerable<string> GetProjectTypes ()
		{
			var types = new HashSet<string> ();
			ProjectExtension.OnGetProjectTypes (types);
			return types;
		}

		protected virtual void OnGetProjectTypes (HashSet<string> types)
		{
		}

		public bool HasFlavor<T> ()
		{
			return GetService (typeof(T)) != null;
		}

		public T GetFlavor<T> () where T:ProjectExtension
		{
			return (T) GetService (typeof(T));
		}

		internal IEnumerable<ProjectExtension> GetFlavors ()
		{
			return ExtensionChain.GetAllExtensions ().OfType<ProjectExtension> ();
		}

		/// <summary>
		/// Gets or sets the icon of the project.
		/// </summary>
		/// <value>
		/// The stock icon.
		/// </value>
		public virtual IconId StockIcon {
			get {
				if (stockIcon != null)
					return stockIcon.Value;
				else
					return ProjectExtension.StockIcon;
			}
			set { this.stockIcon = value; NotifyModified ("StockIcon"); }
		}
		IconId? stockIcon;
		
		/// <summary>
		/// List of languages that this project supports
		/// </summary>
		/// <value>
		/// The identifiers of the supported languages.
		/// </value>
		public virtual string[] SupportedLanguages {
			get { return ProjectExtension.SupportedLanguages; }
		}

		/// <summary>
		/// Gets the default build action for a file
		/// </summary>
		/// <returns>
		/// The default build action.
		/// </returns>
		/// <param name='fileName'>
		/// File name.
		/// </param>
		public virtual string GetDefaultBuildAction (string fileName)
		{
			return ProjectExtension.OnGetDefaultBuildAction (fileName);
		}

		public string GetDefaultResourceId (ProjectFile projectFile)
		{
			return ProjectExtension.OnGetDefaultResourceId (projectFile);
		}

		protected virtual string OnGetDefaultResourceId (ProjectFile projectFile)
		{
			return MSBuildResourceHandler.Instance.GetDefaultResourceId (projectFile);
		}

		internal ProjectItem CreateProjectItem (IMSBuildItemEvaluated item)
		{
			return ProjectExtension.OnCreateProjectItem (item);
		}

		protected virtual ProjectItem OnCreateProjectItem (IMSBuildItemEvaluated item)
		{
			if (item.Name == "Folder")
				return new ProjectFile ();

			// Unknown item. Must be a file.
			if (!string.IsNullOrEmpty (item.Include) && !UnsupportedItems.Contains (item.Name) && IsValidFile (item.Include))
				return new ProjectFile ();

			return new UnknownProjectItem (item.Name, item.Include);
		}

		bool IsValidFile (string path)
		{
			// If it is an absolute uri, it's not a valid file
			try {
				if (Uri.IsWellFormedUriString (path, UriKind.Absolute)) {
					var f = new Uri (path);
					return f.Scheme == "file";
				}
			} catch {
				// Old mono versions may crash in IsWellFormedUriString if the path
				// is not an uri.
			}
			return true;
		}

		// Items generated by VS but which MD is not using and should be ignored

		internal static readonly IList<string> UnsupportedItems = new string[] {
			"BootstrapperFile", "AppDesigner", "WebReferences", "WebReferenceUrl", "Service",
			"ProjectReference", "Reference", // Reference elements are included here because they are special-cased for DotNetProject, and they are unsupported in other types of projects
			"InternalsVisibleTo",
			"InternalsVisibleToTest"
		};

		/// <summary>
		/// Gets a project file.
		/// </summary>
		/// <returns>
		/// The project file.
		/// </returns>
		/// <param name='fileName'>
		/// File name.
		/// </param>
		public ProjectFile GetProjectFile (string fileName)
		{
			return files.GetFile (fileName);
		}
		
		/// <summary>
		/// Determines whether a file belongs to this project
		/// </summary>
		/// <param name='fileName'>
		/// File name
		/// </param>
		public bool IsFileInProject (string fileName)
		{
			return files.GetFile (fileName) != null;
		}

		/// <summary>
		/// Gets a list of build actions supported by this project
		/// </summary>
		/// <remarks>
		/// Common actions are grouped at the top, separated by a "--" entry *IF* there are 
		/// more "uncommon" actions than "common" actions
		/// </remarks>
		public string[] GetBuildActions ()
		{
			if (buildActions != null)
				return buildActions;

			// find all the actions in use and add them to the list of standard actions
			Hashtable actions = new Hashtable ();
			object marker = new object (); //avoid using bools as they need to be boxed. re-use single object instead
			//ad the standard actions
			foreach (string action in GetStandardBuildActions ())
				actions[action] = marker;

			//add any more actions that are in the project file
			foreach (ProjectFile pf in files)
				if (!actions.ContainsKey (pf.BuildAction))
					actions[pf.BuildAction] = marker;

			//remove the "common" actions, since they're handled separately
			IList<string> commonActions = GetCommonBuildActions ();
			foreach (string action in commonActions)
				if (actions.Contains (action))
					actions.Remove (action);

			//calculate dimensions for our new array and create it
			int dashPos = commonActions.Count;
			bool hasDash = commonActions.Count > 0 && actions.Count > 0;
			int arrayLen = commonActions.Count + actions.Count;
			int uncommonStart = hasDash ? dashPos + 1 : dashPos;
			if (hasDash)
				arrayLen++;
			buildActions = new string[arrayLen];

			//populate it
			if (commonActions.Count > 0)
				commonActions.CopyTo (buildActions, 0);
			if (hasDash)
				buildActions[dashPos] = "--";
			if (actions.Count > 0)
				actions.Keys.CopyTo (buildActions, uncommonStart);

			//sort the actions
			if (hasDash) {
				//it may be better to leave common actions in the order that the project specified
				//Array.Sort (buildActions, 0, commonActions.Count, StringComparer.Ordinal);
				Array.Sort (buildActions, uncommonStart, arrayLen - uncommonStart, StringComparer.Ordinal);
			} else {
				Array.Sort (buildActions, StringComparer.Ordinal);
			}
			return buildActions;
		}
		
		/// <summary>
		/// Gets a list of standard build actions.
		/// </summary>
		protected virtual IEnumerable<string> GetStandardBuildActions ()
		{
			return ProjectExtension.OnGetStandardBuildActions ();
		}

		/// <summary>
		/// Gets a list of common build actions (common actions are shown first in the project build action list)
		/// </summary>
		protected virtual IList<string> GetCommonBuildActions ()
		{
			return ProjectExtension.OnGetCommonBuildActions ();
		}

		public override void Dispose ()
		{
			FileService.FileChanged -= OnFileChanged;
			Runtime.SystemAssemblyService.DefaultRuntimeChanged -= OnDefaultRuntimeChanged;
			CleanupProjectBuilder ();
			base.Dispose ();
		}

		/// <summary>
		/// Runs a build or execution target.
		/// </summary>
		/// <returns>
		/// The result of the operation
		/// </returns>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='target'>
		/// Name of the target
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to run the target
		/// </param>
		public Task<BuildResult> RunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			return ProjectExtension.OnRunTarget (monitor, target, configuration);
		}

		public bool SupportsTarget (string target)
		{
			return !IsUnsupportedProject && ProjectExtension.OnGetSupportsTarget (target);
		}

		protected virtual bool OnGetSupportsTarget (string target)
		{
			return target == "Build" || target == "Clean";
		}

		/// <summary>
		/// Runs a build or execution target.
		/// </summary>
		/// <returns>
		/// The result of the operation
		/// </returns>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='target'>
		/// Name of the target
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to run the target
		/// </param>
		/// <remarks>
		/// Subclasses can override this method to provide a custom implementation of project operations such as
		/// build or clean. The default implementation delegates the execution to the more specific OnBuild
		/// and OnClean methods, or to the item handler for other targets.
		/// </remarks>
		internal async protected virtual Task<BuildResult> OnRunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			if (target == ProjectService.BuildTarget)
				return await RunBuildTarget (monitor, configuration);
			else if (target == ProjectService.CleanTarget)
				return await RunCleanTarget (monitor, configuration);
			return await RunMSBuildTarget (monitor, target, configuration) ?? new BuildResult ();
		}


		async Task<BuildResult> DoRunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			if (target == ProjectService.BuildTarget) {
				SolutionItemConfiguration conf = GetConfiguration (configuration);
				if (conf != null && conf.CustomCommands.HasCommands (CustomCommandType.Build)) {
					if (!await conf.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.Build, configuration)) {
						var r = new BuildResult ();
						r.AddError (GettextCatalog.GetString ("Custom command execution failed"));
						return r;
					}
					return BuildResult.Success;
				}
			} else if (target == ProjectService.CleanTarget) {
				SolutionItemConfiguration config = GetConfiguration (configuration);
				if (config != null && config.CustomCommands.HasCommands (CustomCommandType.Clean)) {
					if (!await config.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.Clean, configuration)) {
						var r = new BuildResult ();
						r.AddError (GettextCatalog.GetString ("Custom command execution failed"));
						return r;
					}
					return BuildResult.Success;
				}
			}
			return await OnRunTarget (monitor, target, configuration);
		}

		async Task<BuildResult> RunMSBuildTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			if (CheckUseMSBuildEngine (configuration)) {
				LogWriter logWriter = new LogWriter (monitor.Log);
				RemoteProjectBuilder builder = GetProjectBuilder ();
				var configs = GetConfigurations (configuration);

				MSBuildResult result = null;
				await Task.Factory.StartNew (delegate {
					result = builder.Run (configs, logWriter, MSBuildProjectService.DefaultMSBuildVerbosity, new[] { target }, null, null);
					System.Runtime.Remoting.RemotingServices.Disconnect (logWriter);
				});

				var br = new BuildResult ();
				foreach (var err in result.Errors) {
					FilePath file = null;
					if (err.File != null)
						file = Path.Combine (Path.GetDirectoryName (err.ProjectFile), err.File);

					if (err.IsWarning)
						br.AddWarning (file, err.LineNumber, err.ColumnNumber, err.Code, err.Message);
					else
						br.AddError (file, err.LineNumber, err.ColumnNumber, err.Code, err.Message);
				}
				return br;
			}
			else {
				CleanupProjectBuilder ();
				if (this is DotNetProject) {
					var handler = new MonoDevelop.Projects.Formats.MD1.MD1DotNetProjectHandler ((DotNetProject)this);
					return await handler.RunTarget (monitor, target, configuration);
				}
			}
			return null;
		}

		internal ProjectConfigurationInfo[] GetConfigurations (ConfigurationSelector configuration)
		{
			// Returns a list of project/configuration information for the provided item and all its references
			List<ProjectConfigurationInfo> configs = new List<ProjectConfigurationInfo> ();
			var c = GetConfiguration (configuration);
			configs.Add (new ProjectConfigurationInfo () {
				ProjectFile = FileName,
				Configuration = c.Name,
				Platform = GetExplicitPlatform (c)
			});
			foreach (var refProject in GetReferencedItems (configuration).OfType<Project> ()) {
				var refConfig = refProject.GetConfiguration (configuration);
				if (refConfig != null) {
					configs.Add (new ProjectConfigurationInfo () {
						ProjectFile = refProject.FileName,
						Configuration = refConfig.Name,
						Platform = GetExplicitPlatform (refConfig)
					});
				}
			}
			return configs.ToArray ();
		}

		//for some reason, MD internally handles "AnyCPU" as "", but we need to be explicit when
		//passing it to the build engine
		static string GetExplicitPlatform (SolutionItemConfiguration configObject)
		{
			if (string.IsNullOrEmpty (configObject.Platform)) {
				return "AnyCPU";
			}
			return configObject.Platform;
		}

		#region Project builder management

		RemoteProjectBuilder projectBuilder;
		string lastBuildToolsVersion;
		string lastBuildRuntime;
		string lastFileName;
		string lastSlnFileName;
		object builderLock = new object ();

		internal RemoteProjectBuilder GetProjectBuilder ()
		{
			//FIXME: we can't really have per-project runtimes, has to be per-solution
			TargetRuntime runtime = null;
			var ap = this as IAssemblyProject;
			runtime = ap != null ? ap.TargetRuntime : Runtime.SystemAssemblyService.CurrentRuntime;

			var sln = ParentSolution;
			var slnFile = sln != null ? sln.FileName : null;

			lock (builderLock) {
				if (projectBuilder == null || lastBuildToolsVersion != ToolsVersion || lastBuildRuntime != runtime.Id || lastFileName != FileName || lastSlnFileName != slnFile) {
					if (projectBuilder != null) {
						projectBuilder.Dispose ();
						projectBuilder = null;
					}
					projectBuilder = MSBuildProjectService.GetProjectBuilder (runtime, ToolsVersion, FileName, slnFile);
					projectBuilder.Disconnected += delegate {
						CleanupProjectBuilder ();
					};
					lastBuildToolsVersion = ToolsVersion;
					lastBuildRuntime = runtime.Id;
					lastFileName = FileName;
					lastSlnFileName = slnFile;
				} else if (modifiedInMemory) {
					modifiedInMemory = false;
					// TODO NPM
//				var p = SaveProject (new NullProgressMonitor ());
//				projectBuilder.RefreshWithContent (p.SaveToString ());
				}
			}
			return projectBuilder;
		}

		void CleanupProjectBuilder ()
		{
			if (projectBuilder != null) {
				projectBuilder.Dispose ();
				projectBuilder = null;
			}
		}

		public void RefreshProjectBuilder ()
		{
			if (projectBuilder != null)
				projectBuilder.Refresh ();
		}

		#endregion

		/// <summary>Whether to use the MSBuild engine for the specified item.</summary>
		internal bool CheckUseMSBuildEngine (ConfigurationSelector sel, bool checkReferences = true)
		{
			// if the item mandates MSBuild, always use it
			if (RequireMSBuildEngine)
				return true;
			// if the user has set the option, use the setting
			if (UseMSBuildEngine.HasValue)
				return UseMSBuildEngine.Value;

			// If the item type defaults to using MSBuild, only use MSBuild if its direct references also use MSBuild.
			// This prevents a not-uncommon common error referencing non-MSBuild projects from MSBuild projects
			// NOTE: This adds about 11ms to the load/build/etc times of the MonoDevelop solution. Doing it recursively
			// adds well over a second.
			return UseMSBuildEngineByDefault && (
				!checkReferences || GetReferencedItems (sel).OfType<Project>().All (i => i.CheckUseMSBuildEngine (sel, false))
			);
		}

		/// <summary>
		/// Adds a file to the project
		/// </summary>
		/// <returns>
		/// The file instance.
		/// </returns>
		/// <param name='filename'>
		/// Absolute path to the file.
		/// </param>
		public ProjectFile AddFile (string filename)
		{
			return AddFile (filename, null);
		}
		
		public IEnumerable<ProjectFile> AddFiles (IEnumerable<FilePath> files)
		{
			return AddFiles (files, null);
		}
		
		/// <summary>
		/// Adds a file to the project
		/// </summary>
		/// <returns>
		/// The file instance.
		/// </returns>
		/// <param name='filename'>
		/// Absolute path to the file.
		/// </param>
		/// <param name='buildAction'>
		/// Build action to assign to the file.
		/// </param>
		public ProjectFile AddFile (string filename, string buildAction)
		{
			foreach (ProjectFile fInfo in Files) {
				if (fInfo.Name == filename) {
					return fInfo;
				}
			}

			if (String.IsNullOrEmpty (buildAction)) {
				buildAction = GetDefaultBuildAction (filename);
			}

			ProjectFile newFileInformation = new ProjectFile (filename, buildAction);
			Files.Add (newFileInformation);
			return newFileInformation;
		}
		
		public IEnumerable<ProjectFile> AddFiles (IEnumerable<FilePath> files, string buildAction)
		{
			List<ProjectFile> newFiles = new List<ProjectFile> ();
			foreach (FilePath filename in files) {
				string ba = buildAction;
				if (String.IsNullOrEmpty (ba))
					ba = GetDefaultBuildAction (filename);

				ProjectFile newFileInformation = new ProjectFile (filename, ba);
				newFiles.Add (newFileInformation);
			}
			Files.AddRange (newFiles);
			return newFiles;
		}
		
		/// <summary>
		/// Adds a file to the project
		/// </summary>
		/// <param name='projectFile'>
		/// The file.
		/// </param>
		public void AddFile (ProjectFile projectFile)
		{
			Files.Add (projectFile);
		}
		
		/// <summary>
		/// Adds a directory to the project.
		/// </summary>
		/// <returns>
		/// The directory instance.
		/// </returns>
		/// <param name='relativePath'>
		/// Relative path of the directory.
		/// </param>
		/// <remarks>
		/// The directory is created if it doesn't exist
		/// </remarks>
		public ProjectFile AddDirectory (string relativePath)
		{
			string newPath = Path.Combine (BaseDirectory, relativePath);

			foreach (ProjectFile fInfo in Files)
				if (fInfo.Name == newPath && fInfo.Subtype == Subtype.Directory)
					return fInfo;

			if (!Directory.Exists (newPath)) {
				if (File.Exists (newPath)) {
					string message = GettextCatalog.GetString ("Cannot create directory {0}, as a file with that name exists.", newPath);
					throw new InvalidOperationException (message);
				}
				FileService.CreateDirectory (newPath);
			}

			ProjectFile newDir = new ProjectFile (newPath);
			newDir.Subtype = Subtype.Directory;
			AddFile (newDir);
			return newDir;
		}
		
		//HACK: the build code is structured such that support file copying is in here instead of the item handler
		//so in order to avoid doing them twice when using the msbuild engine, we special-case them
		bool UsingMSBuildEngine (ConfigurationSelector sel)
		{
			return CheckUseMSBuildEngine (sel);
		}

		protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return RunTarget (monitor, "Build", configuration);
		}

		async Task<BuildResult> RunBuildTarget (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			// create output directory, if not exists
			ProjectConfiguration conf = GetConfiguration (configuration) as ProjectConfiguration;
			if (conf == null) {
				BuildResult cres = new BuildResult ();
				cres.AddError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", configuration.ToString (), Name));
				return cres;
			}
			
			StringParserService.Properties["Project"] = Name;
			
			if (UsingMSBuildEngine (configuration)) {
				return await DoBuild (monitor, configuration);
			}
			
			string outputDir = conf.OutputDirectory;
			try {
				DirectoryInfo directoryInfo = new DirectoryInfo (outputDir);
				if (!directoryInfo.Exists) {
					directoryInfo.Create ();
				}
			} catch (Exception e) {
				throw new ApplicationException ("Can't create project output directory " + outputDir + " original exception:\n" + e.ToString ());
			}

			//copy references and files marked to "CopyToOutputDirectory"
			CopySupportFiles (monitor, configuration);
		
			monitor.Log.WriteLine ("Performing main compilation...");
			
			BuildResult res = await DoBuild (monitor, configuration);

			if (res != null) {
				string errorString = GettextCatalog.GetPluralString ("{0} error", "{0} errors", res.ErrorCount, res.ErrorCount);
				string warningString = GettextCatalog.GetPluralString ("{0} warning", "{0} warnings", res.WarningCount, res.WarningCount);

				monitor.Log.WriteLine (GettextCatalog.GetString ("Build complete -- ") + errorString + ", " + warningString);
			}

			return res;
		}
		
		/// <summary>
		/// Copies the support files to the output directory
		/// </summary>
		/// <param name='monitor'>
		/// Progress monitor.
		/// </param>
		/// <param name='configuration'>
		/// Configuration for which to copy the files.
		/// </param>
		/// <remarks>
		/// Copies all support files to the output directory of the given configuration. Support files
		/// include: assembly references with the Local Copy flag, data files with the Copy to Output option, etc.
		/// </remarks>
		public void CopySupportFiles (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			ProjectConfiguration config = (ProjectConfiguration) GetConfiguration (configuration);

			foreach (FileCopySet.Item item in GetSupportFileList (configuration)) {
				FilePath dest = Path.GetFullPath (Path.Combine (config.OutputDirectory, item.Target));
				FilePath src = Path.GetFullPath (item.Src);

				try {
					if (dest == src)
						continue;

					if (item.CopyOnlyIfNewer && File.Exists (dest) && (File.GetLastWriteTimeUtc (dest) >= File.GetLastWriteTimeUtc (src)))
						continue;

					// Use Directory.Create so we don't trigger the VersionControl addin and try to
					// add the directory to version control.
					if (!Directory.Exists (Path.GetDirectoryName (dest)))
						Directory.CreateDirectory (Path.GetDirectoryName (dest));

					if (File.Exists (src)) {
						dest.Delete ();
						FileService.CopyFile (src, dest);
						
						// Copied files can't be read-only, so they can be removed when rebuilding the project
						FileAttributes atts = File.GetAttributes (dest);
						if (atts.HasFlag (FileAttributes.ReadOnly))
							File.SetAttributes (dest, atts & ~FileAttributes.ReadOnly);
					}
					else
						monitor.ReportError (GettextCatalog.GetString ("Could not find support file '{0}'.", src), null);

				} catch (IOException ex) {
					monitor.ReportError (GettextCatalog.GetString ("Error copying support file '{0}'.", dest), ex);
				}
			}
		}

		/// <summary>
		/// Removes all support files from the output directory
		/// </summary>
		/// <param name='monitor'>
		/// Progress monitor.
		/// </param>
		/// <param name='configuration'>
		/// Configuration for which to delete the files.
		/// </param>
		/// <remarks>
		/// Deletes all support files from the output directory of the given configuration. Support files
		/// include: assembly references with the Local Copy flag, data files with the Copy to Output option, etc.
		/// </remarks>
		public async Task DeleteSupportFiles (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			ProjectConfiguration config = (ProjectConfiguration) GetConfiguration (configuration);

			foreach (FileCopySet.Item item in GetSupportFileList (configuration)) {
				FilePath dest = Path.Combine (config.OutputDirectory, item.Target);

				// Ignore files which were not copied
				if (Path.GetFullPath (dest) == Path.GetFullPath (item.Src))
					continue;

				try {
					await dest.DeleteAsync ();
				} catch (IOException ex) {
					monitor.ReportError (GettextCatalog.GetString ("Error deleting support file '{0}'.", dest), ex);
				}
			}
		}
		
		/// <summary>
		/// Gets a list of files required to use the project output
		/// </summary>
		/// <returns>
		/// A list of files.
		/// </returns>
		/// <param name='configuration'>
		/// Build configuration for which get the list
		/// </param>
		/// <remarks>
		/// Returns a list of all files that are required to use the project output binary, for example: data files with
		/// the Copy to Output option, debug information files, generated resource files, etc.
		/// </remarks>
		public FileCopySet GetSupportFileList (ConfigurationSelector configuration)
		{
			var list = new FileCopySet ();
			PopulateSupportFileList (list, configuration);
			return list;
		}

		/// <summary>
		/// Gets a list of files required to use the project output
		/// </summary>
		/// <param name='list'>
		/// List where to add the support files.
		/// </param>
		/// <param name='configuration'>
		/// Build configuration for which get the list
		/// </param>
		/// <remarks>
		/// Returns a list of all files that are required to use the project output binary, for example: data files with
		/// the Copy to Output option, debug information files, generated resource files, etc.
		/// </remarks>
		internal protected virtual void PopulateSupportFileList (FileCopySet list, ConfigurationSelector configuration)
		{
			ProjectExtension.OnPopulateSupportFileList (list, configuration);
		}
		void DoPopulateSupportFileList (FileCopySet list, ConfigurationSelector configuration)
		{
			foreach (ProjectFile pf in Files) {
				if (pf.CopyToOutputDirectory == FileCopyMode.None)
					continue;
				list.Add (pf.FilePath, pf.CopyToOutputDirectory == FileCopyMode.PreserveNewest, pf.ProjectVirtualPath);
			}
		}

		/// <summary>
		/// Gets a list of files generated when building this project
		/// </summary>
		/// <returns>
		/// A list of files.
		/// </returns>
		/// <param name='configuration'>
		/// Build configuration for which get the list
		/// </param>
		/// <remarks>
		/// Returns a list of all files that are generated when this project is built, including: the generated binary,
		/// debug information files, satellite assemblies.
		/// </remarks>
		public List<FilePath> GetOutputFiles (ConfigurationSelector configuration)
		{
			List<FilePath> list = new List<FilePath> ();
			PopulateOutputFileList (list, configuration);
			return list;
		}

		/// <summary>
		/// Gets a list of files retuired to use the project output
		/// </summary>
		/// <param name='list'>
		/// List where to add the support files.
		/// </param>
		/// <param name='configuration'>
		/// Build configuration for which get the list
		/// </param>
		/// <remarks>
		/// Returns a list of all files that are required to use the project output binary, for example: data files with
		/// the Copy to Output option, debug information files, generated resource files, etc.
		/// </remarks>
		internal protected virtual void PopulateOutputFileList (List<FilePath> list, ConfigurationSelector configuration)
		{
			ProjectExtension.OnPopulateOutputFileList (list, configuration);
		}		
		void DoPopulateOutputFileList (List<FilePath> list, ConfigurationSelector configuration)
		{
			string file = GetOutputFileName (configuration);
			if (file != null)
				list.Add (file);
		}		

		/// <summary>
		/// Builds the project.
		/// </summary>
		/// <returns>
		/// The build result.
		/// </returns>
		/// <param name='monitor'>
		/// Progress monitor.
		/// </param>
		/// <param name='configuration'>
		/// Configuration to build.
		/// </param>
		/// <remarks>
		/// This method is invoked to build the project. Support files such as files with the Copy to Output flag will
		/// be copied before calling this method.
		/// </remarks>
		protected async virtual Task<BuildResult> DoBuild (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			BuildResult res = await RunMSBuildTarget (monitor, "Build", configuration);
			return res ?? new BuildResult ();
		}

		protected override Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return RunTarget (monitor, "Clean", configuration);
		}

		async Task<BuildResult> RunCleanTarget (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			ProjectConfiguration config = GetConfiguration (configuration) as ProjectConfiguration;
			if (config == null) {
				monitor.ReportError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", configuration, Name), null);
				return BuildResult.Success;
			}
			
			if (UsingMSBuildEngine (configuration)) {
				return await DoClean (monitor, config.Selector);
			}
			
			monitor.Log.WriteLine ("Removing output files...");

			var filesToDelete = GetOutputFiles (configuration).ToArray ();

			await Task.Factory.StartNew (delegate {
				// Delete generated files
				foreach (FilePath file in filesToDelete) {
					if (File.Exists (file)) {
						file.Delete ();
						if (file.ParentDirectory.CanonicalPath != config.OutputDirectory.CanonicalPath && Directory.GetFiles (file.ParentDirectory).Length == 0)
							file.ParentDirectory.Delete ();
					}
				}
			});
	
			await DeleteSupportFiles (monitor, configuration);
			
			var res = await DoClean (monitor, config.Selector);
			monitor.Log.WriteLine (GettextCatalog.GetString ("Clean complete"));
			return res;
		}

		protected virtual Task<BuildResult> DoClean (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return RunMSBuildTarget (monitor, "Clean", configuration);
		}

		protected async override Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			ProjectConfiguration config = GetConfiguration (configuration) as ProjectConfiguration;
			if (config == null) {
				monitor.ReportError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", configuration, Name), null);
				return;
			}
			await DoExecute (monitor, context, configuration);
		}
		
		/// <summary>
		/// Executes the project
		/// </summary>
		/// <param name='monitor'>
		/// Progress monitor.
		/// </param>
		/// <param name='context'>
		/// Execution context.
		/// </param>
		/// <param name='configuration'>
		/// Configuration to execute.
		/// </param>
		protected virtual Task DoExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return new Task (delegate {});
		}

		/// <summary>
		/// Gets the absolute path to the output file generated by this project.
		/// </summary>
		/// <returns>
		/// Absolute path the the output file.
		/// </returns>
		/// <param name='configuration'>
		/// Build configuration.
		/// </param>
		public virtual FilePath GetOutputFileName (ConfigurationSelector configuration)
		{
			return ProjectExtension.OnGetOutputFileName (configuration);
		}

		internal protected override bool OnGetNeedsBuilding (ConfigurationSelector configuration)
		{
			return CheckNeedsBuild (configuration);
		}

		protected override void OnSetNeedsBuilding (ConfigurationSelector configuration)
		{
			var of = GetOutputFileName (configuration);
			if (File.Exists (of))
				File.Delete (of);
		}

		/// <summary>
		/// Checks if the project needs to be built
		/// </summary>
		/// <returns>
		/// <c>True</c> if the project needs to be built (it has changes)
		/// </returns>
		/// <param name='configuration'>
		/// Build configuration.
		/// </param>
		protected virtual bool CheckNeedsBuild (ConfigurationSelector configuration)
		{
			DateTime tim = GetLastBuildTime (configuration);
			if (tim == DateTime.MinValue)
				return true;

			foreach (ProjectFile file in Files) {
				if (file.BuildAction == BuildAction.Content || file.BuildAction == BuildAction.None)
					continue;
				try {
					if (File.GetLastWriteTime (file.FilePath) > tim)
						return true;
				} catch (IOException) {
					// Ignore.
				}
			}

			foreach (SolutionFolderItem pref in GetReferencedItems (configuration)) {
				if (pref.GetLastBuildTime (configuration) > tim)
					return true;
			}

			try {
				if (File.GetLastWriteTime (FileName) > tim)
					return true;
			} catch {
				// Ignore
			}

			return false;
		}

		protected internal override DateTime OnGetLastBuildTime (ConfigurationSelector configuration)
		{
			string file = GetOutputFileName (configuration);
			if (file == null)
				return DateTime.MinValue;

			FileInfo finfo = new FileInfo (file);
			if (!finfo.Exists)
				return DateTime.MinValue;
			else
				return finfo.LastWriteTime;
		}

		internal virtual void OnFileChanged (object source, FileEventArgs e)
		{
			foreach (FileEventInfo fi in e) {
				ProjectFile file = GetProjectFile (fi.FileName);
				if (file != null) {
					try {
						NotifyFileChangedInProject (file);
					} catch {
						// Workaround Mono bug. The watcher seems to
						// stop watching if an exception is thrown in
						// the event handler
					}
				}
			}
		}

		protected override IEnumerable<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			var baseFiles = base.OnGetItemFiles (includeReferencedFiles);

			if (includeReferencedFiles) {
				List<FilePath> col = new List<FilePath> ();
				foreach (ProjectFile pf in Files) {
					if (pf.Subtype != Subtype.Directory)
						col.Add (pf.FilePath);
				}
				baseFiles = baseFiles.Concat (col);
			}
			return baseFiles;
		}

		protected internal override void OnItemsAdded (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsAdded (objs);
			NotifyFileAddedToProject (objs.OfType<ProjectFile> ());
		}

		protected internal override void OnItemsRemoved (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsRemoved (objs);
			NotifyFileRemovedFromProject (objs.OfType<ProjectFile> ());
		}

		internal void NotifyFileChangedInProject (ProjectFile file)
		{
			OnFileChangedInProject (new ProjectFileEventArgs (this, file));
		}

		internal void NotifyFilePropertyChangedInProject (ProjectFile file, string property)
		{
			NotifyModified ("Files");
			OnFilePropertyChangedInProject (new ProjectFileEventArgs (this, file, property));
		}

		// A collection of files that depend on other files for which the dependencies
		// have not yet been resolved.
		UnresolvedFileCollection unresolvedDeps;

		void NotifyFileRemovedFromProject (IEnumerable<ProjectFile> objs)
		{
			if (!objs.Any ())
				return;
			
			var args = new ProjectFileEventArgs ();
			
			foreach (ProjectFile file in objs) {
				file.SetProject (null);
				args.Add (new ProjectFileEventInfo (this, file));
				if (DependencyResolutionEnabled) {
					unresolvedDeps.Remove (file);
					foreach (ProjectFile f in file.DependentChildren) {
						f.DependsOnFile = null;
						if (!string.IsNullOrEmpty (f.DependsOn))
							unresolvedDeps.Add (f);
					}
					file.DependsOnFile = null;
				}
			}
			NotifyModified ("Files");
			OnFileRemovedFromProject (args);
		}

		void NotifyFileAddedToProject (IEnumerable<ProjectFile> objs)
		{
			if (!objs.Any ())
				return;
			
			var args = new ProjectFileEventArgs ();
			
			foreach (ProjectFile file in objs) {
				if (file.Project != null)
					throw new InvalidOperationException ("ProjectFile already belongs to a project");
				file.SetProject (this);
				args.Add (new ProjectFileEventInfo (this, file));
				ResolveDependencies (file);
			}

			NotifyModified ("Files");
			OnFileAddedToProject (args);
		}

		internal void UpdateDependency (ProjectFile file, FilePath oldPath)
		{
			unresolvedDeps.Remove (file, oldPath);
			ResolveDependencies (file);
		}

		internal void ResolveDependencies (ProjectFile file)
		{
			if (!DependencyResolutionEnabled)
				return;

			if (!file.ResolveParent ())
				unresolvedDeps.Add (file);

			List<ProjectFile> resolved = null;
			foreach (ProjectFile unres in unresolvedDeps.GetUnresolvedFilesForPath (file.FilePath)) {
				if (string.IsNullOrEmpty (unres.DependsOn)) {
					if (resolved == null)
						resolved = new List<ProjectFile> ();
					resolved.Add (unres);
				}
				if (unres.ResolveParent (file)) {
					if (resolved == null)
						resolved = new List<ProjectFile> ();
					resolved.Add (unres);
				}
			}
			if (resolved != null)
				foreach (ProjectFile pf in resolved)
					unresolvedDeps.Remove (pf);
		}

		bool DependencyResolutionEnabled {

			get { return unresolvedDeps != null; }
			set {
				if (value) {
					if (unresolvedDeps != null)
						return;
					unresolvedDeps = new UnresolvedFileCollection ();
					foreach (ProjectFile file in files)
						ResolveDependencies (file);
				} else {
					unresolvedDeps = null;
				}
			}
		}

		internal void ReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			OnReadProjectHeader (monitor, msproject);
			ProjectExtension.OnReadProject (monitor, msproject);
			NeedsReload = false;
		}

		bool newProject;

		internal MSBuildProject WriteProject (ProgressMonitor monitor)
		{
			MSBuildProject msproject = new MSBuildProject ();
			newProject = FileName == null || !File.Exists (FileName);
			if (newProject) {
				if (SupportsBuild ())
					msproject.DefaultTargets = "Build";
				msproject.FileName = FileName;
			} else {
				msproject.Load (FileName);
			}

			OnWriteProjectHeader (monitor, msproject);
			ProjectExtension.OnWriteProject (monitor, msproject);

			return msproject;
		}

		class ConfigData
		{
			public ConfigData (string conf, string plt, IMSBuildPropertySet grp)
			{
				Config = conf;
				Platform = plt;
				Group = grp;
			}

			public bool FullySpecified {
				get { return Config != Unspecified && Platform != Unspecified; }
			}

			public string Config;
			public string Platform;
			public IMSBuildPropertySet Group;
			public bool Exists;
			public bool IsNew; // The group did not exist in the original file
		}

		const string Unspecified = null;
		ITimeTracker timer;

		protected virtual void OnReadProjectHeader (ProgressMonitor monitor, MSBuildProject msproject)
		{
			timer = Counters.ReadMSBuildProject.BeginTiming ();

			ToolsVersion = msproject.ToolsVersion;
			if (string.IsNullOrEmpty (ToolsVersion))
				ToolsVersion = "2.0";

			IMSBuildPropertySet globalGroup = msproject.GetGlobalPropertyGroup ();
			// Avoid crash if there is not global group
			if (globalGroup == null)
				globalGroup = msproject.AddNewPropertyGroup (false);

			productVersion = globalGroup.GetValue ("ProductVersion");
			schemaVersion = globalGroup.GetValue ("SchemaVersion");

			// Get the project ID

			string itemGuid = globalGroup.GetValue ("ProjectGuid");
			if (itemGuid == null)
				throw new UserException ("Project file doesn't have a valid ProjectGuid");

			// Workaround for a VS issue. VS doesn't include the curly braces in the ProjectGuid
			// of shared projects.
			if (!itemGuid.StartsWith ("{", StringComparison.Ordinal))
				itemGuid = "{" + itemGuid + "}";

			ItemId = itemGuid.ToUpper ();

			// Get the project GUIDs

			string projectTypeGuids = globalGroup.GetValue ("ProjectTypeGuids");

			var subtypeGuids = new List<string> ();
			if (projectTypeGuids != null) {
				foreach (string guid in projectTypeGuids.Split (';')) {
					string sguid = guid.Trim ();
					if (sguid.Length > 0 && string.Compare (sguid, TypeGuid, StringComparison.OrdinalIgnoreCase) != 0)
						subtypeGuids.Add (guid);
				}
			}
			flavorGuids = subtypeGuids.ToArray ();

			if (!CheckAllFlavorsSupported ()) {
				var guids = new [] { TypeGuid };
				var projectInfo = MSBuildProjectService.GetUnknownProjectTypeInfo (guids.Concat (flavorGuids).ToArray (), FileName);
				IsUnsupportedProject = true;
				if (projectInfo != null)
					UnsupportedProjectMessage = projectInfo.GetInstructions ();
			}

			// Common properties

			Description = msproject.EvaluatedProperties.GetValue ("Description", "");
			baseIntermediateOutputPath = msproject.EvaluatedProperties.GetPathValue ("BaseIntermediateOutputPath", defaultValue:BaseDirectory.Combine ("obj"), relativeToProject:true);

			RemoveDuplicateItems (msproject);
		}

		protected virtual void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			timer.Trace ("Read project items");
			LoadProjectItems (msproject, ProjectItemFlags.None);

			timer.Trace ("Read configurations");

			List<ConfigData> configData = GetConfigData (msproject, false);
			List<ConfigData> partialConfigurations = new List<ConfigData> ();
			HashSet<string> handledConfigurations = new HashSet<string> ();
			var configurations = new HashSet<string> ();
			var platforms = new HashSet<string> ();

			IMSBuildPropertySet globalGroup = msproject.GetGlobalPropertyGroup ();
			configData.Insert (0, new ConfigData (Unspecified, Unspecified, globalGroup));

			// Load configurations, skipping the dummy config at index 0.
			for (int i = 1; i < configData.Count; i++) {
				ConfigData cgrp = configData[i];
				string platform = cgrp.Platform;
				string conf = cgrp.Config;

				if (platform != Unspecified)
					platforms.Add (platform);

				if (conf != Unspecified)
					configurations.Add (conf);

				if (conf == Unspecified || platform == Unspecified) {
					// skip partial configurations for now...
					partialConfigurations.Add (cgrp);
					continue;
				}

				string key = conf + "|" + platform;
				if (handledConfigurations.Contains (key))
					continue;

				LoadConfiguration (monitor, configData, conf, platform);

				handledConfigurations.Add (key);
			}

			// Now we can load any partial configurations by combining them with known configs or platforms.
			if (partialConfigurations.Count > 0) {
				if (platforms.Count == 0)
					platforms.Add (string.Empty); // AnyCpu

				foreach (ConfigData cgrp in partialConfigurations) {
					if (cgrp.Config != Unspecified && cgrp.Platform == Unspecified) {
						string conf = cgrp.Config;

						foreach (var platform in platforms) {
							string key = conf + "|" + platform;

							if (handledConfigurations.Contains (key))
								continue;

							LoadConfiguration (monitor, configData, conf, platform);

							handledConfigurations.Add (key);
						}
					} else if (cgrp.Config == Unspecified && cgrp.Platform != Unspecified) {
						string platform = cgrp.Platform;

						foreach (var conf in configurations) {
							string key = conf + "|" + platform;

							if (handledConfigurations.Contains (key))
								continue;

							LoadConfiguration (monitor, configData, conf, platform);

							handledConfigurations.Add (key);
						}
					}
				}
			}

			// Read extended properties

			timer.Trace ("Read extended properties");
		}

		List<ConfigData> GetConfigData (MSBuildProject msproject, bool includeGlobalGroups)
		{
			List<ConfigData> configData = new List<ConfigData> ();
			foreach (MSBuildPropertyGroup cgrp in msproject.PropertyGroups) {
				string conf, platform;
				if (ParseConfigCondition (cgrp.Condition, out conf, out platform) || includeGlobalGroups)
					configData.Add (new ConfigData (conf, platform, cgrp));
			}
			return configData;
		}

		bool ParseConfigCondition (string cond, out string config, out string platform)
		{
			config = platform = Unspecified;
			int i = cond.IndexOf ("==", StringComparison.Ordinal);
			if (i == -1)
				return false;
			if (cond.Substring (0, i).Trim () == "'$(Configuration)|$(Platform)'") {
				if (!ExtractConfigName (cond.Substring (i + 2), out cond))
					return false;
				i = cond.IndexOf ('|');
				if (i != -1) {
					config = cond.Substring (0, i);
					platform = cond.Substring (i+1);
				} else {
					// Invalid configuration
					return false;
				}
				if (platform == "AnyCPU")
					platform = string.Empty;
				return true;
			}
			else if (cond.Substring (0, i).Trim () == "'$(Configuration)'") {
				if (!ExtractConfigName (cond.Substring (i + 2), out config))
					return false;
				platform = Unspecified;
				return true;
			}
			else if (cond.Substring (0, i).Trim () == "'$(Platform)'") {
				config = Unspecified;
				if (!ExtractConfigName (cond.Substring (i + 2), out platform))
					return false;
				if (platform == "AnyCPU")
					platform = string.Empty;
				return true;
			}
			return false;
		}

		bool ExtractConfigName (string name, out string config)
		{
			config = name.Trim (' ');
			if (config.Length <= 2)
				return false;
			if (config [0] != '\'' || config [config.Length - 1] != '\'')
				return false;
			config = config.Substring (1, config.Length - 2);
			return config.IndexOf ('\'') == -1;
		}

		void LoadConfiguration (ProgressMonitor monitor, List<ConfigData> configData, string conf, string platform)
		{
			IMSBuildPropertySet grp = GetMergedConfiguration (configData, conf, platform, null);
			ProjectConfiguration config = (ProjectConfiguration) CreateConfiguration (conf);

			config.Platform = platform;
			projectExtension.OnReadConfiguration (monitor, config, grp);
			Configurations.Add (config);
		}

		protected virtual void OnReadConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IMSBuildPropertySet grp)
		{
			config.Read (grp, GetToolsFormat ());
		}

		void RemoveDuplicateItems (MSBuildProject msproject)
		{
			timer.Trace ("Checking for duplicate items");

			var uniqueIncludes = new Dictionary<string,object> ();
			var toRemove = new List<MSBuildItem> ();
			foreach (MSBuildItem bi in msproject.GetAllItems ()) {
				object existing;
				string key = bi.Name + "<" + bi.Include;
				if (!uniqueIncludes.TryGetValue (key, out existing)) {
					uniqueIncludes[key] = bi;
					continue;
				}
				var exBi = existing as MSBuildItem;
				if (exBi != null) {
					if (exBi.Condition != bi.Condition || exBi.Element.InnerXml != bi.Element.InnerXml) {
						uniqueIncludes[key] = new List<MSBuildItem> { exBi, bi };
					} else {
						toRemove.Add (bi);
					}
					continue;
				}

				var exList = (List<MSBuildItem>)existing;
				bool found = false;
				foreach (var m in (exList)) {
					if (m.Condition == bi.Condition && m.Element.InnerXml == bi.Element.InnerXml) {
						found = true;
						break;
					}
				}
				if (!found) {
					exList.Add (bi);
				} else {
					toRemove.Add (bi);
				}
			}
			if (toRemove.Count == 0)
				return;

			timer.Trace ("Removing duplicate items");

			foreach (var t in toRemove)
				msproject.RemoveItem (t);
		}

		IMSBuildPropertySet GetMergedConfiguration (List<ConfigData> configData, string conf, string platform, IMSBuildPropertySet propGroupLimit)
		{
			IMSBuildPropertySet merged = null;

			foreach (ConfigData grp in configData) {
				if (grp.Group == propGroupLimit)
					break;
				if ((grp.Config == conf || grp.Config == Unspecified || conf == Unspecified) && (grp.Platform == platform || grp.Platform == Unspecified || platform == Unspecified)) {
					if (merged == null)
						merged = grp.Group;
					else if (merged is MSBuildPropertyGroupMerged)
						((MSBuildPropertyGroupMerged)merged).Add (grp.Group);
					else {
						MSBuildPropertyGroupMerged m = new MSBuildPropertyGroupMerged (merged.Project, GetToolsFormat());
						m.Add (merged);
						m.Add (grp.Group);
						merged = m;
					}
				}
			}
			return merged;
		}

		//HACK: the solution's format is irrelevant to MSBuild projects, what matters is the ToolsVersion
		// but other parts of the MD API expect a FileFormat
		internal MSBuildFileFormat GetToolsFormat ()
		{
			switch (ToolsVersion) {
			case "2.0":
				return new MSBuildFileFormatVS05 ();
			case "3.5":
				return new MSBuildFileFormatVS08 ();
			case "4.0":
				if (SolutionFormat != null && SolutionFormat.Id == "MSBuild10")
					return SolutionFormat;
				return new MSBuildFileFormatVS12 ();
			case "12.0":
				return new MSBuildFileFormatVS12 ();
			default:
				throw new Exception ("Unknown ToolsVersion '" + ToolsVersion + "'");
			}
		}

		internal void LoadProjectItems (MSBuildProject msproject, ProjectItemFlags flags)
		{
			foreach (var buildItem in msproject.GetAllItems ()) {
				ProjectItem it = ReadItem (buildItem);
				if (it == null)
					continue;
				it.Flags = flags;
				if (it is ProjectFile) {
					var file = (ProjectFile)it;
					if (file.Name.IndexOf ('*') > -1) {
						// Thanks to IsOriginatedFromWildcard, these expanded items will not be saved back to disk.
						foreach (var expandedItem in ResolveWildcardItems (file))
							Items.Add (expandedItem);
						// Add to wildcard items (so it can be re-saved) instead of Items (where tools will 
						// try to compile and display these nonstandard items
						WildcardItems.Add (it);
						continue;
					}
				}
				Items.Add (it);
			}
		}

		internal override void SetSolutionFormat (MSBuildFileFormat format, bool converting)
		{
			base.SetSolutionFormat (format, converting);

			// when converting formats, set ToolsVersion, ProductVersion, SchemaVersion to default values written by VS 
			// this happens on creation too
			// else we leave them alone and just roundtrip them
			if (converting) {
				ToolsVersion = format.DefaultToolsVersion;
				productVersion = format.DefaultProductVersion;
				schemaVersion = format.DefaultSchemaVersion;
			}
		}

		internal ProjectItem ReadItem (IMSBuildItemEvaluated buildItem)
		{
			var item = CreateProjectItem (buildItem);
			item.Read (this, buildItem);
			return item;
		}

		const string RecursiveDirectoryWildcard = "**";
		static readonly char[] directorySeparators = new [] {
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		};

		static string GetWildcardDirectoryName (string path)
		{
			int indexOfLast = path.LastIndexOfAny (directorySeparators);
			if (indexOfLast < 0)
				return String.Empty;
			return path.Substring (0, indexOfLast);
		}

		static string GetWildcardFileName (string path)
		{
			int indexOfLast = path.LastIndexOfAny (directorySeparators);
			if (indexOfLast < 0)
				return path;
			if (indexOfLast == path.Length)
				return String.Empty;
			return path.Substring (indexOfLast + 1, path.Length - (indexOfLast + 1));
		}


		static IEnumerable<string> ExpandWildcardFilePath (string filePath)
		{
			if (String.IsNullOrWhiteSpace (filePath))
				throw new ArgumentException ("Not a wildcard path");

			string dir = GetWildcardDirectoryName (filePath);
			string file = GetWildcardFileName (filePath);

			if (String.IsNullOrEmpty (dir) || String.IsNullOrEmpty (file))
				return null;

			SearchOption searchOption = SearchOption.TopDirectoryOnly;
			if (dir.EndsWith (RecursiveDirectoryWildcard, StringComparison.Ordinal)) {
				dir = dir.Substring (0, dir.Length - RecursiveDirectoryWildcard.Length);
				searchOption = SearchOption.AllDirectories;
			}

			if (!Directory.Exists (dir))
				return null;

			return Directory.GetFiles (dir, file, searchOption);
		}

		static IEnumerable<ProjectFile> ResolveWildcardItems (ProjectFile wildcardFile)
		{
			var paths = ExpandWildcardFilePath (wildcardFile.Name);
			if (paths == null)
				yield break;
			foreach (var resolvedFilePath in paths) {
				var projectFile = (ProjectFile)wildcardFile.Clone ();
				projectFile.Name = resolvedFilePath;
				projectFile.Flags |= ProjectItemFlags.DontPersist;
				yield return projectFile;
			}
		}

		struct MergedPropertyValue
		{
			public readonly string XmlValue;
			public readonly bool PreserveExistingCase;
			public readonly bool IsDefault;

			public MergedPropertyValue (string xmlValue, bool preserveExistingCase, bool isDefault)
			{
				this.XmlValue = xmlValue;
				this.PreserveExistingCase = preserveExistingCase;
				this.IsDefault = isDefault;
			}
		}

		protected virtual void OnWriteProjectHeader (ProgressMonitor monitor, MSBuildProject msproject)
		{
			IMSBuildPropertySet globalGroup = msproject.GetGlobalPropertyGroup ();
			if (globalGroup == null)
				globalGroup = msproject.AddNewPropertyGroup (false);

			if (Configurations.Count > 0) {
				ItemConfiguration conf = Configurations.FirstOrDefault<ItemConfiguration> (c => c.Name == "Debug");
				if (conf == null) conf = Configurations [0];
				globalGroup.SetValue ("Configuration", conf.Name, condition:" '$(Configuration)' == '' ");

				string platform = conf.Platform.Length == 0 ? "AnyCPU" : conf.Platform;
				globalGroup.SetValue ("Platform", platform, condition:" '$(Platform)' == '' ");
			}

			if (TypeGuid == MSBuildProjectService.GenericItemGuid) {
				DataType dt = MSBuildProjectService.DataContext.GetConfigurationDataType (GetType ());
				globalGroup.SetValue ("ItemType", dt.Name);
			}

			globalGroup.SetValue ("ProductVersion", productVersion);
			globalGroup.SetValue ("SchemaVersion", schemaVersion);

			globalGroup.SetValue ("ProjectGuid", ItemId);

			if (flavorGuids.Length > 0) {
				string gg = string.Join (";", flavorGuids);
				gg += ";" + TypeGuid;
				globalGroup.SetValue ("ProjectTypeGuids", gg.ToUpper (), preserveExistingCase:true);
			} else {
				globalGroup.RemoveProperty ("ProjectTypeGuids");
			}

			// having no ToolsVersion is equivalent to 2.0, roundtrip that correctly
			if (ToolsVersion != "2.0")
				msproject.ToolsVersion = ToolsVersion;
			else if (string.IsNullOrEmpty (msproject.ToolsVersion))
				msproject.ToolsVersion = null;
			else
				msproject.ToolsVersion = "2.0";

			msproject.GetGlobalPropertyGroup ().SetValue ("Description", Description, "");
			msproject.GetGlobalPropertyGroup ().SetValue ("BaseIntermediateOutputPath", BaseIntermediateOutputPath, defaultValue:BaseDirectory.Combine ("obj"), relativeToProject:true);
		}

		protected virtual void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			var toolsFormat = GetToolsFormat ();

			IMSBuildPropertySet globalGroup = msproject.GetGlobalPropertyGroup ();

			// Configurations

			if (Configurations.Count > 0) {
				List<ConfigData> configData = GetConfigData (msproject, true);

				// Write configuration data, creating new property groups if necessary

				foreach (ProjectConfiguration conf in Configurations) {
					ConfigData cdata = FindPropertyGroup (configData, conf);
					if (cdata == null) {
						MSBuildPropertyGroup pg = msproject.AddNewPropertyGroup (true);
						pg.IgnoreDefaultValues = true;
						pg.Condition = BuildConfigCondition (conf.Name, conf.Platform);
						cdata = new ConfigData (conf.Name, conf.Platform, pg);
						cdata.IsNew = true;
						configData.Add (cdata);
					}
					((MSBuildPropertyGroup)cdata.Group).IgnoreDefaultValues = true;
					cdata.Exists = true;
					ProjectExtension.OnWriteConfiguration (monitor, conf, cdata.Group);
				}

				// Find the properties in all configurations that have the MergeToProject flag set
				var mergeToProjectProperties = new HashSet<MergedProperty> (GetMergeToProjectProperties (configData));
				var mergeToProjectPropertyNames = new HashSet<string> (mergeToProjectProperties.Select (p => p.Name));
				var mergeToProjectPropertyValues = new Dictionary<string,MergedPropertyValue> ();

				foreach (ProjectConfiguration conf in Configurations) {
					ConfigData cdata = FindPropertyGroup (configData, conf);
					var propGroup = (MSBuildPropertyGroup) cdata.Group;

					IMSBuildPropertySet baseGroup = GetMergedConfiguration (configData, conf.Name, conf.Platform, propGroup);

					CollectMergetoprojectProperties (propGroup, mergeToProjectProperties, mergeToProjectPropertyValues);

					propGroup.UnMerge (baseGroup, mergeToProjectPropertyNames);
					propGroup.IgnoreDefaultValues = false;
				}

				// Move properties with common values from configurations to the main
				// property group
				foreach (KeyValuePair<string,MergedPropertyValue> prop in mergeToProjectPropertyValues) {
					if (!prop.Value.IsDefault)
						globalGroup.SetValue (prop.Key, prop.Value.XmlValue, preserveExistingCase: prop.Value.PreserveExistingCase);
					else
						globalGroup.RemoveProperty (prop.Key);
				}
				foreach (string prop in mergeToProjectPropertyNames) {
					if (!mergeToProjectPropertyValues.ContainsKey (prop))
						globalGroup.RemoveProperty (prop);
				}
				foreach (SolutionItemConfiguration conf in Configurations) {
					var propGroup = FindPropertyGroup (configData, conf).Group;
					foreach (string mp in mergeToProjectPropertyValues.Keys)
						propGroup.RemoveProperty (mp);
				}

				// Remove groups corresponding to configurations that have been removed
				// or groups which don't have any property and did not already exist
				foreach (ConfigData cd in configData) {
					if ((!cd.Exists && cd.FullySpecified) || (cd.IsNew && !cd.Group.Properties.Any ()))
						msproject.RemoveGroup ((MSBuildPropertyGroup)cd.Group);
				}
			}
			SaveProjectItems (monitor, toolsFormat, msproject);

			if (msproject.IsNewProject) {
				foreach (var im in DefaultImports)
					msproject.AddNewImport (im);
			}

			foreach (var im in importsAdded) {
				if (msproject.GetImport (im.Name, im.Condition) == null)
					msproject.AddNewImport (im.Name, im.Condition);
			}
			foreach (var im in importsRemoved) {
				var i = msproject.GetImport (im.Name, im.Condition);
				if (i != null)
					msproject.RemoveImport (i);
			}
		}

		void WriteConfiguration (ProgressMonitor monitor, List<ConfigData> configData, string conf, string platform)
		{
			IMSBuildPropertySet grp = GetMergedConfiguration (configData, conf, platform, null);
			ProjectConfiguration config = (ProjectConfiguration) CreateConfiguration (conf);

			config.Platform = platform;
			config.Read (grp, GetToolsFormat ());
			Configurations.Add (config);
			projectExtension.OnReadConfiguration (monitor, config, grp);
		}

		protected virtual void OnWriteConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IMSBuildPropertySet pset)
		{
			config.Write (pset, SolutionFormat);
		}

		IEnumerable<MergedProperty> GetMergeToProjectProperties (List<ConfigData> configData)
		{
			Dictionary<string,MergedProperty> mergeProps = new Dictionary<string, MergedProperty> ();
			foreach (var cd in configData.Where (d => d.FullySpecified)) {
				foreach (var prop in cd.Group.Properties) {
					if (!prop.MergeToMainGroup) {
						mergeProps [prop.Name] = null;
					} else if (!mergeProps.ContainsKey (prop.Name))
						mergeProps [prop.Name] = prop.CreateMergedProperty ();
				}
			}
			return mergeProps.Values.Where (p => p != null);
		}

		void CollectMergetoprojectProperties (IMSBuildPropertySet pgroup, HashSet<MergedProperty> properties, Dictionary<string,MergedPropertyValue> mergeToProjectProperties)
		{
			// This method checks every property in pgroup which has the MergeToProject flag.
			// If the value of this property is the same as the one stored in mergeToProjectProperties
			// it means that the property can be merged to the main project property group (so far).

			foreach (var pinfo in new List<MergedProperty> (properties)) {
				MSBuildProperty prop = pgroup.GetProperty (pinfo.Name);

				MergedPropertyValue mvalue;
				if (!mergeToProjectProperties.TryGetValue (pinfo.Name, out mvalue)) {
					if (prop != null) {
						// This is the first time the value is checked. Just assign it.
						mergeToProjectProperties.Add (pinfo.Name, new MergedPropertyValue (prop.Value, pinfo.PreserveExistingCase, pinfo.IsDefault));
						continue;
					}
					// If there is no value, it can't be merged
				}
				else if (prop != null && string.Equals (prop.Value, mvalue.XmlValue, mvalue.PreserveExistingCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
					// Same value. It can be merged.
					continue;

				// The property can't be merged because different configurations have different
				// values for it. Remove it from the list.
				properties.Remove (pinfo);
				mergeToProjectProperties.Remove (pinfo.Name);
			}
		}

		struct ItemInfo {
			public MSBuildItem Item;
			public bool Added;
		}

		internal void SaveProjectItems (ProgressMonitor monitor, MSBuildFileFormat toolsFormat, MSBuildProject msproject, string pathPrefix = null)
		{
			// Remove old items
			Dictionary<string, ItemInfo> oldItems = new Dictionary<string, ItemInfo> ();
			foreach (MSBuildItem item in msproject.GetAllItems ())
				oldItems [item.Name + "<" + item.UnevaluatedInclude + "<" + item.Condition] = new ItemInfo () {
				Item = item
			};
			// Add the new items
			foreach (ProjectItem ob in Items.Concat (WildcardItems).Where (it => !it.Flags.HasFlag (ProjectItemFlags.DontPersist)))
				SaveProjectItem (monitor, toolsFormat, msproject, ob, oldItems, pathPrefix);
			foreach (ItemInfo itemInfo in oldItems.Values) {
				if (!itemInfo.Added)
					msproject.RemoveItem (itemInfo.Item);
			}
		}

		void SaveProjectItem (ProgressMonitor monitor, MSBuildFileFormat fmt, MSBuildProject msproject, ProjectItem item, Dictionary<string,ItemInfo> oldItems, string pathPrefix = null)
		{
			var include = item.UnevaluatedInclude ?? item.Include;
			if (pathPrefix != null && !include.StartsWith (pathPrefix))
				include = pathPrefix + include;

			MSBuildItem buildItem = AddOrGetBuildItem (msproject, oldItems, item.ItemName, include, item.Condition);
			item.Write (fmt, buildItem);
			if (pathPrefix != null)
				buildItem.Include = include;
		}

		MSBuildItem AddOrGetBuildItem (MSBuildProject msproject, Dictionary<string,ItemInfo> oldItems, string name, string include, string condition)
		{
			ItemInfo itemInfo;
			string key = name + "<" + include + "<" + condition;
			if (oldItems.TryGetValue (key, out itemInfo)) {
				if (!itemInfo.Added) {
					itemInfo.Added = true;
					oldItems [key] = itemInfo;
				}
				return itemInfo.Item;
			} else {
				return msproject.AddNewItem (name, include);
			}
		}

		ConfigData FindPropertyGroup (List<ConfigData> configData, SolutionItemConfiguration config)
		{
			foreach (ConfigData data in configData) {
				if (data.Config == config.Name && data.Platform == config.Platform)
					return data;
			}
			return null;
		}

		string BuildConfigCondition (string config, string platform)
		{
			if (platform.Length == 0)
				platform = "AnyCPU";
			return " '$(Configuration)|$(Platform)' == '" + config + "|" + platform + "' ";
		}

		bool IsMergeToProjectProperty (ItemProperty prop)
		{
			foreach (object at in prop.CustomAttributes) {
				if (at is MergeToProjectAttribute)
					return true;
			}
			return false;
		}

		public void AddImportIfMissing (string name, string condition)
		{
			importsAdded.Add (new DotNetProjectImport (name, condition));
		}

		public void RemoveImport (string name)
		{
			importsRemoved.Add (new DotNetProjectImport (name));
		}

		List <DotNetProjectImport> importsAdded = new List<DotNetProjectImport> ();

		internal IList<DotNetProjectImport> ImportsAdded {
			get { return importsAdded; }
		}

		List <DotNetProjectImport> importsRemoved = new List<DotNetProjectImport> ();

		internal IList<DotNetProjectImport> ImportsRemoved {
			get { return importsRemoved; }
		}

		void ImportsSaved ()
		{
			importsAdded.Clear ();
			importsRemoved.Clear ();
		}
		internal void NotifyFileRenamedInProject (ProjectFileRenamedEventArgs args)
		{
			NotifyModified ("Files");
			OnFileRenamedInProject (args);
		}
		
		/// <summary>
		/// Raises the FileRemovedFromProject event.
		/// </summary>
		protected virtual void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			ProjectExtension.OnFileRemovedFromProject (e);
		}
		void DoOnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			buildActions = null;
			if (FileRemovedFromProject != null) {
				FileRemovedFromProject (this, e);
			}
		}

		/// <summary>
		/// Raises the FileAddedToProject event.
		/// </summary>
		protected virtual void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			ProjectExtension.OnFileAddedToProject (e);
		}
		void DoOnFileAddedToProject (ProjectFileEventArgs e)
		{
			buildActions = null;
			if (FileAddedToProject != null) {
				FileAddedToProject (this, e);
			}
		}

		/// <summary>
		/// Raises the FileChangedInProject event.
		/// </summary>
		protected virtual void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			ProjectExtension.OnFileChangedInProject (e);
		}
		void DoOnFileChangedInProject (ProjectFileEventArgs e)
		{
			if (FileChangedInProject != null) {
				FileChangedInProject (this, e);
			}
		}

		/// <summary>
		/// Raises the FilePropertyChangedInProject event.
		/// </summary>
		protected virtual void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			ProjectExtension.OnFilePropertyChangedInProject (e);
		}
		void DoOnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			buildActions = null;
			if (FilePropertyChangedInProject != null) {
				FilePropertyChangedInProject (this, e);
			}
		}

		/// <summary>
		/// Raises the FileRenamedInProject event.
		/// </summary>
		protected virtual void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			ProjectExtension.OnFileRenamedInProject (e);
		}
		void DoOnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			if (FileRenamedInProject != null) {
				FileRenamedInProject (this, e);
			}
		}

		/// <summary>
		/// Occurs when a file is removed from this project.
		/// </summary>
		public event ProjectFileEventHandler FileRemovedFromProject;
		
		/// <summary>
		/// Occurs when a file is added to this project.
		/// </summary>
		public event ProjectFileEventHandler FileAddedToProject;

		/// <summary>
		/// Occurs when a file of this project has been modified
		/// </summary>
		public event ProjectFileEventHandler FileChangedInProject;
		
		/// <summary>
		/// Occurs when a property of a file of this project has changed
		/// </summary>
		public event ProjectFileEventHandler FilePropertyChangedInProject;
		
		/// <summary>
		/// Occurs when a file of this project has been renamed
		/// </summary>
		public event ProjectFileRenamedEventHandler FileRenamedInProject;


		class DefaultMSBuildProjectExtension: ProjectExtension
		{
			internal protected override bool SupportsFlavor (string guid)
			{
				return false;
			}

			internal protected override bool OnGetIsCompileable (string fileName)
			{
				return false;
			}

			internal protected override void OnGetProjectTypes (HashSet<string> types)
			{
				Project.OnGetProjectTypes (types);
			}

			internal protected override Task<BuildResult> OnRunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration)
			{
				return Project.DoRunTarget (monitor, target, configuration);
			}

			internal protected override bool OnGetSupportsTarget (string target)
			{
				return Project.OnGetSupportsTarget (target);
			}

			internal protected override string OnGetDefaultBuildAction (string fileName)
			{
				return Project.IsCompileable (fileName) ? BuildAction.Compile : BuildAction.None;
			}

			internal protected override string OnGetDefaultResourceId (ProjectFile projectFile)
			{
				return Project.OnGetDefaultResourceId (projectFile);
			}

			internal protected override IEnumerable<string> OnGetStandardBuildActions ()
			{
				return BuildAction.StandardActions;
			}

			internal protected override IList<string> OnGetCommonBuildActions ()
			{
				return BuildAction.StandardActions;
			}

			internal protected override ProjectItem OnCreateProjectItem (IMSBuildItemEvaluated item)
			{
				return Project.OnCreateProjectItem (item);
			}

			internal protected override void OnPopulateSupportFileList (FileCopySet list, ConfigurationSelector configuration)
			{
				Project.DoPopulateSupportFileList (list, configuration);
			}

			internal protected override void OnPopulateOutputFileList (List<FilePath> list, ConfigurationSelector configuration)
			{
				Project.DoPopulateOutputFileList (list, configuration);
			}

			internal protected override FilePath OnGetOutputFileName (ConfigurationSelector configuration)
			{
				return FilePath.Null;
			}

			internal protected override string[] SupportedLanguages {
				get {
					return new String[] { "" };
				}
			}

			internal protected override void OnFileRemovedFromProject (ProjectFileEventArgs e)
			{
				Project.DoOnFileRemovedFromProject (e);
			}

			internal protected override void OnFileAddedToProject (ProjectFileEventArgs e)
			{
				Project.DoOnFileAddedToProject (e);
			}

			internal protected override void OnFileChangedInProject (ProjectFileEventArgs e)
			{
				Project.DoOnFileChangedInProject (e);
			}

			internal protected override void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
			{
				Project.DoOnFilePropertyChangedInProject (e);
			}

			internal protected override void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
			{
				Project.DoOnFileRenamedInProject (e);
			}

			internal protected override void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
			{
				Project.OnReadProject (monitor, msproject);
			}

			internal protected override void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
			{
				Project.OnWriteProject (monitor, msproject);
			}

			internal protected override void OnReadConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IMSBuildPropertySet grp)
			{
				Project.OnReadConfiguration (monitor, config, grp);
			}

			internal protected override void OnWriteConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IMSBuildPropertySet grp)
			{
				Project.OnWriteConfiguration (monitor, config, grp);
			}

			internal protected override void OnGetDefaultImports (List<string> imports)
			{
				Project.OnGetDefaultImports (imports);
			}
		}
	}

	public delegate void ProjectEventHandler (Object sender, ProjectEventArgs e);
	public class ProjectEventArgs : EventArgs
	{
		public ProjectEventArgs (Project project)
		{
			this.project = project;
		}

		private Project project;
		public Project Project {
			get { return project; }
		}
	}

	class UnresolvedFileCollection
	{
		// Holds a dictionary of files that depend on other files, and for which the dependency
		// has not yet been resolved. The key of the dictionary is the path to a parent
		// file to be resolved, and the value can be a ProjectFile object or a List<ProjectFile>
		// (This may happen if several files depend on the same parent file)
		Dictionary<FilePath,object> unresolvedDeps = new Dictionary<FilePath, object> ();

		public void Remove (ProjectFile file)
		{
			Remove (file, null);
		}

		public void Remove (ProjectFile file, FilePath dependencyPath)
		{
			if (dependencyPath.IsNullOrEmpty) {
				if (string.IsNullOrEmpty (file.DependsOn))
					return;
				dependencyPath = file.DependencyPath;
			}

			object depFile;
			if (unresolvedDeps.TryGetValue (dependencyPath, out depFile)) {
				if ((depFile is ProjectFile) && ((ProjectFile)depFile == file))
					unresolvedDeps.Remove (dependencyPath);
				else if (depFile is List<ProjectFile>) {
					var list = (List<ProjectFile>) depFile;
					list.Remove (file);
					if (list.Count == 1)
						unresolvedDeps [dependencyPath] = list[0];
				}
			}
		}

		public void Add (ProjectFile file)
		{
			object depFile;
			if (unresolvedDeps.TryGetValue (file.DependencyPath, out depFile)) {
				if (depFile is ProjectFile) {
					if ((ProjectFile)depFile != file) {
						var list = new List<ProjectFile> ();
						list.Add ((ProjectFile)depFile);
						list.Add (file);
						unresolvedDeps [file.DependencyPath] = list;
					}
				}
				else if (depFile is List<ProjectFile>) {
					var list = (List<ProjectFile>) depFile;
					if (!list.Contains (file))
						list.Add (file);
				}
			} else
				unresolvedDeps [file.DependencyPath] = file;
		}

		public IEnumerable<ProjectFile> GetUnresolvedFilesForPath (FilePath filePath)
		{
			object depFile;
			if (unresolvedDeps.TryGetValue (filePath, out depFile)) {
				if (depFile is ProjectFile)
					yield return (ProjectFile) depFile;
				else {
					foreach (var f in (List<ProjectFile>) depFile)
						yield return f;
				}
			}
		}
	}
}
