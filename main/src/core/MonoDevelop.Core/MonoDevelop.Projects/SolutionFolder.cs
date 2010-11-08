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
using System.Xml;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects
{
	[DataInclude (typeof(SolutionConfiguration))]
	public class SolutionFolder : SolutionItem
	{
		SolutionFolderItemCollection items;
		SolutionFolderFileCollection files;
		string name;
		
		public SolutionFolder ()
		{
		}
		
		public SolutionFolderItemCollection Items {
			get {
				if (items == null)
					items = new SolutionFolderItemCollection (this);
				return items;
			}
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
		
		public virtual bool IsRoot {
			get { return ParentFolder == null; }
		}

		public override string Name {
			get {
				if (ParentFolder == null && ParentSolution != null)
					return ParentSolution.Name;
				else
					return name;
			}
			set {
				if (value != name) {
					string oldName = name;
					name = value;
					OnNameChanged (new SolutionItemRenamedEventArgs (this, oldName, name));
				}
			}
		}

		protected override FilePath GetDefaultBaseDirectory ( )
		{
			// Since solution folders don't are not bound to a specific directory, we have to guess it.
			// First of all try to find a common root of all child projects
			
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

			foreach (SolutionItem it in Items) {
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
		
		internal override IDictionary InternalGetExtendedProperties {
			get {
				if (ParentSolution != null && ParentFolder == null)
					return ParentSolution.ExtendedProperties;
				else
					return base.InternalGetExtendedProperties;
			}
		}
		
		protected override void InitializeItemHandler ()
		{
			SetItemHandler (new DummySolutionFolderHandler (this));
		}

		
		protected internal override bool OnGetNeedsBuilding (ConfigurationSelector configuration)
		{
			foreach (SolutionItem item in Items)
				if (item.NeedsBuilding (configuration)) return true;
			return false;
		}
		
		protected internal override void OnSetNeedsBuilding (bool value, ConfigurationSelector configuration)
		{
			// Ignore
		}
		
		public override void Dispose()
		{
			base.Dispose ();
			foreach (SolutionItem e in Items)
				e.Dispose ();
		}
		
		public SolutionItem ReloadItem (IProgressMonitor monitor, SolutionItem sitem)
		{
			if (Items.IndexOf (sitem) == -1)
				throw new InvalidOperationException ("Solution item '" + sitem.Name + "' does not belong to folder '" + Name + "'");

			SolutionEntityItem item = sitem as SolutionEntityItem;
			if (item != null) {
				// Load the new item
				
				SolutionEntityItem newItem;
				try {
					newItem = Services.ProjectService.ReadSolutionItem (monitor, item.FileName);
				} catch (Exception ex) {
					UnknownSolutionItem e = new UnknownSolutionItem ();
					e.LoadError = ex.Message;
					e.FileName = item.FileName;
					newItem = e;
				}
				
				// Replace in the file list
				Items.Replace (item, newItem);
				
				DisconnectChildEntryEvents (item);
				ConnectChildEntryEvents (newItem);
	
				NotifyModified ("Items");
				OnItemRemoved (new SolutionItemChangeEventArgs (item, ParentSolution, true), true);
				OnItemAdded (new SolutionItemChangeEventArgs (newItem, ParentSolution, true), true);
				
				item.Dispose ();
				return newItem;
			}
			else
				return sitem;
		}
		
		internal void NotifyItemAdded (SolutionItem item, bool newToSolution)
		{
			ConnectChildEntryEvents (item);

			NotifyModified ("Items");
			OnItemAdded (new SolutionItemChangeEventArgs (item, ParentSolution, false), newToSolution);
		}
		
		void ConnectChildEntryEvents (SolutionItem item)
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
			}
			
			if (item is SolutionEntityItem) {
				((SolutionEntityItem)item).Saved += NotifyItemSaved;
//				((SolutionEntityItem)item).ReloadRequired += NotifyItemReloadRequired;
			}
			item.Modified += NotifyItemModified;
		}
		
		public override void Save (IProgressMonitor monitor)
		{
			foreach (SolutionItem item in Items)
				item.Save (monitor);
		}

		public SolutionEntityItem AddItem (IProgressMonitor monitor, string filename)
		{
			return AddItem (monitor, filename, false);
		}

		public SolutionEntityItem AddItem (IProgressMonitor monitor, string filename, bool createSolutionConfigurations)
		{
			if (monitor == null) monitor = new NullProgressMonitor ();
			SolutionEntityItem entry = Services.ProjectService.ReadSolutionItem (monitor, filename);
			AddItem (entry, createSolutionConfigurations);
			return entry;
		}

		public void AddItem (SolutionItem item)
		{
			AddItem (item, false);
		}
		
		public void AddItem (SolutionItem item, bool createSolutionConfigurations)
		{
			Items.Add (item);
			
			SolutionEntityItem eitem = item as SolutionEntityItem;
			if (eitem != null && createSolutionConfigurations) {
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
						sconf.AddItem (eitem);
						ParentSolution.Configurations.Add (sconf);
					}
				}
			}
		}

		internal void NotifyItemRemoved (SolutionItem item, bool removedFromSolution)
		{
			DisconnectChildEntryEvents (item);
			NotifyModified ("Items");
			OnItemRemoved (new SolutionItemChangeEventArgs (item, ParentSolution, false), removedFromSolution);
		}
		
		void DisconnectChildEntryEvents (SolutionItem entry)
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
			}
			
			if (entry is SolutionEntityItem) {
				((SolutionEntityItem)entry).Saved -= NotifyItemSaved;
//				((SolutionEntityItem)entry).ReloadRequired -= NotifyItemReloadRequired;
			}
			entry.Modified -= NotifyItemModified;
		}
		
		protected internal override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
		}
		
		/// <remarks>
		/// Returns a collection containing all entries in this folder and 
		/// undercombines
		/// </remarks>
		public ReadOnlyCollection<SolutionItem> GetAllItems ()
		{
			return GetAllItems<SolutionItem> ();
		}
		
		/// <remarks>
		/// Returns a collection containing all entries of the given type in this folder and 
		/// undercombines
		/// </remarks>
		public ReadOnlyCollection<T> GetAllItems<T> () where T: SolutionItem
		{
			List<T> list = new List<T> ();
			GetAllItems<T> (list, this);
			return list.AsReadOnly ();
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
		
		void GetAllItems<T> (List<T> list, SolutionItem item) where T: SolutionItem
		{
			if (item is T) {
				list.Add ((T)item);
			}
		
			if (item is SolutionFolder) {
				foreach (SolutionItem ce in ((SolutionFolder)item).Items)
					GetAllItems<T> (list, ce);
			}
		}
		
		public ReadOnlyCollection<SolutionItem> GetAllBuildableEntries (ConfigurationSelector configuration, bool topologicalSort, bool includeExternalReferences)
		{
			List<SolutionItem> list = new List<SolutionItem> ();
			GetAllBuildableEntries (list, configuration, includeExternalReferences);
			if (topologicalSort)
				return TopologicalSort<SolutionItem> (list, configuration);
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

			foreach (SolutionItem item in Items) {
				if (item is SolutionFolder)
					((SolutionFolder)item).GetAllBuildableEntries (list, configuration, includeExternalReferences);
				else if ((item is SolutionEntityItem) && conf.BuildEnabledForItem ((SolutionEntityItem) item))
					GetAllBuildableReferences (list, item, configuration, includeExternalReferences);
			}
		}

		void GetAllBuildableReferences (List<SolutionItem> list, SolutionItem item, ConfigurationSelector configuration, bool includeExternalReferences)
		{
			if (list.Contains (item))
				return;
			list.Add (item);
			if (includeExternalReferences) {
				foreach (SolutionItem it in item.GetReferencedItems (configuration))
					GetAllBuildableReferences (list, it, configuration, includeExternalReferences);
			}
		}
		
		public Project GetProjectContainingFile (string fileName) 
		{
			ReadOnlyCollection<Project> projects = GetAllProjects ();
			foreach (Project projectEntry in projects) {
				if (projectEntry.IsFileInProject(fileName)) {
					return projectEntry;
				}
			}
			return null;
		}
		
		public SolutionEntityItem FindSolutionItem (string fileName)
		{
			string path = Path.GetFullPath (fileName);
			foreach (SolutionItem it in Items) {
				if (it is SolutionFolder) {
					SolutionEntityItem r = ((SolutionFolder)it).FindSolutionItem (fileName);
					if (r != null)
						return r;
				}
				else if (it is SolutionEntityItem) {
					SolutionEntityItem se = (SolutionEntityItem) it;
					if (!string.IsNullOrEmpty (se.FileName) && path == Path.GetFullPath (se.FileName))
						return (SolutionEntityItem) it;
				}
			}
			return null;
		}
		
		public Project FindProjectByName (string name)
		{
			foreach (SolutionItem it in Items) {
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

		protected internal override BuildResult OnRunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			if (target == ProjectService.BuildTarget)
				return OnBuild (monitor, configuration);
			else if (target == ProjectService.CleanTarget) {
				OnClean (monitor, configuration);
				return new BuildResult ();
			}
			
			ReadOnlyCollection<SolutionItem> allProjects;
				
			try {
				allProjects = GetAllBuildableEntries (configuration, true, true);
			} catch (CyclicDependencyException) {
				monitor.ReportError (GettextCatalog.GetString ("Cyclic dependencies are not supported."), null);
				return new BuildResult ("", 1, 1);
			}
			
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Building Solution {0}", Name), allProjects.Count);
				
				BuildResult cres = new BuildResult ();
				cres.BuildCount = 0;
				HashSet<SolutionItem> failedItems = new HashSet<SolutionItem> ();
				
				foreach (SolutionItem item in allProjects) {
					if (monitor.IsCancelRequested)
						break;

					if (!item.ContainsReferences (failedItems, configuration)) {
						BuildResult res = item.RunTarget (monitor, target, configuration);
						if (res != null) {
							cres.Append (res);
							if (res.ErrorCount > 0)
								failedItems.Add (item);
						}
					} else
						failedItems.Add (item);
					monitor.Step (1);
				}
				return cres;
			} finally {
				monitor.EndTask ();
			}
		}
		
		protected override void OnClean (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			if (ParentSolution == null)
				return;
			SolutionConfiguration conf = ParentSolution.GetConfiguration (configuration);
			if (conf == null)
				return;

			foreach (SolutionItem item in Items) {
				if (item is SolutionFolder)
					item.Clean (monitor, configuration);
				else if (item is SolutionEntityItem) {
					SolutionEntityItem si = (SolutionEntityItem) item;
					SolutionConfigurationEntry ce = conf.GetEntryForItem (si);
					if (ce.Build)
						si.Clean (monitor, ce.ItemConfigurationSelector);
				} else {
					item.Clean (monitor, configuration);
				}
			}
		}
		
		protected override BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			ReadOnlyCollection<SolutionItem> allProjects;
				
			try {
				allProjects = GetAllBuildableEntries (configuration, true, true);
			} catch (CyclicDependencyException) {
				monitor.ReportError (GettextCatalog.GetString ("Cyclic dependencies are not supported."), null);
				return new BuildResult ("", 1, 1);
			}
			
			try {
				List<SolutionItem> toBuild = new List<SolutionItem> (allProjects.Where (p => p.NeedsBuilding (configuration)));
				
				monitor.BeginTask (GettextCatalog.GetString ("Building Solution {0}", Name), toBuild.Count);
				
				BuildResult cres = new BuildResult ();
				cres.BuildCount = 0;
				HashSet<SolutionItem> failedItems = new HashSet<SolutionItem> ();
				
				foreach (SolutionItem item in toBuild) {
					if (monitor.IsCancelRequested)
						break;

					if (!item.ContainsReferences (failedItems, configuration)) {
						BuildResult res = item.Build (monitor, configuration, false);
						if (res != null) {
							cres.Append (res);
							if (res.ErrorCount > 0)
								failedItems.Add (item);
						}
					} else
						failedItems.Add (item);
					monitor.Step (1);
				}
				return cres;
			} finally {
				monitor.EndTask ();
			}
		}

		protected internal override DateTime OnGetLastBuildTime (ConfigurationSelector configuration)
		{
			// Return the min value, since that the last time all items in the
			// folder were built
			DateTime tim = DateTime.MaxValue;
			foreach (SolutionItem it in Items) {
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
		
		public void RenameFileInProjects (string sourceFile, string targetFile)
		{
			if (Directory.Exists (targetFile)) {
				RenameDirectoryInAllProjects (sourceFile, targetFile);
			} else {
				RenameFileInAllProjects(sourceFile, targetFile);
			}
		}
		
		void RenameFileInAllProjects (string oldName, string newName)
		{
			foreach (Project projectEntry in GetAllProjects()) {
				foreach (ProjectFile fInfo in projectEntry.Files) {
					if (fInfo.Name == oldName)
						fInfo.Name = newName;
				}
			}
		}

		void RenameDirectoryInAllProjects (string oldName, string newName)
		{
			foreach (Project projectEntry in GetAllProjects()) {
				foreach (ProjectFile fInfo in projectEntry.Files) {
					if (fInfo.Name.StartsWith (oldName))
						fInfo.Name = newName + fInfo.Name.Substring(oldName.Length);
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
		
		internal void NotifyItemSaved (object sender, SolutionItemEventArgs e)
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
		
		protected virtual void OnItemAdded (SolutionItemChangeEventArgs e)
		{
			if (ItemAdded != null)
				ItemAdded (this, e);
		}
		
		void OnItemRemoved (SolutionItemChangeEventArgs e, bool removedFromSolution)
		{
			OnItemRemoved (e);
			NotifyItemRemovedFromFolder (this, e, removedFromSolution);
		}
		
		protected virtual void OnItemRemoved (SolutionItemChangeEventArgs e)
		{
			if (ItemRemoved != null)
				ItemRemoved (this, e);
		}
		
		protected virtual void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnFileRemovedFromProject (e);
			if (FileRemovedFromProject != null) {
				FileRemovedFromProject (this, e);
			}
		}

		protected virtual void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnFileChangedInProject (e);
			if (FileChangedInProject != null) {
				FileChangedInProject (this, e);
			}
		}
		
		protected virtual void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnFilePropertyChangedInProject (e);
			if (FilePropertyChangedInProject != null) {
				FilePropertyChangedInProject (this, e);
			}
		}
		
		protected virtual void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnFileAddedToProject (e);
			if (FileAddedToProject != null) {
				FileAddedToProject (this, e);
			}
		}
		
		protected virtual void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnFileRenamedInProject (e);
			if (FileRenamedInProject != null) {
				FileRenamedInProject (this, e);
			}
		}
		
		protected virtual void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnReferenceRemovedFromProject (e);
			if (ReferenceRemovedFromProject != null) {
				ReferenceRemovedFromProject (this, e);
			}
		}
		
		protected virtual void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnReferenceAddedToProject (e);
			if (ReferenceAddedToProject != null) {
				ReferenceAddedToProject (this, e);
			}
		}

		protected virtual void OnItemModified (SolutionItemModifiedEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnEntryModified (e);
			if (ItemModified != null)
				ItemModified (this, e);
		}
		
		protected virtual void OnItemSaved (SolutionItemEventArgs e)
		{
			if (ParentFolder == null && ParentSolution != null)
				ParentSolution.OnEntrySaved (e);
			if (ItemSaved != null)
				ItemSaved (this, e);
		}
		
		protected virtual void OnSolutionItemFileAdded (SolutionItemFileEventArgs args)
		{
			if (SolutionItemFileAdded != null)
				SolutionItemFileAdded (this, args);
		}
		
		protected virtual void OnSolutionItemFileRemoved (SolutionItemFileEventArgs args)
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
		public event SolutionItemEventHandler ItemSaved;
		public event EventHandler<SolutionItemFileEventArgs> SolutionItemFileAdded;
		public event EventHandler<SolutionItemFileEventArgs> SolutionItemFileRemoved;
//		public event EventHandler<SolutionItemEventArgs> ItemReloadRequired;
	}
	
	class DummySolutionFolderHandler: ISolutionItemHandler
	{
		SolutionFolder folder;
		
		public DummySolutionFolderHandler (SolutionFolder folder)
		{
			this.folder = folder;
		}
		
		public string ItemId {
			get { return folder.Name; }
		}
		
		public BuildResult RunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}
		
		public void Save (IProgressMonitor monitor)
		{
			throw new NotImplementedException ();
		}
		
		public bool SyncFileName {
			get { return false; }
		}
		
		public void Dispose ()
		{
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
}
