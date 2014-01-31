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

namespace MonoDevelop.Projects
{
	[ProjectModelDataItem]
	public class Solution: WorkspaceItem, IConfigurationTarget, IPolicyProvider
	{
		internal object MemoryProbe = Counters.SolutionsInMemory.CreateMemoryProbe ();
		SolutionFolder rootFolder;
		string defaultConfiguration;
		
		SolutionEntityItem startupItem;
		List<SolutionEntityItem> startupItems; 
		bool singleStartup = true;

		// Used for serialization only
		List<string> multiStartupItems;
		string startItemFileName;
		
		ReadOnlyCollection<SolutionItem> solutionItems;
		SolutionConfigurationCollection configurations;
		
		[ItemProperty ("description", DefaultValue = "")]
		string description;
		
		[ItemProperty ("version", DefaultValue = "0.1")]
		string version = "0.1";
		
		[ProjectPathItemProperty ("outputpath")]
		string outputdir     = null;
		
		public Solution ()
		{
			Counters.SolutionsLoaded++;
			configurations = new SolutionConfigurationCollection (this);
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
				if (solutionItems == null) {
					List<SolutionItem> list = new List<SolutionItem> ();
					foreach (SolutionItem item in GetAllSolutionItems ())
						if (!(item is SolutionFolder))
							list.Add (item);
					solutionItems = list.AsReadOnly ();
				}
				return solutionItems;
			}
		}
		
		public SolutionEntityItem StartupItem {
			get {
				if (startItemFileName != null) {
					startupItem = FindSolutionItem (startItemFileName);
					startItemFileName = null;
					singleStartup = true;
				}
				if (startupItem == null && singleStartup) {
					ReadOnlyCollection<SolutionEntityItem> its = GetAllSolutionItems<SolutionEntityItem> ();
					if (its.Count > 0)
						startupItem = its [0];
				}
				return startupItem;
			}
			set {
				startupItem = value;
				startItemFileName = null;
				NotifyModified ();
				OnStartupItemChanged(null);
			}
		}
		
		public bool SingleStartup {
			get {
				if (startItemFileName != null)
					return true;
				if (multiStartupItems != null)
					return false;
				return singleStartup; 
			}
			set {
				if (SingleStartup == value)
					return;
				singleStartup = value;
				if (value) {
					if (MultiStartupItems.Count > 0)
						startupItem = startupItems [0];
				} else {
					MultiStartupItems.Clear ();
					if (StartupItem != null)
						MultiStartupItems.Add (StartupItem);
				}
				NotifyModified ();
				OnStartupItemChanged(null);
			}
		}
		
		public List<SolutionEntityItem> MultiStartupItems {
			get {
				if (multiStartupItems != null) {
					startupItems = new List<SolutionEntityItem> ();
					foreach (string file in multiStartupItems) {
						SolutionEntityItem it = FindSolutionItem (file);
						if (it != null)
							startupItems.Add (it);
					}
					multiStartupItems = null;
					singleStartup = false;
				}
				else if (startupItems == null)
					startupItems = new List<SolutionEntityItem> ();
				return startupItems;
			}
		}

		// Used by serialization only
		[ProjectPathItemProperty ("StartupItem", DefaultValue=null)]
		internal string StartupItemFileName {
			get {
				if (SingleStartup && StartupItem != null)
					return StartupItem.FileName;
				else
					return null ;
			}
			set { startItemFileName = value; }
		}
		
