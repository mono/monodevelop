// SolutionEntityItem.cs
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
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Core.Execution;
using Mono.Addins;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Collections;
using System.Threading.Tasks;
using MonoDevelop.Projects.MSBuild;
using System.Collections.Immutable;

namespace MonoDevelop.Projects
{
	public abstract class SolutionItem : SolutionFolderItem, IWorkspaceFileObject, IConfigurationTarget, IBuildTarget, IMSBuildFileObject, IRunTarget
	{
		internal object MemoryProbe = Counters.ItemsInMemory.CreateMemoryProbe ();

		int loading;
		ItemCollection<SolutionItem> dependencies = new ItemCollection<SolutionItem> ();
		SolutionItemEventArgs thisItemArgs;
		FileStatusTracker<SolutionItemEventArgs> fileStatusTracker;
		FilePath fileName;
		string name;
		SolutionItemExtension itemExtension;
		MSBuildFileFormat fileFormat;
		
		SolutionItemConfiguration activeConfiguration;
		SolutionItemConfigurationCollection configurations;

		public event EventHandler ConfigurationsChanged;
		public event ConfigurationEventHandler DefaultConfigurationChanged;
		public event ConfigurationEventHandler ConfigurationAdded;
		public event ConfigurationEventHandler ConfigurationRemoved;
		public EventHandler RunConfigurationsChanged;

		// When set, it means this item is saved as part of a global solution save operation
		internal bool SavingSolution { get; set; }
		
		public SolutionItem ()
		{
			TypeGuid = MSBuildProjectService.GetTypeGuidForItem (this);

			fileFormat = MSBuildFileFormat.DefaultFormat;
			thisItemArgs = new SolutionItemEventArgs (this);
			configurations = new SolutionItemConfigurationCollection (this);
			configurations.ConfigurationAdded += OnConfigurationAddedToCollection;
			configurations.ConfigurationRemoved += OnConfigurationRemovedFromCollection;
			Counters.ItemsLoaded++;
			fileStatusTracker = new FileStatusTracker<SolutionItemEventArgs> (this, OnReloadRequired, new SolutionItemEventArgs (this));
		}

		protected override void OnExtensionChainInitialized ()
		{
			itemExtension = ExtensionChain.GetExtension<SolutionItemExtension> ();
			base.OnExtensionChainInitialized ();
		}

		SolutionItemExtension ItemExtension {
			get {
				if (itemExtension == null)
					AssertExtensionChainCreated ();
				return itemExtension;
			}
		}

		protected override IEnumerable<WorkspaceObjectExtension> CreateDefaultExtensions ()
		{
			foreach (var e in base.CreateDefaultExtensions ())
				yield return e;
			yield return new DefaultMSBuildItemExtension ();
		}

		internal protected virtual IEnumerable<string> GetItemTypeGuids ()
		{
			yield return TypeGuid;
		}

		protected override void OnDispose ()
		{
			if (Disposing != null)
				Disposing (this, EventArgs.Empty);
			
			base.OnDispose ();
			Counters.ItemsLoaded--;

			// items = null;
			// wildcardItems = null;
			// thisItemArgs = null;
			// fileStatusTracker = null;
			// fileFormat = null;
			// activeConfiguration = null;
			// configurations = null;
		}

		void HandleSolutionItemAdded (object sender, SolutionItemChangeEventArgs e)
		{
			if (e.Reloading && dependencies.Count > 0 && (e.SolutionItem is SolutionItem) && (e.ReplacedItem is SolutionItem)) {
				int i = dependencies.IndexOf ((SolutionItem)e.ReplacedItem);
				if (i != -1)
					dependencies [i] = (SolutionItem) e.SolutionItem;
			}
		}

		void HandleSolutionItemRemoved (object sender, SolutionItemChangeEventArgs e)
		{
			if (!e.Reloading && (e.SolutionItem is SolutionItem))
				dependencies.Remove ((SolutionItem)e.SolutionItem);
		}

		internal void BeginLoad ()
		{
			loading++;
			OnBeginLoad ();
			ItemExtension.BeginLoad ();
		}

		internal void EndLoad ()
		{
			loading--;
			ItemExtension.EndLoad ();
			OnEndLoad ();
		}

		/// <summary>
		/// Called when an item has been fully created and/or loaded
		/// </summary>
		/// <remarks>>
		/// This method is invoked when all operations required for creating or loading this item have finished.
		/// If the item is being created in memory, this method will be called just after OnExtensionChainInitialized.
		/// If the item is being loaded from a file, it will be called after OnEndLoad.
		/// If the item is being created from a template, it will be called after InitializeNew
		/// </remarks>
		protected virtual void OnItemReady ()
		{
		}

		internal void NotifyItemReady ()
		{
			ItemExtension.ItemReady ();
			OnItemReady ();
		}

		protected override void OnSetShared ()
		{
			base.OnSetShared ();
			configurations.SetShared ();
		}


		/// <summary>
		/// Called when a load operation for this solution item has started
		/// </summary>
		protected virtual void OnBeginLoad ()
		{
		}

		/// <summary>
		/// Called when a load operation for this solution item has finished
		/// </summary>
		protected virtual void OnEndLoad ()
		{
			fileStatusTracker.ResetLoadTimes ();

			if (syncReleaseVersion && ParentSolution != null)
				releaseVersion = ParentSolution.Version;
		}

		[ItemProperty ("ReleaseVersion", DefaultValue="0.1")]
		string releaseVersion = "0.1";
		
		[ItemProperty ("SynchReleaseVersion", DefaultValue = true)]
		bool syncReleaseVersion = true;

		public string Version {
			get {
				// If syncReleaseVersion is set, releaseVersion will already contain the solution's version
				// That's because the version must be up to date even when loading the project individually
				return releaseVersion;
			}
			set {
				AssertMainThread ();
				releaseVersion = value;
				NotifyModified ("Version");
			}
		}
		
		public bool SyncVersionWithSolution {
			get {
				return syncReleaseVersion;
			}
			set {
				AssertMainThread ();
				syncReleaseVersion = value;
				if (syncReleaseVersion && ParentSolution != null)
					Version = ParentSolution.Version;
				NotifyModified ("SyncVersionWithSolution");
			}
		}
		
		[ThreadSafe]
		protected override string OnGetName ()
		{
			return name ?? string.Empty;
		}

		protected override void OnSetName (string value)
		{
			name = value;
			if (!Loading) {
				if (string.IsNullOrEmpty (fileName))
					FileName = value;
				else {
					string ext = fileName.Extension;
					FileName = fileName.ParentDirectory.Combine (value) + ext;
				}
			}
		}

