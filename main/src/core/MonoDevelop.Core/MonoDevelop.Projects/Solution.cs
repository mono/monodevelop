// Solution.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core.Execution;
using System.Threading.Tasks;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	[ProjectModelDataItem]
	public class Solution: WorkspaceItem, IConfigurationTarget, IPolicyProvider, IBuildTarget, IMSBuildFileObject, IRunTarget
	{
		internal object MemoryProbe = Counters.SolutionsInMemory.CreateMemoryProbe ();

		SolutionFolder rootFolder;
		string defaultConfiguration;
		MSBuildFileFormat format;
		bool loadingFromConstructor;

		SolutionRunConfiguration startupSolutionConfiguration;

		ReadOnlyCollection<SolutionItem> solutionItems;
		SolutionConfigurationCollection configurations;
		MultiItemSolutionRunConfigurationCollection runConfigurations;

		MultiItemSolutionRunConfiguration multiStartupConfig = new MultiItemSolutionRunConfiguration (MultiStartupConfigId, "Multi-Startup");

		const string MultiStartupConfigId = "MonoDevelop.Projects.MultiStartup";

		MSBuildEngineManager msbuildEngineManager = new MSBuildEngineManager ();
		
		[ItemProperty ("description", DefaultValue = "")]
		string description;
		
		[ItemProperty ("version", DefaultValue = "0.1")]
		string version = "0.1";
		
		[ProjectPathItemProperty ("outputpath")]
		string outputdir     = null;
		
		public Solution (): this (false)
		{
		}

		internal Solution (bool loading)
		{
			loadingFromConstructor = loading;
			Counters.SolutionsLoaded++;
			configurations = new SolutionConfigurationCollection (this);
			runConfigurations = new MultiItemSolutionRunConfigurationCollection (this);
			format = MSBuildFileFormat.DefaultFormat;
			Initialize (this);
		}

		public override FilePath FileName {
			[ThreadSafe] get {
				return base.FileName;
			}
			set {
				AssertMainThread ();
				if (FileFormat != null)
					value = FileFormat.GetValidFormatName (this, value);
				base.FileName = value;
			}
		}

		internal HashSet<string> LoadedProjects {
			get;
			set;
		}

		protected override void OnExtensionChainInitialized ()
		{
			itemExtension = ExtensionChain.GetExtension<SolutionExtension> ();
			base.OnExtensionChainInitialized ();

			if (!loadingFromConstructor)
				NotifyItemReady ();
		}

		SolutionExtension itemExtension;

		SolutionExtension SolutionExtension {
			get {
				if (itemExtension == null)
					AssertExtensionChainCreated ();
				return itemExtension;
			}
		}

		public SolutionFolder RootFolder {
			get {
				if (rootFolder == null) {
					rootFolder = new SolutionFolder ();
					rootFolder.ParentSolution = this;
				}
				return rootFolder;
			}
			internal set {
				rootFolder = value;
			}
		}

		internal MSBuildEngineManager MSBuildEngineManager {
			get { return msbuildEngineManager; }
		}

		/// <summary>
		/// Folder where to add solution files, when none is created
		/// </summary>
		public SolutionFolder DefaultSolutionFolder {
			get {
				var itemsFolder = (SolutionFolder)RootFolder.Items.FirstOrDefault (item => item.Name == "Solution Items");
				if (itemsFolder == null) {
					itemsFolder = new SolutionFolder ();
					itemsFolder.Name = "Solution Items";
					RootFolder.AddItem (itemsFolder);
				}
				return itemsFolder;
			}
		}
		
		// Does not include solution folders
		public ReadOnlyCollection<SolutionItem> Items {
			get {
				if (solutionItems == null)
					solutionItems = GetAllSolutionItems ().ToList().AsReadOnly ();
				return solutionItems;
			}
		}

		public SolutionRunConfiguration StartupConfiguration {
			get {
				return startupSolutionConfiguration;
			}
			set {
				if (startupSolutionConfiguration != value) {
					var oldIt = StartupItem;
					startupSolutionConfiguration = value;
					NotifyModified ();
					OnStartupConfigurationChanged (null);
					if (oldIt != StartupItem)
						OnStartupItemChanged (null);
				}
			}
		}

		public SolutionItem StartupItem {
			get {
				return (StartupConfiguration as SingleItemSolutionRunConfiguration)?.Item;
			}
			set {
				if (value != StartupItem)
					StartupConfiguration = GetRunConfigurations ().OfType<SingleItemSolutionRunConfiguration> ().FirstOrDefault (co => co.Item == value);
			}
		}

		[Obsolete ("Use StartupConfiguration")]
		public bool SingleStartup {
			get {
				return StartupConfiguration is SingleItemSolutionRunConfiguration;
			}
			set {
			}
		}
		
		[Obsolete ("Use StartupConfiguration or MultiStartupRunConfigurations")]
		public List<SolutionItem> MultiStartupItems {
			get {
				var sc = StartupConfiguration as MultiItemSolutionRunConfiguration;
				if (sc != null)
					return sc.Items.Select (it => it.SolutionItem).ToList ();
				else
					return new List<SolutionItem> ();
			}
		}

		public MultiItemSolutionRunConfigurationCollection MultiStartupRunConfigurations {
			get { return runConfigurations; }
		}

		/// <summary>
		/// Gets the author information for this solution. If no specific information is set for this solution, it
		/// will return the author defined in the global settings.
		/// </summary>
		public AuthorInformation AuthorInformation {
			get {
				return LocalAuthorInformation ?? AuthorInformation.Default;
			}
		}

		/// <summary>
		/// Gets or sets the author information for this solution. It returns null if no specific information
		/// has been set for this solution.
		/// </summary>
		public AuthorInformation LocalAuthorInformation {
			get {
				return UserProperties.GetValue<AuthorInformation> ("AuthorInfo");
			}
			set {
				if (value != null)
					UserProperties.SetValue<AuthorInformation> ("AuthorInfo", value);
				else
					UserProperties.RemoveValue ("AuthorInfo");
			}
		}

		internal protected override async Task OnEndLoad ()
		{
			await base.OnEndLoad ();
			LoadItemProperties (UserProperties, RootFolder, "MonoDevelop.Ide.ItemProperties");

			bool startupConfigSet = false;

			var mconfigs = UserProperties.GetValue<MultiItemSolutionRunConfiguration []> ("MultiItemStartupConfigurations");
			if (mconfigs != null) {
				MultiStartupRunConfigurations.Clear ();
				MultiStartupRunConfigurations.AddRange (mconfigs);
			}

			var sitem = UserProperties.GetValue<string> ("StartupItem");
			if (!string.IsNullOrEmpty (sitem)) {
				// Old StartupItem property. Find the corresponding SingleItemSolutionRunConfiguration instance and get rid of the property.
				var startItemFileName = GetAbsoluteChildPath (sitem);
				var item = FindSolutionItem (startItemFileName);
				if (item != null) {
					StartupConfiguration = GetRunConfigurations ().OfType<SingleItemSolutionRunConfiguration> ().FirstOrDefault (c => c.Item == item);
					startupConfigSet = true;
				}
				UserProperties.RemoveValue ("StartupItem");
			}

			var sitems = UserProperties.GetValue<string []> ("StartupItems");
			if (sitems != null && sitems.Length > 0) {
				// Old StartupItems property. Create a corresponding MultiItemSolutionRunConfiguration.
				UserProperties.RemoveValue ("StartupItems");
				var multiStartupItems = sitems.Select (p => (string)GetAbsoluteChildPath (p)).Select (FindSolutionItem).Where (i => i != null);
				var msc = new MultiItemSolutionRunConfiguration ("Multi-Startup", "Multi-Startup");
				foreach (var si in multiStartupItems)
					msc.Items.Add (new StartupItem (si, si.GetDefaultRunConfiguration ()));
				runConfigurations.Add (msc);
				if (!startupConfigSet) {
					// If the config has not been set by StartupItem it means that this is an old solution that had been configured with multiple startup.
					// Select the multi-startup config in this case.
					StartupConfiguration = msc;
					startupConfigSet = true;
				}
			}

			if (!startupConfigSet) {
				// Startup configuration has not been set by legacy properties. Do it now.
				RefreshStartupConfiguration ();
			}
		}

		internal void RefreshStartupConfiguration ()
		{
			var sconfig = UserProperties.GetValue<string> ("StartupConfiguration");
			if (!string.IsNullOrEmpty (sconfig))
				StartupConfiguration = GetRunConfigurations ().FirstOrDefault (c => c.Id == sconfig);
			else
				StartupConfiguration = GetRunConfigurations ().FirstOrDefault ();
		}

		internal protected override Task OnSave (ProgressMonitor monitor)
		{
			return FileFormat.WriteFile (FileName, this, monitor);
		}

		protected override async Task OnSaveUserProperties ()
		{
			UserProperties.SetValue ("StartupConfiguration", (string)StartupConfiguration?.Id);
			UserProperties.SetValue ("MultiItemStartupConfigurations", MultiStartupRunConfigurations.ToArray ());
			              
			// Save the multi-startup configuration only if it is the one that's selected 

			CollectItemProperties (UserProperties, RootFolder, "MonoDevelop.Ide.ItemProperties");
			await base.OnSaveUserProperties ();
			CleanItemProperties (UserProperties, RootFolder, "MonoDevelop.Ide.ItemProperties");
		}
		
		void CollectItemProperties (PropertyBag props, SolutionFolderItem item, string path)
		{
			if (!item.UserProperties.IsEmpty && item.ParentFolder != null)
				props.SetValue (path, item.UserProperties);

			if (item is SolutionFolder sf) {
				foreach (SolutionFolderItem ci in sf.Items)
					CollectItemProperties (props, ci, path + "." + ci.Name);
			}
		}
		
		void CleanItemProperties (PropertyBag props, SolutionFolderItem item, string path)
		{
			props.RemoveValue (path);

			if (item is SolutionFolder sf) {
				foreach (SolutionFolderItem ci in sf.Items)
					CleanItemProperties (props, ci, path + "." + ci.Name);
			}
		}
		
		void LoadItemProperties (PropertyBag props, SolutionFolderItem item, string path)
		{
			PropertyBag info = props.GetValue<PropertyBag> (path);
			if (info != null) {
				item.LoadUserProperties (info);
				props.RemoveValue (path);
			}

			if (item is SolutionFolder sf) {
				foreach (SolutionFolderItem ci in sf.Items)
					LoadItemProperties (props, ci, path + "." + ci.Name);
			}
		}
		
		public void CreateDefaultConfigurations ()
		{
			foreach (SolutionItem item in Items.Where (it => it.SupportsBuild ())) {
				foreach (ItemConfiguration conf in item.Configurations) {
					SolutionConfiguration sc = Configurations [conf.Id];
					if (sc == null) {
						sc = new SolutionConfiguration (conf.Id);
						Configurations.Add (sc);
					}
					sc.AddItem (item);
				}
			}
		}
		
		ItemConfiguration IConfigurationTarget.CreateConfiguration (string name, ConfigurationKind kind)
		{
			return new SolutionConfiguration (name);
		}

		public SolutionConfiguration AddConfiguration (string id, bool createConfigForItems)
		{
			var conf = new SolutionConfiguration (id);
			foreach (SolutionItem item in Items.Where (it => it.SupportsBuild())) {
				if (createConfigForItems && item.GetConfiguration (new ItemConfigurationSelector (id)) == null) {
					SolutionItemConfiguration newc = item.CreateConfiguration (id);
					if (item.DefaultConfiguration != null)
						newc.CopyFrom (item.DefaultConfiguration);
					item.Configurations.Add (newc);
				}
				conf.AddItem (item);
			}
			configurations.Add (conf);
			return conf;
		}
		
		public override ReadOnlyCollection<string> GetConfigurations ()
		{
			var configs = new List<string> ();
			foreach (SolutionConfiguration conf in Configurations)
				configs.Add (conf.Id);
			return configs.AsReadOnly ();
		}
		
		public SolutionConfiguration GetConfiguration (ConfigurationSelector configuration)
		{
			return (SolutionConfiguration) configuration.GetConfiguration (this) ?? DefaultConfiguration;
		}

		public SolutionFolderItem GetSolutionItem (string itemId)
		{
			foreach (SolutionFolderItem item in Items)
				if (item.ItemId == itemId)
					return item;
			return null;
		}
		
		public SolutionItem FindSolutionItem (string fileName)
		{
			return RootFolder.FindSolutionItem (fileName);
		}
		
		public Project FindProjectByName (string name)
		{
			return RootFolder.FindProjectByName (name);
		}
		
		public IEnumerable<SolutionItem> GetAllSolutionItems ()
		{
			return GetAllItems<SolutionItem> ();
		}

		public IEnumerable<Project> GetAllProjects ()
		{
			return GetAllItems<Project> ();
		}

		public IEnumerable<Project> GetAllProjectsWithFlavor<T> () where T:ProjectExtension
		{
			return GetAllItems<Project> ().Where (p => p.HasFlavor<T> ());
		}

		/// <summary>
		/// Returns all flavor instances of the specified type that are implemented in projects of the solution
		/// </summary>
		/// <returns>All project flavors</returns>
		/// <typeparam name="T">Type of the flavor</typeparam>
		public IEnumerable<T> GetAllProjectFlavors<T> () where T:ProjectExtension
		{
			return GetAllItems<Project> ().Select (p => p.GetFlavor<T> ()).Where (p => p != null);
		}

		public ReadOnlyCollection<T> GetAllSolutionItemsWithTopologicalSort<T> (ConfigurationSelector configuration) where T: SolutionItem
		{
			var list = new List<T> (GetAllItems<T> ());
			return SolutionItem.TopologicalSort (list, configuration);
		}
		
		public ReadOnlyCollection<Project> GetAllProjectsWithTopologicalSort (ConfigurationSelector configuration)
		{
			return GetAllSolutionItemsWithTopologicalSort<Project> (configuration);
		}

		public override IEnumerable<Project> GetProjectsContainingFile (FilePath fileName)
		{
			return RootFolder.GetProjectsContainingFile (fileName);
		}
		
		public override bool ContainsItem (WorkspaceObject obj)
		{
			if (base.ContainsItem (obj))
				return true;
			
			foreach (SolutionFolderItem it in GetAllItems<SolutionFolderItem> ()) {
				if (it == obj)
					return true;
			}
			return false;
		}

		protected override IEnumerable<WorkspaceObject> OnGetChildren ()
		{
			yield return RootFolder;
		}
		
		public string Description {
			get {
				return description ?? string.Empty;
			}
			set {
				description = value;
				NotifyModified ();
			}
		}

		public string OutputDirectory 
		{
			get {
				if (outputdir == null) return DefaultOutputDirectory;
				else return outputdir;
			}
			set {
				if (value == DefaultOutputDirectory) outputdir = null;
				else outputdir = value;
				NotifyModified ();
			}
		}
		
		string DefaultOutputDirectory {
			get {
				return (BaseDirectory != FilePath.Null) ? BaseDirectory.Combine ("build", "bin") : FilePath.Null;
			}
		}
		
		public SolutionConfigurationCollection Configurations {
			get {
				return configurations;
			}
		}

		public SolutionConfiguration DefaultConfiguration {
			get {
				if (DefaultConfigurationId != null)
					return Configurations [DefaultConfigurationId];
				else
					return null;
			}
			set {
				if (value != null)
					DefaultConfigurationId = value.Id;
				else
					DefaultConfigurationId = null;
			}
		}
		
		public string DefaultConfigurationId {
			get {
				if (defaultConfiguration == null && configurations.Count > 0)
					DefaultConfigurationId = configurations [0].Id;
				return defaultConfiguration;
			}
			set {
				defaultConfiguration = value;
				UpdateDefaultConfigurations ();
			}
		}

		public ConfigurationSelector DefaultConfigurationSelector {
			get {
				if (defaultConfiguration == null && configurations.Count > 0)
					DefaultConfigurationId = configurations [0].Id;
				return new SolutionConfigurationSelector (DefaultConfigurationId);
			}
		}

		IItemConfigurationCollection IConfigurationTarget.Configurations {
			get {
				return Configurations;
			}
		}

		ItemConfiguration IConfigurationTarget.DefaultConfiguration {
			get {
				return DefaultConfiguration;
			}
			set {
				DefaultConfiguration = (SolutionConfiguration) value;
			}
		}
		
		[ItemProperty ("Policies", IsExternal = true, SkipEmpty = true)]
		public PolicyBag Policies {
			get { return RootFolder.Policies; }
			//this is for deserialisation
			internal set { RootFolder.Policies = value; }
		}
		
		PolicyContainer IPolicyProvider.Policies {
			get {
				return Policies;
			}
		}

		public string Version {
			get {
				return version ?? string.Empty;
			}
			set {
				version = value;
				foreach (SolutionItem item in GetAllItems<SolutionItem> ()) {
					if (item.SyncVersionWithSolution)
						item.Version = value;
				}
			}
		}
		
		protected override void OnDispose ()
		{
			RootFolder.Dispose ();
			Counters.SolutionsLoaded--;
			msbuildEngineManager.Dispose ();
			base.OnDispose ();
		}

		internal bool IsSolutionItemEnabled (string solutionItemPath)
		{
			solutionItemPath = GetRelativeChildPath (Path.GetFullPath (solutionItemPath));
			var list = UserProperties.GetValue<List<string>> ("DisabledProjects");
			return list == null || !list.Contains (solutionItemPath);
		}

		internal void SetSolutionItemEnabled (string solutionItemPath, bool enabled)
		{
			solutionItemPath = GetRelativeChildPath (Path.GetFullPath (solutionItemPath));
			var list = UserProperties.GetValue<List<string>> ("DisabledProjects");
			if (!enabled) {
				if (list == null)
					list = new List<string> ();
				if (!list.Contains (solutionItemPath))
					list.Add (solutionItemPath);
				UserProperties.SetValue ("DisabledProjects", list);
			} else if (list != null) {
				list.Remove (solutionItemPath);
				if (list.Count == 0)
					UserProperties.RemoveValue ("DisabledProjects");
				else
					UserProperties.SetValue ("DisabledProjects", list);
			}
		}

		internal void UpdateDefaultConfigurations ()
		{
			if (DefaultConfiguration != null) {
				foreach (SolutionConfigurationEntry cce in DefaultConfiguration.Configurations) {
					if (cce.Item != null)
						cce.Item.DefaultConfigurationId = cce.ItemConfiguration;
				}
			}
		}	 

		bool IBuildTarget.CanBuild (ConfigurationSelector configuration)
		{
			return true;
		}

		internal void OnRunConfigurationsAdded (IEnumerable<SolutionRunConfiguration> items)
		{
			NotifyRunConfigurationsChanged ();
		}

		internal void OnRunConfigurationRemoved (IEnumerable<SolutionRunConfiguration> items)
		{
			NotifyRunConfigurationsChanged ();
		}

		internal void NotifyRunConfigurationsChanged ()
		{
			RunConfigurationsChanged?.Invoke (this, EventArgs.Empty);
		}

		public Task<BuildResult> Clean (ProgressMonitor monitor, string configuration)
		{
			return Clean (monitor, (SolutionConfigurationSelector) configuration);
		}

		public Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext = null)
		{
			return SolutionExtension.Clean (monitor, configuration, operationContext);
		}

		public Task<BuildResult> Build (ProgressMonitor monitor, string configuration, OperationContext operationContext = null)
		{
			return SolutionExtension.Build (monitor, (SolutionConfigurationSelector) configuration, operationContext);
		}

		Task<BuildResult> IBuildTarget.Build (ProgressMonitor monitor, ConfigurationSelector configuration, bool buildReferencedTargets, OperationContext operationContext)
		{
			return Build (monitor, configuration, operationContext);
		}

		public Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext = null)
		{
			return SolutionExtension.Build (monitor, configuration, operationContext);
		}

		[Obsolete("This method will be removed in future releases")]
		public bool NeedsBuilding (ConfigurationSelector configuration)
		{
			return SolutionExtension.NeedsBuilding (configuration);
		}

		public Task Execute (ProgressMonitor monitor, ExecutionContext context, string configuration)
		{
			return Execute (monitor, context, (SolutionConfigurationSelector) configuration, StartupConfiguration);
		}

		public Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return Execute (monitor, context, configuration, StartupConfiguration);
		}

		public Task Execute (ProgressMonitor monitor, ExecutionContext context, string configuration, SolutionRunConfiguration runConfiguration)
		{
			return Execute (monitor, context, (SolutionConfigurationSelector)configuration, runConfiguration);
		}

		public Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
		{
			return SolutionExtension.Execute (monitor, context, configuration, runConfiguration ?? StartupConfiguration);
		}

		Task IRunTarget.Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, RunConfiguration runConfiguration)
		{
			if (runConfiguration != null && !(runConfiguration is SolutionRunConfiguration))
				throw new ArgumentException ("Invalid configuration type");
			return Execute (monitor, context, configuration, (SolutionRunConfiguration)runConfiguration);
		}

		public Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return PrepareExecution (monitor, context, configuration, StartupConfiguration);
		}

		public Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
		{
			return SolutionExtension.PrepareExecution (monitor, context, configuration, runConfiguration);
		}

		Task IRunTarget.PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, RunConfiguration runConfiguration)
		{
			if (runConfiguration != null && !(runConfiguration is SolutionRunConfiguration))
				throw new ArgumentException ("Invalid configuration type");
			return PrepareExecution (monitor, context, configuration, (SolutionRunConfiguration)runConfiguration);
		}

		public bool CanExecute (ExecutionContext context, string configuration)
		{
			return CanExecute (context, (SolutionConfigurationSelector) configuration, StartupConfiguration);
		}

		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return CanExecute (context, configuration, StartupConfiguration);
		}

		public bool CanExecute (ExecutionContext context, string configuration, SolutionRunConfiguration runConfiguration)
		{
			return CanExecute (context, (SolutionConfigurationSelector)configuration, runConfiguration);
		}

		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
		{
			return SolutionExtension.CanExecute (context, configuration, runConfiguration);
		}

		bool IRunTarget.CanExecute (ExecutionContext context, ConfigurationSelector configuration, RunConfiguration runConfiguration)
		{
			if (runConfiguration != null && !(runConfiguration is SolutionRunConfiguration))
				throw new ArgumentException ("Invalid configuration type");
			return CanExecute (context, configuration, (SolutionRunConfiguration)runConfiguration);
		}

		public new IEnumerable<IBuildTarget> GetExecutionDependencies ()
		{
			return SolutionExtension.OnGetExecutionDependencies ();
		}

		protected virtual IEnumerable<IBuildTarget> OnGetExecutionDependencies ()
		{
			yield return this;
		}

		public IEnumerable<SolutionRunConfiguration> GetRunConfigurations ()
		{
			return SolutionExtension.OnGetRunConfigurations ();
		}

		IEnumerable<RunConfiguration> IRunTarget.GetRunConfigurations ()
		{
			return GetRunConfigurations ();
		}

		protected virtual IEnumerable<SolutionRunConfiguration> OnGetRunConfigurations ()
		{
			IEnumerable<SolutionRunConfiguration> res = runConfigurations;
			foreach (var it in GetAllSolutionItems ().Where (i => i.SupportsExecute ())) {
				var configs = it.GetRunConfigurations ().Select (c => new SingleItemSolutionRunConfiguration (it, c)).ToList ();
				if (configs.Count == 0)
					res = res.Concat (new SingleItemSolutionRunConfiguration (it, null));
				else
					res = res.Concat (configs);
			}
			return res;
		}

		public IEnumerable<ExecutionTarget> GetExecutionTargets (string configuration)
		{
			return GetExecutionTargets ((SolutionConfigurationSelector) configuration, StartupConfiguration);
		}

		public IEnumerable<ExecutionTarget> GetExecutionTargets (ConfigurationSelector configuration)
		{
			return GetExecutionTargets (configuration, StartupConfiguration);
		}

		public IEnumerable<ExecutionTarget> GetExecutionTargets (string configuration, SolutionRunConfiguration runConfiguration)
		{
			return GetExecutionTargets ((SolutionConfigurationSelector)configuration, runConfiguration);
		}

		public IEnumerable<ExecutionTarget> GetExecutionTargets (ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
		{
			return SolutionExtension.GetExecutionTargets (this, configuration, runConfiguration);
		}

		IEnumerable<ExecutionTarget> IRunTarget.GetExecutionTargets (ConfigurationSelector configuration, RunConfiguration runConfiguration)
		{
			if (runConfiguration != null && !(runConfiguration is SolutionRunConfiguration))
				throw new ArgumentException ("Invalid configuration type");
			return SolutionExtension.GetExecutionTargets (this, configuration, (SolutionRunConfiguration)runConfiguration);
		}

		IEnumerable<ExecutionTarget> OnGetExecutionTargets (ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
		{
			return SolutionExtension.GetExecutionTargets (this, configuration);
		}

		/// <summary>
		/// To be called when a new build session starts
		/// </summary>
		/// <returns>True if the session could be started, False otherwise (such as for example, when there is a session already in progress)</returns>
		/// <param name="monitor">Monitor.</param>
		/// <param name="configuration">Build configuration.</param>
		/// <param name="operationContext">Operation context.</param>
		internal async Task<bool> BeginBuildOperation (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			if (operationContext.BatchOperationStarted)
				return false;
			operationContext.BatchOperationStarted = true;
			await SolutionExtension.OnBeginBuildOperation (monitor, configuration, operationContext);
			return true;
		}

		/// <summary>
		/// To be called when the currently active build session ends
		/// </summary>
		/// <param name="monitor">Monitor.</param>
		/// <param name="configuration">Build configuration.</param>
		/// <param name="operationContext">Operation context.</param>
		/// <param name="result">Build results.</param>
		internal Task EndBuildOperation (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext, BuildResult result)
		{
			if (result == null)
				result = new BuildResult ().AddError ("Build operation failed");
			operationContext.BatchOperationStarted = false;
			return SolutionExtension.OnEndBuildOperation (monitor, configuration, operationContext, result);
		}

		/*protected virtual*/
		Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return RootFolder.Build (monitor, configuration, operationContext:operationContext);
		}

		[Obsolete("This method will be removed in future releases")]
		/*protected virtual*/ bool OnGetNeedsBuilding (ConfigurationSelector configuration)
		{
			return RootFolder.NeedsBuilding (configuration);
		}

		/*protected virtual*/ Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{	
			return RootFolder.Clean (monitor, configuration, operationContext);
		}

		/*protected virtual*/ bool OnGetCanExecute(ExecutionContext context, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
		{
			if (runConfiguration is SingleItemSolutionRunConfiguration ssc)
				return ssc.Item.CanExecute (context, configuration, ssc.RunConfiguration);

			if (runConfiguration is MultiItemSolutionRunConfiguration msc) {
				var multiProject = context.ExecutionTarget as MultiProjectExecutionTarget;
				foreach (StartupItem it in msc.Items) {
					var localContext = context;
					//Set project specific execution target to context if exists
					if (multiProject?.GetTarget (it.SolutionItem) != null)
						localContext = new ExecutionContext (context.ExecutionHandler, context.ExternalConsoleFactory, multiProject?.GetTarget (it.SolutionItem));
					if (it.SolutionItem.CanExecute (localContext, configuration, it.RunConfiguration))
						return true;
				}
				return false;
			}
			return false;
		}
		
		/*protected virtual*/ async Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
		{
			if (runConfiguration is SingleItemSolutionRunConfiguration ssc) {
				await ssc.Item.Execute (monitor, context, configuration, ssc.RunConfiguration);
				return;
			}
			if (runConfiguration is MultiItemSolutionRunConfiguration msc) {
				var tasks = new List<Task> ();
				var monitors = new List<AggregatedProgressMonitor> ();
				monitor.BeginTask ("Executing projects", 1);

				var multiProject = context.ExecutionTarget as MultiProjectExecutionTarget;
				foreach (StartupItem it in msc.Items) {
					var localContext = context;
					//Set project specific execution target to context if exists
					if (multiProject?.GetTarget (it.SolutionItem) != null)
						localContext = new ExecutionContext (context.ExecutionHandler, context.ConsoleFactory, multiProject?.GetTarget (it.SolutionItem));
					if (!it.SolutionItem.CanExecute (localContext, configuration, it.RunConfiguration))
						continue;
					var mon = new AggregatedProgressMonitor ();
					mon.AddFollowerMonitor (monitor, MonitorAction.ReportError | MonitorAction.ReportWarning | MonitorAction.FollowerCancel);
					monitors.Add (mon);
					tasks.Add (it.SolutionItem.Execute (mon, localContext, configuration, it.RunConfiguration));
				}
				try {
					await Task.WhenAll (tasks);
				} catch (Exception ex) {
					LoggingService.LogError ("Project execution failed", ex);
				} finally {
					foreach (var m in monitors)
						m.Dispose ();
				}

				monitor.EndTask ();
			}
		}

		/*protected virtual*/ Task OnPrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
		{
			return Task.FromResult (0);
		}

		/*protected virtual*/ void OnStartupItemChanged(EventArgs e)
		{
			StartupItemChanged?.Invoke (this, e);
		}

		void OnStartupConfigurationChanged (EventArgs e)
		{
			StartupConfigurationChanged?.Invoke (this, e);
		}

		/// <summary>
		/// Builds a set of SolutionItems from this solution and their dependencies. They will be built in parallel, and common dependencies will be deduplicated.
		/// </summary>
		public async Task<BuildResult> CleanItems (ProgressMonitor monitor, ConfigurationSelector configuration, IEnumerable<SolutionItem> items, OperationContext operationContext = null, string beginTaskMessage = null)
		{
			SolutionConfiguration slnConf = GetConfiguration (configuration);
			if (slnConf == null)
				return new BuildResult ();

			ReadOnlyCollection<SolutionItem> sortedItems;
			try {
				sortedItems = GetItemsAndDependenciesSortedForBuild (items, slnConf, configuration);
			} catch (CyclicDependencyException) {
				monitor.ReportError (GettextCatalog.GetString ("Cyclic dependencies are not supported."), null);
				return new BuildResult ("", 1, 1);
			}

			if (operationContext == null)
				operationContext = new OperationContext ();

			monitor.BeginTask (
				beginTaskMessage ?? GettextCatalog.GetString ("Cleaning {0} items in solution {1} ({2})", sortedItems.Count, Name, configuration.ToString ()),
				sortedItems.Count
			);

			bool operationStarted = false;
			BuildResult result = null;

			try {
				operationStarted = await BeginBuildOperation (monitor, configuration, operationContext);

				return result = await RunParallelBuildOperation (monitor, configuration, slnConf, sortedItems, operationContext, (ProgressMonitor m, SolutionItem item) => {
					return item.Clean (m, configuration, operationContext);
				}, false, false);
			} finally {
				if (operationStarted)
					await EndBuildOperation (monitor, configuration, operationContext, result);
				monitor.EndTask ();
			}
		}

		/// <summary>
		/// Builds a set of SolutionItems from this solution and their dependencies. They will be built in parallel, and common dependencies will be deduplicated.
		/// </summary>
		public async Task<BuildResult> BuildItems (ProgressMonitor monitor, ConfigurationSelector configuration, IEnumerable<SolutionItem> items, OperationContext operationContext = null, string beginTaskMessage = null)
		{
			SolutionConfiguration slnConf = GetConfiguration (configuration);
			if (slnConf == null)
				return new BuildResult ();

			ReadOnlyCollection<SolutionItem> sortedItems;

			try {
				sortedItems = GetItemsAndDependenciesSortedForBuild (items, slnConf, configuration);
			} catch (CyclicDependencyException) {
				monitor.ReportError (GettextCatalog.GetString ("Cyclic dependencies are not supported."), null);
				return new BuildResult ("", 1, 1);
			}

			if (operationContext == null)
				operationContext = new OperationContext ();

			bool operationStarted = false;
			BuildResult result = null;

			try {
				monitor.BeginTask (
					beginTaskMessage ?? GettextCatalog.GetString ("Building {0} items in solution {1} ({2})", sortedItems.Count, Name, configuration.ToString ()),
					sortedItems.Count
				);

				operationStarted = await BeginBuildOperation (monitor, configuration, operationContext);

				return result = await RunParallelBuildOperation (monitor, configuration, slnConf, sortedItems, operationContext, (ProgressMonitor m, SolutionItem item) => {
					return item.Build (m, configuration, false, operationContext);
				}, false, Runtime.Preferences.SkipBuildingUnmodifiedProjects);

			} finally {
				if (operationStarted)
					await EndBuildOperation (monitor, configuration, operationContext, result);
				monitor.EndTask ();
			}
		}

		static async Task<BuildResult> RunParallelBuildOperation (
			ProgressMonitor monitor, ConfigurationSelector configuration, SolutionConfiguration slnConf,
			IEnumerable<SolutionItem> sortedItems, OperationContext operationContext,
			Func<ProgressMonitor, SolutionItem, Task<BuildResult>> buildAction,
			bool ignoreFailed, bool skipUnmodified)
		{
			var toBuild = new List<SolutionItem> (sortedItems);
			var cres = new BuildResult { BuildCount = 0 };

			// Limit the number of concurrent builders to processors / 2

			var slotScheduler = new TaskSlotScheduler (Environment.ProcessorCount / 2);

			// Create a dictionary with the status objects of all items

			var buildStatus = new Dictionary<SolutionItem, BuildStatus> ();
			foreach (var it in toBuild)
				buildStatus.Add (it, new BuildStatus ());

			// Start the build tasks for all items

			foreach (var itemToBuild in toBuild) {
				if (monitor.CancellationToken.IsCancellationRequested)
					break;

				var item = itemToBuild;

				var myStatus = buildStatus[item];

				if (skipUnmodified && item is Project p && !p.FastCheckNeedsBuild (configuration, (operationContext as TargetEvaluationContext) ?? new TargetEvaluationContext (operationContext))) {
					myStatus.Result = BuildResult.CreateUpToDate (item);
					myStatus.Task = Task.CompletedTask;
					monitor.Step (1);
					continue;
				}

				if (!slnConf.BuildEnabledForItem (item)) {
					myStatus.Result = BuildResult.CreateSkipped (item);
					myStatus.Task = Task.CompletedTask;

					var mapped = slnConf.GetMappedConfiguration (item) ?? slnConf.Id;
					monitor.Log.WriteLine (
						"------ {0} ------\n{1}",
						GettextCatalog.GetString ("Skipped Build: Project: {0}, Configuration: {1}", item.Name, mapped),
						GettextCatalog.GetString ("Project not selected to build for this solution configuration")
					);
					monitor.Step (1);
					continue;
				}

				var myMonitor = monitor.BeginAsyncStep (1);

				// Get a list of the status objects for all items on which this one depends

				var refStatus = item.GetReferencedItems (configuration).Select (it => {
					buildStatus.TryGetValue (it, out var bs);
					return bs;
				}).Where (t => t != null).ToArray ();

				// Build the item when all its dependencies have been built

				var refTasks = refStatus.Select (bs => bs.Task);

				myStatus.Task = Task.WhenAll (refTasks).ContinueWith (async t => {
					if (!ignoreFailed && (refStatus.Any (bs => bs.Failed) || t.IsFaulted)) {
						myStatus.Failed = true;
					} else {
						using (await slotScheduler.GetTaskSlot ())
							myStatus.Result = await buildAction (myMonitor, item);
						myStatus.Failed = myStatus.Result != null && myStatus.Result.ErrorCount > 0;
					}
					myMonitor.Dispose ();
				}, Runtime.MainTaskScheduler).Unwrap ();

				if (!Runtime.Preferences.ParallelBuild.Value)
					await myStatus.Task;
			}

			// Wait for all tasks to end

			await Task.WhenAll (buildStatus.Values.Select (bs => bs.Task));

			// Generate the errors in the order they were supposed to build

			foreach (var it in toBuild) {
				if (buildStatus.TryGetValue (it, out var bs) && bs.Result != null)
					cres.Append (bs.Result);
			}

			return cres;
		}

		class BuildStatus
		{
			public bool Failed;
			public Task Task;
			public BuildResult Result;
		}

		/// <summary>
		/// Given a set of SolutionItems from this solution, collects them and their buildable dependencies, and topologically sorts them in preparation for a build.
		/// </summary>
		/// <remarks>Nonrecursively includes skipped items so the build can report them.</remarks>
		ReadOnlyCollection<SolutionItem> GetItemsAndDependenciesSortedForBuild (IEnumerable<SolutionItem> items, SolutionConfiguration slnConf, ConfigurationSelector configuration)
		{
			var collected = new HashSet<SolutionItem> ();

			foreach (var item in items) {
				if (item.ParentSolution != this) {
					throw new ArgumentException ("All items must be in this solution", nameof(items));
				}
				CollectBuildableDeps (item);
			}

			void CollectBuildableDeps (SolutionItem item)
			{
				//we add skipped items but this their deps, so the build can report skips
				if (collected.Add (item) && slnConf.BuildEnabledForItem (item)) {
					foreach (var it in item.GetReferencedItems (configuration)) {
						CollectBuildableDeps (it);
					}
				}
			}

			return SolutionItem.TopologicalSort (collected, configuration);
		}

		[ThreadSafe]
		public MSBuildFileFormat FileFormat {
			get {
				return format;
			}
			internal set {
				format = value;
			}
		}

		public void ConvertToFormat (MSBuildFileFormat format)
		{
			SolutionExtension.OnSetFormat (format);
		}

		[ThreadSafe]
		public bool SupportsFormat (MSBuildFileFormat format)
		{
			return true;
		}

		void OnSetFormat (MSBuildFileFormat format)
		{
			this.format = format;
			if (!string.IsNullOrEmpty (FileName))
				FileName = format.GetValidFormatName (this, FileName);
			foreach (SolutionItem item in GetAllItems<SolutionItem> ())
				item.ConvertToFormat (format);
		}

		bool OnGetSupportsFormat (MSBuildFileFormat format)
		{
			return GetAllItems<SolutionItem> ().All (p => p.SupportsFormat (format));
		}

		protected override IEnumerable<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			List<FilePath> files = base.OnGetItemFiles (includeReferencedFiles).ToList ();
			if (includeReferencedFiles) {
				foreach (SolutionItem item in GetAllItems<SolutionItem> ())
					files.AddRange (item.GetItemFiles (true));
			}
			return files;
		}
		
#region Notifications from children
		
		internal /*protected virtual*/ void OnSolutionItemAdded (SolutionItemChangeEventArgs args)
		{
			if (IsShared)
				args.Solution.SetShared ();
			
			solutionItems = null;

			if (args.SolutionItem is SolutionFolder sf) {
				foreach (SolutionFolderItem eitem in sf.GetAllItems<SolutionFolderItem> ())
					SetupNewItem (eitem, null);
			} else {
				SetupNewItem (args.SolutionItem, args.ReplacedItem);
			}

			OnRootDirectoriesChanged ();

			SolutionItemAdded?.Invoke (this, args);
		}
		
		void SetupNewItem (SolutionFolderItem item, SolutionFolderItem replacedItem)
		{
			if (item is SolutionItem eitem) {
				eitem.ConvertToFormat (FileFormat);
				eitem.NeedsReload = false;
				if (eitem.SupportsConfigurations () || replacedItem != null) {
					if (replacedItem == null) {
						// Register the new entry in every solution configuration
						foreach (SolutionConfiguration conf in Configurations)
							conf.AddItem (eitem);
						// If there is no startup project or it is an invalid one, use the new project as startup if possible
						if (!Loading && (StartupItem == null || !StartupItem.SupportsExecute ()) && eitem.SupportsExecute ())
							StartupItem = eitem;
					} else {
						// Reuse the configuration information of the replaced item
						foreach (SolutionConfiguration conf in Configurations)
							conf.ReplaceItem ((SolutionItem)replacedItem, eitem);

						if (StartupItem == replacedItem)
							StartupItem = eitem;

						foreach (var sc in MultiStartupRunConfigurations.OfType<MultiItemSolutionRunConfiguration> ())
							sc.ReplaceItem ((SolutionItem)replacedItem, eitem);
					}
				}
			}
		}
		
		internal /*protected virtual*/ void OnSolutionItemRemoved (SolutionItemChangeEventArgs args)
		{
			solutionItems = null;

			if (args.SolutionItem is SolutionFolder sf) {
				foreach (SolutionItem eitem in sf.GetAllItems<SolutionItem> ())
					DetachItem (eitem, args.Reloading);
			} else {
				if (args.SolutionItem is SolutionItem item)
					DetachItem (item, args.Reloading);
			}

			OnRootDirectoriesChanged ();

			SolutionItemRemoved?.Invoke (this, args);
		}
		
		void DetachItem (SolutionItem item, bool reloading)
		{
			item.NeedsReload = false;
			if (!reloading) {
				foreach (SolutionConfiguration conf in Configurations)
					conf.RemoveItem (item);
				if (item is Project)
					RemoveReferencesToProject ((Project)item);

				if (StartupItem == item)
					StartupItem = null;

				foreach (var sc in MultiStartupRunConfigurations)
					sc.RemoveItem (item);
			}
			
			// Update the file name because the file format may have changed
			item.FileName = item.FileName;
		}
		
		void RemoveReferencesToProject (Project projectToRemove)
		{
			if (projectToRemove == null)
				return;

			foreach (DotNetProject project in GetAllItems <DotNetProject>()) {
				if (project == projectToRemove)
					continue;
				
				var toDelete = new List<ProjectReference> ();
				
				foreach (ProjectReference pref in project.References) {
					if (pref.ReferenceType == ReferenceType.Project && pref.Reference == projectToRemove.Name)
							toDelete.Add (pref);
				}
				
				foreach (ProjectReference pref in toDelete) {
					project.References.Remove (pref);
				}
			}
		}

		SolutionLoadContext currentLoadContext;

		internal void ReadSolution (ProgressMonitor monitor)
		{
			var sln = new SlnFile ();
			sln.Read (FileName);

			using (currentLoadContext = new SolutionLoadContext (this))
				SolutionExtension.OnReadSolution (monitor, sln);
			currentLoadContext = null;
		}

		/*protected virtual*/ void OnReadSolution (ProgressMonitor monitor, SlnFile file)
		{
			FileFormat.SlnFileFormat.LoadSolution (this, file, monitor, currentLoadContext);
			var s = file.Sections.GetSection ("MonoDevelopProperties", SlnSectionType.PreProcess); 
			if (s != null)
				s.ReadObjectProperties (this);
		}

		internal void ReadConfigurationData (ProgressMonitor monitor, SlnPropertySet properties, SolutionConfiguration configuration)
		{
			SolutionExtension.OnReadConfigurationData (monitor, properties, configuration);
		}

		/*protected virtual*/ void OnReadConfigurationData (ProgressMonitor monitor, SlnPropertySet properties, SolutionConfiguration configuration)
		{
			// Do nothing by default
		}

		internal void ReadSolutionFolderItemData (ProgressMonitor monitor, SlnPropertySet properties, SolutionFolderItem item)
		{
			SolutionExtension.OnReadSolutionFolderItemData (monitor, properties, item);
		}

		/*protected virtual*/ void OnReadSolutionFolderItemData (ProgressMonitor monitor, SlnPropertySet properties, SolutionFolderItem item)
		{
			if (item is SolutionItem)
				((SolutionItem)item).ReadSolutionData (monitor, properties);
		}
			
		internal void WriteSolution (ProgressMonitor monitor, SlnFile file)
		{
			SolutionExtension.OnWriteSolution (monitor, file);
		}
		
		/*protected virtual*/ void OnWriteSolution (ProgressMonitor monitor, SlnFile file)
		{
			FileFormat.SlnFileFormat.WriteFileInternal (file, this, monitor);
			var s = file.Sections.GetOrCreateSection ("MonoDevelopProperties", SlnSectionType.PreProcess); 
			s.SkipIfEmpty = true;
			s.WriteObjectProperties (this);
		}

		internal void WriteConfigurationData (ProgressMonitor monitor, SlnPropertySet properties, SolutionConfiguration configuration)
		{
			SolutionExtension.OnWriteConfigurationData (monitor, properties, configuration);
		}

		/*protected virtual*/ void OnWriteConfigurationData (ProgressMonitor monitor, SlnPropertySet properties, SolutionConfiguration configuration)
		{
			// Do nothing by default
		}

		internal void WriteSolutionFolderItemData (ProgressMonitor monitor, SlnPropertySet properties, SolutionFolderItem item)
		{
			SolutionExtension.OnWriteSolutionFolderItemData (monitor, properties, item);
		}

		/*protected virtual*/ void OnWriteSolutionFolderItemData (ProgressMonitor monitor, SlnPropertySet properties, SolutionFolderItem item)
		{
			if (item is SolutionItem)
				((SolutionItem)item).WriteSolutionData (monitor, properties);
		}

		internal void NotifyConfigurationsChanged ()
		{
			OnConfigurationsChanged ();
		}
		
		internal /*protected virtual*/ void OnFileAddedToProject (ProjectFileEventArgs args)
		{
			FileAddedToProject?.Invoke (this, args);
		}
		
		internal /*protected virtual*/ void OnFileRemovedFromProject (ProjectFileEventArgs args)
		{
			FileRemovedFromProject?.Invoke (this, args);
		}
		
		internal /*protected virtual*/ void OnFileChangedInProject (ProjectFileEventArgs args)
		{
			FileChangedInProject?.Invoke (this, args);
		}
		
		internal /*protected virtual*/ void OnFilePropertyChangedInProject (ProjectFileEventArgs args)
		{
			FilePropertyChangedInProject?.Invoke (this, args);
		}
		
		internal /*protected virtual*/ void OnFileRenamedInProject (ProjectFileRenamedEventArgs args)
		{
			FileRenamedInProject?.Invoke (this, args);
		}
		
		internal /*protected virtual*/ void OnReferenceAddedToProject (ProjectReferenceEventArgs args)
		{
			ReferenceAddedToProject?.Invoke (this, args);
		}
		
		internal /*protected virtual*/ void OnReferenceRemovedFromProject (ProjectReferenceEventArgs args)
		{
			ReferenceRemovedFromProject?.Invoke (this, args);
		}
		
		internal /*protected virtual*/ void OnEntryModified (SolutionItemModifiedEventArgs args)
		{
			EntryModified?.Invoke (this, args);
		}
		
		internal /*protected virtual*/ void OnEntrySaved (SolutionItemSavedEventArgs args)
		{
			EntrySaved?.Invoke (this, args);
		}
		
		internal /*protected virtual*/ void OnItemReloadRequired (SolutionItemEventArgs args)
		{
			ItemReloadRequired?.Invoke (this, args);
		}
		
#endregion
		
		public event EventHandler StartupItemChanged;
		public event EventHandler StartupConfigurationChanged;
		public event EventHandler RunConfigurationsChanged;

		public event SolutionItemChangeEventHandler SolutionItemAdded;
		public event SolutionItemChangeEventHandler SolutionItemRemoved;
		
		public event ProjectFileEventHandler FileAddedToProject;
		public event ProjectFileEventHandler FileRemovedFromProject;
		public event ProjectFileEventHandler FileChangedInProject;
		public event ProjectFileEventHandler FilePropertyChangedInProject;
		public event ProjectFileRenamedEventHandler FileRenamedInProject;
		public event ProjectReferenceEventHandler ReferenceAddedToProject;
		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;
		public event SolutionItemModifiedEventHandler EntryModified;
		public event SolutionItemSavedEventHandler EntrySaved;
		public event EventHandler<SolutionItemEventArgs> ItemReloadRequired;

		protected override IEnumerable<WorkspaceObjectExtension> CreateDefaultExtensions ()
		{
			return base.CreateDefaultExtensions ().Concat (Enumerable.Repeat (new DefaultSolutionExtension (), 1));
		}

		internal class DefaultSolutionExtension: SolutionExtension
		{
			internal protected override IEnumerable<FilePath> GetItemFiles (bool includeReferencedFiles)
			{
				return Solution.OnGetItemFiles (includeReferencedFiles);
			}

			internal protected override Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
			{
				return Solution.OnBuild (monitor, configuration, operationContext);
			}

			[Obsolete("This method will be removed in future releases")]
			internal protected override bool NeedsBuilding (ConfigurationSelector configuration)
			{
				return Solution.OnGetNeedsBuilding (configuration);
			}

			internal protected override Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
			{
				return Solution.OnClean (monitor, configuration, operationContext);
			}

			internal protected override Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
			{
				return Solution.OnExecute (monitor, context, configuration, runConfiguration);
			}

			internal protected override Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
			{
				return Solution.OnPrepareExecution (monitor, context, configuration, runConfiguration);
			}

			internal protected override bool CanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
			{
				return Solution.OnGetCanExecute (context, configuration, runConfiguration);
			}

			internal protected override void OnReadSolution (ProgressMonitor monitor, SlnFile file)
			{
				Solution.OnReadSolution (monitor, file);
			}

			internal protected override void OnWriteSolution (ProgressMonitor monitor, SlnFile file)
			{
				Solution.OnWriteSolution (monitor, file);
			}

			internal protected override void OnWriteSolutionFolderItemData (ProgressMonitor monitor, SlnPropertySet properties, SolutionFolderItem item)
			{
				Solution.OnWriteSolutionFolderItemData (monitor, properties, item);
			}

			internal protected override void OnWriteConfigurationData (ProgressMonitor monitor, SlnPropertySet properties, SolutionConfiguration configuration)
			{
				Solution.OnWriteConfigurationData (monitor, properties, configuration);
			}

			internal protected override void OnReadConfigurationData (ProgressMonitor monitor, SlnPropertySet properties, SolutionConfiguration configuration)
			{
				Solution.OnReadConfigurationData (monitor, properties, configuration);
			}

			internal protected override void OnReadSolutionFolderItemData (ProgressMonitor monitor, SlnPropertySet properties, SolutionFolderItem item)
			{
				Solution.OnReadSolutionFolderItemData (monitor, properties, item);
			}

			internal protected override IEnumerable<ExecutionTarget> GetExecutionTargets (Solution solution, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
			{
				return Solution.OnGetExecutionTargets (configuration, runConfiguration);
			}

			internal protected override IEnumerable<ExecutionTarget> GetExecutionTargets (Solution solution, ConfigurationSelector configuration)
			{
				yield break;
			}

			protected internal override IEnumerable<IBuildTarget> OnGetExecutionDependencies ()
			{
				return Solution.OnGetExecutionDependencies ();
			}

			internal protected override bool OnGetSupportsFormat (MSBuildFileFormat format)
			{
				return Solution.OnGetSupportsFormat (format);
			}

			internal protected override void OnSetFormat (MSBuildFileFormat value)
			{
				Solution.OnSetFormat (value);
			}

			internal protected override IEnumerable<SolutionRunConfiguration> OnGetRunConfigurations ()
			{
				return Solution.OnGetRunConfigurations ();
			}

			internal protected override Task OnBeginBuildOperation (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
			{
				return Task.CompletedTask;
			}

			internal protected override Task OnEndBuildOperation (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext, BuildResult result)
			{
				return Task.CompletedTask;
			}
		}
	}

	[Mono.Addins.Extension]
	class SolutionTagProvider: StringTagProvider<Solution>, IStringTagProvider
	{
		public override IEnumerable<StringTagDescription> GetTags ()
		{
			yield return new StringTagDescription ("SolutionFile", GettextCatalog.GetString ("Solution File"));
			yield return new StringTagDescription ("SolutionName", GettextCatalog.GetString ("Solution Name"));
			yield return new StringTagDescription ("SolutionDir", GettextCatalog.GetString ("Solution Directory"));
		}
		
		public override object GetTagValue (Solution sol, string tag)
		{
			switch (tag) {
				case "SOLUTIONNAME": return sol.Name;
				case "COMBINEFILENAME":
				case "SOLUTIONFILE": return sol.FileName;
				case "SOLUTIONDIR": return sol.BaseDirectory;
			}
			throw new NotSupportedException ();
		}
	}

	public class SolutionLoadContext: IDisposable
	{
		public SolutionLoadContext (Solution solution)
		{
			Solution = solution;
		}

		public event EventHandler LoadCompleted;

		public Solution Solution { get; private set; }

		void IDisposable.Dispose ()
		{
			LoadCompleted?.Invoke (this, EventArgs.Empty);
		}
	}
}