		[ItemProperty ("StartupItems")]
		[ProjectPathItemProperty ("Item", Scope="*")]
		internal List<string> MultiStartupItemFileNames {
			get {
				if (SingleStartup)
					return null;
				if (multiStartupItems != null)
					return multiStartupItems;
				List<string> files = new List<string> ();
				foreach (SolutionEntityItem item in MultiStartupItems)
					files.Add (item.FileName);
				return files;
			}
			set {
				multiStartupItems = value;
			}
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

		protected override void OnEndLoad ()
		{
			base.OnEndLoad ();
			LoadItemProperties (UserProperties, RootFolder, "MonoDevelop.Ide.ItemProperties");
		}

		public override void SaveUserProperties ()
		{
			CollectItemProperties (UserProperties, RootFolder, "MonoDevelop.Ide.ItemProperties");
			base.SaveUserProperties ();
			CleanItemProperties (UserProperties, RootFolder, "MonoDevelop.Ide.ItemProperties");
		}
		
		void CollectItemProperties (PropertyBag props, SolutionItem item, string path)
		{
			if (!item.UserProperties.IsEmpty && item.ParentFolder != null)
				props.SetValue (path, item.UserProperties);
			
			SolutionFolder sf = item as SolutionFolder;
			if (sf != null) {
				foreach (SolutionItem ci in sf.Items)
					CollectItemProperties (props, ci, path + "." + ci.Name);
			}
		}
		
		void CleanItemProperties (PropertyBag props, SolutionItem item, string path)
		{
			props.RemoveValue (path);
			
			SolutionFolder sf = item as SolutionFolder;
			if (sf != null) {
				foreach (SolutionItem ci in sf.Items)
					CleanItemProperties (props, ci, path + "." + ci.Name);
			}
		}
		
		void LoadItemProperties (PropertyBag props, SolutionItem item, string path)
		{
			PropertyBag info = props.GetValue<PropertyBag> (path);
			if (info != null) {
				item.LoadUserProperties (info);
				props.RemoveValue (path);
			}
			
			SolutionFolder sf = item as SolutionFolder;
			if (sf != null) {
				foreach (SolutionItem ci in sf.Items)
					LoadItemProperties (props, ci, path + "." + ci.Name);
			}
		}
		
		public void CreateDefaultConfigurations ()
		{
			foreach (SolutionEntityItem item in Items) {
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
		
		ItemConfiguration IConfigurationTarget.CreateConfiguration (string name)
		{
			return new SolutionConfiguration (name);
		}

		public SolutionConfiguration AddConfiguration (string name, bool createConfigForItems)
		{
			SolutionConfiguration conf = new SolutionConfiguration (name);
			foreach (SolutionEntityItem item in Items) {
				if (createConfigForItems && item.GetConfiguration (new ItemConfigurationSelector (name)) == null) {
					SolutionItemConfiguration newc = item.CreateConfiguration (name);
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
			List<string> configs = new List<string> ();
			foreach (SolutionConfiguration conf in Configurations)
				configs.Add (conf.Id);
			return configs.AsReadOnly ();
		}
		
		public virtual SolutionConfiguration GetConfiguration (ConfigurationSelector configuration)
		{
			return (SolutionConfiguration) configuration.GetConfiguration (this) ?? DefaultConfiguration;
		}
		
		public SolutionItem GetSolutionItem (string itemId)
		{
			foreach (SolutionItem item in Items)
				if (item.ItemId == itemId)
					return item;
			return null;
		}
		
		public override SolutionEntityItem FindSolutionItem (string fileName)
		{
			return RootFolder.FindSolutionItem (fileName);
		}
		
		public Project FindProjectByName (string name)
		{
			return RootFolder.FindProjectByName (name);
		}
		
		public override ReadOnlyCollection<T> GetAllSolutionItems<T> ()
		{
			return RootFolder.GetAllItems<T> ();
		}
		
		public ReadOnlyCollection<T> GetAllSolutionItemsWithTopologicalSort<T> (ConfigurationSelector configuration) where T: SolutionItem
		{
			return RootFolder.GetAllItemsWithTopologicalSort<T> (configuration);
		}
		
		public ReadOnlyCollection<Project> GetAllProjectsWithTopologicalSort (ConfigurationSelector configuration)
		{
			return RootFolder.GetAllProjectsWithTopologicalSort (configuration);
		}

		public override Project GetProjectContainingFile (FilePath fileName) 
		{
			return RootFolder.GetProjectContainingFile (fileName);
		}
		
		public override bool ContainsItem (IWorkspaceObject obj)
		{
			if (base.ContainsItem (obj))
				return true;
			
			foreach (SolutionItem it in GetAllSolutionItems<SolutionItem> ()) {
				if (it == obj)
					return true;
			}
			return false;
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
		public MonoDevelop.Projects.Policies.PolicyBag Policies {
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
				foreach (SolutionEntityItem item in GetAllSolutionItems<SolutionEntityItem> ()) {
					if (item.SyncVersionWithSolution)
						item.Version = value;
				}
			}
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			RootFolder.Dispose ();
			Counters.SolutionsLoaded--;
		}

		internal bool IsSolutionItemEnabled (string solutionItemPath)
		{
			solutionItemPath = GetRelativeChildPath (Path.GetFullPath (solutionItemPath));
			var list = UserProperties.GetValue<List<string>> ("DisabledProjects");
			return list == null || !list.Contains (solutionItemPath);
		}

		public void SetSolutionItemEnabled (string solutionItemPath, bool enabled)
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
		
		protected override BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return RootFolder.Build (monitor, configuration);
		}
		
		protected override void OnClean (IProgressMonitor monitor, ConfigurationSelector configuration)
		{	
			RootFolder.Clean (monitor, configuration);
		}

		protected internal override bool OnGetCanExecute(ExecutionContext context, ConfigurationSelector configuration)
		{
			if (SingleStartup) {
				if (StartupItem == null)
					return false;
				return StartupItem.CanExecute (context, configuration);
			} else {
				foreach (SolutionEntityItem it in MultiStartupItems) {
					if (it.CanExecute (context, configuration))
						return true;
				}
				return false;
			}
		}
		
		protected internal override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			if (SingleStartup) {
				if (StartupItem == null) {
					monitor.ReportError (GettextCatalog.GetString ("Startup item not set"), null);
					return;
				}
				StartupItem.Execute (monitor, context, configuration);
			} else {
				List<IAsyncOperation> list = new List<IAsyncOperation> ();
				monitor.BeginTask ("Executing projects", 1);
				
				SynchronizedProgressMonitor syncMonitor = new SynchronizedProgressMonitor (monitor);
				
				foreach (SolutionEntityItem it in MultiStartupItems) {
					if (!it.CanExecute (context, configuration))
						continue;
					AggregatedProgressMonitor mon = new AggregatedProgressMonitor ();
					mon.AddSlaveMonitor (syncMonitor, MonitorAction.ReportError | MonitorAction.ReportWarning | MonitorAction.SlaveCancel);
					list.Add (mon.AsyncOperation);
					SolutionEntityItem cit = it;
					
					Thread t = new Thread (delegate () {
						try {
							using (mon) {
								cit.Execute (mon, context, configuration);
							}
						} catch (Exception ex) {
							LoggingService.LogError ("Project execution failed", ex);
						}
					});
					t.Name = "Project execution";
					t.IsBackground = true;
					t.Start ();
				}
				foreach (IAsyncOperation op in list)
					op.WaitForCompleted ();
				
				monitor.EndTask ();
			}
		}

		protected virtual void OnStartupItemChanged(EventArgs e)
		{
			if (StartupItemChanged != null)
				StartupItemChanged (this, e);
		}
		
		public override void ConvertToFormat (FileFormat format, bool convertChildren)
		{
			base.ConvertToFormat (format, convertChildren);
			foreach (SolutionItem item in GetAllSolutionItems<SolutionItem> ())
				ConvertToSolutionFormat (item, convertChildren);
		}
		
		public override bool SupportsFormat (FileFormat format)
		{
			if (!base.SupportsFormat (format))
				return false;
			return GetAllSolutionItems<SolutionEntityItem> ().All (p => p.SupportsFormat (format));
		}

		public override List<FilePath> GetItemFiles (bool includeReferencedFiles)
		{
			List<FilePath> files = base.GetItemFiles (includeReferencedFiles);
			if (includeReferencedFiles) {
				foreach (SolutionEntityItem item in GetAllSolutionItems<SolutionEntityItem> ())
					files.AddRange (item.GetItemFiles (true));
			}
			return files;
		}
		
#region Notifications from children
		
		internal protected virtual void OnSolutionItemAdded (SolutionItemChangeEventArgs args)
		{
			solutionItems = null;

			SolutionFolder sf = args.SolutionItem as SolutionFolder;
			if (sf != null) {
				foreach (SolutionItem eitem in sf.GetAllItems<SolutionItem> ())
					SetupNewItem (eitem, null);
			}
			else {
				SetupNewItem (args.SolutionItem, args.ReplacedItem);
			}
			
			if (SolutionItemAdded != null)
				SolutionItemAdded (this, args);
		}
		
		void SetupNewItem (SolutionItem item, SolutionItem replacedItem)
		{
			ConvertToSolutionFormat (item, false);
			
			SolutionEntityItem eitem = item as SolutionEntityItem;
			if (eitem != null) {
				eitem.NeedsReload = false;
				if (replacedItem == null) {
					// Register the new entry in every solution configuration
					foreach (SolutionConfiguration conf in Configurations)
						conf.AddItem (eitem);
				} else {
					// Reuse the configuration information of the replaced item
					foreach (SolutionConfiguration conf in Configurations)
						conf.ReplaceItem ((SolutionEntityItem)replacedItem, eitem);
					if (StartupItem == replacedItem)
						StartupItem = eitem;
					else {
						int i = MultiStartupItems.IndexOf ((SolutionEntityItem)replacedItem);
						if (i != -1)
							MultiStartupItems [i] = eitem;
					}
				}
			}
		}
		
		void ConvertToSolutionFormat (SolutionItem item, bool force)
		{
			SolutionEntityItem eitem = item as SolutionEntityItem;
			if (force || !FileFormat.Format.SupportsMixedFormats || eitem == null || !eitem.IsSaved) {
				this.FileFormat.Format.ConvertToFormat (item);
				if (eitem != null)
					eitem.InstallFormat (this.FileFormat);
			}
		}
		
		internal protected virtual void OnSolutionItemRemoved (SolutionItemChangeEventArgs args)
		{
			solutionItems = null;
			
			SolutionFolder sf = args.SolutionItem as SolutionFolder;
			if (sf != null) {
				foreach (SolutionEntityItem eitem in sf.GetAllItems<SolutionEntityItem> ())
					DetachItem (eitem, args.Reloading);
			}
			else {
				SolutionEntityItem item = args.SolutionItem as SolutionEntityItem;
				if (item != null)
					DetachItem (item, args.Reloading);
			}
			
			if (SolutionItemRemoved != null)
				SolutionItemRemoved (this, args);
		}
		
		void DetachItem (SolutionEntityItem item, bool reloading)
		{
			item.NeedsReload = false;
			if (!reloading) {
				foreach (SolutionConfiguration conf in Configurations)
					conf.RemoveItem (item);
				if (item is DotNetProject)
					RemoveReferencesToProject ((DotNetProject)item);

				if (StartupItem == item)
					StartupItem = null;
				else
					MultiStartupItems.Remove (item);
			}
			
			// Update the file name because the file format may have changed
			item.FileName = item.FileName;
		}
		
		void RemoveReferencesToProject (DotNetProject projectToRemove)
		{
			if (projectToRemove == null)
				return;

			foreach (DotNetProject project in GetAllSolutionItems <DotNetProject>()) {
				if (project == projectToRemove)
					continue;
				
				List<ProjectReference> toDelete = new List<ProjectReference> ();
				
				foreach (ProjectReference pref in project.References) {
					if (pref.ReferenceType == ReferenceType.Project && pref.Reference == projectToRemove.Name)
							toDelete.Add (pref);
				}
				
				foreach (ProjectReference pref in toDelete) {
					project.References.Remove (pref);
				}
			}
		}
		
		internal void NotifyConfigurationsChanged ()
		{
			OnConfigurationsChanged ();
		}
		
		internal protected virtual void OnFileAddedToProject (ProjectFileEventArgs args)
		{
			if (FileAddedToProject != null)
				FileAddedToProject (this, args);
		}
		
		internal protected virtual void OnFileRemovedFromProject (ProjectFileEventArgs args)
		{
			if (FileRemovedFromProject != null)
				FileRemovedFromProject (this, args);
		}
		
		internal protected virtual void OnFileChangedInProject (ProjectFileEventArgs args)
		{
			if (FileChangedInProject != null)
				FileChangedInProject (this, args);
		}
		
		internal protected virtual void OnFilePropertyChangedInProject (ProjectFileEventArgs args)
		{
			if (FilePropertyChangedInProject != null)
				FilePropertyChangedInProject (this, args);
		}
		
		internal protected virtual void OnFileRenamedInProject (ProjectFileRenamedEventArgs args)
		{
			if (FileRenamedInProject != null)
				FileRenamedInProject (this, args);
		}
		
		internal protected virtual void OnReferenceAddedToProject (ProjectReferenceEventArgs args)
		{
			if (ReferenceAddedToProject != null)
				ReferenceAddedToProject (this, args);
		}
		
		internal protected virtual void OnReferenceRemovedFromProject (ProjectReferenceEventArgs args)
		{
			if (ReferenceRemovedFromProject != null)
				ReferenceRemovedFromProject (this, args);
		}
		
		internal protected virtual void OnEntryModified (SolutionItemModifiedEventArgs args)
		{
			if (EntryModified != null)
				EntryModified (this, args);
		}
		
		internal protected virtual void OnEntrySaved (SolutionItemEventArgs args)
		{
			if (EntrySaved != null)
				EntrySaved (this, args);
		}
		
		internal protected virtual void OnItemReloadRequired (SolutionItemEventArgs args)
		{
			if (ItemReloadRequired != null)
				ItemReloadRequired (this, args);
		}
		
#endregion
		
		public event EventHandler StartupItemChanged;
		
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
		public event SolutionItemEventHandler EntrySaved;
		public event EventHandler<SolutionItemEventArgs> ItemReloadRequired;
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
}
