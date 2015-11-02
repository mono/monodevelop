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
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using System.Threading.Tasks;
using MonoDevelop.Projects.Formats.MSBuild;
using System.Xml;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects.Extensions;
using System.Collections.Immutable;
using System.Threading;

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
		static Counter ProjectOpenedCounter = InstrumentationService.CreateCounter ("Project Opened", "Project Model", id:"Ide.Project.Open");

		string[] buildActions;
		MSBuildProject sourceProject;

		string productVersion;
		string schemaVersion;
		bool modifiedInMemory;
		bool msbuildUpdatePending;
		ProjectExtension projectExtension;

		List<string> defaultImports;

		ProjectItemCollection items;

		IEnumerable<string> loadedAvailableItemNames = ImmutableList<string>.Empty;

		protected Project ()
		{
			items = new ProjectItemCollection (this);
			FileService.FileChanged += HandleFileChanged;
			Runtime.SystemAssemblyService.DefaultRuntimeChanged += OnDefaultRuntimeChanged;
			files = new ProjectFileCollection ();
			Items.Bind (files);
			DependencyResolutionEnabled = true;
        }

		public ProjectItemCollection Items {
			get { return items; }
		}

		protected Project (params string[] flavorGuids): this()
		{
			this.flavorGuids = flavorGuids;
		}

		protected Project (ProjectCreateInformation projectCreateInfo, XmlElement projectOptions): this()
		{
			var ids = projectOptions != null ? projectOptions.GetAttribute ("flavorIds") : null;
			if (!string.IsNullOrEmpty (ids)) {
				this.flavorGuids = ids.Split (new [] {';'}, StringSplitOptions.RemoveEmptyEntries);
			}
		}

		protected override void OnSetShared ()
		{
			base.OnSetShared ();
			items.SetShared ();
			files.SetShared ();
		}

		internal class CreationContext
		{
			public MSBuildProject Project { get; set; }
			public string TypeGuid { get; set; }
			public string[] FlavorGuids { get; set; }

			internal static CreationContext Create (MSBuildProject p, string typeGuid)
			{
				return new CreationContext {
					Project = p,
					TypeGuid = typeGuid
				};
			}

			internal static CreationContext Create (string typeGuid, string[] flavorGuids)
			{
				return new CreationContext {
					TypeGuid = typeGuid,
					FlavorGuids = flavorGuids
				};
			}
		}

		CreationContext creationContext;

		internal void SetCreationContext (CreationContext ctx)
		{
			creationContext = ctx;
		}

		protected override void OnInitialize ()
		{
			base.OnInitialize ();

			if (creationContext != null) {

				if (IsExtensionChainCreated)
					throw new InvalidOperationException ("Extension chain already created for this object");

				TypeGuid = creationContext.TypeGuid;

				string projectTypeGuids;

				if (creationContext.Project != null) {
					this.sourceProject = creationContext.Project;
					IMSBuildPropertySet globalGroup = sourceProject.GetGlobalPropertyGroup ();
					projectTypeGuids = globalGroup.GetValue ("ProjectTypeGuids");
					if (projectTypeGuids != null) {
						var subtypeGuids = new List<string> ();
						foreach (string guid in projectTypeGuids.Split (';')) {
							string sguid = guid.Trim ();
							if (sguid.Length > 0 && string.Compare (sguid, creationContext.TypeGuid, StringComparison.OrdinalIgnoreCase) != 0)
								subtypeGuids.Add (guid);
						}
						flavorGuids = subtypeGuids.ToArray ();
					}
				} else {
					sourceProject = new MSBuildProject ();
					sourceProject.FileName = FileName;
					flavorGuids = creationContext.FlavorGuids;
				}
			}

			if (sourceProject == null) {
				sourceProject = new MSBuildProject ();
				sourceProject.FileName = FileName;
			}
		}

		protected override void OnExtensionChainInitialized ()
		{
			projectExtension = ExtensionChain.GetExtension<ProjectExtension> ();
			base.OnExtensionChainInitialized ();
			if (creationContext != null && creationContext.Project != null)
				FileName = creationContext.Project.FileName;

			MSBuildEngineSupport = MSBuildProjectService.GetMSBuildSupportForProject (this);
			InitFormatProperties ();
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

		public IPropertySet ProjectProperties {
			get { return MSBuildProject.GetGlobalPropertyGroup (); }
		}

		public MSBuildProject MSBuildProject {
			get { 
				if (msbuildUpdatePending && !saving)
					WriteProject (new ProgressMonitor ());
				return sourceProject;
			}
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

		new public ProjectConfiguration CreateConfiguration (string name, ConfigurationKind kind = ConfigurationKind.Blank)
		{
			return (ProjectConfiguration) base.CreateConfiguration (name, kind);
		}

		protected virtual void OnGetDefaultImports (List<string> imports)
		{
		}

		public string ToolsVersion { get; private set; }

		internal bool CheckAllFlavorsSupported ()
		{
			return FlavorGuids.All (g => ProjectExtension.SupportsFlavor (g));
		}

		ProjectExtension ProjectExtension {
			get {
				if (projectExtension == null)
					AssertExtensionChainCreated ();
				return projectExtension;
			}
		}

		public MSBuildSupport MSBuildEngineSupport { get; private set; }

		protected override void OnModified (SolutionItemModifiedEventArgs args)
		{
			if (!Loading) {
				modifiedInMemory = true;
				msbuildUpdatePending = true;
			}
			base.OnModified (args);
		}

		protected override Task OnLoad (ProgressMonitor monitor)
		{
			return Task.Run (delegate {
				if (sourceProject == null || sourceProject.IsNewProject) {
					sourceProject = MSBuildProject.LoadAsync (FileName).Result;
					if (MSBuildEngineSupport == MSBuildSupport.NotSupported)
						sourceProject.UseMSBuildEngine = false;
					sourceProject.Evaluate ();
				}

				IMSBuildPropertySet globalGroup = sourceProject.GetGlobalPropertyGroup ();
				// Avoid crash if there is not global group
				if (globalGroup == null)
					sourceProject.AddNewPropertyGroup (false);

				ProjectExtension.OnPrepareForEvaluation (sourceProject);

				ReadProject (monitor, sourceProject);
			});
		}

		/// <summary>
		/// Runs the generator target and returns any files that the target included into the compilation that were not already part of the 
		/// build. This is the list of files assumed to have been possibly modified by the target.
		/// </summary>
		public Task<ProjectFile[]> PerformGeneratorAsync (ConfigurationSelector configuration, string generatorTarget)
		{
			return BindTask<ProjectFile[]> (async cancelToken => {
				var cancelSource = new CancellationTokenSource ();
				cancelToken.Register (() => cancelSource.Cancel ());

				using (var monitor = new ProgressMonitor (cancelSource)) {
					return await this.PerformGeneratorAsync (monitor, configuration, generatorTarget);
				}
			});
		}

		/// <summary>
		/// Runs the generator target and returns any files that the target included into the compilation that were not already part of the 
		/// build. This is the list of files assumed to have been possibly modified by the target.
		/// </summary>
		async Task<ProjectFile[]> PerformGeneratorAsync (ProgressMonitor monitor, ConfigurationSelector configuration, string generatorTarget)
		{
			var ctx = new TargetEvaluationContext ();
			ctx.ItemsToEvaluate.Add ("Compile");

			var result = new List<ProjectFile> ();
			var exisitingFiles = this.Files.ToList ();

			var evalResult = await this.RunTarget (monitor, generatorTarget, configuration, ctx);
			if (evalResult != null && !evalResult.BuildResult.HasErrors) {
				var compileItems = evalResult.Items.Select (i =>
				                                            new ProjectFile (Path.Combine (sourceProject.BaseDirectory, i.Include), "Compile") { Project = this }
				                                           ).ToList ();

				// we need to grab any items that are in this result but not explicitly part of the project
				result.AddRange (compileItems.Where (ci => exisitingFiles.All (ei => ei.FilePath != ci.FilePath)));
			}

			return result.ToArray ();
		}

		/// <summary>
		/// Gets the source files that are included in the project, including any that are added by `CoreCompileDependsOn`
		/// </summary>
		public Task<ProjectFile[]> GetSourceFilesAsync (ConfigurationSelector configuration)
		{
			if (sourceProject == null)
				return Task.FromResult (new ProjectFile [0]);

			return BindTask<ProjectFile []> (async cancelToken => {
				var cancelSource = new CancellationTokenSource ();
				cancelToken.Register (() => cancelSource.Cancel ());

				using (var monitor = new ProgressMonitor (cancelSource)) {
					return await GetSourceFilesAsync (monitor, configuration);
				}
			});
		}

		readonly object evaluatedCompileItemsLock = new object ();
		List<ProjectFile> evaluatedCompileItems;
		readonly ManualResetEvent evaluatedCompileItemsWaitHandle = new ManualResetEvent (false);

		/// <summary>
		/// Gets the source files that are included in the project, including any that are added by `CoreCompileDependsOn`
		/// </summary>
		public async Task<ProjectFile[]> GetSourceFilesAsync (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			var coreCompileDependsOn = sourceProject.EvaluatedProperties.GetValue<string> ("CoreCompileDependsOn");

			if (string.IsNullOrEmpty (coreCompileDependsOn))
				return this.Files.ToArray ();

			// pre-load the results with the current list of files in the project
			var results = new List<ProjectFile> (this.Files);

			var performEvaluation = false;
			lock (evaluatedCompileItemsLock) {
				if (evaluatedCompileItems == null) {
					evaluatedCompileItems = new List<ProjectFile> ();
					performEvaluation = true;
				}
			}

			if (performEvaluation) {
				var dependsList = coreCompileDependsOn.Split (new [] { ";" }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var dependTarget in dependsList) {
					try {
						// evaluate the Compile targets
						var ctx = new TargetEvaluationContext ();
						ctx.ItemsToEvaluate.Add ("Compile");

						var evalResult = await this.RunTarget (monitor, dependTarget, configuration, ctx);
						if (evalResult != null && !evalResult.BuildResult.HasErrors) {
							var evalItems = evalResult.Items.Select (i =>
																		new ProjectFile (Path.Combine (sourceProject.BaseDirectory, i.Include), "Compile") { Project = this }
																	   ).ToList ();

							evaluatedCompileItems.AddRange (evalItems);
						}
					} catch (Exception ex) {
						LoggingService.LogInternalError (string.Format ("Error running target {0}", dependTarget), ex);
					}

					evaluatedCompileItemsWaitHandle.Set ();
				}
			} else {
				// wait for evaluation
				await Task.Run (() => {
					evaluatedCompileItemsWaitHandle.WaitOne ();
				});
			}

			// add only files that aren't already in the list
			results.AddRange (evaluatedCompileItems.Where (i =>  results.All (pi => pi.FilePath != i.FilePath)));

			return results.ToArray ();
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
			SetFastBuildCheckDirty ();
			modifiedInMemory = false;

			return Task.Run (delegate {
				WriteProject (monitor);

				// Doesn't save the file to disk if the content did not change
				if (sourceProject.Save (FileName) && projectBuilder != null) {
					projectBuilder.Refresh ().Wait ();
				}
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

		protected override void OnGetProjectEventMetadata (IDictionary<string, string> metadata)
		{
			base.OnGetProjectEventMetadata (metadata);
			var sb = new System.Text.StringBuilder ();
			var first = true;

			var projectTypes = this.GetTypeTags ().ToList ();
			foreach (var p in projectTypes.Where (x => (x != "DotNet") || projectTypes.Count == 1)) {
				if (!first)
					sb.Append (", ");
				sb.Append (p);
				first = false;
			}
			metadata ["ProjectTypes"] = sb.ToString ();
		}

		protected override void OnEndLoad ()
		{
			base.OnEndLoad ();

			ProjectOpenedCounter.Inc (1, null, GetProjectEventMetadata (null));
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
		public bool IsCompileable (string fileName)
		{
			return ProjectExtension.OnGetIsCompileable (fileName);
		}

		protected virtual bool OnGetIsCompileable (string fileName)
		{
			return false;
		}

		/// <summary>
		/// Determines whether the provided build action is a compile action
		/// </summary>
		/// <returns><c>true</c> if this instance is compile build action the specified buildAction; otherwise, <c>false</c>.</returns>
		/// <param name="buildAction">Build action.</param>
		public bool IsCompileBuildAction (string buildAction)
		{
			return ProjectExtension.OnGetIsCompileBuildAction (buildAction);
		}

		protected virtual bool OnGetIsCompileBuildAction (string buildAction)
		{
			return buildAction == BuildAction.Compile;
		}

		/// <summary>
		/// Files of the project
		/// </summary>
		public ProjectFileCollection Files {
			get { return files; }
		}
		private ProjectFileCollection files;

		FilePath baseIntermediateOutputPath;

		public FilePath BaseIntermediateOutputPath {
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
		/// Gets the project type and its base types.
		/// </summary>
		public IEnumerable<string> GetTypeTags ()
		{
			HashSet<string> sset = new HashSet<string> ();
			ProjectExtension.OnGetTypeTags (sset);
			return sset;
		}

		protected virtual void OnGetTypeTags (HashSet<string> types)
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
		public IconId StockIcon {
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
		public string[] SupportedLanguages {
			get { return ProjectExtension.SupportedLanguages; }
		}

		protected virtual string[] OnGetSupportedLanguages ()
		{
			return new String[] { "" };
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
		public string GetDefaultBuildAction (string fileName)
		{
			return ProjectExtension.OnGetDefaultBuildAction (fileName);
		}

		protected virtual string OnGetDefaultBuildAction (string fileName)
		{
			return IsCompileable (fileName) ? BuildAction.Compile : BuildAction.None;
		}

		internal ProjectItem CreateProjectItem (IMSBuildItemEvaluated item)
		{
			return ProjectExtension.OnCreateProjectItem (item);
		}

		protected virtual ProjectItem OnCreateProjectItem (IMSBuildItemEvaluated item)
		{
			if (item.Name == "Folder")
				return new ProjectFile ();

			var type = MSBuildProjectService.GetProjectItemType (item.Name);
			if (type != null)
				return (ProjectItem) Activator.CreateInstance (type, true);

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
			HashSet<string> actions = new HashSet<string> ();
			//ad the standard actions
			foreach (string action in ProjectExtension.OnGetStandardBuildActions ().Concat (loadedAvailableItemNames))
				actions.Add (action);

			//add any more actions that are in the project file
			foreach (ProjectFile pf in files)
				actions.Add (pf.BuildAction);

			//remove the "common" actions, since they're handled separately
			IList<string> commonActions = ProjectExtension.OnGetCommonBuildActions ();
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
				actions.CopyTo (buildActions, uncommonStart);

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
		protected virtual IEnumerable<string> OnGetStandardBuildActions ()
		{
			return BuildAction.StandardActions;
		}

		/// <summary>
		/// Gets a list of common build actions (common actions are shown first in the project build action list)
		/// </summary>
		protected virtual IList<string> OnGetCommonBuildActions ()
		{
			return BuildAction.StandardActions;
		}

		protected override void OnDispose ()
		{
			foreach (var item in items) {
				IDisposable disp = item as IDisposable;
				if (disp != null)
					disp.Dispose ();
			}

			FileService.FileChanged -= HandleFileChanged;
			Runtime.SystemAssemblyService.DefaultRuntimeChanged -= OnDefaultRuntimeChanged;
			CleanupProjectBuilder ();

			if (sourceProject != null) {
				sourceProject.Dispose ();
				sourceProject = null;
			}
			base.OnDispose ();
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
		public async Task<TargetEvaluationResult> RunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context = null)
		{
			return await ProjectExtension.OnRunTarget (monitor, target, configuration, context);
		}

		public bool SupportsTarget (string target)
		{
			return !IsUnsupportedProject && ProjectExtension.OnGetSupportsTarget (target);
		}

		protected virtual bool OnGetSupportsTarget (string target)
		{
			return sourceProject.EvaluatedTargets.Any (t => t.Name == target);
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
		internal protected virtual Task<TargetEvaluationResult> OnRunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			if (target == ProjectService.BuildTarget)
				return RunBuildTarget (monitor, configuration, context);
			else if (target == ProjectService.CleanTarget)
				return RunCleanTarget (monitor, configuration, context);
			return RunMSBuildTarget (monitor, target, configuration, context);
		}


		async Task<TargetEvaluationResult> DoRunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			if (target == ProjectService.BuildTarget) {
				SolutionItemConfiguration conf = GetConfiguration (configuration);
				if (conf != null && conf.CustomCommands.HasCommands (CustomCommandType.Build)) {
					if (monitor.CancellationToken.IsCancellationRequested)
						return new TargetEvaluationResult (BuildResult.CreateCancelled ().SetSource (this));
					if (!await conf.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.Build, configuration)) {
						var r = new BuildResult ();
						r.AddError (GettextCatalog.GetString ("Custom command execution failed"));
						return new TargetEvaluationResult (r.SetSource (this));
					}
					return new TargetEvaluationResult (BuildResult.CreateSuccess ().SetSource (this));
				}
			} else if (target == ProjectService.CleanTarget) {
				SetFastBuildCheckDirty ();
				SolutionItemConfiguration config = GetConfiguration (configuration);
				if (config != null && config.CustomCommands.HasCommands (CustomCommandType.Clean)) {
					if (monitor.CancellationToken.IsCancellationRequested)
						return new TargetEvaluationResult (BuildResult.CreateCancelled ().SetSource (this));
					if (!await config.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.Clean, configuration)) {
						var r = new BuildResult ();
						r.AddError (GettextCatalog.GetString ("Custom command execution failed"));
						return new TargetEvaluationResult (r.SetSource (this));
					}
					return new TargetEvaluationResult (BuildResult.CreateSuccess ().SetSource (this));
				}
			}

			// Collect last write times for the files generated by this project
			var fileTimes = new Dictionary<FilePath, DateTime> ();
			foreach (var f in GetOutputFiles (configuration))
				fileTimes [f] = File.GetLastWriteTime (f);

			try {
				var tr = await OnRunTarget (monitor, target, configuration, context);
				tr.BuildResult.SourceTarget = this;
				return tr;
			} finally {
				// If any of the project generated files changes, notify it
				foreach (var e in fileTimes) {
					if (File.GetLastWriteTime (e.Key) != e.Value)
						FileService.NotifyFileChanged (e.Key);
				}
			}
		}

		async Task<TargetEvaluationResult> RunMSBuildTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			if (CheckUseMSBuildEngine (configuration)) {
				LogWriter logWriter = new LogWriter (monitor.Log);
				var configs = GetConfigurations (configuration);	

				string [] evaluateItems = context != null ? context.ItemsToEvaluate.ToArray () : new string [0];
				string [] evaluateProperties = context != null ? context.PropertiesToEvaluate.ToArray () : new string [0];

				var globalProperties = new Dictionary<string, string> ();
				if (context != null) {
					var md = (ProjectItemMetadata)context.GlobalProperties;
					md.SetProject (sourceProject);
					foreach (var p in md.GetProperties ())
						globalProperties [p.Name] = p.Value;
				}

				MSBuildResult result = null;
				await Task.Run (async delegate {

					TimerCounter buildTimer = null;
					switch (target) {
					case "Build": buildTimer = Counters.BuildMSBuildProjectTimer; break;
					case "Clean": buildTimer = Counters.CleanMSBuildProjectTimer; break;
					}

					var t1 = Counters.RunMSBuildTargetTimer.BeginTiming (GetProjectEventMetadata (configuration));
					var t2 = buildTimer != null ? buildTimer.BeginTiming (GetProjectEventMetadata (configuration)) : null;

					RemoteProjectBuilder builder = await GetProjectBuilder ();
					if (builder.IsBusy)
						builder = await RequestLockedBuilder ();
					else
						builder.Lock ();

					try {
						result = await builder.Run (configs, logWriter, MSBuildProjectService.DefaultMSBuildVerbosity, new [] { target }, evaluateItems, evaluateProperties, globalProperties, monitor.CancellationToken);
					} finally {
						builder.Unlock ();
						if (builder != this.projectBuilder) {
							// Dispose the builder after a while, so that it can be reused
							Task.Delay (10000).ContinueWith (t => builder.Dispose ());
						}
						t1.End ();
						if (t2 != null)
							t2.End ();
					}

					System.Runtime.Remoting.RemotingServices.Disconnect (logWriter);
				});

				var br = new BuildResult ();
				foreach (var err in result.Errors) {
					FilePath file = null;
					if (err.File != null)
						file = Path.Combine (Path.GetDirectoryName (err.ProjectFile), err.File);

					br.Append (new BuildError (file, err.LineNumber, err.ColumnNumber, err.Code, err.Message) {
						Subcategory = err.Subcategory,
						EndLine = err.EndLineNumber,
						EndColumn = err.EndColumnNumber,
						IsWarning = err.IsWarning,
						HelpKeyword = err.HelpKeyword,
					});
				}

				// Get the evaluated properties

				var properties = new Dictionary<string, MSBuildPropertyEvaluated> ();
				foreach (var p in result.Properties)
					properties [p.Key] = new MSBuildPropertyEvaluated (sourceProject, p.Key, p.Value, p.Value);

				var props = new MSBuildPropertyGroupEvaluated (sourceProject);
				props.SetProperties (properties);

				// Get the evaluated items

				var evItems = new List<IMSBuildItemEvaluated> ();
				foreach (var it in result.Items.SelectMany (d => d.Value)) {
					var eit = new MSBuildItemEvaluated (sourceProject, it.Name, it.ItemSpec, it.ItemSpec);
					if (it.Metadata.Count > 0) {
						var imd = (MSBuildPropertyGroupEvaluated)eit.Metadata;
						properties = new Dictionary<string, MSBuildPropertyEvaluated> ();
						foreach (var m in it.Metadata)
							properties [m.Key] = new MSBuildPropertyEvaluated (sourceProject, m.Key, m.Value, m.Value);
						imd.SetProperties (properties);
					}
					evItems.Add (eit);
				}

				return new TargetEvaluationResult (br, evItems, props);
			}
			else {
				CleanupProjectBuilder ();
				if (this is DotNetProject) {
					var handler = new MonoDevelop.Projects.Formats.MD1.MD1DotNetProjectHandler ((DotNetProject)this);
					return new TargetEvaluationResult (await handler.RunTarget (monitor, target, configuration));
				}
			}
			return null;
		}

		internal ProjectConfigurationInfo [] GetConfigurations (ConfigurationSelector configuration, bool includeReferencedProjects = true)
		{
			var visitedProjects = new HashSet<Project> ();
			visitedProjects.Add (this);
			return GetConfigurations (configuration, includeReferencedProjects, visitedProjects);
		}

		ProjectConfigurationInfo[] GetConfigurations (ConfigurationSelector configuration, bool includeReferencedProjects, HashSet<Project> visited)
		{
			var sc = ParentSolution != null ? ParentSolution.GetConfiguration (configuration) : null;

			// Returns a list of project/configuration information for the provided item and all its references
			List<ProjectConfigurationInfo> configs = new List<ProjectConfigurationInfo> ();
			var c = GetConfiguration (configuration);
			configs.Add (new ProjectConfigurationInfo () {
				ProjectFile = FileName,
				Configuration = c != null ? c.Name : "",
				Platform = c != null ? GetExplicitPlatform (c) : "",
				ProjectGuid = ItemId,
				Enabled = sc == null || sc.BuildEnabledForItem (this)
			});
			if (includeReferencedProjects) {
				foreach (var refProject in GetReferencedItems (configuration).OfType<Project> ().Where (p => p.SupportsBuild ())) {
					if (!visited.Add (refProject))
						continue;
					// Recursively get all referenced projects. This is necessary if one of the referenced
					// projects is using the local copy flag.
					foreach (var rp in refProject.GetConfigurations (configuration, true, visited)) {
						if (!configs.Any (pc => pc.ProjectFile == rp.ProjectFile))
							configs.Add (rp);
					}
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
		AsyncCriticalSection builderLock = new AsyncCriticalSection ();

		internal async Task<RemoteProjectBuilder> GetProjectBuilder ()
		{
			//FIXME: we can't really have per-project runtimes, has to be per-solution
			TargetRuntime runtime = null;
			var ap = this as IAssemblyProject;
			runtime = ap != null ? ap.TargetRuntime : Runtime.SystemAssemblyService.CurrentRuntime;

			var sln = ParentSolution;
			var slnFile = sln != null ? sln.FileName : null;

			using (await builderLock.EnterAsync ()) {
				if (projectBuilder == null || lastBuildToolsVersion != ToolsVersion || lastBuildRuntime != runtime.Id || lastFileName != FileName || lastSlnFileName != slnFile) {
					if (projectBuilder != null) {
						projectBuilder.Dispose ();
						projectBuilder = null;
					}
					projectBuilder = await MSBuildProjectService.GetProjectBuilder (runtime, ToolsVersion, FileName, slnFile, 0);
					projectBuilder.Disconnected += delegate {
						CleanupProjectBuilder ();
					};
					lastBuildToolsVersion = ToolsVersion;
					lastBuildRuntime = runtime.Id;
					lastFileName = FileName;
					lastSlnFileName = slnFile;
				}
				if (modifiedInMemory) {
					modifiedInMemory = false;
					WriteProject (new ProgressMonitor ());
					await projectBuilder.RefreshWithContent (sourceProject.SaveToString ());
				}
			}
			return projectBuilder;
		}

		async Task<RemoteProjectBuilder> RequestLockedBuilder ()
		{
			TargetRuntime runtime = null;
			var ap = this as IAssemblyProject;
			runtime = ap != null ? ap.TargetRuntime : Runtime.SystemAssemblyService.CurrentRuntime;

			var sln = ParentSolution;
			var slnFile = sln != null ? sln.FileName : null;

			var pb = await MSBuildProjectService.GetProjectBuilder (runtime, ToolsVersion, FileName, slnFile, 0, true);
			if (modifiedInMemory) {
				WriteProject (new ProgressMonitor ());
				await pb.RefreshWithContent (sourceProject.SaveToString ());
			}
			return pb;
		}

		void CleanupProjectBuilder ()
		{
			if (projectBuilder != null) {
				projectBuilder.Dispose ();
				projectBuilder = null;
			}
		}

		public Task RefreshProjectBuilder ()
		{
			if (projectBuilder != null)
				return projectBuilder.Refresh ();
			else
				return Task.FromResult (true);
		}

		public void ReloadProjectBuilder ()
		{
			CleanupProjectBuilder ();
		}

		#endregion

		/// <summary>Whether to use the MSBuild engine for the specified item.</summary>
		internal bool CheckUseMSBuildEngine (ConfigurationSelector sel, bool checkReferences = true)
		{
			// if the item mandates MSBuild, always use it
			if (MSBuildEngineSupport.HasFlag (MSBuildSupport.Required))
				return true;
			// if the user has set the option, use the setting
			if (UseMSBuildEngine.HasValue)
				return UseMSBuildEngine.Value;

			// If the item type defaults to using MSBuild, only use MSBuild if its direct references also use MSBuild.
			// This prevents a not-uncommon common error referencing non-MSBuild projects from MSBuild projects
			// NOTE: This adds about 11ms to the load/build/etc times of the MonoDevelop solution. Doing it recursively
			// adds well over a second.
			return MSBuildEngineSupport.HasFlag (MSBuildSupport.Supported) && (
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

		protected override async Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return (await RunTarget (monitor, "Build", configuration, new TargetEvaluationContext (operationContext))).BuildResult;
		}

		async Task<TargetEvaluationResult> RunBuildTarget (ProgressMonitor monitor, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			// create output directory, if not exists
			ProjectConfiguration conf = GetConfiguration (configuration) as ProjectConfiguration;
			if (conf == null) {
				BuildResult cres = new BuildResult ();
				cres.AddError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", configuration.ToString (), Name));
				return new TargetEvaluationResult (cres);
			}
			
			StringParserService.Properties["Project"] = Name;
			
			if (UsingMSBuildEngine (configuration)) {
				var result = await RunMSBuildTarget (monitor, "Build", configuration, context);
				if (!result.BuildResult.Failed)
					SetFastBuildCheckClean (configuration);
				return result;			
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

			return new TargetEvaluationResult (res);
		}

		bool disableFastUpToDateCheck;

		//the configuration of the last build that completed successfully
		//null if any file in the project has since changed
		string fastUpToDateCheckGoodConfig;

		public bool FastCheckNeedsBuild (ConfigurationSelector configuration)
		{
			return ProjectExtension.OnFastCheckNeedsBuild (configuration);
		}
		
		protected virtual bool OnFastCheckNeedsBuild (ConfigurationSelector configuration)
		{
			if (disableFastUpToDateCheck || fastUpToDateCheckGoodConfig == null)
				return true;
			var cfg = GetConfiguration (configuration);
			return cfg == null || cfg.Id != fastUpToDateCheckGoodConfig;
		}

		protected void SetFastBuildCheckDirty ()
		{
			fastUpToDateCheckGoodConfig = null;
		}
		
		void SetFastBuildCheckClean (ConfigurationSelector configuration)
		{
			var cfg = GetConfiguration (configuration);
			fastUpToDateCheckGoodConfig = cfg != null ? cfg.Id : null;
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
		/// Builds the project
		/// </summary>
		/// <param name="monitor">A progress monitor</param>
		/// <param name="solutionConfiguration">Configuration to use to build the project</param>
		/// <param name="operationContext">Context information.</param>
		public Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector solutionConfiguration, ProjectOperationContext operationContext)
		{
			return base.Build (monitor, solutionConfiguration, false, operationContext);
		}

		/// <summary>
		/// Builds the project
		/// </summary>
		/// <param name="monitor">A progress monitor</param>
		/// <param name="solutionConfiguration">Configuration to use to build the project</param>
		/// <param name="buildReferences">When set to <c>true</c>, the referenced items will be built before building this item.</param>
		/// <param name="operationContext">Context information.</param>
		public Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector solutionConfiguration, bool buildReferences, ProjectOperationContext operationContext)
		{
			return base.Build (monitor, solutionConfiguration, buildReferences, operationContext);
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
		protected virtual Task<BuildResult> DoBuild (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return Task.FromResult (BuildResult.CreateSuccess ());
		}

		protected override async Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return (await RunTarget (monitor, "Clean", configuration, new TargetEvaluationContext (operationContext))).BuildResult;
		}

		async Task<TargetEvaluationResult> RunCleanTarget (ProgressMonitor monitor, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			ProjectConfiguration config = GetConfiguration (configuration) as ProjectConfiguration;
			if (config == null) {
				monitor.ReportError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", configuration, Name), null);
				return new TargetEvaluationResult (BuildResult.CreateSuccess ());
			}
			
			if (UsingMSBuildEngine (configuration)) {
				var result = await RunMSBuildTarget (monitor, "Clean", configuration, context);
				if (!result.BuildResult.Failed)
					SetFastBuildCheckClean (configuration);
				return result;			
			}
			
			monitor.Log.WriteLine ("Removing output files...");

			var filesToDelete = GetOutputFiles (configuration).ToArray ();

			await Task.Run (delegate {
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
			return new TargetEvaluationResult (res);
		}

		protected virtual Task<BuildResult> DoClean (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return Task.FromResult (BuildResult.CreateSuccess ());
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
		public FilePath GetOutputFileName (ConfigurationSelector configuration)
		{
			return ProjectExtension.OnGetOutputFileName (configuration);
		}

		protected virtual FilePath OnGetOutputFileName (ConfigurationSelector configuration)
		{
			return FilePath.Null;
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

		void HandleFileChanged (object source, FileEventArgs e)
		{
			// File change events are fired asynchronously, so the project might already be
			// disposed when the event is received.
			if (Disposed)
				return;
			
			OnFileChanged (source, e);
		}

		internal virtual void OnFileChanged (object source, FileEventArgs e)
		{
			foreach (FileEventInfo fi in e) {
				ProjectFile file = GetProjectFile (fi.FileName);
				if (file != null) {
					SetFastBuildCheckDirty ();
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
			foreach (var it in objs) {
				if (it.Project != null)
					throw new InvalidOperationException (it.GetType ().Name + " already belongs to a project");
				it.Project = this;
			}
			NotifyFileAddedToProject (objs.OfType<ProjectFile> ());
		}

		protected internal override void OnItemsRemoved (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsRemoved (objs);
			foreach (var it in objs)
				it.Project = null;
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
				args.Add (new ProjectFileEventInfo (this, file));
				if (DependencyResolutionEnabled) {
					unresolvedDeps.Remove (file);
					foreach (ProjectFile f in file.DependentChildren) {
						f.DependsOnFile = null;
						if (!string.IsNullOrEmpty (f.DependsOn))
							unresolvedDeps.Add (f);
					}
					file.DependsOn = null;
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
			ProjectExtension.OnReadProjectHeader (monitor, msproject);
			modifiedInMemory = false;
			msbuildUpdatePending = false;
			ProjectExtension.OnReadProject (monitor, msproject);
			NeedsReload = false;
		}

		internal void WriteProject (ProgressMonitor monitor)
		{
			if (saving) {
				LoggingService.LogError ("WriteProject called while the project is already being written");
				return;
			}
			
			saving = true;
			MSBuildProjectInstance pi = null;

			try {
				msbuildUpdatePending = false;
				sourceProject.FileName = FileName;

				// Create a project instance to be used for comparing old and new values in the global property group
				// We use a dummy configuration and platform to avoid loading default values from the configurations
				// while evaluating
				pi = CreateProjectInstaceForConfiguration ("", "");

				IMSBuildPropertySet globalGroup = sourceProject.GetGlobalPropertyGroup ();

				// Store properties that already exist in the project. We'll always keep those properties, even if they have default values.
				var preexistingGlobalProps = new HashSet<string> (globalGroup != null ? globalGroup.GetProperties ().Select (p => p.Name) : Enumerable.Empty<string> ());

				OnWriteProjectHeader (monitor, sourceProject);
				ProjectExtension.OnWriteProject (monitor, sourceProject);

				// Remove properties whose value has not changed and which were not set when the project was loaded
				((MSBuildPropertyGroup)globalGroup).UnMerge (pi.EvaluatedProperties, preexistingGlobalProps);

				sourceProject.IsNewProject = false;
			} finally {
				if (pi != null)
					pi.Dispose ();
				saving = false;
			}
		}

		bool saving;

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
			public MSBuildProjectInstance ProjectInstance;
			public HashSet<string> PreExistingProperties;
		}

		const string Unspecified = null;
		ITimeTracker timer;

		protected virtual void OnReadProjectHeader (ProgressMonitor monitor, MSBuildProject msproject)
		{
			timer = Counters.ReadMSBuildProject.BeginTiming ();

			ToolsVersion = msproject.ToolsVersion;
			if (string.IsNullOrEmpty (ToolsVersion))
				ToolsVersion = "2.0";

			productVersion = msproject.EvaluatedProperties.GetValue ("ProductVersion");
			schemaVersion = msproject.EvaluatedProperties.GetValue ("SchemaVersion");

			// Get the project ID

			string itemGuid = msproject.EvaluatedProperties.GetValue ("ProjectGuid");
			if (itemGuid == null)
				throw new UserException ("Project file doesn't have a valid ProjectGuid");

			// Workaround for a VS issue. VS doesn't include the curly braces in the ProjectGuid
			// of shared projects.
			if (!itemGuid.StartsWith ("{", StringComparison.Ordinal))
				itemGuid = "{" + itemGuid + "}";

			ItemId = itemGuid.ToUpper ();

			// Get the project GUIDs

			string projectTypeGuids = msproject.EvaluatedProperties.GetValue ("ProjectTypeGuids");

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
			disableFastUpToDateCheck = msproject.EvaluatedProperties.GetValue ("DisableFastUpToDateCheck", false);

			msproject.EvaluatedProperties.ReadObjectProperties (this, GetType (), true);

			RemoveDuplicateItems (msproject);
		}

		protected virtual void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			timer.Trace ("Read project items");
			LoadProjectItems (msproject, ProjectItemFlags.None, usedMSBuildItems);

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

				LoadConfiguration (monitor, cgrp, conf, platform);

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

							LoadConfiguration (monitor, cgrp, conf, platform);

							handledConfigurations.Add (key);
						}
					} else if (cgrp.Config == Unspecified && cgrp.Platform != Unspecified) {
						string platform = cgrp.Platform;

						foreach (var conf in configurations) {
							string key = conf + "|" + platform;

							if (handledConfigurations.Contains (key))
								continue;

							LoadConfiguration (monitor, cgrp, conf, platform);

							handledConfigurations.Add (key);
						}
					}
				}
			}

			// Read extended properties

			timer.Trace ("Read extended properties");

			msproject.ReadExternalProjectProperties (this, GetType (), true);

			// Read available item types

			loadedAvailableItemNames = msproject.EvaluatedItems.Where (i => i.Name == "AvailableItemName").Select (i => i.Include).ToArray ();
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

		void LoadConfiguration (ProgressMonitor monitor, ConfigData cgrp, string conf, string platform)
		{
			ProjectConfiguration config = (ProjectConfiguration) CreateConfiguration (conf);

			// If the group is not fully specified it is not assigned to the configuration.
			// In that case, a new group will be created
			if (cgrp.FullySpecified)
				config.Properties = cgrp.Group;

			var pi = CreateProjectInstaceForConfiguration (conf, platform);

			config.Platform = platform;
			projectExtension.OnReadConfiguration (monitor, config, pi.EvaluatedProperties);
			Configurations.Add (config);
		}

		MSBuildProjectInstance CreateProjectInstaceForConfiguration (string conf, string platform)
		{
			var t = System.Diagnostics.Stopwatch.StartNew ();
			var pi = sourceProject.CreateInstance ();
			pi.SetGlobalProperty ("Configuration", conf);
			if (platform == string.Empty)
				pi.SetGlobalProperty ("Platform", "AnyCPU");
			else
				pi.SetGlobalProperty ("Platform", platform);
			pi.OnlyEvaluateProperties = true;
			pi.Evaluate ();
			return pi;
		}

		protected virtual void OnReadConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IMSBuildEvaluatedPropertyCollection grp)
		{
			config.Read (grp, ToolsVersion);
		}

		void RemoveDuplicateItems (MSBuildProject msproject)
		{
/*			timer.Trace ("Checking for duplicate items");

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
				msproject.RemoveItem (t);*/
		}

		internal void LoadProjectItems (MSBuildProject msproject, ProjectItemFlags flags, HashSet<MSBuildItem> loadedItems)
		{
			if (loadedItems != null)
				loadedItems.Clear ();
			
			foreach (var buildItem in msproject.EvaluatedItemsIgnoringCondition) {
				if (buildItem.IsImported)
					continue;
				if (BuildAction.ReserverIdeActions.Contains (buildItem.Name))
					continue;
				ProjectItem it = ReadItem (buildItem);
				if (it == null)
					continue;
				it.Flags = flags;
				Items.Add (it);
				if (loadedItems != null)
					loadedItems.Add (buildItem.SourceItem);
			}
		}

		protected override void OnSetFormat (MSBuildFileFormat format)
		{
			base.OnSetFormat (format);
			InitFormatProperties ();
		}

		void InitFormatProperties ()
		{
			ToolsVersion = FileFormat.DefaultToolsVersion;
			schemaVersion = FileFormat.DefaultSchemaVersion;

			// Don't change the product version if it is already set. We don't really use this,
			// and we can avoid unnecessary changes in the proj file.
			if (string.IsNullOrEmpty (productVersion))
				productVersion = FileFormat.DefaultProductVersion;
		}

		internal ProjectItem ReadItem (IMSBuildItemEvaluated buildItem)
		{
			var item = CreateProjectItem (buildItem);
			item.Read (this, buildItem);
			item.BackingItem = buildItem.SourceItem;
			item.BackingEvalItem = buildItem;
			return item;
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
			if (string.IsNullOrEmpty (sourceProject.DefaultTargets) && SupportsBuild ())
				sourceProject.DefaultTargets = "Build";
			
			IMSBuildPropertySet globalGroup = msproject.GetGlobalPropertyGroup ();
			if (globalGroup == null)
				globalGroup = msproject.AddNewPropertyGroup (false);

			if (Configurations.Count > 0) {
				// Set the default configuration of the project.
				// First of all get the properties that define the default configuration and platform
				var defaultConfProp = globalGroup.GetProperties ().FirstOrDefault (p => p.Name == "Configuration" && IsDefaultSetter (p));
				var defaultPlatProp = globalGroup.GetProperties ().FirstOrDefault (p => p.Name == "Platform" && IsDefaultSetter (p));

				if (msproject.IsNewProject || (defaultConfProp != null && defaultPlatProp != null)) {
					// If there is no config property, or if the config doesn't exist anymore, give it a new value
					if (defaultConfProp == null || !Configurations.Any<SolutionItemConfiguration> (c => c.Name == defaultConfProp.Value)) {
						ItemConfiguration conf = Configurations.FirstOrDefault<ItemConfiguration> (c => c.Name == "Debug");
						if (conf == null) conf = Configurations [0];
						string platform = conf.Platform.Length == 0 ? "AnyCPU" : conf.Platform;
						globalGroup.SetValue ("Configuration", conf.Name, condition: " '$(Configuration)' == '' ");
						globalGroup.SetValue ("Platform", platform, condition: " '$(Platform)' == '' ");
					} else if (defaultPlatProp == null || !Configurations.Any<SolutionItemConfiguration> (c => c.Name == defaultConfProp.Value && c.Platform == defaultPlatProp.Value)) {
						ItemConfiguration conf = Configurations.FirstOrDefault<ItemConfiguration> (c => c.Name == defaultConfProp.Value);
						string platform = conf.Platform.Length == 0 ? "AnyCPU" : conf.Platform;
						globalGroup.SetValue ("Platform", platform, condition: " '$(Platform)' == '' ");
					}
				}
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
			} else if (!string.Equals (globalGroup.GetValue ("ProjectTypeGuids"), TypeGuid, StringComparison.OrdinalIgnoreCase)) {
				// Keep the property if it already was there with the same value, remove otherwise
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
			msproject.GetGlobalPropertyGroup ().SetValue ("DisableFastUpToDateCheck", disableFastUpToDateCheck, false);

			globalGroup.WriteObjectProperties (this, GetType (), true);
		}

		protected virtual void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			IMSBuildPropertySet globalGroup = msproject.GetGlobalPropertyGroup ();

			// Configurations

			if (Configurations.Count > 0) {
				
				List<ConfigData> configData = GetConfigData (msproject, true);

				try {
					// Write configuration data, creating new property groups if necessary

					foreach (ProjectConfiguration conf in Configurations) {

						MSBuildPropertyGroup pg = (MSBuildPropertyGroup)conf.Properties;
						ConfigData cdata = configData.FirstOrDefault (cd => cd.Group == pg);

						var pi = CreateProjectInstaceForConfiguration (conf.Name, conf.Platform);

						if (cdata == null) {
							msproject.AddPropertyGroup (pg, true);
							pg.IgnoreDefaultValues = true;
							pg.Condition = BuildConfigCondition (conf.Name, conf.Platform);
							cdata = new ConfigData (conf.Name, conf.Platform, pg);
							cdata.IsNew = true;
							configData.Add (cdata);
						} else {
							// The configuration name may have changed
							if (cdata.Config != conf.Name || cdata.Platform != conf.Platform) {
								((MSBuildPropertyGroup)cdata.Group).Condition = BuildConfigCondition (conf.Name, conf.Platform);
								cdata.Config = conf.Name;
								cdata.Platform = conf.Platform;
							}
						}
						cdata.ProjectInstance = pi;

						((MSBuildPropertyGroup)cdata.Group).IgnoreDefaultValues = true;
						cdata.Exists = true;
						cdata.PreExistingProperties = new HashSet<string> (cdata.Group.GetProperties ().Select (p => p.Name));
						ProjectExtension.OnWriteConfiguration (monitor, conf, cdata.Group);
					}

					// Find the properties in all configurations that have the MergeToProject flag set
					var mergeToProjectProperties = new HashSet<MergedProperty> (GetMergeToProjectProperties (configData));
					var mergeToProjectPropertyValues = new Dictionary<string, MergedPropertyValue> ();

					foreach (ProjectConfiguration conf in Configurations) {
						ConfigData cdata = FindPropertyGroup (configData, conf);
						var propGroup = (MSBuildPropertyGroup)cdata.Group;
						CollectMergetoprojectProperties (propGroup, mergeToProjectProperties, mergeToProjectPropertyValues);
					}

					foreach (ProjectConfiguration conf in Configurations) {
						ConfigData cdata = FindPropertyGroup (configData, conf);
						var propGroup = (MSBuildPropertyGroup)cdata.Group;

						cdata.PreExistingProperties.UnionWith (mergeToProjectPropertyValues.Select (p => p.Key));
						propGroup.UnMerge (cdata.ProjectInstance.EvaluatedProperties, cdata.PreExistingProperties);
						propGroup.IgnoreDefaultValues = false;
					}

					// Move properties with common values from configurations to the main
					// property group
					foreach (KeyValuePair<string, MergedPropertyValue> prop in mergeToProjectPropertyValues) {
						if (!prop.Value.IsDefault)
							globalGroup.SetValue (prop.Key, prop.Value.XmlValue, preserveExistingCase: prop.Value.PreserveExistingCase);
						else {
							// if the value is default, only remove the property if it was not already the default to avoid unnecessary project file churn
							globalGroup.SetValue (prop.Key, prop.Value.XmlValue, defaultValue: prop.Value.XmlValue, preserveExistingCase: prop.Value.PreserveExistingCase);
						}
					}
					foreach (SolutionItemConfiguration conf in Configurations) {
						var propGroup = FindPropertyGroup (configData, conf).Group;
						foreach (string mp in mergeToProjectPropertyValues.Keys)
							propGroup.RemoveProperty (mp);
					}

					// Remove groups corresponding to configurations that have been removed
					// or groups which don't have any property and did not already exist
					foreach (ConfigData cd in configData) {
						if ((!cd.Exists && cd.FullySpecified) || (cd.IsNew && !cd.Group.GetProperties ().Any ()))
							msproject.Remove ((MSBuildPropertyGroup)cd.Group);
					}
				} finally {
					foreach (var cd in configData)
						if (cd.ProjectInstance != null)
							cd.ProjectInstance.Dispose ();
				}
			}

			SaveProjectItems (monitor, msproject, usedMSBuildItems);

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
			msproject.WriteExternalProjectProperties (this, GetType (), true);
		}

		protected virtual void OnWriteConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IMSBuildPropertySet pset)
		{
			config.Write (pset, ToolsVersion);
		}

		IEnumerable<MergedProperty> GetMergeToProjectProperties (List<ConfigData> configData)
		{
			Dictionary<string,MergedProperty> mergeProps = new Dictionary<string, MergedProperty> ();
			foreach (var cd in configData.Where (d => d.FullySpecified)) {
				foreach (var prop in cd.Group.GetProperties ()) {
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

		bool IsDefaultSetter (MSBuildProperty prop)
		{
			var val = prop.Condition;
			int i = val.IndexOf ("==");
			if (i == -1)
				return false;
			return val.Substring (0, i).Trim () == "'$(" + prop.Name + ")'" && val.Substring (i + 2).Trim () == "''";
		}

		class ExpandedItemList: List<MSBuildItem>
		{
			public bool Modified { get; set; }
		}

		HashSet<MSBuildItem> usedMSBuildItems = new HashSet<MSBuildItem> ();

		internal virtual void SaveProjectItems (ProgressMonitor monitor, MSBuildProject msproject, HashSet<MSBuildItem> loadedItems, string pathPrefix = null)
		{
			HashSet<MSBuildItem> unusedItems = new HashSet<MSBuildItem> (loadedItems);
			Dictionary<MSBuildItem,ExpandedItemList> expandedItems = new Dictionary<MSBuildItem, ExpandedItemList> ();

			// Add the new items

			foreach (ProjectItem ob in Items.Where (it => !it.Flags.HasFlag (ProjectItemFlags.DontPersist)))
				SaveProjectItem (monitor, msproject, ob, expandedItems, unusedItems, loadedItems, pathPrefix);

			// Process items generated from wildcards

			foreach (var itemInfo in expandedItems) {
				if (itemInfo.Value.Modified || msproject.EvaluatedItemsIgnoringCondition.Where (i => i.SourceItem == itemInfo.Key).Count () != itemInfo.Value.Count) {
					// Expand the list
					unusedItems.Add (itemInfo.Key);
					foreach (var it in itemInfo.Value)
						msproject.AddItem (it);
				}
			}

			// Remove unused items

			foreach (var it in unusedItems) {
				if (it.ParentGroup != null) // It may already have been deleted
					msproject.RemoveItem (it);
				loadedItems.Remove (it);
			}
		}

		void SaveProjectItem (ProgressMonitor monitor, MSBuildProject msproject, ProjectItem item, Dictionary<MSBuildItem,ExpandedItemList> expandedItems, HashSet<MSBuildItem> unusedItems, HashSet<MSBuildItem> loadedItems, string pathPrefix = null)
		{
			if (item.IsFromWildcardItem) {
				// Store the item in the list of expanded items
				ExpandedItemList items;
				if (!expandedItems.TryGetValue (item.BackingItem, out items))
					items = expandedItems [item.BackingItem] = new ExpandedItemList ();

				// We need to check if the item has changed, in which case all the items included by the wildcard
				// must be individually included
				var bitem = msproject.CreateItem (item.ItemName, GetPrefixedInclude (pathPrefix, item.Include));
				item.Write (this, bitem);
				items.Add (bitem);

				unusedItems.Remove (item.BackingItem);

				if (!items.Modified && (item.Metadata.PropertyCountHasChanged || !ItemsAreEqual (bitem, item.BackingEvalItem)))
					items.Modified = true;
				return;
			}

			var include = GetPrefixedInclude (pathPrefix, item.UnevaluatedInclude ?? item.Include);

			MSBuildItem buildItem;
			if (item.BackingItem != null && item.BackingItem.Name == item.ItemName) {
				buildItem = item.BackingItem;
			} else {
				buildItem = msproject.AddNewItem (item.ItemName, include);
				item.BackingItem = buildItem;
				item.BackingEvalItem = null;
			}

			loadedItems.Add (buildItem);
			unusedItems.Remove (buildItem);

			item.Write (this, buildItem);
			if (buildItem.Include != include)
				buildItem.Include = include;
		}

		bool ItemsAreEqual (MSBuildItem item, IMSBuildItemEvaluated evalItem)
		{
			// Compare only metadata, since item name and include can't change

			foreach (var p in item.Metadata.GetProperties ()) {
				if (!object.Equals (p.Value, evalItem.Metadata.GetValue (p.Name)))
					return false;
			}
			return true;
		}

		string GetPrefixedInclude (string pathPrefix, string include)
		{
			if (pathPrefix != null && !include.StartsWith (pathPrefix))
				return pathPrefix + include;
			else
				return include;
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
				return Project.OnGetIsCompileable (fileName);
			}

			internal protected override bool OnGetIsCompileBuildAction (string buildAction)
			{
				return Project.OnGetIsCompileBuildAction (buildAction);
			}

			internal protected override void OnGetTypeTags (HashSet<string> types)
			{
				Project.OnGetTypeTags (types);
			}

			internal protected override Task<TargetEvaluationResult> OnRunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
			{
				return Project.DoRunTarget (monitor, target, configuration, context);
			}

			internal protected override bool OnGetSupportsTarget (string target)
			{
				return Project.OnGetSupportsTarget (target);
			}

			internal protected override string OnGetDefaultBuildAction (string fileName)
			{
				return Project.OnGetDefaultBuildAction (fileName);
			}

			internal protected override IEnumerable<string> OnGetStandardBuildActions ()
			{
				return Project.OnGetStandardBuildActions ();
			}

			internal protected override IList<string> OnGetCommonBuildActions ()
			{
				return Project.OnGetCommonBuildActions ();
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
				return Project.OnGetOutputFileName (configuration);
			}

			internal protected override string[] SupportedLanguages {
				get {
					return Project.OnGetSupportedLanguages ();
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

			internal protected override void OnReadProjectHeader (ProgressMonitor monitor, MSBuildProject msproject)
			{
				Project.OnReadProjectHeader (monitor, msproject);
			}

			internal protected override void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
			{
				Project.OnReadProject (monitor, msproject);
			}

			internal protected override void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
			{
				Project.OnWriteProject (monitor, msproject);
			}

			internal protected override void OnReadConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IMSBuildEvaluatedPropertyCollection grp)
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

			internal protected override void OnPrepareForEvaluation (MSBuildProject project)
			{
				Project.OnPrepareForEvaluation (project);
			}

			internal protected override bool OnFastCheckNeedsBuild (ConfigurationSelector configuration)
			{
				return Project.OnFastCheckNeedsBuild (configuration);
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

	public static class ProjectExtensions
	{
		/// <summary>
		/// Given a project, if the project implements the specified flavor type, this
		/// method returns the flavor instance. It returns null if the project is null or
		/// if the project doesn't implement the flavor.
		/// </summary>
		public static T AsFlavor<T> (this Project project) where T:ProjectExtension
		{
			return project != null ? project.GetFlavor<T> () : null;
		}
	}
}