		public virtual FilePath FileName {
			get {
				return fileName;
			}
			set {
				if (FileFormat != null)
					value = FileFormat.GetValidFormatName (this, value);
				if (value != fileName) {
					fileName = value;
					Name = fileName.FileNameWithoutExtension;
					NotifyModified ("FileName");
				}
			}
		}

		public bool Enabled {
			get { return ParentSolution != null ? ParentSolution.IsSolutionItemEnabled (FileName) : true; }
			set { 
				if (ParentSolution != null)
					ParentSolution.SetSolutionItemEnabled (FileName, value);
			}
		}

		public MSBuildFileFormat FileFormat {
			get {
				if (ParentSolution != null)
					return ParentSolution.FileFormat;
				return fileFormat; 
			}
			internal set {
				fileFormat = value;
			}
		}

		/// <summary>
		/// Changes the format of this item. This method doesn't save the item, it only does in memory-changes.
		/// </summary>
		public void ConvertToFormat (MSBuildFileFormat format)
		{
			if (format == fileFormat)
				return;

			if (ParentSolution != null && ParentSolution.FileFormat != format)
				throw new InvalidOperationException ("The file format can't be changed when the item belongs to a solution.");
			InternalConvertToFormat (format);
		}

		internal void InternalConvertToFormat (MSBuildFileFormat format)
		{
			ItemExtension.OnSetFormat (format);
			NeedsReload = false;
			NotifyModified ("FileFormat");
		}

		protected virtual void OnSetFormat (MSBuildFileFormat format)
		{
			fileFormat = format;
			if (fileName != FilePath.Null)
				fileName = fileFormat.GetValidFormatName (this, fileName);
		}

		public bool SupportsFormat (MSBuildFileFormat format)
		{
			return ItemExtension.OnGetSupportsFormat (format);
		}

		protected virtual bool OnGetSupportsFormat (MSBuildFileFormat format)
		{
			return true;
		}
			
		protected override object OnGetService (Type t)
		{
			return null;
		}

		/// <summary>
		/// Projects that need to be built before building this one
		/// </summary>
		/// <value>The dependencies.</value>
		public ItemCollection<SolutionItem> ItemDependencies {
			get { return dependencies; }
		}

		/// <summary>
		/// Gets a value indicating whether this item is currently being loaded from a file
		/// </summary>
		/// <remarks>
		/// While an item is loading, some events such as project file change events may be fired.
		/// This flag can be used to check if change events are caused by data being loaded.
		/// </remarks>
		public bool Loading {
			get { return loading > 0; }
		}

		public IEnumerable<IBuildTarget> GetExecutionDependencies ()
		{
			return ItemExtension.OnGetExecutionDependencies ();
		}

		protected virtual IEnumerable<IBuildTarget> OnGetExecutionDependencies ()
		{
			yield break;
		}

		/// <summary>
		/// Gets solution items referenced by this instance (items on which this item depends)
		/// </summary>
		/// <returns>
		/// The referenced items.
		/// </returns>
		/// <param name='configuration'>
		/// Configuration for which to get the referenced items
		/// </param>
		public IEnumerable<SolutionItem> GetReferencedItems (ConfigurationSelector configuration)
		{
			return ItemExtension.OnGetReferencedItems (configuration);
		}

		protected virtual IEnumerable<SolutionItem> OnGetReferencedItems (ConfigurationSelector configuration)
		{
			return dependencies;
		}

		/// <summary>
		/// Initializes a new instance of this item, using an xml element as template
		/// </summary>
		/// <param name='template'>
		/// The template
		/// </param>
		public void InitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement template)
		{
			// TODO NPM: should be internal
			ItemExtension.OnInitializeFromTemplate (projectCreateInfo, template);
		}

