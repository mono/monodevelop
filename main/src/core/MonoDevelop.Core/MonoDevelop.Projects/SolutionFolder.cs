// SolutionFolder.cs
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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core.Serialization;
using System.Threading.Tasks;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	[DataInclude (typeof(SolutionConfiguration))]
	public sealed class SolutionFolder : SolutionFolderItem, IBuildTarget
	{
		SolutionFolderItemCollection items;
		SolutionFolderFileCollection files;
		string name;
		
		public SolutionFolder ()
		{
			Initialize (this);
		}
		
		public SolutionFolderItemCollection Items {
			get {
				if (items == null)
					items = new SolutionFolderItemCollection (this);
				return items;
			}
		}

		protected override IEnumerable<WorkspaceObject> OnGetChildren ()
		{
			return Items;
		}

		internal SolutionFolderItemCollection GetItemsWithoutCreating ()
		{
			return items;
		}
		
		[ItemProperty]
		[ProjectPathItemProperty ("File", Scope="*")]
		public SolutionFolderFileCollection Files {
			get {
				if (files == null)
					files = new SolutionFolderFileCollection (this);
				return files;
			}
		}
		
		public bool IsRoot {
			get { return ParentFolder == null; }
		}

		[ThreadSafe]
		protected override string OnGetName ()
		{
			var parent = ParentFolder == null && ParentSolution != null ? ParentSolution : null;
			if (parent != null)
				return parent.Name;
			else
				return name;
		}

		protected override void OnSetName (string value)
		{
			name = value;
		}

		protected override FilePath GetDefaultBaseDirectory ( )
		{
			// Since solution folders don't are not bound to a specific directory, we have to guess it.
			// First of all try to find a common root of all child projects

			if (ParentSolution == null)
				return FilePath.Null;

			if (ParentFolder == null)
				return ParentSolution.BaseDirectory;

			FilePath path = GetCommonPathRoot ();
			if (!string.IsNullOrEmpty (path))
			    return path;
			
			// Now try getting the folder using the folder name
			
			SolutionFolder folder = this;
			path = FilePath.Empty;
			do {
				// Root folder name is ignored
				path = path.Combine (folder.Name);
				folder = folder.ParentFolder;
			}
			while (folder.ParentFolder != null);
			
			path = ParentSolution.BaseDirectory.Combine (path);
			if (!Directory.Exists (path))
				return ParentFolder.BaseDirectory;
			else
				return path;
		}

		FilePath GetCommonPathRoot ( )
		{
			FilePath path = null;

			foreach (SolutionFolderItem it in Items) {
				FilePath subdir;
				if (it is SolutionFolder) {
					SolutionFolder sf = (SolutionFolder) it;
					if (sf.HasCustomBaseDirectory)
						subdir = sf.BaseDirectory;
					else
						subdir = sf.GetCommonPathRoot ();
				} else
					subdir = it.BaseDirectory;
				
				if (subdir.IsNullOrEmpty)
					return FilePath.Null;
				
				if (!path.IsNull) {
					// Find the common root
					path = GetCommonPathRoot (path, subdir);
					if (path.IsNullOrEmpty)
						break;
				} else
					path = subdir;
			}
		    return path;
		}
		
		string GetCommonPathRoot (string path1, string path2)
		{
			path1 = Path.GetFullPath (path1);
			path2 = Path.GetFullPath (path2);
			
			if (path1 == path2)
				return path1;
			
			path1 += Path.DirectorySeparatorChar;
			path2 += Path.DirectorySeparatorChar;
			
			int lastCommonSep = -1;
			for (int n=0; n<path1.Length && n<path2.Length; n++) {
				if (path1[n] != path2[n])
					break;
				else if (path1[n] == Path.DirectorySeparatorChar)
					lastCommonSep = n;
			}
			if (lastCommonSep > 0)
				return path1.Substring (0, lastCommonSep);
			else
				return null;
		}

		internal override IDictionary OnGetExtendedProperties ()
		{
			if (ParentSolution != null && ParentFolder == null)
				return ParentSolution.ExtendedProperties;
			else
				return base.OnGetExtendedProperties ();
		}
		
		protected override void OnDispose ()
		{
			if (items != null) {
				foreach (SolutionFolderItem e in items)
					e.Dispose ();
				items = null;
			}
			files = null;
			base.OnDispose ();
		}
		
		public async Task<SolutionFolderItem> ReloadItem (ProgressMonitor monitor, SolutionFolderItem sitem)
		{
			if (Items.IndexOf (sitem) == -1)
				throw new InvalidOperationException ("Solution item '" + sitem.Name + "' does not belong to folder '" + Name + "'");

			SolutionItem item = sitem as SolutionItem;
			if (item != null) {
				// Load the new item
				
				SolutionItem newItem;
				try {
					if (ParentSolution.IsSolutionItemEnabled (item.FileName)) {
						using (var ctx = new SolutionLoadContext (ParentSolution))
							newItem = await Services.ProjectService.ReadSolutionItem (monitor, item.FileName, null, ctx: ctx, itemGuid: item.ItemId);
					}
					else {
						UnknownSolutionItem e = new UnloadedSolutionItem () {
							FileName = item.FileName
						};
						e.ItemId = item.ItemId;
						e.TypeGuid = item.TypeGuid;
						newItem = e;
					}
				} catch (Exception ex) {
					UnknownSolutionItem e = new UnknownSolutionItem ();
					e.LoadError = ex.Message;
					e.FileName = item.FileName;
					newItem = e;
				}

				if (!Items.Contains (item)) {
					// The old item is gone, which probably means it has already been reloaded (BXC20615), or maybe removed.
					// In this case, there isn't anything else we can do
					newItem.Dispose ();

					// Find the replacement if it exists
					return Items.OfType<SolutionItem> ().FirstOrDefault (it => it.FileName == item.FileName);
				}

				// Replace in the file list
				Items.Replace (item, newItem);

				item.ParentFolder = null;
				DisconnectChildEntryEvents (item);
				ConnectChildEntryEvents (newItem);
	
				NotifyModified ("Items");
				OnItemRemoved (new SolutionItemChangeEventArgs (item, ParentSolution, true) { ReplacedItem = item } , true);
				OnItemAdded (new SolutionItemChangeEventArgs (newItem, ParentSolution, true) { ReplacedItem = item }, true);
				
				item.Dispose ();
				return newItem;
			}
			else
				return sitem;
		}
		
		internal void NotifyItemAdded (SolutionFolderItem item, bool newToSolution)
		{
			ConnectChildEntryEvents (item);

			NotifyModified ("Items");
			OnItemAdded (new SolutionItemChangeEventArgs (item, ParentSolution, false), newToSolution);
		}
		
		void ConnectChildEntryEvents (SolutionFolderItem item)
		{
			if (item is Project) {
				Project project = item as Project;
				project.FileRemovedFromProject += NotifyFileRemovedFromProject;
				project.FileAddedToProject += NotifyFileAddedToProject;
				project.FileChangedInProject += NotifyFileChangedInProject;
				project.FilePropertyChangedInProject += NotifyFilePropertyChangedInProject;
				project.FileRenamedInProject += NotifyFileRenamedInProject;
				if (item is DotNetProject) {
					((DotNetProject)project).ReferenceRemovedFromProject += NotifyReferenceRemovedFromProject;
					((DotNetProject)project).ReferenceAddedToProject += NotifyReferenceAddedToProject;
				}
			}
			
			if (item is SolutionFolder) {
				SolutionFolder folder = item as SolutionFolder;
				folder.FileRemovedFromProject += NotifyFileRemovedFromProject;
				folder.FileAddedToProject += NotifyFileAddedToProject;
				folder.FileChangedInProject += NotifyFileChangedInProject;
				folder.FilePropertyChangedInProject += NotifyFilePropertyChangedInProject;
				folder.FileRenamedInProject += NotifyFileRenamedInProject;
				folder.ReferenceRemovedFromProject += NotifyReferenceRemovedFromProject;
				folder.ReferenceAddedToProject += NotifyReferenceAddedToProject;
				folder.ItemSaved += NotifyItemSaved;
			}
			
			if (item is SolutionItem) {
				((SolutionItem)item).Saved += NotifyItemSaved;
//				((SolutionEntityItem)item).ReloadRequired += NotifyItemReloadRequired;
			}
			item.Modified += NotifyItemModified;
		}
		
		public Task<SolutionItem> AddItem (ProgressMonitor monitor, string filename)
		{
			return AddItem (monitor, filename, false);
		}

		public async Task<SolutionItem> AddItem (ProgressMonitor monitor, string filename, bool createSolutionConfigurations)
		{
			if (monitor == null) monitor = new ProgressMonitor ();
			using (var ctx = new SolutionLoadContext (ParentSolution)) {
				var entry = await Services.ProjectService.ReadSolutionItem (monitor, filename, null, ctx: ctx);
				AddItem (entry, createSolutionConfigurations);
				return entry;
			}
		}

		public void AddItem (SolutionFolderItem item)
		{
			AddItem (item, false);
		}
		
		public void AddItem (SolutionFolderItem item, bool createSolutionConfigurations)
		{
			Items.Add (item);
			
			SolutionItem eitem = item as SolutionItem;
			if (eitem != null && createSolutionConfigurations && eitem.SupportsBuild ()) {
				// Create new solution configurations for item configurations
				foreach (ItemConfiguration iconf in eitem.Configurations) {
					bool found = false;
					foreach (SolutionConfiguration conf in ParentSolution.Configurations) {
						if (conf.Name == iconf.Name && (iconf.Platform == conf.Platform || iconf.Platform.Length == 0)) {
							found = true;
							break;
						}
					}
					if (!found) {
						SolutionConfiguration sconf = new SolutionConfiguration (iconf.Id);
						// Add all items to the new configuration
						foreach (var it in ParentSolution.GetAllItems<SolutionItem> ())
							sconf.AddItem (it);
						ParentSolution.Configurations.Add (sconf);
					}
				}
			}
		}

		internal void NotifyItemRemoved (SolutionFolderItem item, bool removedFromSolution)
		{
			DisconnectChildEntryEvents (item);
			NotifyModified ("Items");
			OnItemRemoved (new SolutionItemChangeEventArgs (item, ParentSolution, false), removedFromSolution);
		}
		
		void DisconnectChildEntryEvents (SolutionFolderItem entry)
		{
			if (entry is Project) {
				Project pce = entry as Project;
				pce.FileRemovedFromProject -= NotifyFileRemovedFromProject;
				pce.FileAddedToProject -= NotifyFileAddedToProject;
				pce.FileChangedInProject -= NotifyFileChangedInProject;
				pce.FilePropertyChangedInProject -= NotifyFilePropertyChangedInProject;
				pce.FileRenamedInProject -= NotifyFileRenamedInProject;
				if (pce is DotNetProject) {
					((DotNetProject)pce).ReferenceRemovedFromProject -= NotifyReferenceRemovedFromProject;
					((DotNetProject)pce).ReferenceAddedToProject -= NotifyReferenceAddedToProject;
				}
			}
			
			if (entry is SolutionFolder) {
				SolutionFolder cce = entry as SolutionFolder;
				cce.FileRemovedFromProject -= NotifyFileRemovedFromProject;
				cce.FileAddedToProject -= NotifyFileAddedToProject;
				cce.FileChangedInProject -= NotifyFileChangedInProject;
				cce.FilePropertyChangedInProject -= NotifyFilePropertyChangedInProject;
				cce.FileRenamedInProject -= NotifyFileRenamedInProject;
				cce.ReferenceRemovedFromProject -= NotifyReferenceRemovedFromProject;
				cce.ReferenceAddedToProject -= NotifyReferenceAddedToProject;
				cce.ItemSaved -= NotifyItemSaved;
			}
			
			if (entry is SolutionItem) {
				((SolutionItem)entry).Saved -= NotifyItemSaved;
//				((SolutionEntityItem)entry).ReloadRequired -= NotifyItemReloadRequired;
			}
			entry.Modified -= NotifyItemModified;
		}
		
		public Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return Task.FromResult (false);
		}

		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return false;
		}

		public Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return Task.FromResult (false);
		}

		public IEnumerable<IBuildTarget> GetExecutionDependencies ()
		{
			yield break;
		}

		/// <remarks>
		/// Returns a collection containing all entries in this folder and 
		/// undercombines
		/// </remarks>
		public IEnumerable<SolutionFolderItem> GetAllItems ()
		{
			return GetAllItems<SolutionFolderItem> ();
		}
		
		public ReadOnlyCollection<T> GetAllItemsWithTopologicalSort<T> (ConfigurationSelector configuration) where T: SolutionItem
		{
			List<T> list = new List<T> ();
			GetAllItems<T> (list, this);
			return SolutionItem.TopologicalSort<T> (list, configuration);
		}
		
		public ReadOnlyCollection<Project> GetAllProjects ()
		{
			List<Project> list = new List<Project> ();
			GetAllItems<Project> (list, this);
			return list.AsReadOnly ();
		}
		
		// The projects are returned in the order
		// they should be compiled, acording to their references.
		public ReadOnlyCollection<Project> GetAllProjectsWithTopologicalSort (ConfigurationSelector configuration)
		{
			List<Project> list = new List<Project> ();
			GetAllItems<Project> (list, this);
			return SolutionItem.TopologicalSort<Project> (list, configuration);
		}
		
		void GetAllItems<T> (List<T> list, SolutionFolderItem item) where T: SolutionFolderItem
		{
			if (item is T) {
				list.Add ((T)item);
			}
		
			if (item is SolutionFolder) {
				foreach (SolutionFolderItem ce in ((SolutionFolder)item).Items)
					GetAllItems<T> (list, ce);
			}
		}
		
		public ReadOnlyCollection<SolutionItem> GetAllBuildableEntries (ConfigurationSelector configuration, bool topologicalSort, bool includeExternalReferences)
		{
			var list = new List<SolutionItem> ();
			GetAllBuildableEntries (list, configuration, includeExternalReferences);
			if (topologicalSort)
				return SolutionItem.TopologicalSort<SolutionItem> (list, configuration);
			else
				return list.AsReadOnly ();
		}
		
		public ReadOnlyCollection<SolutionItem> GetAllBuildableEntries (ConfigurationSelector configuration)
		{
			return GetAllBuildableEntries (configuration, false, false);
		}
		
		void GetAllBuildableEntries (List<SolutionItem> list, ConfigurationSelector configuration, bool includeExternalReferences)
		{
			if (ParentSolution == null)
				return;
			SolutionConfiguration conf = ParentSolution.GetConfiguration (configuration);
			if (conf == null)
				return;

			foreach (SolutionFolderItem item in Items) {
				if (item is SolutionFolder)
					((SolutionFolder)item).GetAllBuildableEntries (list, configuration, includeExternalReferences);
				else if ((item is SolutionItem) && conf.BuildEnabledForItem ((SolutionItem) item) && ((SolutionItem)item).SupportsBuild ())
					GetAllBuildableReferences (list, (SolutionItem)item, configuration, conf, includeExternalReferences, false);
			}
		}

		void GetAllBuildableReferences (List<SolutionItem> list, SolutionItem item, ConfigurationSelector configuration, SolutionConfiguration conf, bool includeExternalReferences, bool isDirectReference)
		{
			if (list.Contains (item) || !conf.BuildEnabledForItem (item))
				return;
			// Skip unsupported projects which are not directly referenced by other (supported) projects
			if (!isDirectReference && item.IsUnsupportedProject)
				return;
			list.Add (item);
			if (includeExternalReferences) {
				foreach (var it in item.GetReferencedItems (configuration))
					GetAllBuildableReferences (list, it, configuration, conf, includeExternalReferences, true);
			}
		}

		[Obsolete("Use GetProjectsContainingFile() (plural) instead")]
		public Project GetProjectContainingFile (string fileName) 
		{
			ReadOnlyCollection<Project> projects = GetAllProjects ();
			foreach (Project projectEntry in projects) {
				if (projectEntry.FileName == fileName || projectEntry.IsFileInProject(fileName)) {
					return projectEntry;
				}
			}
			return null;
		}

		public IEnumerable<Project> GetProjectsContainingFile (string fileName)
		{
			ReadOnlyCollection<Project> projects = GetAllProjects ();

			Project mainProject = null;
			var projectsWithLinks = new List<Project>();
			foreach (Project projectEntry in projects) {
				if (projectEntry.FileName == fileName || projectEntry.IsFileInProject(fileName)) {
					var projectPath = Path.GetDirectoryName (projectEntry.FileName);
					if (fileName.StartsWith (projectPath)) {
						mainProject = projectEntry;
					} else {
						projectsWithLinks.Add (projectEntry);
					}
				}
			}

			if (mainProject != null) {
				yield return mainProject;
			}
			foreach (var project in projectsWithLinks) {
				yield return project;
			}
		}
		
		public SolutionItem FindSolutionItem (string fileName)
		{
			string path = Path.GetFullPath (fileName);
			foreach (SolutionFolderItem it in Items) {
				if (it is SolutionFolder) {
					SolutionItem r = ((SolutionFolder)it).FindSolutionItem (fileName);
					if (r != null)
						return r;
				}
				else if (it is SolutionItem) {
					SolutionItem se = (SolutionItem) it;
					if (!string.IsNullOrEmpty (se.FileName) && path == Path.GetFullPath (se.FileName))
						return (SolutionItem) it;
				}
			}
			return null;
		}
		
		public Project FindProjectByName (string name)
		{
			foreach (SolutionFolderItem it in Items) {
				if (it is SolutionFolder) {
					Project r = ((SolutionFolder)it).FindProjectByName (name);
					if (r != null)
						return r;
				}
				else if (it is Project) {
					if (name == it.Name)
						return (Project) it;
				}
			}
			return null;
		}

		bool IBuildTarget.CanBuild (ConfigurationSelector configuration)
		{
			return true;
		}

		public async Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext = null)
		{
			if (ParentSolution == null)
				return new BuildResult();
			SolutionConfiguration conf = ParentSolution.GetConfiguration (configuration);
			if (conf == null)
				return new BuildResult();

			if (operationContext == null)
				operationContext = new OperationContext ();

			ReadOnlyCollection<SolutionItem> allProjects;
			try {
				allProjects = GetAllBuildableEntries (configuration, true, true);
			} catch (CyclicDependencyException) {
				monitor.ReportError (GettextCatalog.GetString ("Cyclic dependencies are not supported."), null);
				return new BuildResult ("", 1, 1);
			}

			monitor.BeginTask (GettextCatalog.GetString ("Cleaning Solution: {0} ({1})", Name, configuration.ToString ()), allProjects.Count);

			bool operationStarted = false;
			BuildResult result = null;

			try {
				operationStarted = ParentSolution != null && await ParentSolution.BeginBuildOperation (monitor, configuration, operationContext);

				return result = await RunParallelBuildOperation (monitor, configuration, allProjects, (ProgressMonitor m, SolutionItem item) => {
					return item.Clean (m, configuration, operationContext);
				}, false);
			}
			finally {
				if (operationStarted)
					await ParentSolution.EndBuildOperation (monitor, configuration, operationContext, result);
				monitor.EndTask ();
			}
		}

		class BuildStatus
		{
			public bool Failed;
			public Task Task;
			public BuildResult Result;
		}

		public async Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector configuration, bool buildReferencedTargets = false, OperationContext operationContext = null)
		{
			ReadOnlyCollection<SolutionItem> allProjects;
				
			try {
				allProjects = GetAllBuildableEntries (configuration, true, true);
			} catch (CyclicDependencyException) {
				monitor.ReportError (GettextCatalog.GetString ("Cyclic dependencies are not supported."), null);
				return new BuildResult ("", 1, 1);
			}

			if (operationContext == null)
				operationContext = new OperationContext ();

			bool operationStarted = false;
			BuildResult result = null;

			try {
				
				monitor.BeginTask (GettextCatalog.GetString ("Building Solution: {0} ({1})", Name, configuration.ToString ()), allProjects.Count);

				operationStarted = ParentSolution != null && await ParentSolution.BeginBuildOperation (monitor, configuration, operationContext);

				return result = await RunParallelBuildOperation (monitor, configuration, allProjects, (ProgressMonitor m, SolutionItem item) => {
					return item.Build (m, configuration, false, operationContext);
				}, false);

			} finally {
				if (operationStarted)
					await ParentSolution.EndBuildOperation (monitor, configuration, operationContext, result);
				monitor.EndTask ();
			}
        }

		internal static async Task<BuildResult> RunParallelBuildOperation (ProgressMonitor monitor, ConfigurationSelector configuration, IEnumerable<SolutionItem> sortedItems, Func<ProgressMonitor,SolutionItem,Task<BuildResult>> buildAction, bool ignoreFailed)
		{
			List<SolutionItem> toBuild = new List<SolutionItem> (sortedItems);
			BuildResult cres = new BuildResult ();
			cres.BuildCount = 0;

			// Limit the number of concurrent builders to processors / 2

			var slotScheduler = new TaskSlotScheduler (Environment.ProcessorCount / 2);

			// Create a dictionary with the status objects of all items

			var buildStatus = new Dictionary<SolutionItem, BuildStatus> ();
			foreach (var it in toBuild)
				buildStatus.Add (it, new BuildStatus ());

			// Start the build tasks for all itemsw

			foreach (var itemToBuild in toBuild) {
				if (monitor.CancellationToken.IsCancellationRequested)
					break;

				var item = itemToBuild;

				var myStatus = buildStatus [item];

				var myMonitor = monitor.BeginAsyncStep (1);

				// Get a list of the status objects for all items on which this one depends

				var refStatus = item.GetReferencedItems (configuration).Select (it => {
					BuildStatus bs;
					buildStatus.TryGetValue (it, out bs);
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
				BuildStatus bs;
				if (buildStatus.TryGetValue (it, out bs) && bs.Result != null)
					cres.Append (bs.Result);
			}

			return cres;
		}

		[Obsolete("This method will be removed in future releases")]
		public bool NeedsBuilding (ConfigurationSelector configuration)
		{
			return Items.OfType<IBuildTarget>().Any (t => t.NeedsBuilding (configuration));
		}

		protected internal override DateTime OnGetLastBuildTime (ConfigurationSelector configuration)
		{
			// Return the min value, since that the last time all items in the
			// folder were built
			DateTime tim = DateTime.MaxValue;
			foreach (SolutionFolderItem it in Items) {
				DateTime t = it.GetLastBuildTime (configuration);
				if (t < tim)
					tim = t;
			}
			return tim;
		}


		public void RemoveFileFromProjects (string fileName)
		{
			if (Directory.Exists (fileName)) {
				RemoveAllInDirectory(fileName);
			} else {
				RemoveFileFromAllProjects(fileName);
			}
		}

		void RemoveAllInDirectory (string dirName)
		{
			foreach (Project projectEntry in GetAllProjects()) {
				foreach (ProjectFile file in projectEntry.Files.GetFilesInPath (dirName))
					projectEntry.Files.Remove (file);
			}
		}
		
		void RemoveFileFromAllProjects (string fileName)
		{
			foreach (SolutionFolder sf in GetAllItems<SolutionFolder> ()) {
				sf.Files.Remove (fileName);
			}
			foreach (Project projectEntry in GetAllProjects()) {
				List<ProjectFile> toDelete = new List<ProjectFile> ();
				foreach (ProjectFile fInfo in projectEntry.Files) {
					if (fInfo.Name == fileName)
						toDelete.Add (fInfo);
				}
				foreach (ProjectFile file in toDelete)
					projectEntry.Files.Remove (file);
			}
		}
		
		public void RenameFileInProjects (FilePath sourceFile, FilePath targetFile)
		{
			if (Directory.Exists (targetFile)) {
				RenameDirectoryInAllProjects (sourceFile, targetFile);
			} else {
				RenameFileInAllProjects(sourceFile, targetFile);
			}
		}
		
		void RenameFileInAllProjects (FilePath oldName, FilePath newName)
		{
			foreach (Project projectEntry in GetAllProjects()) {
				foreach (ProjectFile fInfo in projectEntry.Files) {
					if (fInfo.FilePath == oldName) {
						if (fInfo.BuildAction == projectEntry.GetDefaultBuildAction (oldName))
							fInfo.BuildAction = projectEntry.GetDefaultBuildAction (newName);
						fInfo.Name = newName;
					}
				}
			}
		}

		void RenameDirectoryInAllProjects (FilePath oldName, FilePath newName)
		{
			foreach (Project projectEntry in GetAllProjects()) {
				foreach (ProjectFile fInfo in projectEntry.Files) {
					if (fInfo.FilePath == oldName)
						fInfo.Name = newName;
					else if (fInfo.FilePath.IsChildPathOf (oldName))
						fInfo.Name = newName.Combine (fInfo.FilePath.ToRelative (oldName));
					else if (fInfo.IsLink) {
						// update links
						var fullVirtualPath = projectEntry.BaseDirectory.Combine(fInfo.ProjectVirtualPath);
						if (fullVirtualPath.IsChildPathOf (oldName))
							fInfo.Link = newName.ToRelative (projectEntry.BaseDirectory)
										        .Combine (fullVirtualPath.ToRelative (oldName));
					}
				}
			}
		}

		internal void NotifyFileRemovedFromProject (object sender, ProjectFileEventArgs e)
		{
			OnFileRemovedFromProject (e);
		}
		
		internal void NotifyFileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			OnFileAddedToProject (e);
		}

		internal void NotifyFileChangedInProject (object sender, ProjectFileEventArgs e)
		{
			OnFileChangedInProject (e);
		}
		
		internal void NotifyFilePropertyChangedInProject (object sender, ProjectFileEventArgs e)
		{
			OnFilePropertyChangedInProject (e);
		}
		
		internal void NotifyFileRenamedInProject (object sender, ProjectFileRenamedEventArgs e)
		{
			OnFileRenamedInProject (e);
		}
		
		internal void NotifyReferenceRemovedFromProject (object sender, ProjectReferenceEventArgs e)
		{
			OnReferenceRemovedFromProject (e);
		}
		
		internal void NotifyReferenceAddedToProject (object sender, ProjectReferenceEventArgs e)
		{
			OnReferenceAddedToProject (e);
		}
		
		internal void NotifyItemModified (object sender, SolutionItemModifiedEventArgs e)
		{
			OnItemModified (e);
		}
		
		internal void NotifyItemSaved (object sender, SolutionItemSavedEventArgs e)
		{
			OnItemSaved (e);
		}
		
/*		internal void NotifyItemReloadRequired (object sender, SolutionItemEventArgs e)
		{
			OnItemReloadRequired (e);
		}
				 */
		internal void NotifyItemAddedToFolder (object sender, SolutionItemChangeEventArgs e, bool newToSolution)
		{
			if (ParentFolder != null)
				ParentFolder.NotifyItemAddedToFolder (sender, e, newToSolution);
			else if (ParentSolution != null && newToSolution)
				ParentSolution.OnSolutionItemAdded (e);
			if (DescendantItemAdded != null)
				DescendantItemAdded (sender, e);
		}
		
		internal void NotifyItemRemovedFromFolder (object sender, SolutionItemChangeEventArgs e, bool removedFromSolution)
		{
			if (DescendantItemRemoved != null)
				DescendantItemRemoved (sender, e);
			if (ParentFolder != null)
				ParentFolder.NotifyItemRemovedFromFolder (sender, e, removedFromSolution);
			else if (ParentSolution != null && removedFromSolution)
				ParentSolution.OnSolutionItemRemoved (e);
		}
		
		internal void NotifyFilesAdded (params FilePath[] files)
		{
			foreach (FilePath f in files)
				OnSolutionItemFileAdded (new SolutionItemFileEventArgs (f));
		}
		
		internal void NotifyFilesRemoved (params FilePath[] files)
		{
			foreach (FilePath f in files)
				OnSolutionItemFileRemoved (new SolutionItemFileEventArgs (f));
		}
		
		void OnItemAdded (SolutionItemChangeEventArgs e, bool newToSolution)
		{
			NotifyItemAddedToFolder (this, e, newToSolution);
			
			// Fire the event after notifying the parent because the parent may need to complete
			// some data initialization before the item is accessible to the clients. For example,
			// the solution needs to setup the configuration maps.
			OnItemAdded (e);
		}
		
		void OnItemAdded (SolutionItemChangeEventArgs e)
		{
			if (ItemAdded != null)
				ItemAdded (this, e);
		}
		
		void OnItemRemoved (SolutionItemChangeEventArgs e, bool removedFromSolution)
		{
			OnItemRemoved (e);
			NotifyItemRemovedFromFolder (this, e, removedFromSolution);
		}
		
		void OnItemRemoved (SolutionItemChangeEventArgs e)
		{
			if (ItemRemoved != null)
				ItemRemoved (this, e);
		}
		
		void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnFileRemovedFromProject (e);
			if (FileRemovedFromProject != null) {
				FileRemovedFromProject (this, e);
			}
		}

		void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnFileChangedInProject (e);
			if (FileChangedInProject != null) {
				FileChangedInProject (this, e);
			}
		}
		
		void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnFilePropertyChangedInProject (e);
			if (FilePropertyChangedInProject != null) {
				FilePropertyChangedInProject (this, e);
			}
		}
		
		void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnFileAddedToProject (e);
			if (FileAddedToProject != null) {
				FileAddedToProject (this, e);
			}
		}
		
		void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnFileRenamedInProject (e);
			if (FileRenamedInProject != null) {
				FileRenamedInProject (this, e);
			}
		}
		
		void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnReferenceRemovedFromProject (e);
			if (ReferenceRemovedFromProject != null) {
				ReferenceRemovedFromProject (this, e);
			}
		}
		
		void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnReferenceAddedToProject (e);
			if (ReferenceAddedToProject != null) {
				ReferenceAddedToProject (this, e);
			}
		}

		void OnItemModified (SolutionItemModifiedEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnEntryModified (e);
			if (ItemModified != null)
				ItemModified (this, e);
		}
		
		void OnItemSaved (SolutionItemSavedEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnEntrySaved (e);
			if (ItemSaved != null)
				ItemSaved (this, e);
		}
		
		void OnSolutionItemFileAdded (SolutionItemFileEventArgs args)
		{
			if (SolutionItemFileAdded != null)
				SolutionItemFileAdded (this, args);
		}
		
		void OnSolutionItemFileRemoved (SolutionItemFileEventArgs args)
		{
			if (SolutionItemFileRemoved != null)
				SolutionItemFileRemoved (this, args);
		}
		
