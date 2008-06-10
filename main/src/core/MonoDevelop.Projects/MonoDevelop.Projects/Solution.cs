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
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Projects
{
	public class Solution: WorkspaceItem, IConfigurationTarget
	{
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
		
		[ItemProperty ("version", DefaultValue = "")]
		string version;
		
		[ProjectPathItemProperty ("outputpath")]
		string outputdir     = null;
		
		public Solution ()
		{
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
		[ProjectPathItemProperty ("Item", Scope=1)]
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
			if (createConfigForItems) {
				foreach (SolutionEntityItem item in Items)
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
		
		public ReadOnlyCollection<T> GetAllSolutionItemsWithTopologicalSort<T> (string configuration) where T: SolutionItem
		{
			return RootFolder.GetAllItemsWithTopologicalSort<T> (configuration);
		}
		
		public ReadOnlyCollection<Project> GetAllProjectsWithTopologicalSort (string configuration)
		{
			return RootFolder.GetAllProjectsWithTopologicalSort (configuration);
		}
		
		public override Project GetProjectContainingFile (string fileName) 
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
				return (BaseDirectory != null) ? Path.Combine (BaseDirectory, Path.Combine ("build", "bin")) : null;
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

		public string Version {
			get {
				return version ?? string.Empty;
			}
			set {
				version = value;
			}
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			RootFolder.Dispose ();
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
		
		protected override BuildResult OnBuild (IProgressMonitor monitor, string configuration)
		{
			return RootFolder.Build (monitor, configuration);
		}
		
		protected override void OnClean (IProgressMonitor monitor, string configuration)
		{
			SolutionConfiguration config = Configurations [configuration];
			if (config == null)
				return;
			
			foreach (SolutionConfigurationEntry cce in config.Configurations) {
				if (cce.Item == null)
					LoggingService.LogWarning ("Combine.OnClean '{0}', configuration '{1}', entry '{2}': Entry is null", Name, config.Id, cce.Item.Name);
				else if (cce.Build)
					cce.Item.Clean (monitor, configuration);
			}
		}
		
		protected internal override void OnExecute (IProgressMonitor monitor, ExecutionContext context, string configuration)
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
					t.IsBackground = true;
					t.Start ();
				}
				foreach (IAsyncOperation op in list)
					op.WaitForCompleted ();
				
				monitor.EndTask ();
			}
		}
		
		protected internal override bool OnGetNeedsBuilding (string configuration)
		{
			return RootFolder.NeedsBuilding (configuration);
		}
		
		protected internal override void OnSetNeedsBuilding (bool val, string configuration)
		{
			RootFolder.SetNeedsBuilding (val, configuration);
		}
		
		protected virtual void OnStartupItemChanged(EventArgs e)
		{
			if (StartupItemChanged != null)
				StartupItemChanged (this, e);
		}
		
		public override FileFormat FileFormat {
			get {
				return base.FileFormat;
			}
			set {
				base.FileFormat = value;
				foreach (SolutionItem eitem in GetAllSolutionItems<SolutionItem> ()) {
					value.Format.ConvertToFormat (eitem);
					if (eitem is SolutionEntityItem)
						((SolutionEntityItem)eitem).InstallFormat (value);
				}
			}
		}
		
		public override List<string> GetItemFiles (bool includeReferencedFiles)
		{
			List<string> files = base.GetItemFiles (includeReferencedFiles);
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
					SetupNewItem (eitem);
			}
			else {
				SetupNewItem (args.SolutionItem);
			}
			
			if (SolutionItemAdded != null)
				SolutionItemAdded (this, args);
		}
		
		void SetupNewItem (SolutionItem item)
		{
			SolutionEntityItem eitem = item as SolutionEntityItem;
			if (!FileFormat.Format.SupportsMixedFormats || eitem == null || !eitem.IsSaved) {
				this.FileFormat.Format.ConvertToFormat (item);
				if (eitem != null)
					eitem.InstallFormat (this.FileFormat);
			}
			
			if (eitem != null) {
				eitem.NeedsReload = false;
				// Register the new entry in every solution configuration
				foreach (SolutionConfiguration conf in Configurations)
					conf.AddItem (eitem);
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
			foreach (SolutionConfiguration conf in Configurations)
				conf.RemoveItem (item);
			if (!reloading && item is DotNetProject)
				RemoveReferencesToProject ((DotNetProject) item);
			
			// Update the file name because the file format may have changed
			item.FileName = item.FileName;
			
			if (StartupItem == item)
				StartupItem = null;
			else
				MultiStartupItems.Remove (item);
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
		
		internal protected virtual void OnEntryModified (SolutionItemEventArgs args)
		{
			if (EntryModified != null)
				EntryModified (this, args);
		}
		
		internal protected virtual void OnEntrySaved (SolutionItemEventArgs args)
		{
			if (EntrySaved != null)
				EntrySaved (this, args);
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
		public event SolutionItemEventHandler EntryModified;
		public event SolutionItemEventHandler EntrySaved;
	}
}
