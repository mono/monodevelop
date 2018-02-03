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
using MonoDevelop.Projects.MSBuild;
using System.Xml;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects.Extensions;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

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
		MSBuildProject userProject;

		string productVersion;
		string schemaVersion;
		bool modifiedInMemory;
		ProjectExtension projectExtension;
		RunConfigurationCollection runConfigurations;
		bool defaultRunConfigurationCreated;

		List<string> defaultImports;

		ProjectItemCollection items;
		List<string> projectCapabilities;

		IEnumerable<string> loadedAvailableItemNames = ImmutableList<string>.Empty;

		protected Project ()
		{
			runConfigurations = new RunConfigurationCollection (this);
			items = new ProjectItemCollection (this);
			FileService.FileChanged += HandleFileChanged;
			files = new ProjectFileCollection ();
			Items.Bind (files);
			DependencyResolutionEnabled = true;
        }

		public ProjectItemCollection Items {
			get { return items; }
		}

		public RunConfigurationCollection RunConfigurations {
			get {
				CreateDefaultConfiguration ();
				return runConfigurations; 
			}
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
					// Configure target framework here for projects that target multiple frameworks so the
					// project capabilities are correct when they are initialized in InitBeforeProjectExtensionLoad.
					ConfigureActiveTargetFramework ();
					projectTypeGuids = sourceProject.EvaluatedProperties.GetValue ("ProjectTypeGuids");
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

			// Loads minimal data required to instantiate extensions and prepare for project loading
			InitBeforeProjectExtensionLoad ();
		}

		/// <summary>
		/// Initialization to be done before extensions are loaded
		/// </summary>
		void InitBeforeProjectExtensionLoad ()
		{
			var ggroup = sourceProject.GetGlobalPropertyGroup ();
			// Avoid crash if there is not global group
			if (ggroup == null)
				ggroup = sourceProject.AddNewPropertyGroup (false);

			// Load the evaluated properties
			InitMainGroupProperties (ggroup);

			// Capabilities have to be loaded here since extensions may be activated or deactivated depending on them
			LoadProjectCapabilities ();
		}

		void InitMainGroupProperties (MSBuildPropertyGroup globalGroup)
		{
			// Create a project instance to be used for comparing old and new values in the global property group
			// We use a dummy configuration and platform to avoid loading default values from the configurations
			// while evaluating
			var c = Guid.NewGuid ().ToString ();
			using (var pi = CreateProjectInstaceForConfiguration (c, c))
				mainGroupProperties = pi.GetPropertiesLinkedToGroup (globalGroup);
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

		public IEnumerable<string> FlavorGuids {
			get { return flavorGuids; }
		}

		public IPropertySet ProjectProperties {
			get { return mainGroupProperties ?? MSBuildProject.GetGlobalPropertyGroup (); }
		}

		public MSBuildProject MSBuildProject {
			get {
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

		new public ProjectConfiguration CreateConfiguration (string name, string platform, ConfigurationKind kind = ConfigurationKind.Blank)
		{
			return (ProjectConfiguration) base.CreateConfiguration (name, platform, kind);
		}

		new public ProjectConfiguration CreateConfiguration (string id, ConfigurationKind kind = ConfigurationKind.Blank)
		{
			return (ProjectConfiguration) base.CreateConfiguration (id, kind);
		}

		new public ProjectConfiguration CloneConfiguration (SolutionItemConfiguration configuration, string newName, string newPlatform)
		{
			return (ProjectConfiguration) base.CloneConfiguration (configuration, newName, newPlatform);
		}

		new public ProjectConfiguration CloneConfiguration (SolutionItemConfiguration configuration, string newId)
		{
			return (ProjectConfiguration) base.CloneConfiguration (configuration, newId);
		}

		protected override void OnConfigurationAdded (ConfigurationEventArgs args)
		{
			var conf = (ProjectConfiguration)args.Configuration;

			// Initialize the property group only if the project is not being loaded (in which case it will
			// be initialized by the ReadProject method) or if the project is new (because it will be initialized
			// after the project is fully written, since only then all imports are in place
			if (!Loading && !sourceProject.IsNewProject)
				InitConfiguration (conf);

			base.OnConfigurationAdded (args);
		}

		void InitConfiguration (ProjectConfiguration conf)
		{
			var pi = CreateProjectInstaceForConfiguration (conf.Name, conf.Platform);
			conf.Properties = pi.GetPropertiesLinkedToGroup (conf.MainPropertyGroup);
			conf.ProjectInstance = pi;
		}

		protected override void OnConfigurationRemoved (ConfigurationEventArgs args)
		{
			var conf = (ProjectConfiguration) args.Configuration;
			if (conf.ProjectInstance != null) {
				// Dispose the project instance that was used to load the configuration
				conf.Properties = conf.MainPropertyGroup;
				conf.ProjectInstance.Dispose ();
				conf.ProjectInstance = null;
			}
			base.OnConfigurationRemoved (args);
		}

		protected override void OnItemReady ()
		{
			base.OnItemReady ();
		}

		internal virtual void ImportDefaultRunConfiguration (ProjectRunConfiguration config)
		{
		}

		public ProjectRunConfiguration CreateRunConfiguration (string name)
		{
			var c = CreateRunConfigurationInternal (name);

			// When creating a ProcessRunConfiguration, set the value of ExternalConsole and PauseConsoleOutput from the default configuration
			var pc = c as ProcessRunConfiguration;
			if (pc != null) {
				var dc = RunConfigurations.FirstOrDefault (rc => rc.IsDefaultConfiguration) as ProcessRunConfiguration;
				if (dc != null) {
					pc.ExternalConsole = dc.ExternalConsole;
					pc.PauseConsoleOutput = dc.PauseConsoleOutput;
				}
			}
			return c;
		}

		ProjectRunConfiguration CreateRunConfigurationInternal (string name)
		{
			var c = CreateUninitializedRunConfiguration (name);
			c.Initialize (this);
			return c;
		}

		public ProjectRunConfiguration CreateUninitializedRunConfiguration (string name)
		{
			return ProjectExtension.OnCreateRunConfiguration (name);
		}

		public ProjectRunConfiguration CloneRunConfiguration (ProjectRunConfiguration runConfig)
		{
			var clone = CreateUninitializedRunConfiguration (runConfig.Name);
			clone.CopyFrom (runConfig, false);
			return clone;
		}

		public ProjectRunConfiguration CloneRunConfiguration (ProjectRunConfiguration runConfig, string newName)
		{
			var clone = CreateUninitializedRunConfiguration (newName);
			clone.CopyFrom (runConfig, true);
			return clone;
		}

		void CreateDefaultConfiguration ()
		{
			// If the project doesn't have a Default run configuration, create one
			if (!defaultRunConfigurationCreated) {
				defaultRunConfigurationCreated = true;
				if (!runConfigurations.Any (c => c.IsDefaultConfiguration)) {
					var rc = CreateRunConfigurationInternal ("Default");
					ImportDefaultRunConfiguration (rc);
					runConfigurations.Insert (0, rc);
				}
			}
		}

		protected override IEnumerable<SolutionItemRunConfiguration> OnGetRunConfigurations ()
		{
			return RunConfigurations;
		}

		protected virtual void OnGetDefaultImports (List<string> imports)
		{
		}

		public string ToolsVersion { get; private set; }

		internal bool CheckAllFlavorsSupported ()
		{
			return FlavorGuids.All (ProjectExtension.SupportsFlavor);
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
				ClearCachedData ().Ignore ();
			}
			base.OnModified (args);
		}

		protected override Task OnLoad (ProgressMonitor monitor)
		{
			return Task.Run (async delegate {
				await LoadAsync (monitor);
			});
		}

		async Task LoadAsync (ProgressMonitor monitor)
		{
			if (sourceProject == null || sourceProject.IsNewProject) {
				sourceProject = await MSBuildProject.LoadAsync (FileName).ConfigureAwait (false);
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
		}

		void LoadProjectCapabilities ()
		{
			projectCapabilities = sourceProject.EvaluatedItems.Where (it => it.Name == "ProjectCapability").Select (it => it.Include.Trim ()).Where (s => s.Length > 0).Distinct ().ToList ();
		}

		/// <summary>
		/// Runs the generator target and sends file change notifications if any files were modified, returns the build result
		/// </summary>
		public Task<TargetEvaluationResult> PerformGeneratorAsync (ConfigurationSelector configuration, string generatorTarget)
		{
			return BindTask<TargetEvaluationResult> (async cancelToken => {
				var cancelSource = new CancellationTokenSource ();
				cancelToken.Register (() => cancelSource.Cancel ());

				using (var monitor = new ProgressMonitor (cancelSource)) {
					return await this.PerformGeneratorAsync (monitor, configuration, generatorTarget);
				}
			});
		}

		/// <summary>
		/// Runs the generator target and sends file change notifications if any files were modified, returns the build result
		/// </summary>
		public async Task<TargetEvaluationResult> PerformGeneratorAsync (ProgressMonitor monitor, ConfigurationSelector configuration, string generatorTarget)
		{
			var fileInfo = await GetProjectFileTimestamps (monitor, configuration);
			var evalResult = await this.RunTarget (monitor, generatorTarget, configuration);
			SendFileChangeNotifications (monitor, configuration, fileInfo);

			return evalResult;
		}

		/// <summary>
		/// Returns a list containing FileInfo for all the source files in the project
		/// </summary>
		async Task<List<FileInfo>> GetProjectFileTimestamps (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			var infoList = new List<FileInfo> ();
			var projectFiles = await this.GetSourceFilesAsync (monitor, configuration);

			foreach (var projectFile in projectFiles) {
				var info = new FileInfo (projectFile.FilePath);
				infoList.Add (info);
				info.Refresh ();
			}

			return infoList;
		}

		/// <summary>
		/// Sends a file change notification via FileService for any file that has changed since the timestamps in beforeFileInfo
		/// </summary>
		void SendFileChangeNotifications (ProgressMonitor monitor, ConfigurationSelector configuration, List<FileInfo> beforeFileInfo)
		{
			var changedFiles = new List<FileInfo> ();

			foreach (var file in beforeFileInfo) {
				var info = new FileInfo (file.FullName);

				if (file.Exists && info.Exists) {
					if (file.LastWriteTime != info.LastWriteTime) {
						changedFiles.Add (info);
					}
				} else if (info.Exists) {
					changedFiles.Add (info);
				} else if (file.Exists) {
					// not sure if this should or could happen, it doesn't really make much sense
					FileService.NotifyFileRemoved (file.FullName);
				}
			}

			FileService.NotifyFilesChanged (changedFiles.Select (cf => new FilePath (cf.FullName)));
		}

		/// <summary>
		/// Gets the source files that are included in the project, including any that are added by `CoreCompileDependsOn`
		/// </summary>
		public Task<ProjectFile[]> GetSourceFilesAsync (ConfigurationSelector configuration)
		{
			if (sourceProject == null)
				return Task.FromResult (new ProjectFile [0]);

			return BindTask<ProjectFile []> (cancelToken => {
				var cancelSource = new CancellationTokenSource ();
				cancelToken.Register (() => cancelSource.Cancel ());

				using (var monitor = new ProgressMonitor (cancelSource)) {
					return GetSourceFilesAsync (monitor, configuration);
				}
			});
		}

		/// <summary>
		/// Gets the source files that are included in the project, including any that are added by `CoreCompileDependsOn`
		/// </summary>
		public Task<ProjectFile []> GetSourceFilesAsync (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return ProjectExtension.OnGetSourceFiles (monitor, configuration);
		}

		/// <summary>
		/// Gets the source files that are included in the project, including any that are added by `CoreCompileDependsOn`
		/// </summary>
		protected virtual async Task<ProjectFile[]> OnGetSourceFiles (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			// pre-load the results with the current list of files in the project
			var results = new List<ProjectFile> ();

			var buildActions = GetBuildActions ().Where (a => a != "Folder" && a != "--").ToArray ();

			var config = configuration != null ? GetConfiguration (configuration) : null;
			var pri = await CreateProjectInstaceForConfigurationAsync (config?.Name, config?.Platform, false);
			foreach (var it in pri.EvaluatedItems.Where (i => buildActions.Contains (i.Name)))
				results.Add (CreateProjectFile (it));

			// add in any compile items that we discover from running the CoreCompile dependencies
			var evaluatedCompileItems = await GetCompileItemsFromCoreCompileDependenciesAsync (monitor, configuration);
			var addedItems = evaluatedCompileItems.Where (i => results.All (pi => pi.FilePath != i.FilePath)).ToList ();
			results.AddRange (addedItems);

			return results.ToArray ();
		}

		object evaluatedCompileItemsLock = new object ();
		string evaluatedCompileItemsConfiguration;
		TaskCompletionSource<ProjectFile[]> evaluatedCompileItemsTask;

		/// <summary>
		/// Gets the list of files that are included as Compile items from the evaluation of the CoreCompile dependecy targets
		/// </summary>
		async Task<ProjectFile[]> GetCompileItemsFromCoreCompileDependenciesAsync (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			var config = configuration != null ? GetConfiguration (configuration) : DefaultConfiguration;
			if (config == null)
				return new ProjectFile [0];

			// Check if there is already a task for getting the items for the provided configuration

			TaskCompletionSource<ProjectFile []> currentTask = null;
			bool startTask = false;

			lock (evaluatedCompileItemsLock) {
				if (evaluatedCompileItemsConfiguration != config.Id) {
					// The configuration changed or query not yet done
					evaluatedCompileItemsConfiguration = config.Id;
					evaluatedCompileItemsTask = new TaskCompletionSource<ProjectFile []> ();
					startTask = true;
				}
				currentTask = evaluatedCompileItemsTask;
			}

			if (startTask) {
				var coreCompileDependsOn = sourceProject.EvaluatedProperties.GetValue<string> ("CoreCompileDependsOn");

				if (string.IsNullOrEmpty (coreCompileDependsOn)) {
					currentTask.SetResult (new ProjectFile [0]);
					return currentTask.Task.Result;
				}

				ProjectFile [] result = null;
				var dependsList = string.Join (";", coreCompileDependsOn.Split (new [] { ";" }, StringSplitOptions.RemoveEmptyEntries).Select (s => s.Trim ()).Where (s => s.Length > 0));
				try {
					// evaluate the Compile targets
					var ctx = new TargetEvaluationContext ();
					ctx.ItemsToEvaluate.Add ("Compile");
					ctx.BuilderQueue = BuilderQueue.ShortOperations;

					var evalResult = await this.RunTarget (monitor, dependsList, config.Selector, ctx);
					if (evalResult != null && evalResult.Items != null) {
						result = evalResult
							.Items
							.Select (CreateProjectFile)
							.ToArray ();
					}
				} catch (Exception ex) {
					LoggingService.LogInternalError (string.Format ("Error running target {0}", dependsList), ex);
				}
				currentTask.SetResult (result ?? new ProjectFile [0]);
			}

			return await currentTask.Task;
		}

		void ResetCachedCompileItems ()
		{
			lock (evaluatedCompileItemsLock) {
				evaluatedCompileItemsConfiguration = null;
			}
		}

		ProjectFile CreateProjectFile (IMSBuildItemEvaluated item)
		{
			return new ProjectFile (MSBuildProjectService.FromMSBuildPath (sourceProject.BaseDirectory, item.Include), item.Name) { Project = this };
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

		internal protected override async Task OnSave (ProgressMonitor monitor)
		{
			SetFastBuildCheckDirty ();
			modifiedInMemory = false;

			string content = await WriteProjectAsync (monitor);

			// Doesn't save the file to disk if the content did not change
			if (await sourceProject.SaveAsync (FileName, content)) {
				if (userProject != null) {
					if (!userProject.GetAllObjects ().Any ())
						File.Delete (userProject.FileName);
					else
						await userProject.SaveAsync (userProject.FileName);
				}

				await ClearCachedData ();
				RefreshProjectBuilder ().Ignore ();
			}
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

			InitializeFileWatcher ();
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

		public IEnumerable<string> GetProjectCapabilities ()
		{
			return (IEnumerable<string>)projectCapabilities ?? ImmutableList<string>.Empty;
		}

		/// <summary>
		/// Checks if the project has a capability or a combination of capabilities (including AND, OR, NOT logic).
		/// </summary>
		/// <returns><c>true</c> if the project has the required capabilities.</returns>
		/// <param name="capabilityExpression">Expression of capabilities</param>
		/// <remarks>The expression can be something like "(VisualC | CSharp) + (MSTest | NUnit)".
		/// The "|" is the OR operator. The "&amp;" and "+" characters are both AND operators.
		/// The "!" character is the NOT operator. Parentheses force evaluation precedence order.
		/// A null or empty expression is evaluated as a match.</remarks>
		public bool IsCapabilityMatch (string capabilityExpression)
		{
			return SimpleExpressionEvaluator.Evaluate (capabilityExpression, (IList<string>)projectCapabilities ?? ImmutableList<string>.Empty);
		}

		public event EventHandler ProjectCapabilitiesChanged;

		void NotifyProjectCapabilitiesChanged ()
		{
			ProjectCapabilitiesChanged?.Invoke (this, EventArgs.Empty);
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
			DisposeFileWatcher ();

			foreach (ProjectConfiguration c in Configurations)
				c.ProjectInstance?.Dispose ();
			
			foreach (var item in items) {
				IDisposable disp = item as IDisposable;
				if (disp != null)
					disp.Dispose ();
			}

			FileService.FileChanged -= HandleFileChanged;
			RemoteBuildEngineManager.UnloadProject (FileName).Ignore ();

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
		public Task<TargetEvaluationResult> RunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context = null)
		{
			// Initialize the evaluation context. This initialization is shared with FastCheckNeedsBuild.
			// Extenders will override OnConfigureTargetEvaluationContext to add custom properties and do other
			// initializations required by MSBuild.
			context = ProjectExtension.OnConfigureTargetEvaluationContext (target, configuration, context ?? new TargetEvaluationContext ());

			return ProjectExtension.OnRunTarget (monitor, target, configuration, context);
		}

		public bool SupportsTarget (string target)
		{
			return !IsUnsupportedProject && ProjectExtension.OnGetSupportsTarget (target);
		}

		protected virtual bool OnGetSupportsTarget (string target)
		{
			return sourceProject.EvaluatedTargetsIgnoringCondition.Any (t => t.Name == target);
		}

		protected virtual bool OnGetSupportsImportedItem (IMSBuildItemEvaluated buildItem)
		{
			return false;
		}

		/// <summary>
		/// Initialize the evaluation context that is going to be used to execute an MSBuild target.
		/// </summary>
		/// <returns>The updated context.</returns>
		/// <param name="target">Target.</param>
		/// <param name="configuration">Configuration.</param>
		/// <param name="context">Context.</param>
		/// <remarks>
		/// This method can be overriden to add custom properties and do other initializations on the evaluation
		/// context. The method is always called before executing OnRunTarget and other methods that do
		/// target evaluations. The method can modify the provided context instance and return it, or it can
		/// create a new instance.
		/// </remarks>
		protected virtual TargetEvaluationContext OnConfigureTargetEvaluationContext (string target, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			return context;
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
			if (configuration == null) {
				throw new ArgumentNullException ("configuration");
			}
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
				if (tr != null)
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
				var includeReferencedProjects = context != null ? context.LoadReferencedProjects : false;
				var configs = GetConfigurations (configuration, includeReferencedProjects);	

				string [] evaluateItems = context != null ? context.ItemsToEvaluate.ToArray () : new string [0];
				string [] evaluateProperties = context != null ? context.PropertiesToEvaluate.ToArray () : new string [0];

				var globalProperties = CreateGlobalProperties ();
				if (context != null) {
					var md = (ProjectItemMetadata)context.GlobalProperties;
					md.SetProject (sourceProject);
					foreach (var p in md.GetProperties ())
						globalProperties [p.Name] = p.Value;
				}

				MSBuildResult result = null;
				await Task.Run (async delegate {

					bool operationRequiresExclusiveLock = context.BuilderQueue == BuilderQueue.LongOperations;
					TimerCounter buildTimer = null;
					switch (target) {
					case "Build": buildTimer = Counters.BuildMSBuildProjectTimer; break;
					case "Clean": buildTimer = Counters.CleanMSBuildProjectTimer; break;
					}

					var metadata = GetProjectEventMetadata (configuration);
					var t1 = Counters.RunMSBuildTargetTimer.BeginTiming (metadata);
					var t2 = buildTimer != null ? buildTimer.BeginTiming (metadata) : null;

					IRemoteProjectBuilder builder = await GetProjectBuilder (monitor.CancellationToken, context, setBusy:operationRequiresExclusiveLock).ConfigureAwait (false);

					string [] targets;
					if (target.IndexOf (';') != -1)
						targets = target.Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					else
						targets = new string [] { target };

					var logger = context.Loggers.Count != 1 ? new ProxyLogger (this, context.Loggers) : context.Loggers.First ();
					
					try {
						result = await builder.Run (configs, monitor.Log, logger, context.LogVerbosity, targets, evaluateItems, evaluateProperties, globalProperties, monitor.CancellationToken).ConfigureAwait (false);
					} finally {
						builder.Dispose ();
						t1.End ();
						if (t2 != null) {
							AddRunMSBuildTargetTimerMetadata (metadata, result, target, configuration);
							t2.End ();
							if (IsFirstBuild && target == "Build") {
								await Runtime.RunInMainThread (() => IsFirstBuild = false);
							}
						}
					}
				});

				var br = new BuildResult ();
				foreach (var err in result.Errors) {
					FilePath file = null;
					if (err.File != null)
						file = Path.Combine (Path.GetDirectoryName (err.ProjectFile ?? ItemDirectory.ToString ()), err.File);

					br.Append (new BuildError (file, err.LineNumber, err.ColumnNumber, err.Code, err.Message) {
						Subcategory = err.Subcategory,
						EndLine = err.EndLineNumber,
						EndColumn = err.EndColumnNumber,
						IsWarning = err.IsWarning,
						HelpKeyword = err.HelpKeyword,
					});
				}

				// Get the evaluated properties

				var properties = new Dictionary<string, IMSBuildPropertyEvaluated> ();
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
						properties = new Dictionary<string, IMSBuildPropertyEvaluated> ();
						foreach (var m in it.Metadata)
							properties [m.Key] = new MSBuildPropertyEvaluated (sourceProject, m.Key, m.Value, m.Value);
						imd.SetProperties (properties);
					}
					evItems.Add (eit);
				}

				return new TargetEvaluationResult (br, evItems, props);
			}
			else {
				RemoteBuildEngineManager.UnloadProject (FileName).Ignore ();
				if (this is DotNetProject) {
					var handler = new MonoDevelop.Projects.MD1.MD1DotNetProjectHandler ((DotNetProject)this);
					return new TargetEvaluationResult (await handler.RunTarget (monitor, target, configuration));
				}
			}
			return null;
		}

		/// <summary>
		/// Gets or sets the FirstBuild user property. This is true if this is a new
		/// project and has not yet been built.
		/// </summary>
		internal bool IsFirstBuild {
			get {
				return UserProperties.GetValue ("FirstBuild", false);
			}
			set {
				if (value) {
					UserProperties.SetValue ("FirstBuild", value);
				} else {
					UserProperties.RemoveValue ("FirstBuild");
				}
			}
		}

		void AddRunMSBuildTargetTimerMetadata (
			IDictionary<string, string> metadata,
			MSBuildResult result,
			string target,
			ConfigurationSelector configuration)
		{
			if (target == "Build") {
				metadata ["BuildType"] = "4";
			} else if (target == "Clean") {
				metadata ["BuildType"] = "1";
			}
			metadata ["BuildTypeString"] = target;

			metadata ["FirstBuild"] = IsFirstBuild.ToString ();
			metadata ["ProjectID"] = ItemId;
			metadata ["ProjectType"] = TypeGuid;
			metadata ["ProjectFlavor"] = FlavorGuids.FirstOrDefault () ?? TypeGuid;

			var c = GetConfiguration (configuration);
			if (c != null) {
				metadata ["Configuration"] = c.Id;
				metadata ["Platform"] = GetExplicitPlatform (c);
			}

			bool success = false;
			bool cancelled = false;

			if (result != null) {
				success = !result.Errors.Any (error => !error.IsWarning);

				if (!success) {
					cancelled = result.Errors [0].Message == "Build cancelled";
				}
			}

			metadata ["Success"] = success.ToString ();
			metadata ["Cancelled"] = cancelled.ToString ();
		}

		string activeTargetFramework;

		void ConfigureActiveTargetFramework ()
		{
			activeTargetFramework = GetActiveTargetFramework ();
			if (activeTargetFramework != null) {
				MSBuildProject.SetGlobalProperty ("TargetFramework", activeTargetFramework);
				MSBuildProject.Evaluate ();
			}
		}

		/// <summary>
		/// If an SDK project targets multiple target frameworks then this returns the first
		/// target framework. Otherwise it returns null. This also handles the odd case if
		/// the TargetFrameworks property is being used but only one framework is defined
		/// there. Since here an active target framework must be returned even though multiple
		/// target frameworks are not being used.
		/// </summary>
		string GetActiveTargetFramework ()
		{
			var frameworks = GetTargetFrameworks (MSBuildProject);
			if (frameworks != null && frameworks.Any ())
				return frameworks.FirstOrDefault ();

			return null;
		}

		/// <summary>
		/// Returns target frameworks defined in the TargetFrameworks property for SDK projects
		/// if the TargetFramework property is not defined. It returns null otherwise.
		/// </summary>
		static string[] GetTargetFrameworks (MSBuildProject project)
		{
			if (string.IsNullOrEmpty (project.Sdk))
				return null;

			var propertyGroup = project.GetGlobalPropertyGroup ();
			string propertyValue = propertyGroup.GetValue ("TargetFramework", null);
			if (propertyValue != null)
				return null;

			propertyValue = project.EvaluatedProperties.GetValue ("TargetFrameworks", null);
			if (propertyValue != null)
				return propertyValue.Split (new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

			return null;
		}

		internal Dictionary<string, string> CreateGlobalProperties ()
		{
			var properties = new Dictionary<string, string> ();
			string framework = activeTargetFramework;
			if (framework != null)
				properties ["TargetFramework"] = framework;
			return properties;
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

		AsyncCriticalSection builderLock = new AsyncCriticalSection ();

		internal async Task<IRemoteProjectBuilder> GetProjectBuilder (CancellationToken token, OperationContext context, bool setBusy = false, bool allowBusy = false)
		{
			TargetRuntime runtime = null;
			var ap = this as IAssemblyProject;
			runtime = ap != null ? ap.TargetRuntime : Runtime.SystemAssemblyService.CurrentRuntime;

			var sln = ParentSolution;
			var slnFile = sln != null ? sln.FileName : null;

			// Extract the session ID from the current build context, if there is one
			object buildSessionId = null;
			if (context != null)
				context.SessionData.TryGetValue (MSBuildSolutionExtension.MSBuildProjectOperationId, out buildSessionId);

			var builder = await RemoteBuildEngineManager.GetRemoteProjectBuilder (FileName, slnFile, runtime, ToolsVersion, RequiresMicrosoftBuild, buildSessionId, setBusy, allowBusy);

			if (modifiedInMemory) {
				modifiedInMemory = false;
				string content = await WriteProjectAsync (new ProgressMonitor ());
				try {
					await RemoteBuildEngineManager.RefreshProjectWithContent (FileName, content);
				} catch {
					builder.Dispose ();
					throw;
				}
			}
			return builder;
		}

		void GetReferencedSDKs (Project project, ref HashSet<string> sdks, HashSet<string> traversedProjects)
		{
			traversedProjects.Add (project.ItemId);

			var projectSdks = project.MSBuildProject.GetReferencedSDKs ();
			if (projectSdks.Length > 0) {
				if (sdks == null)
					sdks = new HashSet<string> ();
				sdks.UnionWith (projectSdks);
			}

			var dotNetProject = project as DotNetProject;
			if (dotNetProject == null)
				return;

			// Check project references.
			foreach (var projectReference in dotNetProject.References.Where (pr => pr.ReferenceType == ReferenceType.Project)) {
				if (traversedProjects.Contains (projectReference.ProjectGuid))
					continue;

				var p = projectReference.ResolveProject (ParentSolution);
				if (p != null)
					GetReferencedSDKs (p, ref sdks, traversedProjects);
			}
		}

		public Task RefreshProjectBuilder ()
		{
			return RemoteBuildEngineManager.RefreshProject (FileName);
		}

		public void ReloadProjectBuilder ()
		{
			RemoteBuildEngineManager.RefreshProject (FileName).Ignore ();
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

		bool requiresMicrosoftBuild;

		internal protected bool RequiresMicrosoftBuild {
			get {
				return requiresMicrosoftBuild || ProjectExtension.IsMicrosoftBuildRequired;
			}
			set {
				requiresMicrosoftBuild = value;
			}
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
				// Build is always a long operation. Make sure we build the project in the right builder.
				context.BuilderQueue = BuilderQueue.LongOperations;
				var result = await RunMSBuildTarget (monitor, "Build", configuration, context);
				if (!result.BuildResult.Failed)
					SetFastBuildCheckClean (configuration, context);
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
		
			monitor.Log.WriteLine (GettextCatalog.GetString ("Performing main compilation…"));
			
			BuildResult res = await DoBuild (monitor, configuration);

			if (res != null) {
				string errorString = GettextCatalog.GetPluralString ("{0} error", "{0} errors", res.ErrorCount, res.ErrorCount);
				string warningString = GettextCatalog.GetPluralString ("{0} warning", "{0} warnings", res.WarningCount, res.WarningCount);

				monitor.Log.WriteLine (GettextCatalog.GetString ("Build complete -- ") + errorString + ", " + warningString);
			}

			return new TargetEvaluationResult (res);
		}

		bool disableFastUpToDateCheck;

		// The configuration of the last build that completed successfully,
		// null if any file in the project has since changed
		string fastUpToDateCheckGoodConfig;

		// The global properties used in the last build
		IPropertySet fastUpToDateCheckGlobalProperties;

		// Timestamp of the last build
		DateTime fastUpToDateTimestamp;

		public bool FastCheckNeedsBuild (ConfigurationSelector configuration)
		{
			return FastCheckNeedsBuild (configuration, new TargetEvaluationContext ());
		}

		public bool FastCheckNeedsBuild (ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			// Initialize the evaluation context. This initialization is shared with RunTarget.
			// Extenders will override OnConfigureTargetEvaluationContext to add custom properties and do other
			// initializations required by MSBuild.
			context = ProjectExtension.OnConfigureTargetEvaluationContext ("Build", configuration, context ?? new TargetEvaluationContext ());
			return ProjectExtension.OnFastCheckNeedsBuild (configuration, context);
		}

		[Obsolete ("Use OnFastCheckNeedsBuild (configuration, TargetEvaluationContext)")]
		protected virtual bool OnFastCheckNeedsBuild (ConfigurationSelector configuration)
		{
			if (disableFastUpToDateCheck || fastUpToDateCheckGoodConfig == null)
				return true;
			var cfg = GetConfiguration (configuration);
			if (cfg == null || cfg.Id != fastUpToDateCheckGoodConfig)
				return true;

			return false;
		}

		/// <summary>
		/// Checks if this project needs to be built.
		/// </summary>
		/// <returns><c>true</c>, if the project is dirty and needs to be rebuilt, <c>false</c> otherwise.</returns>
		/// <param name="configuration">Build configuration.</param>
		/// <param name="context">Evaluation context.</param>
		/// <remarks>
		/// This method can be overriden to provide custom logic for checking if a project needs to be built, either
		/// due to changes in the content or in the configuration.
		/// </remarks>
		protected virtual bool OnFastCheckNeedsBuild (ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			// Chain the new OnFastCheckNeedsBuild override to the old one, so that extensions
			// using the old API keep working
#pragma warning disable 618
			if (ProjectExtension.OnFastCheckNeedsBuild (configuration))
				return true;
#pragma warning restore 618

			// Shouldn't need to build, but if a dependency was changed since this project build flag was reset,
			// the project needs to be rebuilt

			foreach (var dep in GetReferencedItems (configuration).OfType<Project> ()) {
				if (dep.FastCheckNeedsBuild (configuration, context) || dep.fastUpToDateTimestamp >= fastUpToDateTimestamp) {
					fastUpToDateCheckGoodConfig = null;
					return true;
				}
			}

			// Check if global properties have changed

			var cachedCount = fastUpToDateCheckGlobalProperties != null ? fastUpToDateCheckGlobalProperties.GetProperties ().Count () : 0;

			if (cachedCount != context.GlobalProperties.GetProperties ().Count ())
				return true;

			if (cachedCount == 0)
				return false;
			
			foreach (var p in context.GlobalProperties.GetProperties ()) {
				if (fastUpToDateCheckGlobalProperties.GetValue (p.Name) != p.Value)
					return true;
			}
			return false;
		}

		protected void SetFastBuildCheckDirty ()
		{
			fastUpToDateCheckGoodConfig = null;
		}
		
		void SetFastBuildCheckClean (ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			var cfg = GetConfiguration (configuration);
			fastUpToDateCheckGoodConfig = cfg != null ? cfg.Id : null;
			fastUpToDateCheckGlobalProperties = context.GlobalProperties;
			fastUpToDateTimestamp = DateTime.Now;
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
			if (configuration == null) {
				throw new ArgumentNullException ("configuration");
			}
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
				// Clean is considered a long operation. Make sure we build the project in the right builder.
				context.BuilderQueue = BuilderQueue.LongOperations;
				return await RunMSBuildTarget (monitor, "Clean", configuration, context);
			}
			
			monitor.Log.WriteLine ("Removing output files...");

			var filesToDelete = GetOutputFiles (configuration).ToArray ();

			await Task.Run (delegate {
				// Delete generated files
				foreach (FilePath file in filesToDelete) {
					if (File.Exists (file)) {
						file.Delete ();
						if (file.ParentDirectory.CanonicalPath != config.OutputDirectory.CanonicalPath && !Directory.EnumerateFiles (file.ParentDirectory).Any ())
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

		protected override Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			ProjectConfiguration config = GetConfiguration (configuration) as ProjectConfiguration;
			if (config == null)
				monitor.ReportError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", configuration, Name), null);
			return Task.FromResult (0);
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

		internal void NotifyItemsAdded (IEnumerable<ProjectItem> objs)
		{
			ProjectExtension.OnItemsAdded (objs);
		}

		internal void NotifyItemsRemoved (IEnumerable<ProjectItem> objs)
		{
			ProjectExtension.OnItemsRemoved (objs);
		}

		protected virtual void OnItemsAdded (IEnumerable<ProjectItem> objs)
		{
			foreach (var it in objs) {
				if (it.Project != null)
					throw new InvalidOperationException (it.GetType ().Name + " already belongs to a project");
				it.Project = this;
			}
		
			NotifyModified ("Items");
			if (ProjectItemAdded != null)
				ProjectItemAdded (this, new ProjectItemEventArgs (objs.Select (pi => new ProjectItemEventInfo (this, pi))));
		
			NotifyFileAddedToProject (objs.OfType<ProjectFile> ());
		}

		protected virtual void OnItemsRemoved (IEnumerable<ProjectItem> objs)
		{
			foreach (var it in objs)
				it.Project = null;
		
			NotifyModified ("Items");
			if (ProjectItemRemoved != null)
				ProjectItemRemoved (this, new ProjectItemEventArgs (objs.Select (pi => new ProjectItemEventInfo (this, pi))));
		
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

		IPropertySet mainGroupProperties;

		void ReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			if (File.Exists (msproject.FileName + ".user")) {
				userProject = new MSBuildProject (msproject.EngineManager);
				userProject.Load (msproject.FileName + ".user");
			}
			ProjectExtension.OnReadProjectHeader (monitor, msproject);
			modifiedInMemory = false;
			ProjectExtension.OnReadProject (monitor, msproject);
			NeedsReload = false;
		}

		AsyncCriticalSection writeProjectLock = new AsyncCriticalSection ();

		internal async Task<string> WriteProjectAsync (ProgressMonitor monitor)
		{
			using (await writeProjectLock.EnterAsync ().ConfigureAwait (false)) {
				return await Task.Run (() => {
					WriteProject (monitor);
					return sourceProject.SaveToString ();
				}).ConfigureAwait (false);
			}
		}

		ITimeTracker writeTimer;

		void WriteProject (ProgressMonitor monitor)
		{
			if (saving) {
				LoggingService.LogError ("WriteProject called while the project is already being written");
				return;
			}
			
			saving = true;

			writeTimer = Counters.WriteMSBuildProject.BeginTiming ();

			try {
				sourceProject.FileName = FileName;

				writeTimer.Trace ("Writing project header");
				OnWriteProjectHeader (monitor, sourceProject);

				writeTimer.Trace ("Writing project content");
				ProjectExtension.OnWriteProject (monitor, sourceProject);

				var globalGroup = sourceProject.GetGlobalPropertyGroup ();
				globalGroup.PurgeDefaultProperties ();
				globalGroup.ResetIsNewFlags ();

				if (sourceProject.IsNewProject) {
					// If the project is new, the evaluated properties lists are empty. Now that the project is saved,
					// those lists can be filled, so that the project is left in the same state it would have if it
					// was just loaded.
					sourceProject.Evaluate ();
					InitMainGroupProperties (globalGroup);
					foreach (ProjectConfiguration conf in Configurations)
						InitConfiguration (conf);
					foreach (var es in runConfigurations)
						InitRunConfiguration ((ProjectRunConfiguration)es);
				}

				sourceProject.IsNewProject = false;
				writeTimer.Trace ("Project written");
			} finally {
				writeTimer.End ();
				saving = false;
			}
		}

		bool saving;

		class ConfigData
		{
			public ConfigData (string conf, string plt, MSBuildPropertyGroup grp)
			{
				Config = conf;
				Platform = plt;
				Group = grp;
			}

			public string Config;
			public string Platform;
			public MSBuildPropertyGroup Group;
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

			productVersion = msproject.EvaluatedProperties.GetValue ("ProductVersion");
			schemaVersion = msproject.EvaluatedProperties.GetValue ("SchemaVersion");

			if (!IsReevaluating) {
				// Get the project ID

				string itemGuid = msproject.EvaluatedProperties.GetValue ("ProjectGuid");
				if (itemGuid == null)
					itemGuid = defaultItemId ?? Guid.NewGuid ().ToString ("B").ToUpper ();

				// Workaround for a VS issue. VS doesn't include the curly braces in the ProjectGuid
				// of shared projects.
				if (!itemGuid.StartsWith ("{", StringComparison.Ordinal))
					itemGuid = "{" + itemGuid + "}";

				ItemId = itemGuid.ToUpper ();
			}

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
		}

		protected virtual void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			timer.Trace ("Read project items");
			LoadProjectItems (msproject, ProjectItemFlags.None, usedMSBuildItems);
			loadedProjectItems = new HashSet<ProjectItem> (Items);

			timer.Trace ("Read configurations");

			List<ConfigData> configData = GetConfigData (msproject, true);

			var configs = new List<ProjectConfiguration> ();
			foreach (var cgrp in configData)
				configs.Add (LoadConfiguration (monitor, cgrp, cgrp.Config, cgrp.Platform));

			Configurations.SetItems (configs);

			timer.Trace ("Read run configurations");

			List<ConfigData> runConfigData = new List<ConfigData> ();
			GetRunConfigData (runConfigData, msproject, true);
			GetRunConfigData (runConfigData, userProject, true);

			var runConfigs = new List<ProjectRunConfiguration> ();
			foreach (var cgrp in runConfigData)
				runConfigs.Add (LoadRunConfiguration (monitor, cgrp, cgrp.Config));

			defaultRunConfigurationCreated = false;
			runConfigurations.SetItems (runConfigs);

			// Read extended properties

			timer.Trace ("Read extended properties");

			msproject.ReadExternalProjectProperties (this, GetType (), true);

			// Read available item types

			loadedAvailableItemNames = msproject.EvaluatedItems.Where (i => i.Name == "AvailableItemName").Select (i => i.Include).ToArray ();
		}

		List<ConfigData> GetConfigData (MSBuildProject msproject, bool includeEvaluated)
		{
			List<ConfigData> configData = new List<ConfigData> ();
			foreach (MSBuildPropertyGroup cgrp in msproject.PropertyGroups) {
				string conf, platform;
				if (ParseConfigCondition (cgrp.Condition, out conf, out platform) && conf != null && platform != null) {
					// If a group for this configuration already was found, set the new group. If there are changes we want to modify the last group.
					var existing = configData.FirstOrDefault (cd => cd.Config == conf && cd.Platform == platform);
					if (existing == null)
						configData.Add (new ConfigData (conf, platform, cgrp));
					else
						existing.Group = cgrp;
				}
			}
			if (includeEvaluated) {
				var confValues = msproject.ConditionedProperties.GetCombinedPropertyValues ("Configuration");
				var platValues = msproject.ConditionedProperties.GetCombinedPropertyValues ("Platform");
				var confPlatValues = msproject.ConditionedProperties.GetCombinedPropertyValues ("Configuration", "Platform");

				// First of all, add configurations that have been specified using both the Configuration and Platform properties.
				foreach (var co in confPlatValues) {
					var c = co.GetValue ("Configuration");
					var ep = co.GetValue ("Platform");
					ep = ep == "AnyCPU" ? "" : ep;
					if (!configData.Any (cd => cd.Config == c && cd.Platform == ep))
						configData.Add (new ConfigData (c, ep, null));
				}

				// Now add configurations for which a platform has not been specified, but only if no other configuration
				// exists with the same name. Combine them with individually specified platforms, if available
				foreach (var c in confValues.Select (v => v.GetValue ("Configuration"))) {
					if (platValues.Count > 0) {
						foreach (var plat in platValues.Select (v => v.GetValue ("Platform"))) {
							var ep = plat == "AnyCPU" ? "" : plat;
							if (!configData.Any (cd => cd.Config == c && cd.Platform == ep))
								configData.Add (new ConfigData (c, ep, null));
						}
					} else {
						if (!configData.Any (cd => cd.Config == c))
							configData.Add (new ConfigData (c, "", null));
					}
				}
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

		ProjectConfiguration LoadConfiguration (ProgressMonitor monitor, ConfigData cgrp, string conf, string platform)
		{
			ProjectConfiguration config = null;
			if (platform == "AnyCPU")
				platform = "";
			
			string id = string.IsNullOrEmpty (platform) ? conf : conf + "|" + platform;

			if (IsReevaluating)
				config = Configurations.OfType<ProjectConfiguration> ().FirstOrDefault (c => c.Id == id);

			if (config == null)
				config = CreateConfiguration (id);
			
			if (cgrp.Group != null)
				config.MainPropertyGroup = (MSBuildPropertyGroup) cgrp.Group;
			config.MainPropertyGroup.ResetIsNewFlags ();
			InitConfiguration (config);
			projectExtension.OnReadConfiguration (monitor, config, config.Properties);
			return config;
		}

		MSBuildProjectInstance CreateProjectInstaceForConfiguration (string conf, string platform, bool onlyEvaluateProperties = true)
		{
			var pi = PrepareProjectInstaceForConfiguration (conf, platform, onlyEvaluateProperties);
			pi.Evaluate ();
			return pi;
		}

		async Task<MSBuildProjectInstance> CreateProjectInstaceForConfigurationAsync (string conf, string platform, bool onlyEvaluateProperties = true)
		{
			var pi = PrepareProjectInstaceForConfiguration (conf, platform, onlyEvaluateProperties);
			await pi.EvaluateAsync ();
			return pi;
		}

		MSBuildProjectInstance PrepareProjectInstaceForConfiguration (string conf, string platform, bool onlyEvaluateProperties)
		{
			var pi = sourceProject.CreateInstance ();
			pi.SetGlobalProperty ("BuildingInsideVisualStudio", "true");
			if (conf != null)
				pi.SetGlobalProperty ("Configuration", conf);
			if (platform != null) {
				if (platform == string.Empty)
					pi.SetGlobalProperty ("Platform", "AnyCPU");
				else
					pi.SetGlobalProperty ("Platform", platform);
			}
			pi.OnlyEvaluateProperties = onlyEvaluateProperties;
			return pi;
		}

		protected override SolutionItemConfiguration OnCreateConfiguration (string id, ConfigurationKind kind = ConfigurationKind.Blank)
		{
			return new ProjectConfiguration (id);
		}

		protected virtual void OnReadConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IPropertySet grp)
		{
			config.Read (grp);
		}

		void GetRunConfigData (List<ConfigData> configData, MSBuildProject msproject, bool includeEvaluated)
		{
			if (msproject == null)
				return;
			
			foreach (MSBuildPropertyGroup cgrp in msproject.PropertyGroups) {
				string configName;
				if (ParseRunConfigurationCondition (cgrp.Condition, out configName)) {
					// If a group for this configuration already was found, set the new group. If there are changes we want to modify the last group.
					var existing = configData.FirstOrDefault (cd => cd.Config == configName);
					if (existing == null)
						configData.Add (new ConfigData (configName, null, cgrp));
					else
						existing.Group = cgrp;
				}
			}
			if (includeEvaluated) {
				var configValues = msproject.ConditionedProperties.GetAllPropertyValues ("RunConfiguration");

				foreach (var c in configValues) {
					if (!configData.Any (cd => cd.Config == c))
						configData.Add (new ConfigData (c, "", null));
				}
			}
		}

		bool ParseRunConfigurationCondition (string cond, out string configName)
		{
			configName = null;
			int i = cond.IndexOf ("==", StringComparison.Ordinal);
			if (i == -1)
				return false;
			if (cond.Substring (0, i).Trim () == "'$(RunConfiguration)'")
				return ExtractConfigName (cond.Substring (i + 2), out configName);
			return false;
		}

		ProjectRunConfiguration LoadRunConfiguration (ProgressMonitor monitor, ConfigData cgrp, string configName)
		{
			ProjectRunConfiguration runConfig = null;

			if (IsReevaluating)
				runConfig = runConfigurations.FirstOrDefault (c => c.Id == configName);

			if (runConfig == null)
				runConfig = CreateUninitializedRunConfiguration (configName);
			
			if (cgrp.Group != null) {
				runConfig.MainPropertyGroup = cgrp.Group;
				runConfig.StoreInUserFile = cgrp.Group.ParentProject == userProject;
			}
			runConfig.MainPropertyGroup.ResetIsNewFlags ();
			InitRunConfiguration (runConfig);
			projectExtension.OnReadRunConfiguration (monitor, runConfig, runConfig.Properties);
			return runConfig;
		}

		void InitRunConfiguration (ProjectRunConfiguration config)
		{
			var pi = CreateProjectInstaceForRunConfiguration (config.Name);
			config.Properties = pi.GetPropertiesLinkedToGroup (config.MainPropertyGroup);
			config.ProjectInstance = pi;
		}

		MSBuildProjectInstance CreateProjectInstaceForRunConfiguration (string name, bool onlyEvaluateProperties = true)
		{
			var pi = PrepareProjectInstaceForRunConfiguration (name, onlyEvaluateProperties);
			pi.Evaluate ();
			return pi;
		}

		async Task<MSBuildProjectInstance> CreateProjectInstaceForRunConfigurationAsync (string name, bool onlyEvaluateProperties = true)
		{
			var pi = PrepareProjectInstaceForRunConfiguration (name, onlyEvaluateProperties);
			await pi.EvaluateAsync ();
			return pi;
		}

		MSBuildProjectInstance PrepareProjectInstaceForRunConfiguration (string name, bool onlyEvaluateProperties)
		{
			var pi = sourceProject.CreateInstance ();
			pi.SetGlobalProperty ("BuildingInsideVisualStudio", "true");
			pi.SetGlobalProperty ("RunConfiguration", name);
			pi.OnlyEvaluateProperties = onlyEvaluateProperties;
			return pi;
		}

		protected virtual ProjectRunConfiguration OnCreateRunConfiguration (string name)
		{
			return new ProjectRunConfiguration (name);
		}

		protected virtual void OnReadRunConfiguration (ProgressMonitor monitor, ProjectRunConfiguration runConfig, IPropertySet grp)
		{
			runConfig.Read (grp);
		}

		internal void OnRunConfigurationsAdded (IEnumerable<SolutionItemRunConfiguration> items)
		{
			// Initialize the property group only if the project is not being loaded (in which case it will
			// be initialized by the ReadProject method) or if the project is new (because it will be initialized
			// after the project is fully written, since only then all imports are in place
			if (!Loading && !sourceProject.IsNewProject) {
				foreach (var s in items)
					InitRunConfiguration ((ProjectRunConfiguration)s);
			}
		}

		internal void OnRunConfigurationRemoved (IEnumerable<SolutionItemRunConfiguration> items)
		{

		}

		internal void LoadProjectItems (MSBuildProject msproject, ProjectItemFlags flags, HashSet<MSBuildItem> loadedItems)
		{
			if (loadedItems != null)
				loadedItems.Clear ();

			var localItems = new List<ProjectItem> ();
			foreach (var buildItem in msproject.EvaluatedItemsIgnoringCondition) {
				if (buildItem.IsImported && !ProjectExtension.OnGetSupportsImportedItem (buildItem))
					continue;
				if (BuildAction.ReserverIdeActions.Contains (buildItem.Name))
					continue;
				ProjectItem it = ReadItem (buildItem);
				if (it == null)
					continue;
				it.Flags = flags;
				localItems.Add (it);
				if (loadedItems != null)
					loadedItems.Add (buildItem.SourceItem);
			}
			if (IsReevaluating)
				Items.SetItems (localItems);
			else
				Items.AddRange (localItems);
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
			if (IsReevaluating) {
				// If this item already exists in the current collection of items, reuse it
				var eit = Items.FirstOrDefault (it => it.BackingItem != null && it.BackingEvalItem != null && it.BackingEvalItem.Name == buildItem.Name && it.BackingEvalItem.Include == buildItem.Include && ItemsAreEqual (buildItem, it.BackingEvalItem));
				if (eit != null) {
					eit.BackingItem = buildItem.SourceItem;
					eit.BackingEvalItem = buildItem;
					return eit;
				}
			}

			var item = CreateProjectItem (buildItem);
			item.Read (this, buildItem);
			item.BackingItem = buildItem.SourceItem;
			item.BackingEvalItem = buildItem;
			return item;
		}

		struct MergedPropertyValue
		{
			public readonly string XmlValue;
			public readonly MSBuildValueType ValueType;
			public readonly bool IsDefault;

			public MergedPropertyValue (string xmlValue, MSBuildValueType valueType, bool isDefault)
			{
				this.XmlValue = xmlValue;
				this.ValueType = valueType;
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
					if (defaultConfProp == null || !Configurations.Any<SolutionItemConfiguration> (c => c.Name == defaultConfProp.UnevaluatedValue)) {
						ItemConfiguration conf = Configurations.FirstOrDefault<ItemConfiguration> (c => c.Name == "Debug");
						if (conf == null) conf = Configurations [0];
						string platform = conf.Platform.Length == 0 ? "AnyCPU" : conf.Platform;
						globalGroup.SetValue ("Configuration", conf.Name, condition: " '$(Configuration)' == '' ");
						globalGroup.SetValue ("Platform", platform, condition: " '$(Platform)' == '' ");
					} else if (defaultPlatProp == null || !Configurations.Any<SolutionItemConfiguration> (c => c.Name == defaultConfProp.UnevaluatedValue && c.Platform == defaultPlatProp.UnevaluatedValue)) {
						ItemConfiguration conf = Configurations.FirstOrDefault<ItemConfiguration> (c => c.Name == defaultConfProp.UnevaluatedValue);
						string platform = conf.Platform.Length == 0 ? "AnyCPU" : conf.Platform;
						globalGroup.SetValue ("Platform", platform, condition: " '$(Platform)' == '' ");
					}
				}
			}

/*			if (runConfigurations.Count > 0) {
				// Set the default configuration of the project.
				// First of the properties that defines the default run configuration
				var defaultConfProp = globalGroup.GetProperties ().FirstOrDefault (p => p.Name == "RunConfiguration" && IsDefaultSetter (p));

				if (msproject.IsNewProject || (defaultConfProp != null)) {
					// If there is no run configuration property, or if the configuration doesn't exist anymore, give it a new value
					if (defaultConfProp == null || !runConfigurations.Any (c => c.Name == defaultConfProp.UnevaluatedValue)) {
						var runConfig = runConfigurations.FirstOrDefault (c => c.Name == "Default") ?? runConfigurations [0];
						globalGroup.SetValue ("RunConfiguration", runConfig.Name, condition: " '$(RunConfiguration)' == '' ");
					}
				}
			}*/

			if (TypeGuid == MSBuildProjectService.GenericItemGuid) {
				DataType dt = MSBuildProjectService.DataContext.GetConfigurationDataType (GetType ());
				globalGroup.SetValue ("ItemType", dt.Name);
			}

			globalGroup.SetValue ("ProductVersion", productVersion);
			globalGroup.SetValue ("SchemaVersion", schemaVersion);

			globalGroup.SetValue ("ProjectGuid", ItemId, valueType:MSBuildValueType.Guid);

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

			writeTimer.Trace ("Writing configurations");
			WriteConfigurations (monitor, msproject, globalGroup);
			writeTimer.Trace ("Done writing configurations");

			writeTimer.Trace ("Writing run configurations");
			WriteRunConfigurations (monitor, msproject, globalGroup);
			writeTimer.Trace ("Done writing run configurations");

			writeTimer.Trace ("Saving project items");
			SaveProjectItems (monitor, msproject, usedMSBuildItems);
			writeTimer.Trace ("Done saving project items");

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
			importsAdded.Clear ();
			importsRemoved.Clear ();

			writeTimer.Trace ("Writing external properties");
			msproject.WriteExternalProjectProperties (this, GetType (), true);
			writeTimer.Trace ("Done writing external properties");
		}

		void WriteConfigurations (ProgressMonitor monitor, MSBuildProject msproject, IMSBuildPropertySet globalGroup)
		{
			if (Configurations.Count > 0) {

				List<ConfigData> configData = GetConfigData (msproject, false);

				// Write configuration data, creating new property groups if necessary

				foreach (ProjectConfiguration conf in Configurations) {

					MSBuildPropertyGroup pg = conf.MainPropertyGroup;
					ConfigData cdata = configData.FirstOrDefault (cd => cd.Group == pg);

					if (cdata == null) {
						// Try to keep the groups in the same order as the config list
						MSBuildObject nextConf = null;
						int i = Configurations.IndexOf (conf);
						if (i != -1 && i + 1 < Configurations.Count)
							nextConf = ((ProjectConfiguration)Configurations [i + 1]).MainPropertyGroup;

						msproject.AddPropertyGroup (pg, true, nextConf);
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

					cdata.Exists = true;
					ProjectExtension.OnWriteConfiguration (monitor, conf, conf.Properties);
				}

				// Find the properties in all configurations that have the MergeToProject flag set
				var mergeToProjectProperties = new HashSet<MergedProperty> (GetMergeToProjectProperties (configData));
				var mergeToProjectPropertyValues = new Dictionary<string, MergedPropertyValue> ();

				foreach (ProjectConfiguration conf in Configurations) {
					ConfigData cdata = FindPropertyGroup (configData, conf);
					var propGroup = (MSBuildPropertyGroup)cdata.Group;

					// Get properties with the MergeToProject flag, and check that the value they have matches the
					// value all the other groups have so far. If one of the groups have a different value for
					// the same property, then the property is discarded as mergeable to parent.
					CollectMergetoprojectProperties (propGroup, mergeToProjectProperties, mergeToProjectPropertyValues);

					// Remove properties that have been modified and have the default value. Usually such properties
					// would be removed when assigning the value, but we set IgnoreDefaultValues=false so that
					// we can collect MergeToProject properties, so in this case properties are not removed.
					propGroup.PurgeDefaultProperties ();
				}

				// Move properties with common values from configurations to the main
				// property group
				foreach (KeyValuePair<string, MergedPropertyValue> prop in mergeToProjectPropertyValues) {
					if (!prop.Value.IsDefault)
						globalGroup.SetValue (prop.Key, prop.Value.XmlValue, valueType: prop.Value.ValueType);
					else {
						// if the value is default, only remove the property if it was not already the default to avoid unnecessary project file churn
						globalGroup.SetValue (prop.Key, prop.Value.XmlValue, defaultValue: prop.Value.XmlValue, valueType: prop.Value.ValueType);
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
					if (!cd.Exists || (cd.IsNew && !cd.Group.GetProperties ().Any ()))
						msproject.Remove ((MSBuildPropertyGroup)cd.Group);
				}

				foreach (ProjectConfiguration config in Configurations)
					config.MainPropertyGroup.ResetIsNewFlags ();


				// For properties that have changed in the main group, set the
				// dirty flag for the corresponding properties in the evaluated
				// project instances. The evaluated values of those properties
				// can't be used anymore to decide wether or not a property
				// needs to be saved. The ideal solution would be to re-evaluate
				// the instance and get the new evaluated values, but that
				// would have a high impact in performance.

				foreach (var p in globalGroup.GetProperties ()) {
					if (p.Modified) {
						foreach (ProjectConfiguration config in Configurations)
							if (config.ProjectInstance != null)
								config.ProjectInstance.SetPropertyValueStale (p.Name);
					}
				}
			}
		}

		ProjectRunConfiguration defaultBlankRunConfiguration;

		void WriteRunConfigurations (ProgressMonitor monitor, MSBuildProject msproject, IMSBuildPropertySet globalGroup)
		{
			List<ConfigData> configData = new List<ConfigData> ();
			GetRunConfigData (configData, msproject, false);
			GetRunConfigData (configData, userProject, false);

			if (RunConfigurations.Count > 0) {

				// Write configuration data, creating new property groups if necessary

				// Create the default configuration just once, and reuse it for comparing in subsequent writes
				if (defaultBlankRunConfiguration == null)
					defaultBlankRunConfiguration = CreateRunConfigurationInternal ("Default");

				foreach (ProjectRunConfiguration runConfig in RunConfigurations) {

					MSBuildPropertyGroup pg = runConfig.MainPropertyGroup;
					ConfigData cdata = configData.FirstOrDefault (cd => cd.Group == pg);
					var targetProject = runConfig.StoreInUserFile ? userProject : msproject;

					if (runConfig.IsDefaultConfiguration && runConfig.Equals (defaultBlankRunConfiguration)) {
						// If the default configuration has the default values, then there is no need to save it.
						// If this configuration was added after loading the project, we are not adding it to the msproject and we are done.
						// If this configuration was loaded from the project and later modified to the default values, we dont set cdata.Exists=true,
						// so it will be removed from the msproject below.
						continue;
					}

					// Create the user project file if it doesn't yet exist
					if (targetProject == null)
						targetProject = userProject = CreateUserProject (msproject);

					if (cdata == null) {
						// Try to keep the groups in the same order as the config list
						MSBuildObject nextConfig = null;
						int i = runConfigurations.IndexOf (runConfig);
						if (i != -1 && i + 1 < runConfigurations.Count)
							nextConfig = runConfigurations.Skip (i).Cast<ProjectRunConfiguration> ().FirstOrDefault (s => s.MainPropertyGroup.ParentProject == targetProject)?.MainPropertyGroup;
						targetProject.AddPropertyGroup (pg, true, nextConfig);
						pg.Condition = BuildRunConfigurationCondition (runConfig.Name);
						cdata = new ConfigData (runConfig.Name, null, pg);
						cdata.IsNew = true;
						configData.Add (cdata);
					} else {
						// The configuration name may have changed
						if (cdata.Config != runConfig.Name) {
							((MSBuildPropertyGroup)cdata.Group).Condition = BuildRunConfigurationCondition (runConfig.Name);
							cdata.Config = runConfig.Name;
						}
						var groupInUserProject = cdata.Group.ParentProject == userProject;
						if (groupInUserProject != runConfig.StoreInUserFile) {
							cdata.Group.ParentProject.Remove (cdata.Group);
							targetProject.AddPropertyGroup (cdata.Group);
						}
					}

					cdata.Exists = true;
					ProjectExtension.OnWriteRunConfiguration (monitor, runConfig, runConfig.Properties);
					runConfig.MainPropertyGroup.PurgeDefaultProperties ();
				}
			}

			// Remove groups corresponding to configurations that have been removed
			foreach (ConfigData cd in configData) {
				if (!cd.Exists)
					cd.Group.ParentProject.Remove (cd.Group);
			}

			foreach (ProjectRunConfiguration runConfig in runConfigurations)
				runConfig.MainPropertyGroup.ResetIsNewFlags ();
		}

		MSBuildProject CreateUserProject (MSBuildProject msproject)
		{
			var p = new MSBuildProject (msproject.EngineManager);
			// Remove the main property group
			p.Remove (p.PropertyGroups.First ());
			p.FileName = msproject.FileName + ".user";
			return p;
		}

		protected virtual void OnWriteConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IPropertySet pset)
		{
			config.Write (pset);
		}

		protected virtual void OnWriteRunConfiguration (ProgressMonitor monitor, ProjectRunConfiguration config, IPropertySet pset)
		{
			config.Write (pset);
		}

		IEnumerable<MergedProperty> GetMergeToProjectProperties (List<ConfigData> configData)
		{
			Dictionary<string,MergedProperty> mergeProps = new Dictionary<string, MergedProperty> ();
			foreach (var cd in configData) {
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
						mergeToProjectProperties.Add (pinfo.Name, new MergedPropertyValue (prop.Value, pinfo.ValueType, pinfo.IsDefault));
						continue;
					}
					// If there is no value, it can't be merged
				}
				else if (prop != null && mvalue.ValueType.Equals (prop.Value, mvalue.XmlValue))
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

		class ExpandedItemList: List<ExpandedItemInfo>
		{
			public bool Modified { get; set; }
		}

		class ExpandedItemInfo
		{
			public ProjectItem ProjectItem;
			public MSBuildItem MSBuildItem;
			public ExpandedItemAction Action;
		}

		enum ExpandedItemAction
		{
			None,
			Exclude,
			AddUpdateItem
		}

		/// <summary>
		/// When set to true, the project will make use of improved globbing logic to avoid expanding glob in multiple items when
		/// there are changes. Requires the latest version of msbuild to work.
		/// </summary>
		public bool UseAdvancedGlobSupport { get; set; }

		HashSet<MSBuildItem> usedMSBuildItems = new HashSet<MSBuildItem> ();
		HashSet<ProjectItem> loadedProjectItems = new HashSet<ProjectItem> ();

		internal virtual void SaveProjectItems (ProgressMonitor monitor, MSBuildProject msproject, HashSet<MSBuildItem> loadedItems, string pathPrefix = null)
		{
			HashSet<MSBuildItem> unusedItems = new HashSet<MSBuildItem> (loadedItems);
			Dictionary<MSBuildItem,ExpandedItemList> expandedItems = new Dictionary<MSBuildItem, ExpandedItemList> ();

			// Add the new items

			foreach (ProjectItem ob in Items.Where (it => !it.Flags.HasFlag (ProjectItemFlags.DontPersist)))
				SaveProjectItem (monitor, msproject, ob, expandedItems, unusedItems, loadedItems, pathPrefix);

			// Process items generated from wildcards

			foreach (var itemInfo in expandedItems) {
				var expandedList = itemInfo.Value;
				var globItem = itemInfo.Key;
				if (expandedList.Modified || loadedProjectItems.Where (i => i.WildcardItem == globItem).Count () != expandedList.Count) {
					if (UseAdvancedGlobSupport) {
						// Add remove items if necessary
						foreach (var removed in loadedProjectItems.Where (i => i.WildcardItem == globItem && !expandedList.Any (newItem => newItem.ProjectItem.Include == i.Include))) {
							var file = removed as ProjectFile;
							if (file == null || File.Exists (file.FilePath)) {
								var removeItem = new MSBuildItem (removed.ItemName) { Remove = removed.Include };
								msproject.AddItem (removeItem);
							}
							unusedItems.UnionWith (FindUpdateItemsForItem (globItem, removed.Include));
						}

						// Exclude modified items
						foreach (var it in expandedList) {
							if (it.Action == ExpandedItemAction.Exclude) {
								globItem.AddExclude (it.ProjectItem.Include);
								it.ProjectItem.BackingItem = it.MSBuildItem;
								it.ProjectItem.BackingEvalItem = CreateFakeEvaluatedItem (msproject, it.MSBuildItem, it.MSBuildItem.Include, null);
								msproject.AddItem (it.MSBuildItem);
							} else if (it.Action == ExpandedItemAction.AddUpdateItem) {
								msproject.AddItem (it.MSBuildItem);
							}
						}
					} else {
						// Expand the list
						unusedItems.Add (globItem);
						foreach (var it in expandedList) {
							it.ProjectItem.BackingItem = it.MSBuildItem;
							it.ProjectItem.BackingEvalItem = CreateFakeEvaluatedItem (msproject, it.MSBuildItem, it.MSBuildItem.Include, null);
							msproject.AddItem (it.MSBuildItem);
						}
					}
				}
			}

			// Remove unused items

			foreach (var it in unusedItems) {
				if (it.ParentGroup != null) { // It may already have been deleted
					// Remove wildcard item if it is not imported.
					if (!it.IsWildcardItem || it.ParentProject == msproject) {
						msproject.RemoveItem (it);

						if (!UseAdvancedGlobSupport)
							continue;

						var file = loadedProjectItems.FirstOrDefault (i => {
							return i.ItemName == it.Name && (i.Include == it.Include || i.Include == it.Update);
						}) as ProjectFile;
						if (file != null && !file.IsLink) {
							if (File.Exists (file.FilePath)) {
								AddRemoveItemIfMissing (msproject, file);
							} else if (!string.IsNullOrEmpty (it.Include)) {
								// Remove any "Remove" items that match if the file has been deleted.
								var toRemove = msproject.GetAllItems ().Where (i => i.Remove == it.Include).ToList ();
								foreach (var item in toRemove) {
									msproject.RemoveItem (item);
								}
							}
						}
					} else if (it.IsWildcardItem && UseAdvancedGlobSupport) {
						// Add "Remove" items if the file is not deleted.
						foreach (var file in loadedProjectItems.Where (i => i.WildcardItem == it).OfType<ProjectFile> ()) {
							if (File.Exists (file.FilePath)) {
								AddRemoveItemIfMissing (msproject, file);
							}
							// Ensure "Update" items are removed from the project. If there are no
							// files left in the project for the glob then the "Update" item will
							// not have been removed.
							RemoveUpdateItemsForFile (msproject, it, file);
						}
					}
				}
				loadedItems.Remove (it);
			}
			loadedProjectItems = new HashSet<ProjectItem> (Items);
		}

		void SaveProjectItem (ProgressMonitor monitor, MSBuildProject msproject, ProjectItem item, Dictionary<MSBuildItem,ExpandedItemList> expandedItems, HashSet<MSBuildItem> unusedItems, HashSet<MSBuildItem> loadedItems, string pathPrefix = null)
		{
			if (item.IsFromWildcardItem && item.ItemName == item.WildcardItem.Name) {
				var globItem = item.WildcardItem;
				// Store the item in the list of expanded items
				ExpandedItemList items;
				if (!expandedItems.TryGetValue (globItem, out items))
					items = expandedItems [globItem] = new ExpandedItemList ();

				// We need to check if the item has changed, in which case all the items included by the wildcard
				// must be individually included
				var bitem = msproject.CreateItem (item.ItemName, GetPrefixedInclude (pathPrefix, item.Include));
				item.Write (this, bitem);

				var einfo = new ExpandedItemInfo {
					ProjectItem = item,
					MSBuildItem = bitem
				};
				items.Add (einfo);

				foreach (var it in item.BackingEvalItem.SourceItems)
					unusedItems.Remove (it);

				if (UseAdvancedGlobSupport) {
					einfo.Action = GenerateItemDiff (globItem, bitem, item.BackingEvalItem);
					if (einfo.Action != ExpandedItemAction.None)
						items.Modified = true;
				} else if (!items.Modified && (item.Metadata.PropertyCountHasChanged || !ItemsAreEqual (bitem, item.BackingEvalItem))) {
					items.Modified = true;
				}
				return;
			}

			var include = GetPrefixedInclude (pathPrefix, item.UnevaluatedInclude ?? item.Include);

			MSBuildItem buildItem = null;
			IEnumerable<MSBuildItem> sourceItems = null;
			MSBuildEvaluationContext context = null;

			if (item.BackingItem?.ParentObject != null && item.BackingItem.Name == item.ItemName) {
				buildItem = item.BackingItem;
				sourceItems = item.BackingEvalItem.SourceItems;
			} else {
				if (UseAdvancedGlobSupport) {
					// It is a new item. Before adding it, check if there is a Remove for the item. If there is, it is likely the file was excluded from a glob.
					var toRemove = msproject.GetAllItems ().Where (it => it.Name == item.ItemName && it.Remove == include).ToList ();
					if (toRemove.Count > 0) {
						// Remove the "Remove" items
						foreach (var it in toRemove)
							msproject.RemoveItem (it);
					}
					// Check if the file is included in a glob.
					var matchingGlobItems = msproject.FindGlobItemsIncludingFile (item.Include).ToList ();
					var globItem = matchingGlobItems.FirstOrDefault (gi => gi.Name == item.ItemName);

					if (globItem != null) {
						var updateGlobItems = msproject.FindUpdateGlobItemsIncludingFile (item.Include, globItem).ToList ();
						// Globbing magic can only be done if there is no metadata (for now)
						if (globItem.Metadata.GetProperties ().Count () == 0 && !updateGlobItems.Any ()) {
							var it = new MSBuildItem (item.ItemName);
							item.Write (this, it);
							if (it.Metadata.GetProperties ().Count () == 0)
								buildItem = globItem;

							// Add an expanded item so a Remove item does not
							// get added back again.
							ExpandedItemList items;
							if (!expandedItems.TryGetValue (globItem, out items))
								items = expandedItems [globItem] = new ExpandedItemList ();

							var einfo = new ExpandedItemInfo {
								ProjectItem = item,
								MSBuildItem = it
							};
							items.Add (einfo);

							if (buildItem == null && item.BackingItem != null && globItem.Name != item.BackingItem.Name) {
								it.Update = item.Include;
								sourceItems = new [] { globItem };
								item.BackingItem = globItem;
								item.BackingEvalItem = CreateFakeEvaluatedItem (msproject, it, globItem.Include, sourceItems);
								einfo.Action = ExpandedItemAction.AddUpdateItem;
								items.Modified = true;
								return;
							} else if (buildItem == null) {
								buildItem = new MSBuildItem (item.ItemName) { Update = item.Include };
								msproject.AddItem (buildItem);
							}
						} else if (updateGlobItems.Any ()) {
							// Multiple update items not supported yet.
							buildItem = updateGlobItems [0];
							sourceItems = new [] { globItem, buildItem };
							context = CreateEvaluationContext (item);
						} else {
							buildItem = globItem;
						}
					} else if (item.IsFromWildcardItem && item.ItemName != item.WildcardItem.Name) {
						include = item.Include;
						var removeItem = new MSBuildItem (item.WildcardItem.Name) { Remove = include };
						msproject.AddItem (removeItem);
					}

					// Add remove item if file is included in a glob with a different MSBuild item type.
					// But do not add the remove item if the item is already removed with another glob.
					var removeGlobItem = matchingGlobItems.FirstOrDefault (gi => gi.Name != item.ItemName);
					var alreadyRemovedGlobItem = matchingGlobItems.FirstOrDefault (gi => gi.Name == item.ItemName);
					if (removeGlobItem != null && alreadyRemovedGlobItem == null) {
						// Do not add the remove item if one already exists or if the Items contains
						// an include for the item.
						if (!msproject.GetAllItems ().Any (it => it.Name == removeGlobItem.Name && it.Remove == item.Include) &&
							!Items.Any (it => it.ItemName == removeGlobItem.Name && it.Include == item.Include)) {
							var removeItem = new MSBuildItem (removeGlobItem.Name) { Remove = item.Include };
							msproject.AddItem (removeItem);
						}
					}
				}
				if (buildItem == null)
					buildItem = msproject.AddNewItem (item.ItemName, include);
				item.BackingItem = buildItem;
				item.BackingEvalItem = CreateFakeEvaluatedItem (msproject, buildItem, include, sourceItems, context);
			}

			loadedItems.Add (buildItem);
			unusedItems.Remove (buildItem);

			if (!buildItem.IsWildcardItem) {
				if (buildItem.IsUpdate) {
					var propertiesAlreadySet = new HashSet<string> (buildItem.Metadata.GetProperties ().Select (p => p.Name));
					item.Write (this, buildItem);
					PurgeUpdatePropertiesSetInSourceItems (buildItem, item.BackingEvalItem.SourceItems, propertiesAlreadySet);
				} else {
					item.Write (this, buildItem);
					if (buildItem.Include != include)
						buildItem.Include = include;
				}
			}
		}

		static void AddRemoveItemIfMissing (MSBuildProject msproject, ProjectFile file)
		{
			if (!msproject.GetAllItems ().Where (i => i.Remove == file.Include).Any ()) {
				var removeItem = new MSBuildItem (file.ItemName) { Remove = file.Include };
				msproject.AddItem (removeItem);
			}
		}

		void RemoveUpdateItemsForFile (MSBuildProject msproject, MSBuildItem globItem, ProjectFile file)
		{
			foreach (var updateItem in FindUpdateItemsForItem (globItem, file.Include).ToList ()) {
				if (updateItem.ParentGroup != null) {
					msproject.RemoveItem (updateItem);
				}
			}
		}

		void PurgeUpdatePropertiesSetInSourceItems (MSBuildItem buildItem, IEnumerable<MSBuildItem> sourceItems, HashSet<string> propertiesAlreadySet)
		{
			// When the project item is saved to an Update item, it will write values that were set by the Include item and other Update items defined before this Update item.
			// We need to go back to those  items and check if any of the values they set is the same that has
			// been written. In that case, the property doesn't need to be set again in the Update item, and can be removed.
			// We ignore properties that were already set in the original file. We always set those.
			var itemsToCheck = sourceItems.ToList ();
			List<string> propsToRemove = null;

			foreach (var p in buildItem.Metadata.GetProperties ().Where (pr => !propertiesAlreadySet.Contains (pr.Name))) {
				// The last item of the sourceItems list is supposed to be buildItem, so we need to skip it.
				// Also traverse in reverse order, so we check the last property value set.
				for (int n = itemsToCheck.Count - 2; n >= 0; n++) {
					var it = itemsToCheck [n];
					var prop = it.Metadata.GetProperty (p.Name);
					if (prop != null) {
						if (p.ValueType.Equals (p.Value, prop.Value)) {
							// This item defines the same metadata, so that metadata doesn't need to be set in the Update item
							if (propsToRemove == null)
								propsToRemove = new List<string> ();
							propsToRemove.Add (p.Name);
						}
						break;
					}
				}
			}
			if (propsToRemove != null) {
				foreach (var name in propsToRemove)
					buildItem.Metadata.RemoveProperty (name);
			}
		}

		bool ItemsAreEqual (MSBuildItem item, IMSBuildItemEvaluated evalItem)
		{
			// Compare only metadata, since item name and include can't change

			var n = 0;
			foreach (var p in item.Metadata.GetProperties ()) {
				var p2 = evalItem.Metadata.GetProperty (p.Name);
				if (p2 == null)
					return false;
				if (!p.ValueType.Equals (p.Value, p2.UnevaluatedValue))
					return false;
				n++;
			}
			if (evalItem.Metadata.GetProperties ().Count () != n)
				return false;
			return true;
		}

		ExpandedItemAction GenerateItemDiff (MSBuildItem globItem, MSBuildItem item, IMSBuildItemEvaluated evalItem)
		{
			// This method compares the evaluated item that was used to load a project item with the msbuild
			// item that has now been saved. If there are changes, it saves the changes in an item with Update
			// attribute.

			MSBuildItem updateItem = null;
			HashSet<MSBuildItem> itemsToDelete = null;
			List <MSBuildItem> updateItems = null;
			List<MSBuildProperty> unchangedProperties = null;
			bool generateNewUpdateItem = false;

			foreach (var p in item.Metadata.GetProperties ()) {
				var p2 = evalItem.Metadata.GetProperty (p.Name);
				if (p2 == null || !p.ValueType.Equals (p.Value, p2.UnevaluatedValue)) {
					if (generateNewUpdateItem)
						continue;
					if (updateItem == null) {
						updateItems = FindUpdateItemsForItem (globItem, item.Include).ToList ();
						updateItem = updateItems.LastOrDefault ();
						if (updateItem == null) {
							// There is no existing update item. A new one will be generated.
							generateNewUpdateItem = true;
							continue;
						}
					}

					var globProp = globItem.Metadata.GetProperty (p.Name);
					if (globProp != null && p.ValueType.Equals (globProp.Value, p.Value)) {
						// The custom value of the item is defined in the glob item that creates it,
						// so we are actually reverting a custom metadata value. The update item
						// can probably be removed.
						foreach (var upi in updateItems) {
							upi.Metadata.RemoveProperty (p.Name);
							if (!upi.Metadata.GetProperties ().Any ()) {
								if (itemsToDelete == null)
									itemsToDelete = new HashSet<MSBuildItem> ();
								itemsToDelete.Add (upi);
							}
						}
						continue;
					}

					updateItem.Metadata.SetValue (p.Name, p.Value);
					if (itemsToDelete != null)
						itemsToDelete.Remove (updateItem);
				} else {
					if (unchangedProperties == null)
						unchangedProperties = new List<MSBuildProperty> ();
					unchangedProperties.Add (p);
				}
			}

			if (generateNewUpdateItem) {
				// Convert the item into an update item
				item.Update = item.Include;
				item.Include = "";
				if (unchangedProperties != null) {
					// Remove properties that have not changed, so they don't have to
					// be included in the update item.
					foreach (var p in unchangedProperties)
						item.Metadata.RemoveProperty (p.Name);
				}
				return ExpandedItemAction.AddUpdateItem;
			}

			if (itemsToDelete != null) {
				foreach (var it in itemsToDelete)
					it.ParentProject.RemoveItem (it);
			}
			
			foreach (var p in evalItem.Metadata.GetProperties ()) {
				var p2 = item.Metadata.GetProperty (p.Name);
				if (p2 == null) {
					// The evaluated item has a property that the msbuild item doesn't have. If that metadata is
					// set by the glob item, the only option is to exclude it from the glob. If the metadata was set by
					// an update item, we have to remove that metadata definition

					if (updateItems == null)
						updateItems = FindUpdateItemsForItem (globItem, item.Include).ToList ();
					foreach (var it in updateItems.Where (i => i.ParentNode != null)) {
						if (it.Metadata.RemoveProperty (p.Name) && !it.Metadata.GetProperties ().Any ())
							it.ParentProject.RemoveItem (it);
					}
					// If this metadata is defined in the glob item, the only option is to exclude the item from the glob.
					if (globItem.Metadata.HasProperty (p.Name)) {
						// Get rid of all update items, not needed anymore since a full new item will be added
						foreach (var it in updateItems) {
							if (it.ParentNode != null)
								it.ParentGroup.RemoveItem (it);
						}
						return ExpandedItemAction.Exclude;
					}
				}
			}

			if (!evalItem.Metadata.GetProperties ().Any () && !item.Metadata.GetProperties ().Any ()) {
				updateItems = FindUpdateItemsForItem (globItem, item.Include).ToList ();
				foreach (var it in updateItems) {
					if (it.ParentNode != null)
						it.ParentProject.RemoveItem (it);
				}
			}
			return ExpandedItemAction.None;
		}

		IEnumerable<MSBuildItem> FindUpdateItemsForItem (MSBuildItem globItem, string include)
		{
			bool globItemFound = false;
			foreach (var it in globItem.ParentProject.GetAllItems ()) {
				if (!globItemFound)
					globItemFound = (it == globItem);
				else {
					if (it.Update == include)
						yield return it;
				}
			}

			if (globItemFound && globItem.ParentProject != MSBuildProject) {
				foreach (var it in MSBuildProject.GetAllItems ()) {
					if (it.Update == include)
						yield return it;
				}
			}
		}

		bool ItemsAreEqual (IMSBuildItemEvaluated item1, IMSBuildItemEvaluated item2)
		{
			// Compare only metadata, since item name and include can't change

			if (item1.SourceItem == null || item2.SourceItem == null || item1.Metadata.GetProperties ().Count () != item2.Metadata.GetProperties ().Count ())
				return false;

			foreach (var p1 in item1.Metadata.GetProperties ()) {
				var p2 = item2.Metadata.GetProperty (p1.Name);
				if (p2 == null || p2 == null)
					return false;
				if (p1.Value != p2.Value)
					return false;
			}
			return true;
		}

		MSBuildEvaluationContext CreateEvaluationContext (ProjectItem item)
		{
			if (item is ProjectFile file) {
				var context = new MSBuildEvaluationContext ();
				context.SetItemContext (item.Include, file.FilePath, null);
				return context;
			}

			return null;
		}

		IMSBuildItemEvaluated CreateFakeEvaluatedItem (MSBuildProject msproject, MSBuildItem item, string include, IEnumerable<MSBuildItem> sourceItems, MSBuildEvaluationContext context = null)
		{
			// Create the item
			var eit = new MSBuildItemEvaluated (msproject, item.Name, item.Include, include);

			// Copy the metadata
			var md = new Dictionary<string, IMSBuildPropertyEvaluated> ();
			var col = (MSBuildPropertyGroupEvaluated)eit.Metadata;
			foreach (var p in item.Metadata.GetProperties ()) {
				// Use evaluated value for value and unevaluated value. Otherwise
				// an Update item will be generated for a '%(FileName)' property
				// when GenerateItemDiff is called since it compares the value with
				// the unevaluated value. If the project file is loaded from disk
				// the unevaluated value would be the evaluated filename.
				string evaluatedValue = context?.EvaluateString (p.Value) ?? p.Value;
				md [p.Name] = new MSBuildPropertyEvaluated (msproject, p.Name, evaluatedValue, evaluatedValue);
			}
			((MSBuildPropertyGroupEvaluated)eit.Metadata).SetProperties (md);
			if (sourceItems != null) {
				foreach (var s in sourceItems)
					eit.AddSourceItem (s);
			} else
				eit.AddSourceItem (item);
			return eit;
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

		ConfigData FindPropertyGroup (List<ConfigData> configData, ProjectRunConfiguration config)
		{
			foreach (ConfigData data in configData) {
				if (data.Config == config.Name)
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

		string BuildRunConfigurationCondition (string name)
		{
			return " '$(RunConfiguration)' == '" + name + "' ";
		}

		bool IsMergeToProjectProperty (ItemProperty prop)
		{
			foreach (object at in prop.CustomAttributes) {
				if (at is MergeToProjectAttribute)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Reevaluates the MSBuild project
		/// </summary>
		/// <remarks>
		/// Reevaluates the underlying msbuild project and updates the project information acording to the new items and properties.
		/// </remarks>
		public Task ReevaluateProject (ProgressMonitor monitor)
		{
			return BindTask (ct => Runtime.RunInMainThread (async () => {
				using (await writeProjectLock.EnterAsync ()) {
					var oldCapabilities = new HashSet<string> (projectCapabilities);
					bool oldSupportsExecute = SupportsExecute ();

					try {
						IsReevaluating = true;

						// Reevaluate the msbuild project
						await sourceProject.EvaluateAsync ();

						// Loads minimal data required to instantiate extensions and prepare for project loading
						InitBeforeProjectExtensionLoad ();

						// Activate / deactivate extensions based on the new status
						RefreshExtensions ();

						await ProjectExtension.OnReevaluateProject (monitor);

					} finally {
						IsReevaluating = false;
					}

					ResetCachedCompileItems ();

					if (!oldCapabilities.SetEquals (projectCapabilities))
						NotifyProjectCapabilitiesChanged ();

					NotifyExecutionTargetsChanged (); // Maybe...

					if (oldSupportsExecute != SupportsExecute ()) {
						OnSupportsExecuteChanged (!oldSupportsExecute);
					}
				}
			}));
		}

		/// <summary>
		/// If the project's SupportsExecute has changed then check if the solution's startup
		/// configuration needs to be refreshed. If the solution has no startup item and
		/// the project can now be executed then refresh the startup configuration since a
		/// startup item can now be set for the solution. If the solution's startup item is
		/// this project and can no longer be executed then refresh the startup configuration
		/// so another startup item can be selected.
		/// </summary>
		void OnSupportsExecuteChanged (bool supportsExecute)
		{
			if (ParentSolution == null)
				return;

			if ((!supportsExecute && ParentSolution.StartupItem == this) ||
				(supportsExecute && ParentSolution.StartupConfiguration == null)) {
				ParentSolution.RefreshStartupConfiguration ();
			}
		}

		protected virtual async Task OnReevaluateProject (ProgressMonitor monitor)
		{
			await LoadAsync (monitor);
		}

		public bool IsReevaluating { get; private set; }

		/// <summary>
		/// Checks if a file is included in any project item glob, and in this case it adds the require project files.
		/// </summary>
		/// <returns><c>true</c>, if any item was added, <c>false</c> otherwise.</returns>
		/// <param name="file">File path</param>
		/// <remarks>This method is useful to add items for a file that has been created in the project directory,
		/// when the file is included in a glob defined by a project item.
		/// Project items that define custom metadata will be ignored.</remarks>
		public IEnumerable<ProjectItem> AddItemsForFileIncludedInGlob (FilePath file)
		{
			var include = MSBuildProjectService.ToMSBuildPath (ItemDirectory, file);
			foreach (var it in sourceProject.FindGlobItemsIncludingFile (include).Where (it => it.Metadata.GetProperties ().Count () == 0)) {
				var eit = CreateFakeEvaluatedItem (sourceProject, it, include, null);
				var pi = CreateProjectItem (eit);
				pi.Read (this, eit);
				Items.Add (pi);
				yield return pi;
			}
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

		bool useFileWatcher;

		/// <summary>
		/// When set to true with UseAdvancedGlobSupport also true then changes made to files inside the project externally
		/// will be monitored and used to update the project.
		/// </summary>
		public bool UseFileWatcher {
			get { return useFileWatcher; }
			set {
				if (useFileWatcher != value) {
					useFileWatcher = value;

					// File watcher will be created in OnEndLoad.
					if (Loading) {
						if (!useFileWatcher) {
							DisposeFileWatcher ();
						}
					} else {
						OnUseFileWatcherChanged ();
					}
				}
			}
		}

		void OnUseFileWatcherChanged ()
		{
			if (useFileWatcher && UseAdvancedGlobSupport) {
				CreateFileWatcher ();
			} else {
				DisposeFileWatcher ();
			}
		}

		void InitializeFileWatcher ()
		{
			if (useFileWatcher) {
				OnUseFileWatcherChanged ();
			}
		}

		FSW.FileSystemWatcher watcher;

		void CreateFileWatcher ()
		{
			DisposeFileWatcher ();

			if (!Directory.Exists (BaseDirectory))
				return;

			watcher = new FSW.FileSystemWatcher (BaseDirectory);
			watcher.IncludeSubdirectories = true;
			watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
			watcher.Created += OnFileCreated;
			watcher.Deleted += OnFileDeleted;
			watcher.Renamed += OnFileRenamed;
			watcher.Error += OnFileWatcherError;
			watcher.EnableRaisingEvents = true;
		}

		void DisposeFileWatcher ()
		{
			if (watcher != null) {
				watcher.Dispose ();
				watcher = null;
			}
		}

		void OnFileWatcherError (object sender, ErrorEventArgs e)
		{
			LoggingService.LogError ("FileWatcher error", e.GetException ());
		}

		void OnFileRenamed (object sender, RenamedEventArgs e)
		{
			Runtime.RunInMainThread (() => {
				if (Directory.Exists (e.FullPath)) {
					OnDirectoryRenamedExternally (e.OldFullPath, e.FullPath);
				} else {
					OnFileCreatedExternally (e.FullPath);
					OnFileDeletedExternally (e.OldFullPath);
				}
			});
		}

		void OnFileCreated (object sender, FileSystemEventArgs e)
		{
			if (Directory.Exists (e.FullPath))
				return;

			FilePath filePath = e.FullPath;
			if (filePath.FileName == ".DS_Store")
				return;

			Runtime.RunInMainThread (() => {
				OnFileCreatedExternally (e.FullPath);
			});
		}

		void OnFileDeleted (object sender, FileSystemEventArgs e)
		{
			Runtime.RunInMainThread (() => {
				OnFileDeletedExternally (e.FullPath);
			});
		}

		/// <summary>
		/// Move all project files in the old directory to the new directory.
		/// </summary>
		void OnDirectoryRenamedExternally (string oldDirectory, string newDirectory)
		{
			FileService.NotifyDirectoryRenamed (oldDirectory, newDirectory);
		}

		void OnFileCreatedExternally (string fileName)
		{
			if (Files.Any (file => file.FilePath == fileName)) {
				// File exists in project. This can happen if the file was added
				// in the IDE and not externally.
				return;
			}

			string include = MSBuildProjectService.ToMSBuildPath (ItemDirectory, fileName);
			foreach (var it in sourceProject.FindGlobItemsIncludingFile (include).Where (it => it.Metadata.GetProperties ().Count () == 0)) {
				var eit = CreateFakeEvaluatedItem (sourceProject, it, include, null);
				var pi = CreateProjectItem (eit);
				pi.Read (this, eit);
				Items.Add (pi);
			}
		}

		void OnFileDeletedExternally (string fileName)
		{
			if (File.Exists (fileName)) {
				// File has not been deleted. The delete event could have been due to
				// the file being saved. Saving with TextFileUtility will result in
				// FileService.SystemRename being called to move a temporary file
				// to the file being saved which deletes and then creates the file.
				return;
			}

			Files.Remove (fileName);
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

		public event EventHandler<ProjectItemEventArgs> ProjectItemAdded;

		public event EventHandler<ProjectItemEventArgs> ProjectItemRemoved;
	
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

			internal protected override ProjectRunConfiguration OnCreateRunConfiguration (string name)
			{
				return Project.OnCreateRunConfiguration (name);
			}

			internal protected override void OnReadRunConfiguration (ProgressMonitor monitor, ProjectRunConfiguration runConfig, IPropertySet properties)
			{
				Project.OnReadRunConfiguration (monitor, runConfig, properties);
			}

			internal protected override void OnWriteRunConfiguration (ProgressMonitor monitor, ProjectRunConfiguration runConfig, IPropertySet properties)
			{
				Project.OnWriteRunConfiguration (monitor, runConfig, properties);
			}

			internal protected override TargetEvaluationContext OnConfigureTargetEvaluationContext (string target, ConfigurationSelector configuration, TargetEvaluationContext context)
			{
				return Project.OnConfigureTargetEvaluationContext (target, configuration, context);
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

			internal protected override void OnReadConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IPropertySet grp)
			{
				Project.OnReadConfiguration (monitor, config, grp);
			}

			internal protected override void OnWriteConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IPropertySet grp)
			{
				Project.OnWriteConfiguration (monitor, config, grp);
			}

			internal protected override Task OnReevaluateProject (ProgressMonitor monitor)
			{
				return Project.OnReevaluateProject (monitor);
			}

			internal protected override void OnGetDefaultImports (List<string> imports)
			{
				Project.OnGetDefaultImports (imports);
			}

			internal protected override void OnPrepareForEvaluation (MSBuildProject project)
			{
				Project.OnPrepareForEvaluation (project);
			}

#pragma warning disable 672, 618
			internal protected override bool OnFastCheckNeedsBuild (ConfigurationSelector configuration)
			{
				return Project.OnFastCheckNeedsBuild (configuration);
			}
#pragma warning restore 672, 618

			internal protected override bool OnFastCheckNeedsBuild (ConfigurationSelector configuration, TargetEvaluationContext context)
			{
				return Project.OnFastCheckNeedsBuild (configuration, context);
			}

			internal protected override Task<ProjectFile []> OnGetSourceFiles (ProgressMonitor monitor, ConfigurationSelector configuration)
			{
				return Project.OnGetSourceFiles (monitor, configuration);
			}

			internal protected override bool OnGetSupportsImportedItem (IMSBuildItemEvaluated buildItem)
			{
				return Project.OnGetSupportsImportedItem (buildItem);
			}

			internal protected override void OnItemsAdded (IEnumerable<ProjectItem> objs)
			{
				Project.OnItemsAdded (objs);
			}

			internal protected override void OnItemsRemoved (IEnumerable<ProjectItem> objs)
			{
				Project.OnItemsRemoved (objs);
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