/*		protected virtual void OnItemReloadRequired (SolutionItemEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnItemReloadRequired (e);
			if (ItemReloadRequired != null)
				ItemReloadRequired (this, e);
		}
*/
		
		public event SolutionItemChangeEventHandler ItemAdded;
		public event SolutionItemChangeEventHandler ItemRemoved;
		public event SolutionItemChangeEventHandler DescendantItemAdded;     // Fires for child folders
		public event SolutionItemChangeEventHandler DescendantItemRemoved; // Fires for child folders
		public event ProjectFileEventHandler FileAddedToProject;
		public event ProjectFileEventHandler FileRemovedFromProject;
		public event ProjectFileEventHandler FileChangedInProject;
		public event ProjectFileEventHandler FilePropertyChangedInProject;
		public event ProjectFileRenamedEventHandler FileRenamedInProject;
		public event ProjectReferenceEventHandler ReferenceAddedToProject;
		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;
		public event SolutionItemModifiedEventHandler ItemModified;
		public event SolutionItemSavedEventHandler ItemSaved;
		public event EventHandler<SolutionItemFileEventArgs> SolutionItemFileAdded;
		public event EventHandler<SolutionItemFileEventArgs> SolutionItemFileRemoved;
//		public event EventHandler<SolutionItemEventArgs> ItemReloadRequired;
	}
	
	class DummySolutionFolderHandler
	{
		SolutionFolder folder;
		
		public DummySolutionFolderHandler (SolutionFolder folder)
		{
			this.folder = folder;
		}
		
		public string ItemId {
			get { return folder.Name; }
		}
		
		public Task<BuildResult> RunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}
		
		public Task Save (ProgressMonitor monitor)
		{
			throw new NotImplementedException ();
		}
		
		public bool SyncFileName {
			get { return false; }
		}
		
		public void OnModified (string hint)
		{
		}

		public void Dispose ()
		{
		}

		public object GetService (Type t)
		{
			return null;
		}
	}
	
	public class SolutionFolderFileCollection: System.Collections.ObjectModel.Collection<FilePath>
	{
		SolutionFolder parent;
		
		internal SolutionFolderFileCollection (SolutionFolder parent)
		{
			this.parent = parent;
		}
		
		protected override void ClearItems ()
		{
			FilePath[] files = new FilePath [Count];
			CopyTo (files, 0);
			base.ClearItems();
			parent.NotifyFilesRemoved (files);
		}
		
		protected override void InsertItem (int index, FilePath item)
		{
			base.InsertItem(index, item);
			parent.NotifyFilesAdded (item);
		}
		
		protected override void RemoveItem (int index)
		{
			FilePath f = this[index];
			base.RemoveItem (index);
			parent.NotifyFilesRemoved (f);
		}
		
		protected override void SetItem (int index, FilePath item)
		{
			FilePath f = this[index];
			base.SetItem(index, item);
			parent.NotifyFilesRemoved (f);
			parent.NotifyFilesAdded (item);
		}
	}
	
	public class SolutionItemFileEventArgs: EventArgs
	{
		FilePath file;
		
		public SolutionItemFileEventArgs (FilePath file)
		{
			this.file = file;
		}

		public FilePath File {
			get { return this.file; }
		}
	}

	/// <summary>
	/// Keeps track of slots available for executing an operation
	/// </summary>
	class TaskSlotScheduler
	{
		int freeSlots;
		Queue<TaskCompletionSource<IDisposable>> waitQueue = new Queue<TaskCompletionSource<IDisposable>> ();

		class Slot: IDisposable
		{
			public TaskSlotScheduler TaskSlotScheduler;

			public void Dispose ()
			{
				if (TaskSlotScheduler != null) {
					TaskSlotScheduler.FreeSlot ();
					TaskSlotScheduler = null;
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:MonoDevelop.Projects.TaskSlotScheduler"/> class.
		/// </summary>
		/// <param name="slots">Initial number of slots available</param>
		public TaskSlotScheduler (int slots)
		{
			freeSlots = Math.Max (slots, 1);
		}

		/// <summary>
		/// Gets a slot, to be disposed when done with the operation
		/// </summary>
		/// <returns>The task slot.</returns>
		public Task<IDisposable> GetTaskSlot ()
		{
			lock (waitQueue) {
				if (freeSlots > 0) {
					freeSlots--;
					return Task.FromResult ((IDisposable)new Slot { TaskSlotScheduler = this });
				} else {
					var cs = new TaskCompletionSource<IDisposable> ();
					waitQueue.Enqueue (cs);
					return cs.Task;
				}
			}
		}

		void FreeSlot ()
		{
			lock (waitQueue) {
				if (waitQueue.Count > 0) {
					var cs = waitQueue.Dequeue ();
					cs.SetResult (new Slot { TaskSlotScheduler = this });
				} else
					freeSlots++;
			}
		}
	}
}