		protected virtual void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement template)
		{
			if (projectCreateInfo.TemplateInitializationCallback != null)
				projectCreateInfo.TemplateInitializationCallback (this);
		}

		protected sealed override FilePath GetDefaultBaseDirectory ( )
		{
			var file = FileName;
			return file.IsNullOrEmpty ? FilePath.Empty : file.ParentDirectory; 
		}

		internal Task LoadAsync (ProgressMonitor monitor, FilePath fileName, MSBuildFileFormat format)
		{
			fileFormat = format;
			FileName = fileName;
			Name = Path.GetFileNameWithoutExtension (fileName);
			return ItemExtension.OnLoad (monitor);
		}

		public Task SaveAsync (ProgressMonitor monitor, FilePath fileName)
		{
			FileName = fileName;
			return SaveAsync (monitor);
		}
		
		public Task SaveAsync (ProgressMonitor monitor)
		{
			return BindTask (ct => Runtime.RunInMainThread (async () => {
				using (await WriteLock ()) {
					monitor = monitor.WithCancellationToken (ct);
					await ItemExtension.OnSave (monitor);

					if (ItemExtension.OnCheckHasSolutionData () && !SavingSolution && ParentSolution != null) {
						// The project has data that has to be saved in the solution, but the solution is not being saved. Do it now.
						await SolutionFormat.SlnFileFormat.WriteFile (ParentSolution.FileName, ParentSolution, false, monitor);
						ParentSolution.NeedsReload = false;
					}
				}
			}));
		}
		
		async Task DoSave (ProgressMonitor monitor)
		{
			if (string.IsNullOrEmpty (FileName))
				throw new InvalidOperationException ("Project does not have a file name");

			try {
				fileStatusTracker.BeginSave ();
				await OnSave (monitor);
				OnSaved (thisItemArgs);
			} finally {
				fileStatusTracker.EndSave ();
			}
			FileService.NotifyFileChanged (FileName);
		}

		internal bool IsSaved {
			get {
				return !string.IsNullOrEmpty (FileName) && File.Exists (FileName);
			}
		}
		
		public override bool NeedsReload {
			get { return fileStatusTracker.NeedsReload; }
			set { fileStatusTracker.NeedsReload = value; }
		}
		
		public bool ItemFilesChanged {
			get { return ItemExtension.ItemFilesChanged; }
		}
		
		bool BaseItemFilesChanged {
			get { return fileStatusTracker.ItemFilesChanged; }
		}

		bool IBuildTarget.CanBuild (ConfigurationSelector configuration)
		{
			return SupportsBuild ();
		}

		public bool SupportsBuild ()
		{
			return ItemExtension.OnGetSupportedFeatures ().HasFlag (ProjectFeatures.Build);
		}

		public bool SupportsExecute ()
		{
			return ItemExtension.OnGetSupportedFeatures ().HasFlag (ProjectFeatures.Execute);
		}

		public bool SupportsConfigurations ()
		{
			return ItemExtension.OnGetSupportedFeatures ().HasFlag (ProjectFeatures.Configurations);
		}

		public bool SupportsRunConfigurations ()
		{
			return ItemExtension.OnGetSupportedFeatures ().HasFlag (ProjectFeatures.RunConfigurations);
		}

		protected virtual ProjectFeatures OnGetSupportedFeatures ()
		{
			if (IsUnsupportedProject)
				return ProjectFeatures.Configurations;
			else
				return ProjectFeatures.Execute | ProjectFeatures.Build | ProjectFeatures.Configurations | ProjectFeatures.RunConfigurations;
		}

		/// <summary>
		/// Gets a value indicating whether this project is supported.
		/// </summary>
		/// <remarks>
		/// Unsupported projects are shown in the solution pad, but operations such as building on executing won't be available.
		/// </remarks>
		public bool IsUnsupportedProject { get; protected set; }

		/// <summary>
		/// Gets a message that explain why the project is not supported (when IsUnsupportedProject returns true)
		/// </summary>
		public string UnsupportedProjectMessage {
			get { return IsUnsupportedProject ? (loadError ?? GettextCatalog.GetString ("Unknown project type")) : ""; }
			set { loadError = value; }
		}
		string loadError;

		[Obsolete ("This method will be removed in future releases")]
		public bool NeedsBuilding (ConfigurationSelector configuration)
		{
			return ItemExtension.OnNeedsBuilding (configuration);
		}

		internal protected virtual bool OnGetNeedsBuilding (ConfigurationSelector configuration)
		{
			return false;
		}

		[Obsolete ("This method will be removed in future releases")]
		public void SetNeedsBuilding (ConfigurationSelector configuration)
		{
			OnSetNeedsBuilding (configuration);
		}

		protected virtual void OnSetNeedsBuilding (ConfigurationSelector configuration)
		{
		}

		/// <summary>
		/// Builds the solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='solutionConfiguration'>
		/// Configuration to use to build the project
		/// </param>
		public Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector solutionConfiguration)
		{
			return Build (monitor, solutionConfiguration, false);
		}

		/// <summary>
		/// Builds the solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='solutionConfiguration'>
		/// Configuration to use to build the project
		/// </param>
		/// <param name='buildReferences'>
		/// When set to <c>true</c>, the referenced items will be built before building this item
		/// </param>
		public Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector solutionConfiguration, bool buildReferences)
		{
			return BindTask (ct => BuildTask (monitor.WithCancellationToken (ct), solutionConfiguration, buildReferences, new OperationContext ()));
		}

		public Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector solutionConfiguration, bool buildReferences, OperationContext operationContext)
		{
			return BindTask (ct => BuildTask (monitor.WithCancellationToken (ct), solutionConfiguration, buildReferences, operationContext));
		}

		async Task<BuildResult> BuildTask (ProgressMonitor monitor, ConfigurationSelector solutionConfiguration, bool buildReferences, OperationContext operationContext)
		{
			if (!buildReferences) {
				try {
					SolutionItemConfiguration iconf = GetConfiguration (solutionConfiguration);
					string confName = iconf != null ? iconf.Id : solutionConfiguration.ToString ();
					monitor.BeginTask (GettextCatalog.GetString ("Building: {0} ({1})", Name, confName), 1);

					using (Counters.BuildProjectTimer.BeginTiming ("Building " + Name, GetProjectEventMetadata (solutionConfiguration))) {
						return await InternalBuild (monitor, solutionConfiguration, operationContext);
					}

				} finally {
					monitor.EndTask ();
				}
			}

			ITimeTracker tt = Counters.BuildProjectAndReferencesTimer.BeginTiming ("Building " + Name, GetProjectEventMetadata (solutionConfiguration));
			try {
				// Get a list of all items that need to be built (including this),
				// and build them in the correct order

				var referenced = new List<SolutionItem> ();
				var visited = new Set<SolutionItem> ();
				GetBuildableReferencedItems (visited, referenced, this, solutionConfiguration);

				var sortedReferenced = TopologicalSort (referenced, solutionConfiguration);

				SolutionItemConfiguration iconf = GetConfiguration (solutionConfiguration);
				string confName = iconf != null ? iconf.Id : solutionConfiguration.ToString ();
				monitor.BeginTask (GettextCatalog.GetString ("Building: {0} ({1})", Name, confName), sortedReferenced.Count);

				return await SolutionFolder.RunParallelBuildOperation (monitor, solutionConfiguration, sortedReferenced, (ProgressMonitor m, SolutionItem item) => {
					return item.Build (m, solutionConfiguration, false, operationContext);
				}, false);
			} finally {
				monitor.EndTask ();
				tt.End ();
			}
		}

		async Task<BuildResult> InternalBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			if (IsUnsupportedProject) {
				var r = new BuildResult ();
				r.AddError (UnsupportedProjectMessage);
				return r;
			}

			SolutionItemConfiguration conf = GetConfiguration (configuration) as SolutionItemConfiguration;
			if (conf != null) {
				if (conf.CustomCommands.CanExecute (this, CustomCommandType.BeforeBuild, null, configuration)) {
					if (!await conf.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.BeforeBuild, configuration)) {
						var r = new BuildResult ();
						r.AddError (GettextCatalog.GetString ("Custom command execution failed"));
						return r;
					}
				}
			}

			if (monitor.CancellationToken.IsCancellationRequested)
				return new BuildResult (new CompilerResults (null), "");

			BuildResult res = await ItemExtension.OnBuild (monitor, configuration, operationContext);

			if (conf != null && !monitor.CancellationToken.IsCancellationRequested && !res.Failed) {
				if (conf.CustomCommands.CanExecute (this, CustomCommandType.AfterBuild, null, configuration)) {
					if (!await conf.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.AfterBuild, configuration))
						res.AddError (GettextCatalog.GetString ("Custom command execution failed"));
				}
			}

			return res;
		}

		/// <summary>
		/// Builds the solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to build the project
		/// </param>
		protected virtual Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return Task.FromResult (BuildResult.CreateSuccess ());
		}

		void GetBuildableReferencedItems (Set<SolutionItem> visited, List<SolutionItem> referenced, SolutionItem item, ConfigurationSelector configuration)
		{
			if (!visited.Add(item))
				return;

			referenced.Add (item);

			foreach (var ritem in item.GetReferencedItems (configuration))
				GetBuildableReferencedItems (visited, referenced, ritem, configuration);
		}

		internal bool ContainsReferences (HashSet<SolutionItem> items, ConfigurationSelector conf)
		{
			foreach (var it in GetReferencedItems (conf))
				if (items.Contains (it))
					return true;
			return false;
		}

		/// <summary>
		/// Cleans the files produced by this solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to clean the project
		/// </param>
		public Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return BindTask (ct => CleanTask (monitor.WithCancellationToken (ct), configuration, new OperationContext ()));
		}

		public Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return BindTask (ct => CleanTask (monitor.WithCancellationToken (ct), configuration, operationContext));
		}

		async Task<BuildResult> CleanTask (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			ITimeTracker tt = Counters.BuildProjectTimer.BeginTiming ("Cleaning " + Name, GetProjectEventMetadata (configuration));
			try {
				try {
					SolutionItemConfiguration iconf = GetConfiguration (configuration);
					string confName = iconf != null ? iconf.Id : configuration.ToString ();
					monitor.BeginTask (GettextCatalog.GetString ("Cleaning: {0} ({1})", Name, confName), 1);

					SolutionItemConfiguration conf = GetConfiguration (configuration);
					if (conf != null) {
						if (conf.CustomCommands.CanExecute (this, CustomCommandType.BeforeClean, null, configuration)) {
							if (!await conf.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.BeforeClean, configuration)) {
								var r = new BuildResult ();
								r.AddError (GettextCatalog.GetString ("Custom command execution failed"));
								return r;
							}
						}
					}

					if (monitor.CancellationToken.IsCancellationRequested)
						return BuildResult.CreateSuccess ();

					var res = await ItemExtension.OnClean (monitor, configuration, operationContext);

					if (conf != null && !monitor.CancellationToken.IsCancellationRequested) {
						if (conf.CustomCommands.CanExecute (this, CustomCommandType.AfterClean, null, configuration)) {
							if (!await conf.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.AfterClean, configuration))
								res.AddError (GettextCatalog.GetString ("Custom command execution failed"));
						}
					}
					return res;

				} finally {
					monitor.EndTask ();
				}
			} finally {
				tt.End ();
			}
		}

		/// <summary>
		/// Cleans the files produced by this solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to clean the project
		/// </param>
		protected virtual Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext buildSession)
		{
			return Task.FromResult (BuildResult.CreateSuccess ());
		}

		/// <summary>
		/// Sorts a collection of solution items, taking into account the dependencies between them
		/// </summary>
		/// <returns>
		/// The sorted collection of items
		/// </returns>
		/// <param name='items'>
		/// Items to sort
		/// </param>
		/// <param name='configuration'>
		/// A configuration
		/// </param>
		/// <remarks>
		/// This methods sorts a collection of items, ensuring that every item is placed after all the items
		/// on which it depends.
		/// </remarks>
		public static ReadOnlyCollection<T> TopologicalSort<T> (IEnumerable<T> items, ConfigurationSelector configuration) where T: SolutionItem
		{
			IList<T> allItems;
			allItems = items as IList<T>;
			if (allItems == null)
				allItems = new List<T> (items);

			List<T> sortedEntries = new List<T> ();
			bool[] inserted = new bool[allItems.Count];
			bool[] triedToInsert = new bool[allItems.Count];
			for (int i = 0; i < allItems.Count; ++i) {
				if (!inserted[i])
					Insert<T> (i, allItems, sortedEntries, inserted, triedToInsert, configuration);
			}
			return sortedEntries.AsReadOnly ();
		}

		static void Insert<T> (int index, IList<T> allItems, List<T> sortedItems, bool[] inserted, bool[] triedToInsert, ConfigurationSelector solutionConfiguration) where T: SolutionItem
		{
			if (triedToInsert[index]) {
				throw new CyclicDependencyException ();
			}
			triedToInsert[index] = true;
			var insertItem = allItems[index];

			foreach (var reference in insertItem.GetReferencedItems (solutionConfiguration)) {
				for (int j=0; j < allItems.Count; ++j) {
					SolutionFolderItem checkItem = allItems[j];
					if (reference == checkItem) {
						if (!inserted[j])
							Insert (j, allItems, sortedItems, inserted, triedToInsert, solutionConfiguration);
						break;
					}
				}
			}
			sortedItems.Add (insertItem);
			inserted[index] = true;
		}
		
		public IDictionary<string, string> GetProjectEventMetadata (ConfigurationSelector configurationSelector)
		{
			var data = new Dictionary<string, string> ();
			if (configurationSelector != null) {
				var slnConfig = configurationSelector as SolutionConfigurationSelector;
				if (slnConfig != null) {
					data ["Config.Id"] = slnConfig.Id;
				}
			}

			OnGetProjectEventMetadata (data);
			return data;
		}

		protected virtual void OnGetProjectEventMetadata (IDictionary<string, string> metadata)
		{
		}
		/// <summary>
		/// Executes this solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='context'>
		/// An execution context
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to execute the item
		/// </param>
		public Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return Execute (monitor, context, configuration, GetDefaultRunConfiguration ());
		}

		/// <summary>
		/// Executes this solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='context'>
		/// An execution context
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to execute the item
		/// </param>
		/// <param name='runConfiguration'>
		/// Run configuration to use to execute the item
		/// </param>
		public async Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			SolutionItemConfiguration conf = GetConfiguration (configuration) as SolutionItemConfiguration;
			if (conf != null) {
				ExecutionContext localContext = new ExecutionContext (Runtime.ProcessService.DefaultExecutionHandler, context.ConsoleFactory, context.ExecutionTarget);

				if (conf.CustomCommands.CanExecute (this, CustomCommandType.BeforeExecute, localContext, configuration)) {
					if (!await conf.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.BeforeExecute, localContext, configuration))
						return;
				}
			}

			if (monitor.CancellationToken.IsCancellationRequested)
				return;

			await ItemExtension.OnExecute (monitor, context, configuration, runConfiguration ?? GetDefaultRunConfiguration ());

			if (conf != null && !monitor.CancellationToken.IsCancellationRequested) {
				ExecutionContext localContext = new ExecutionContext (Runtime.ProcessService.DefaultExecutionHandler, context.ConsoleFactory, context.ExecutionTarget);

				if (conf.CustomCommands.CanExecute (this, CustomCommandType.AfterExecute, localContext, configuration))
					await conf.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.AfterExecute, localContext, configuration);
			}
		}

		Task IRunTarget.Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, RunConfiguration runConfiguration)
		{
			if (runConfiguration != null && !(runConfiguration is SolutionItemRunConfiguration))
				throw new ArgumentException ("Invalid configuration type");
			return Execute (monitor, context, configuration, (SolutionItemRunConfiguration)runConfiguration);
		}

		/// <summary>
		/// Prepares the target for execution
		/// </summary>
		/// <returns>The execution.</returns>
		/// <param name="monitor">Monitor for tracking progress</param>
		/// <param name="context">Execution context</param>
		/// <param name="configuration">Configuration to execute</param>
		/// <remarks>This method can be called (it is not mandatory) before Execute() to give the target a chance
		/// to asynchronously prepare the execution that is going to be done later on. It can be used for example
		/// to start the simulator that is going to be used for execution. Calling this method is optional, and
		/// there is no guarantee that Execute() will actually be called.</remarks>
		public Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return PrepareExecution (monitor, context, configuration, GetDefaultRunConfiguration ());
		}

		/// <summary>
		/// Prepares the target for execution
		/// </summary>
		/// <returns>The execution.</returns>
		/// <param name="monitor">Monitor for tracking progress</param>
		/// <param name="context">Execution context</param>
		/// <param name="configuration">Configuration to execute</param>
		/// <param name='runConfiguration'>
		/// Run configuration to use to execute the item
		/// </param>
		/// <remarks>This method can be called (it is not mandatory) before Execute() to give the target a chance
		/// to asynchronously prepare the execution that is going to be done later on. It can be used for example
		/// to start the simulator that is going to be used for execution. Calling this method is optional, and
		/// there is no guarantee that Execute() will actually be called.</remarks>
		public Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			return BindTask (ct => ItemExtension.OnPrepareExecution (monitor.WithCancellationToken (ct), context, configuration, runConfiguration ?? GetDefaultRunConfiguration ()));
		}

		Task IRunTarget.PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, RunConfiguration runConfiguration)
		{
			if (runConfiguration != null && !(runConfiguration is SolutionItemRunConfiguration))
				throw new ArgumentException ("Invalid configuration type");
			return PrepareExecution (monitor, context, configuration, (SolutionItemRunConfiguration)runConfiguration);
		}
	
		/// <summary>
		/// Determines whether this solution item can be executed using the specified context and configuration.
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance can be executed; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='context'>
		/// An execution context
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to execute the item
		/// </param>
		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return CanExecute (context, configuration, GetDefaultRunConfiguration ());
		}

		/// <summary>
		/// Determines whether this solution item can be executed using the specified context and configuration.
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance can be executed; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='context'>
		/// An execution context
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to execute the item
		/// </param>
		/// <param name='runConfiguration'>
		/// Run configuration to use to execute the item
		/// </param>
		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			return !IsUnsupportedProject && ItemExtension.OnGetCanExecute (context, configuration, runConfiguration ?? GetDefaultRunConfiguration ());
		}

		bool IRunTarget.CanExecute (ExecutionContext context, ConfigurationSelector configuration, RunConfiguration runConfiguration)
		{
			if (runConfiguration != null && !(runConfiguration is SolutionItemRunConfiguration))
				throw new ArgumentException ("Invalid configuration type");
			return CanExecute (context, configuration, (SolutionItemRunConfiguration)runConfiguration);
		}
	
		async Task DoExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			SolutionItemConfiguration conf = GetConfiguration (configuration) as SolutionItemConfiguration;
			if (conf != null && conf.CustomCommands.HasCommands (CustomCommandType.Execute)) {
				await conf.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.Execute, context, configuration);
				return;
			}
			await OnExecute (monitor, context, configuration, runConfiguration);
		}

		/// <summary>
		/// Executes this solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='context'>
		/// An execution context
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to execute the item
		/// </param>
		protected virtual Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
#pragma warning disable 618 // Type or member is obsolete
			return OnExecute (monitor, context, configuration);
#pragma warning restore 618 // Type or member is obsolete
		}

		[Obsolete ("Use overload that takes a RunConfiguration")]
		protected virtual Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return Task.FromResult (0);
		}

		/// <summary>
		/// Prepares the target for execution
		/// </summary>
		/// <returns>The execution.</returns>
		/// <param name="monitor">Monitor for tracking progress</param>
		/// <param name="context">Execution context</param>
		/// <param name="configuration">Configuration to execute</param>
		/// <param name="runConfiguration">Run configuration to execute</param>
		/// <remarks>This method can be called (it is not mandatory) before Execute() to give the target a chance
		/// to asynchronously prepare the execution that is going to be done later on. It can be used for example
		/// to start the simulator that is going to be used for execution. Calling this method is optional, and
		/// there is no guarantee that Execute() will actually be called.</remarks>
		protected virtual Task OnPrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
#pragma warning disable 618 // Type or member is obsolete
			return OnPrepareExecution (monitor, context, configuration);
#pragma warning restore 618 // Type or member is obsolete
		}

		[Obsolete ("Use overload that takes a RunConfiguration")]
		protected virtual Task OnPrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return Task.FromResult (true);
		}

		bool DoGetCanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			SolutionItemConfiguration conf = GetConfiguration (configuration) as SolutionItemConfiguration;
			if (conf != null && conf.CustomCommands.HasCommands (CustomCommandType.Execute))
				return conf.CustomCommands.CanExecute (this, CustomCommandType.Execute, context, configuration);
			return OnGetCanExecute (context, configuration, runConfiguration);
		}

		/// <summary>
		/// Determines whether this solution item can be executed using the specified context and configuration.
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance can be executed; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='context'>
		/// An execution context
		/// </param>
		/// <param name='configuration'>
		/// Configuration to use to execute the item
		/// </param>
		/// <param name='runConfiguration'>
		/// Run configuration to use to execute the item
		/// </param>
		protected virtual bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
#pragma warning disable 618 // Type or member is obsolete
			return OnGetCanExecute (context, configuration);
#pragma warning restore 618 // Type or member is obsolete
		}

		[Obsolete ("Use overload that takes a RunConfiguration")]
		protected virtual bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			SolutionItemConfiguration conf = GetConfiguration (configuration) as SolutionItemConfiguration;
			if (conf != null && conf.CustomCommands.HasCommands (CustomCommandType.Execute))
				return conf.CustomCommands.CanExecute (this, CustomCommandType.Execute, context, configuration);
			return false;
		}

		/// <summary>
		/// Gets the execution targets.
		/// </summary>
		/// <returns>The execution targets.</returns>
		/// <param name="configuration">The configuration.</param>
		public IEnumerable<ExecutionTarget> GetExecutionTargets (ConfigurationSelector configuration)
		{
			return ItemExtension.OnGetExecutionTargets (new OperationContext (), configuration, GetDefaultRunConfiguration ());
		}

		/// <summary>
		/// Gets the execution targets.
		/// </summary>
		/// <returns>The execution targets.</returns>
		/// <param name="configuration">The configuration.</param>
		public IEnumerable<ExecutionTarget> GetExecutionTargets (ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			return ItemExtension.OnGetExecutionTargets (new OperationContext (), configuration, runConfiguration ?? GetDefaultRunConfiguration ());
		}

		IEnumerable<ExecutionTarget> IRunTarget.GetExecutionTargets (ConfigurationSelector configuration, RunConfiguration runConfiguration)
		{
			if (runConfiguration != null && !(runConfiguration is SolutionItemRunConfiguration))
				throw new ArgumentException ("Invalid configuration type");
			return GetExecutionTargets (configuration, (SolutionItemRunConfiguration)runConfiguration);
		}
	
		protected virtual IEnumerable<ExecutionTarget> OnGetExecutionTargets (ConfigurationSelector configuration, SolutionItemRunConfiguration runConfig)
		{
			return ItemExtension.OnGetExecutionTargets (configuration);
		}

		public void NotifyExecutionTargetsChanged ()
		{
			ItemExtension.OnExecutionTargetsChanged ();
		}

		public event EventHandler ExecutionTargetsChanged;

		protected virtual void OnExecutionTargetsChanged ()
		{
			if (ExecutionTargetsChanged != null)
				ExecutionTargetsChanged (this, EventArgs.Empty);
		}

		/// <summary>
		/// Gets the run configurations.
		/// </summary>
		/// <returns>The execution targets.</returns>
		public IEnumerable<SolutionItemRunConfiguration> GetRunConfigurations ()
		{
			return ItemExtension.OnGetRunConfigurations (new OperationContext ());
		}

		IEnumerable<RunConfiguration> IRunTarget.GetRunConfigurations ()
		{
			return GetRunConfigurations ();
		}

		/// <summary>
		/// Gets the default run configuration for this item.
		/// </summary>
		/// <returns>The configuration.</returns>
		public SolutionItemRunConfiguration GetDefaultRunConfiguration ()
		{
			var configs = GetRunConfigurations ();
			return configs.FirstOrDefault (s => s.IsDefaultConfiguration) ?? configs.FirstOrDefault ();
		}

		public void NotifyRunConfigurationsChanged ()
		{
			ItemExtension.OnRunConfigurationsChanged (new OperationContext ());
			if (ParentSolution != null)
				ParentSolution.NotifyRunConfigurationsChanged ();
		}

		protected virtual void OnRunConfigurationsChanged ()
		{
			if (RunConfigurationsChanged != null)
				RunConfigurationsChanged (this, EventArgs.Empty);
		}

		protected virtual IEnumerable<SolutionItemRunConfiguration>  OnGetRunConfigurations ()
		{
			yield break;
		}


		protected virtual Task OnLoad (ProgressMonitor monitor)
		{
			return Task.FromResult (0);
		}

		protected internal virtual Task OnSave (ProgressMonitor monitor)
		{
			return Task.FromResult (0);
		}

		public FilePath GetAbsoluteChildPath (FilePath relPath)
		{
			return relPath.ToAbsolute (BaseDirectory);
		}

		public FilePath GetRelativeChildPath (FilePath absPath)
		{
			return absPath.ToRelative (BaseDirectory);
		}

		public IEnumerable<FilePath> GetItemFiles (bool includeReferencedFiles)
		{
			return ItemExtension.OnGetItemFiles (includeReferencedFiles);
		}

		protected virtual IEnumerable<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			if (!FileName.IsNullOrEmpty)
				yield return FileName;
		}

		protected override void OnNameChanged (SolutionItemRenamedEventArgs e)
		{
			Solution solution = this.ParentSolution;

			if (solution != null) {
				foreach (DotNetProject project in solution.GetAllItems<DotNetProject>()) {
					if (project == this)
						continue;
					
					project.RenameReferences (e.OldName, e.NewName);
				}
			}
			fileStatusTracker.ResetLoadTimes ();
			base.OnNameChanged (e);
		}
		
		protected virtual void OnSaved (SolutionItemEventArgs args)
		{
			if (Saved != null)
				Saved (this, args);
		}
		
		public SolutionItemConfiguration GetConfiguration (ConfigurationSelector configuration)
		{
			return (SolutionItemConfiguration) configuration.GetConfiguration (this) ?? DefaultConfiguration;
		}

		ItemConfiguration IConfigurationTarget.DefaultConfiguration {
			get { return DefaultConfiguration; }
			set { DefaultConfiguration = (SolutionItemConfiguration) value; }
		}

		public SolutionItemConfiguration DefaultConfiguration {
			get {
				if (activeConfiguration == null && configurations.Count > 0) {
					return configurations[0];
				}
				return activeConfiguration;
			}
			set {
				if (activeConfiguration != value) {
					activeConfiguration = value;
					NotifyModified ("DefaultConfiguration");
					OnDefaultConfigurationChanged (new ConfigurationEventArgs (this, value));
				}
			}
		}
		
		public string DefaultConfigurationId {
			get {
				if (DefaultConfiguration != null)
					return DefaultConfiguration.Id;
				else
					return null;
			}
			set {
				DefaultConfiguration = GetConfiguration (new ItemConfigurationSelector (value));
			}
		}
		
		public ReadOnlyCollection<string> GetConfigurations ()
		{
			List<string> configs = new List<string> ();
			foreach (SolutionItemConfiguration conf in Configurations)
				configs.Add (conf.Id);
			return configs.AsReadOnly ();
		}
		
		public SolutionItemConfigurationCollection Configurations {
			get {
				return configurations;
			}
		}
		
		IItemConfigurationCollection IConfigurationTarget.Configurations {
			get {
				return Configurations;
			}
		}
		
		public SolutionItemConfiguration AddNewConfiguration (string name, ConfigurationKind kind = ConfigurationKind.Blank)
		{
			SolutionItemConfiguration config = CreateConfiguration (name, kind);
			Configurations.Add (config);
			return config;
		}
		
		ItemConfiguration IConfigurationTarget.CreateConfiguration (string id, ConfigurationKind kind)
		{
			return CreateConfiguration (id, kind);
		}

		public SolutionItemConfiguration CreateConfiguration (string name, string platform, ConfigurationKind kind = ConfigurationKind.Blank)
		{
			return ItemExtension.OnCreateConfiguration (name + "|" + platform, kind);
		}

		public SolutionItemConfiguration CreateConfiguration (string id, ConfigurationKind kind = ConfigurationKind.Blank)
		{
			return ItemExtension.OnCreateConfiguration (id, kind);
		}
		
		public SolutionItemConfiguration CloneConfiguration (SolutionItemConfiguration configuration, string newName, string newPlatform)
		{
			return CloneConfiguration (configuration, newName + "|" + newPlatform);
		}

		public SolutionItemConfiguration CloneConfiguration (SolutionItemConfiguration configuration, string newId)
		{
			var clone = CreateConfiguration (newId);
			clone.CopyFrom (configuration, true);
			return clone;
		}

		protected virtual SolutionItemConfiguration OnCreateConfiguration (string id, ConfigurationKind kind = ConfigurationKind.Blank)
		{
			return new SolutionItemConfiguration (id);
		}

		void OnConfigurationAddedToCollection (object ob, ConfigurationEventArgs args)
		{
			NotifyModified ("Configurations");
			OnConfigurationAdded (new ConfigurationEventArgs (this, args.Configuration));
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, EventArgs.Empty);
			if (activeConfiguration == null)
				DefaultConfigurationId = args.Configuration.Id;
		}
		
		void OnConfigurationRemovedFromCollection (object ob, ConfigurationEventArgs args)
		{
			if (activeConfiguration == args.Configuration) {
				if (Configurations.Count > 0)
					DefaultConfiguration = Configurations [0];
				else
					DefaultConfiguration = null;
			}
			NotifyModified ("Configurations");
			OnConfigurationRemoved (new ConfigurationEventArgs (this, args.Configuration));
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, EventArgs.Empty);
		}
		
		protected override StringTagModelDescription OnGetStringTagModelDescription (ConfigurationSelector conf)
		{
			StringTagModelDescription model = base.OnGetStringTagModelDescription (conf);
			SolutionItemConfiguration config = GetConfiguration (conf);
			if (config != null)
				model.Add (config.GetType ());
			else
				model.Add (typeof(SolutionItemConfiguration));
			return model;
		}

		protected override StringTagModel OnGetStringTagModel (ConfigurationSelector conf)
		{
			var source = base.OnGetStringTagModel (conf);
			SolutionItemConfiguration config = GetConfiguration (conf);
			if (config != null)
				source.Add (config);
			return source;
		}

		internal protected override DateTime OnGetLastBuildTime (ConfigurationSelector configuration)
		{
			return ItemExtension.OnGetLastBuildTime (configuration);
		}

		DateTime DoGetLastBuildTime (ConfigurationSelector configuration)
		{
			return base.OnGetLastBuildTime (configuration);
		}

		protected virtual void OnDefaultConfigurationChanged (ConfigurationEventArgs args)
		{
			ItemExtension.OnDefaultConfigurationChanged (args);
		}
		
		void DoOnDefaultConfigurationChanged (ConfigurationEventArgs args)
		{
			if (DefaultConfigurationChanged != null)
				DefaultConfigurationChanged (this, args);
		}

		protected virtual void OnConfigurationAdded (ConfigurationEventArgs args)
		{
			AssertMainThread ();
			ItemExtension.OnConfigurationAdded (args);
		}
		
		void DoOnConfigurationAdded (ConfigurationEventArgs args)
		{
			if (ConfigurationAdded != null)
				ConfigurationAdded (this, args);
		}

		protected virtual void OnConfigurationRemoved (ConfigurationEventArgs args)
		{
			AssertMainThread ();
			ItemExtension.OnConfigurationRemoved (args);
		}
		
		void DoOnConfigurationRemoved (ConfigurationEventArgs args)
		{
			if (ConfigurationRemoved != null)
				ConfigurationRemoved (this, args);
		}

		protected virtual void OnReloadRequired (SolutionItemEventArgs args)
		{
			ItemExtension.OnReloadRequired (args);
		}
		
		void DoOnReloadRequired (SolutionItemEventArgs args)
		{
			fileStatusTracker.FireReloadRequired (args);
		}

		protected override void OnBoundToSolution ()
		{
			ParentSolution.SolutionItemRemoved += HandleSolutionItemRemoved;
			ParentSolution.SolutionItemAdded += HandleSolutionItemAdded;
			ItemExtension.OnBoundToSolution ();
		}

		void DoOnBoundToSolution ()
		{
			base.OnBoundToSolution ();
		}

		protected override void OnUnboundFromSolution ()
		{
			ParentSolution.SolutionItemAdded -= HandleSolutionItemAdded;
			ParentSolution.SolutionItemRemoved -= HandleSolutionItemRemoved;
			ItemExtension.OnUnboundFromSolution ();
		}

		void DoOnUnboundFromSolution ()
		{
			base.OnUnboundFromSolution ();
		}

		/// <summary>
		/// Override to return True if this class needs to store project related data in the solution file
		/// </summary>
		protected virtual bool OnCheckHasSolutionData ()
		{
			return false;
		}

		internal void ReadSolutionData (ProgressMonitor monitor, SlnPropertySet properties)
		{
			ItemExtension.OnReadSolutionData (monitor, properties);
		}

		/// <summary>
		/// Override to read project related information stored in the solution file
		/// </summary>
		protected virtual void OnReadSolutionData (ProgressMonitor monitor, SlnPropertySet properties)
		{
			// Do nothing by default
		}

		internal void WriteSolutionData (ProgressMonitor monitor, SlnPropertySet properties)
		{
			ItemExtension.OnWriteSolutionData (monitor, properties);
		}

		/// <summary>
		/// Override to store project related information in the solution file
		/// </summary>
		protected virtual void OnWriteSolutionData (ProgressMonitor monitor, SlnPropertySet properties)
		{
			// Do nothing by default
		}

		public event SolutionItemEventHandler Saved;

		/// <summary>
		/// Occurs when the object is being disposed
		/// </summary>
		public event EventHandler Disposing;
	
		class DefaultMSBuildItemExtension: SolutionItemExtension
		{
			internal protected override void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement template)
			{
				Item.OnInitializeFromTemplate (projectCreateInfo, template);
			}

			internal protected override IEnumerable<IBuildTarget> OnGetExecutionDependencies ()
			{
				return Item.OnGetExecutionDependencies ();
			}

			internal protected override IEnumerable<SolutionItem> OnGetReferencedItems (ConfigurationSelector configuration)
			{
				return Item.OnGetReferencedItems (configuration);
			}

			internal protected override void OnSetFormat (MSBuildFileFormat format)
			{
				Item.OnSetFormat (format);
			}

			internal protected override bool OnGetSupportsFormat (MSBuildFileFormat format)
			{
				return Item.OnGetSupportsFormat (format);
			}

			internal protected override IEnumerable<FilePath> OnGetItemFiles (bool includeReferencedFiles)
			{
				return Item.OnGetItemFiles (includeReferencedFiles);
			}

			internal protected override SolutionItemConfiguration OnCreateConfiguration (string id, ConfigurationKind kind)
			{
				return Item.OnCreateConfiguration (id, kind);
			}

			internal protected override DateTime OnGetLastBuildTime (ConfigurationSelector configuration)
			{
				return Item.DoGetLastBuildTime (configuration);
			}

			internal protected override Task OnLoad (ProgressMonitor monitor)
			{
				return Item.OnLoad (monitor);
			}

			internal protected override Task OnSave (ProgressMonitor monitor)
			{
				return Item.DoSave (monitor);
			}

			internal protected override ProjectFeatures OnGetSupportedFeatures ()
			{
				return Item.OnGetSupportedFeatures ();
			}

			internal protected override Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
			{
				return Item.DoExecute (monitor, context, configuration, runConfiguration);
			}

			internal protected override Task OnPrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
			{
				return Item.OnPrepareExecution (monitor, context, configuration, runConfiguration);
			}

			internal protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
			{
				return Item.DoGetCanExecute (context, configuration, runConfiguration);
			}

			internal protected override IEnumerable<ExecutionTarget> OnGetExecutionTargets (ConfigurationSelector configuration)
			{
				yield break;
			}

			internal protected override IEnumerable<ExecutionTarget> OnGetExecutionTargets (OperationContext ctx, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfig)
			{
				return Item.OnGetExecutionTargets (configuration, runConfig);
			}

			internal protected override void OnExecutionTargetsChanged ()
			{
				Item.OnExecutionTargetsChanged ();
			}

			internal protected override IEnumerable<SolutionItemRunConfiguration> OnGetRunConfigurations (OperationContext ctx)
			{
				return Item.OnGetRunConfigurations ();
			}

			internal protected override void OnRunConfigurationsChanged (OperationContext ctx)
			{
				Item.OnRunConfigurationsChanged ();
			}

			internal protected override void OnReloadRequired (SolutionItemEventArgs args)
			{
				Item.DoOnReloadRequired (args);
			}

			internal protected override void OnDefaultConfigurationChanged (ConfigurationEventArgs args)
			{
				Item.DoOnDefaultConfigurationChanged (args);
			}

			internal protected override void OnBoundToSolution ()
			{
				Item.DoOnBoundToSolution ();
			}

			internal protected override void OnUnboundFromSolution ()
			{
				Item.DoOnUnboundFromSolution ();
			}

			internal protected override void OnConfigurationAdded (ConfigurationEventArgs args)
			{
				Item.DoOnConfigurationAdded (args);
			}

			internal protected override void OnConfigurationRemoved (ConfigurationEventArgs args)
			{
				Item.DoOnConfigurationRemoved (args);
			}

			internal protected override void OnModified (SolutionItemModifiedEventArgs args)
			{
				Item.OnModified (args);
			}

			internal protected override void OnNameChanged (SolutionItemRenamedEventArgs e)
			{
				Item.OnNameChanged (e);
			}

			internal protected override IconId StockIcon {
				get {
					return "md-project";
				}
			}

			internal protected override bool ItemFilesChanged {
				get {
					return Item.BaseItemFilesChanged;
				}
			}

			internal protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
			{
				return Item.OnBuild (monitor, configuration, operationContext);
			}

			internal protected override Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext buildSession)
			{
				return Item.OnClean (monitor, configuration, buildSession);
			}

			internal protected override bool OnNeedsBuilding (ConfigurationSelector configuration)
			{
				return Item.OnGetNeedsBuilding (configuration);
			}

			internal protected override void OnWriteSolutionData (ProgressMonitor monitor, SlnPropertySet properties)
			{
				Item.OnWriteSolutionData (monitor, properties);
			}

			internal protected override void OnReadSolutionData (ProgressMonitor monitor, SlnPropertySet properties)
			{
				Item.OnReadSolutionData (monitor, properties);
			}

			internal protected override bool OnCheckHasSolutionData ()
			{
				return Item.OnCheckHasSolutionData ();
			}
		}	
	}

	[Mono.Addins.Extension]
	class SolutionItemTagProvider: StringTagProvider<SolutionItem>, IStringTagProvider
	{
		public override IEnumerable<StringTagDescription> GetTags ()
		{
			yield return new StringTagDescription ("ProjectName", GettextCatalog.GetString ("Project Name"));
			yield return new StringTagDescription ("ProjectDir", GettextCatalog.GetString ("Project Directory"));
			yield return new StringTagDescription ("AuthorName", GettextCatalog.GetString ("Project Author Name"));
			yield return new StringTagDescription ("AuthorEmail", GettextCatalog.GetString ("Project Author Email"));
			yield return new StringTagDescription ("AuthorCopyright", GettextCatalog.GetString ("Project Author Copyright"));
			yield return new StringTagDescription ("AuthorCompany", GettextCatalog.GetString ("Project Author Company"));
			yield return new StringTagDescription ("AuthorTrademark", GettextCatalog.GetString ("Project Trademark"));
			yield return new StringTagDescription ("ProjectFile", GettextCatalog.GetString ("Project File"));
		}

		public override object GetTagValue (SolutionItem item, string tag)
		{
			switch (tag) {
			case "ITEMNAME":
			case "PROJECTNAME":
				return item.Name;
			case "AUTHORCOPYRIGHT":
				AuthorInformation authorInfo = item.AuthorInformation ?? AuthorInformation.Default;
				return authorInfo.Copyright;
			case "AUTHORCOMPANY":
				authorInfo = item.AuthorInformation ?? AuthorInformation.Default;
				return authorInfo.Company;
			case "AUTHORTRADEMARK":
				authorInfo = item.AuthorInformation ?? AuthorInformation.Default;
				return authorInfo.Trademark;
			case "AUTHOREMAIL":
				authorInfo = item.AuthorInformation ?? AuthorInformation.Default;
				return authorInfo.Email;
			case "AUTHORNAME":
				authorInfo = item.AuthorInformation ?? AuthorInformation.Default;
				return authorInfo.Name;
			case "ITEMDIR":
			case "PROJECTDIR":
				return item.BaseDirectory;
			case "ITEMFILE":
			case "PROJECTFILE":
			case "PROJECTFILENAME":
				return item.FileName;
			}
			throw new NotSupportedException ();
		}
	}
}
