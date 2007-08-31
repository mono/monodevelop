// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Threading;

using Mono.Unix.Native;
using FileMode = Mono.Unix.Native.FilePermissions;

using MonoDevelop.Core;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Projects
{
	[DataInclude (typeof(CombineConfiguration))]
	public class Combine : CombineEntry
	{
		[ItemProperty ("description", DefaultValue = "")]
		string description = null;
		
		[ItemProperty ("StartMode/startupentry")]
		string startEntryName;
		CombineEntry startupEntry;
		
		[ItemProperty ("StartMode/single")]
		bool   singleStartup = true;
		
		[ExpandedCollection]
		[ItemProperty ("StartMode/Execute", ValueType = typeof(CombineExecuteDefinition))]
		ArrayList combineExecuteDefinitions = new ArrayList();
		
		[ProjectPathItemProperty ("outputpath")]
		string outputdir     = null;
		
		bool deserializing;
		
		ProjectFileEventHandler fileAddedToProjectHandler;
		ProjectFileEventHandler fileRemovedFromProjectHandler;
		ProjectFileEventHandler fileChangedInProjectHandler;
		ProjectFileEventHandler filePropertyChangedInProjectHandler;
		ProjectFileRenamedEventHandler fileRenamedInProjectHandler;
		CombineEntryEventHandler entryModifiedHandler;
		CombineEntryEventHandler entrySavedHandler;

		ProjectReferenceEventHandler referenceAddedToProjectHandler;
		ProjectReferenceEventHandler referenceRemovedFromProjectHandler;
		
		CombineEntryCollection entries;
		
		[Browsable(false)]
		public CombineEntryCollection Entries {
			get {
				if (entries == null) entries = new CombineEntryCollection (this);
				return entries;
			}
		}
		
		public CombineEntry StartupEntry {
			get {
				if (startupEntry == null && startEntryName != null)
					startupEntry = Entries [startEntryName];
				return startupEntry;
			}
			set {
				startupEntry = value;
				if (value == null)
					startEntryName = null;
				NotifyModified ();
				OnStartupPropertyChanged(null);
			}
		}
		
		[Browsable(false)]
		public bool SingleStartupProject {
			get {
				return singleStartup;
			}
			set {
				singleStartup = value;
				NotifyModified ();
				OnStartupPropertyChanged(null);
			}
		}
		
		public ArrayList CombineExecuteDefinitions {
			get {
				return combineExecuteDefinitions;
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

		
		public string Description {
			get {
				return description;
			}
			set {
				description = value;
				NotifyModified ();
			}
		}
		
		protected internal override bool OnGetNeedsBuilding ()
		{
			foreach (CombineEntry entry in Entries)
				if (entry.NeedsBuilding) return true;
			return false;
		}
		
		protected internal override void OnSetNeedsBuilding (bool value)
		{
			// Ignore
		}
		
		public Combine()
		{
			fileAddedToProjectHandler = new ProjectFileEventHandler (NotifyFileAddedToProject);
			fileChangedInProjectHandler = new ProjectFileEventHandler (NotifyFileChangedInProject);
			filePropertyChangedInProjectHandler = new ProjectFileEventHandler (NotifyFilePropertyChangedInProject);
			fileRemovedFromProjectHandler = new ProjectFileEventHandler (NotifyFileRemovedFromProject);
			fileRenamedInProjectHandler = new ProjectFileRenamedEventHandler (NotifyFileRenamedInProject);
			referenceAddedToProjectHandler = new ProjectReferenceEventHandler (NotifyReferenceAddedToProject);
			referenceRemovedFromProjectHandler = new ProjectReferenceEventHandler (NotifyReferenceRemovedFromProject);
			entryModifiedHandler = new CombineEntryEventHandler (NotifyEntryModified);
			entrySavedHandler = new CombineEntryEventHandler (NotifyEntrySaved);
		}
		
		public override void Dispose()
		{
			base.Dispose ();
			foreach (CombineEntry e in Entries)
				e.Dispose ();
		}
		
		public override IConfiguration CreateConfiguration (string name)
		{
			CombineConfiguration cc = new CombineConfiguration ();
			cc.Name = name;
			return cc;
		}
		
		protected override void OnActiveConfigurationChanged (ConfigurationEventArgs args)
		{
			UpdateActiveConfigurationTree ();
			base.OnActiveConfigurationChanged  (args);
		}
		
		internal void UpdateActiveConfigurationTree ()
		{
			if (ActiveConfiguration != null && !deserializing) {
				foreach (CombineConfigurationEntry cce in ((CombineConfiguration)ActiveConfiguration).Entries) {
					if (cce.Entry != null) {
						IConfiguration conf = cce.Entry.GetConfiguration (cce.ConfigurationName);
						cce.Entry.ActiveConfiguration = conf;
					}
				}
			}
		}
		
		public CombineEntry ReloadEntry (IProgressMonitor monitor, CombineEntry entry)
		{
			if (Entries.IndexOf (entry) == -1)
				throw new InvalidOperationException ("Combine entry '" + entry.Name + "' does not belong to combine '" + Name + "'");

			// Load the new entry
			
			CombineEntry newEntry;
			try {
				newEntry = Services.ProjectService.ReadCombineEntry (entry.FileName, monitor);
			} catch (Exception ex) {
				UnknownCombineEntry e = new UnknownCombineEntry ();
				e.LoadError = ex.Message;
				e.FileName = entry.FileName;
				newEntry = e;
			}
			
			// Replace the old entry by the new entry in the execute definitions
			
			foreach (CombineExecuteDefinition exDef in CombineExecuteDefinitions) {
				if (exDef.Entry == entry) {
					exDef.Entry = newEntry;
					break;
				}
			}

			// Replace in the configurations as well
			
			foreach (CombineConfiguration dentry in Configurations) {
				foreach (CombineConfigurationEntry cce in dentry.Entries) {
					if (cce.Entry == entry)
						cce.Entry = newEntry;
				}
			}

			if (StartupEntry == entry)
				StartupEntry = newEntry;
			
			// Replace in the file list
			Entries.Replace (entry, newEntry);
			
			DisconnectChildEntryEvents (entry);
			ConnectChildEntryEvents (newEntry);

			NotifyModified ();
			OnEntryRemoved (new CombineEntryChangeEventArgs (entry, true));
			OnEntryAdded (new CombineEntryChangeEventArgs (newEntry, true));
			
			entry.Dispose ();
			
			return newEntry;
		}
		
		internal void NotifyEntryAdded (CombineEntry entry)
		{
			if (StartupEntry == null)
				StartupEntry = entry;
			
			// If the combine has no configurations, copy them from the
			// new child entry
			
			if (Configurations.Count == 0) {
				foreach (IConfiguration pconf in entry.Configurations) {
					if (pconf.Name == null)
						continue;
					CombineConfiguration cconf = new CombineConfiguration (pconf.Name);
					Configurations.Add (cconf);
					if (ActiveConfiguration == null)
						ActiveConfiguration = cconf;
				}
			}
		
			foreach (CombineConfiguration conf in Configurations) {
				// Register the new entry in every combine configuration
				CombineConfigurationEntry centry = conf.AddEntry (entry);
				
				// Look for a valid configuration in the child entry to be bound
				// to the combine configuration. Look first for a configuration
				// with the same name. Use the active configuration if not found.
				if (entry.GetConfiguration (conf.Name) != null)
					centry.ConfigurationName = conf.Name;
				else if (entry.ActiveConfiguration != null)
					centry.ConfigurationName = entry.ActiveConfiguration.Name;
			}
			
			// If the child is a combine and has no configurations, copy them
			// from the parent combine
			
			if (entry.Configurations.Count == 0 && entry is Combine)			
				foreach (CombineConfiguration conf in Configurations)					
					entry.Configurations.Add(new CombineConfiguration(conf.Name));			
			
			combineExecuteDefinitions.Add (new CombineExecuteDefinition (entry, EntryExecuteType.None));
			
			ConnectChildEntryEvents (entry);

			NotifyModified ();
			OnEntryAdded (new CombineEntryChangeEventArgs (entry, false));
		}
		
		void ConnectChildEntryEvents (CombineEntry entry)
		{
			if (entry is Project)
			{
				Project project = entry as Project;
				project.FileRemovedFromProject += fileRemovedFromProjectHandler;
				project.FileAddedToProject += fileAddedToProjectHandler;
				project.FileChangedInProject += fileChangedInProjectHandler;
				project.FilePropertyChangedInProject += filePropertyChangedInProjectHandler;
				project.FileRenamedInProject += fileRenamedInProjectHandler;
				project.ReferenceRemovedFromProject += referenceRemovedFromProjectHandler;
				project.ReferenceAddedToProject += referenceAddedToProjectHandler;
			}
			else if (entry is Combine)
			{
				Combine combine = entry as Combine;
				combine.FileRemovedFromProject += fileRemovedFromProjectHandler;
				combine.FileAddedToProject += fileAddedToProjectHandler;
				combine.FileChangedInProject += fileChangedInProjectHandler;
				combine.FilePropertyChangedInProject += filePropertyChangedInProjectHandler;
				combine.FileRenamedInProject += fileRenamedInProjectHandler;
				combine.ReferenceRemovedFromProject += referenceRemovedFromProjectHandler;
				combine.ReferenceAddedToProject += referenceAddedToProjectHandler;
			}
			entry.Modified += entryModifiedHandler;
			entry.Saved += entrySavedHandler;
		}
		
		public override DataCollection Serialize (ITypeSerializer handler)
		{
			if (StartupEntry != null)
				startEntryName = StartupEntry.Name;

			return base.Serialize (handler);
		}
		
		public override void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			try {
				deserializing = true;
				startupEntry = null;
				
				// Clean the configuration list, since entries added while deserializing
				// could have generated default configurations.
				Configurations.Clear ();
				
				base.Deserialize (handler, data);
	
				foreach (CombineExecuteDefinition ced in combineExecuteDefinitions)
					ced.SetCombine (this);
				
				foreach (CombineConfiguration conf in Configurations)
					conf.SetCombine (this);
			} finally {
				deserializing = false;
				UpdateActiveConfigurationTree ();
			}
		}

		protected internal override void OnSave (IProgressMonitor monitor)
		{
			base.OnSave (monitor);
			foreach (CombineEntry entry in Entries)
			{
				if (entry is Combine || entry is Project)
					entry.Save (monitor);
			}
		}

		public CombineEntry AddEntry (string filename, IProgressMonitor monitor)
		{
			if (monitor == null) monitor = new NullProgressMonitor ();
			CombineEntry entry = Services.ProjectService.ReadCombineEntry (filename, monitor);
			Entries.Add (entry);
			return entry;
		}

		internal void NotifyEntryRemoved (CombineEntry entry)
		{
			DisconnectChildEntryEvents (entry);

			// remove execute definition
			CombineExecuteDefinition removeExDef = null;
			foreach (CombineExecuteDefinition exDef in CombineExecuteDefinitions) {
				if (exDef.Entry == entry) {
					removeExDef = exDef;
					break;
				}
			}
			CombineExecuteDefinitions.Remove (removeExDef);

			// remove configuration
			foreach (CombineConfiguration dentry in Configurations)
				dentry.RemoveEntry (entry);

			if (StartupEntry == entry) {
				if (Entries.Count > 0)
					StartupEntry = Entries [0];
				else
					StartupEntry = null;
			}
			
			NotifyModified ();
			OnEntryRemoved (new CombineEntryChangeEventArgs (entry, false));
		}
		
		void DisconnectChildEntryEvents (CombineEntry entry)
		{
			Project pce = entry as Project;
			if (pce != null) {
				pce.FileRemovedFromProject -= fileRemovedFromProjectHandler;
				pce.FileAddedToProject -= fileAddedToProjectHandler;
				pce.FileChangedInProject -= fileChangedInProjectHandler;
				pce.FilePropertyChangedInProject -= filePropertyChangedInProjectHandler;
				pce.FileRenamedInProject -= fileRenamedInProjectHandler;
				pce.ReferenceRemovedFromProject -= referenceRemovedFromProjectHandler;
				pce.ReferenceAddedToProject -= referenceAddedToProjectHandler;
			}
			else {
				Combine cce = entry as Combine;
				if (cce != null) {
					cce.FileRemovedFromProject -= fileRemovedFromProjectHandler;
					cce.FileAddedToProject -= fileAddedToProjectHandler;
					cce.FileChangedInProject -= fileChangedInProjectHandler;
					cce.FilePropertyChangedInProject -= filePropertyChangedInProjectHandler;
					cce.FileRenamedInProject -= fileRenamedInProjectHandler;
					cce.ReferenceRemovedFromProject -= referenceRemovedFromProjectHandler;
					cce.ReferenceAddedToProject -= referenceAddedToProjectHandler;
				}
			}
			entry.Modified -= entryModifiedHandler;
			entry.Saved -= entrySavedHandler;
		}
		
		private void RemoveReferencesToProject(Project projectToRemove)
		{			
			if (projectToRemove == null) {
				return;
			}

			if (this.ParentCombine != null)
			{
				this.ParentCombine.RemoveReferencesToProject(projectToRemove);
				return;
			}

			foreach (Project project in this.GetAllProjects()) {

				if (project == projectToRemove) {
					continue;
				}
				
				ArrayList toBeDeleted = new ArrayList();
				
				foreach (ProjectReference refInfo in project.ProjectReferences) {
					switch (refInfo.ReferenceType) {
					case ReferenceType.Project:
						if (refInfo.Reference == projectToRemove.Name) {
							toBeDeleted.Add(refInfo);
						}
						break;
					case ReferenceType.Assembly:
					case ReferenceType.Gac:
						break;
					}
				}
				
				foreach (ProjectReference refInfo in toBeDeleted) {
					project.ProjectReferences.Remove(refInfo);
				}				
			}
		}
			
		public void RemoveEntry (CombineEntry entry)
		{
			RemoveReferencesToProject (entry as Project);
			Entries.Remove (entry);
		}
		
		protected internal override void OnExecute (IProgressMonitor monitor, ExecutionContext context)
		{
			if (singleStartup) {
				if (StartupEntry != null)
					StartupEntry.Execute (monitor, context);
			} else {
				ArrayList list = new ArrayList ();
				monitor.BeginTask ("Executing projects", 1);
				
				SynchronizedProgressMonitor syncMonitor = new SynchronizedProgressMonitor (monitor);
				
				foreach (CombineExecuteDefinition ced in combineExecuteDefinitions) {
					if (ced.Type != EntryExecuteType.Execute) continue;
					
					AggregatedProgressMonitor mon = new AggregatedProgressMonitor ();
					mon.AddSlaveMonitor (syncMonitor, MonitorAction.ReportError | MonitorAction.ReportWarning | MonitorAction.SlaveCancel);
					
					EntryStartData sd = new EntryStartData ();
					sd.Monitor = mon;
					sd.Context = context;
					sd.Entry = ced.Entry;
					
					Thread t = new Thread (new ThreadStart (sd.ExecuteEntryAsync));
					t.IsBackground = true;
					t.Start ();
					list.Add (sd.Monitor.AsyncOperation);
				}
				foreach (IAsyncOperation op in list)
					op.WaitForCompleted ();
				
				monitor.EndTask ();
			}
		}
		
		class EntryStartData {
			public IProgressMonitor Monitor;
			public ExecutionContext Context;
			public CombineEntry Entry;
			
			public void ExecuteEntryAsync ()
			{
				using (Monitor) {
					Entry.Execute (Monitor, Context);
				}
			}
		}
		
		public string[] GetAllConfigurations ()
		{
			ArrayList names = new ArrayList ();
			GetAllConfigurations (this, names);
			return (string[]) names.ToArray (typeof(string));
		}
		
		void GetAllConfigurations (CombineEntry entry, ArrayList names)
		{
			foreach (IConfiguration conf in Configurations)
				if (!names.Contains (conf.Name))
					names.Add (conf.Name);

			if (entry is Combine) {
				foreach (CombineEntry ce in ((Combine)entry).Entries)
					GetAllConfigurations (ce, names);
			}
		}
			
		public void SetAllConfigurations (string configName)
		{
			IConfiguration conf = GetConfiguration (configName);
			if (conf != null) ActiveConfiguration = conf;
				
			foreach (CombineEntry ce in Entries) {
				if (ce is Combine)
					((Combine)ce).SetAllConfigurations (configName);
				else {
					conf = ce.GetConfiguration (configName);
					if (conf != null) ce.ActiveConfiguration = conf;
				}
			}
		}
		
		/// <remarks>
		/// Returns an ArrayList containing all ProjectEntries in this combine and 
		/// undercombines
		/// </remarks>
		public CombineEntryCollection GetAllProjects ()
		{
			return GetAllProjects (false);
		}
		
		/// <remarks>
		/// Returns a collection containing all entries in this combine and 
		/// undercombines
		/// </remarks>
		public CombineEntryCollection GetAllEntries ()
		{
			return GetAllEntries (null);
		}
		
		/// <remarks>
		/// Returns a collection containing all entries of the given type in this combine and 
		/// undercombines
		/// </remarks>
		public CombineEntryCollection GetAllEntries (Type type)
		{
			CombineEntryCollection list = new CombineEntryCollection();
			GetAllEntries (list, type, this);
			return list;
		}
		
		// When topologicalSort=true, the projects are returned in the order
		// they should be compiled, acording to their references.
		public CombineEntryCollection GetAllProjects (bool topologicalSort)
		{
			CombineEntryCollection list = new CombineEntryCollection();
			GetAllEntries (list, typeof(Project), this);
			if (topologicalSort)
				return TopologicalSort (list);
			else
				return list;
		}
		
		void GetAllEntries (CombineEntryCollection list, Type type, CombineEntry entry)
		{
			if (type == null || type.IsInstanceOfType (entry)) {
				list.Add (entry);
			}
		
			if (entry is Combine) {
				foreach (CombineEntry ce in ((Combine)entry).Entries)
					GetAllEntries (list, type, ce);
			}
		}
		
		public CombineEntryCollection GetAllBuildableEntries (string configuration, bool topologicalSort)
		{
			CombineEntryCollection list = GetAllBuildableEntries (configuration);
			if (topologicalSort)
				return TopologicalSort (list);
			else
				return list;
		}
		
		public CombineEntryCollection GetAllBuildableEntries (string configuration)
		{
			CombineEntryCollection list = new CombineEntryCollection();
			GetAllBuildableEntries (list, configuration);
			return list;
		}
		
		void GetAllBuildableEntries (CombineEntryCollection list, string configuration)
		{
			CombineConfiguration conf = (CombineConfiguration) GetConfiguration (configuration);
			if (conf == null)
				return;

			foreach (CombineConfigurationEntry entry in conf.Entries) {
				if (!entry.Build)
					continue;
				if (entry.Entry is Combine)
					((Combine)entry.Entry).GetAllBuildableEntries (list, configuration);
				else if (entry.Entry is Project)
					list.Add (entry.Entry);
			}
		}
		
		public Project GetProjectContainingFile (string fileName) 
		{
			CombineEntryCollection projects = GetAllProjects ();
			foreach (Project projectEntry in projects) {
				if (projectEntry.IsFileInProject(fileName)) {
					return projectEntry;
				}
			}
			return null;
		}
		
		public Project FindProject (string projectName)
		{
			CombineEntryCollection allProjects = GetAllProjects();
			foreach (Project project in allProjects) {
				if (project.Name == projectName)
					return project;
			}
			return null;
		}
		
		internal static CombineEntryCollection TopologicalSort (CombineEntryCollection allProjects)
		{
			CombineEntryCollection sortedEntries = new CombineEntryCollection ();
			bool[]    inserted      = new bool[allProjects.Count];
			bool[]    triedToInsert = new bool[allProjects.Count];
			for (int i = 0; i < allProjects.Count; ++i) {
				inserted[i] = triedToInsert[i] = false;
			}
			for (int i = 0; i < allProjects.Count; ++i) {
				if (!inserted[i]) {
					Insert(i, allProjects, sortedEntries, inserted, triedToInsert);
				}
			}
			return sortedEntries;
		}
		
		static void Insert(int index, CombineEntryCollection allProjects, CombineEntryCollection sortedEntries, bool[] inserted, bool[] triedToInsert)
		{
			if (triedToInsert[index]) {
				throw new CyclicBuildOrderException();
			}
			triedToInsert[index] = true;
			foreach (ProjectReference reference in ((Project)allProjects[index]).ProjectReferences) {
				if (reference.ReferenceType == ReferenceType.Project) {
					int j = 0;
					for (; j < allProjects.Count; ++j) {
						if (reference.Reference == ((Project)allProjects[j]).Name) {
							if (!inserted[j]) {
								Insert(j, allProjects, sortedEntries, inserted, triedToInsert);
							}
							break;
						}
					}
				}
			}
			sortedEntries.Add(allProjects[index]);
			inserted[index] = true;
		}
		
		protected internal override void OnClean (IProgressMonitor monitor)
		{
			CombineConfiguration config = ActiveConfiguration as CombineConfiguration;
			if (config == null)
				return;

			foreach (CombineConfigurationEntry cce in config.Entries)
				cce.Entry.Clean (monitor);
		}
		
		protected internal override ICompilerResult OnBuild (IProgressMonitor monitor)
		{
			if (ActiveConfiguration == null) {
				monitor.ReportError (GettextCatalog.GetString ("The solution does not have an active configuration."), null);
				return new DefaultCompilerResult (new CompilerResults (null), "", 0, 1);
			}
			
			CombineEntryCollection allProjects = GetAllBuildableEntries (ActiveConfiguration.Name);
			monitor.BeginTask (GettextCatalog.GetString ("Building Solution {0}", Name), allProjects.Count);
			try {
				CompilerResults cres = new CompilerResults (null);
				
				try {
					allProjects = TopologicalSort (allProjects);
				} catch (CyclicBuildOrderException) {
					monitor.ReportError (GettextCatalog.GetString ("Cyclic dependencies can not be built with this version.\nBut we are working on it."), null);
					return new DefaultCompilerResult (cres, "", 1, 1);
				}
				
				int builds = 0;
				int failedBuilds = 0;
				
				foreach (Project entry in allProjects) {
					if (monitor.IsCancelRequested)
						break;

					ICompilerResult res = entry.Build (monitor, false);
					builds++;
					if (res != null)
						cres.Errors.AddRange (res.CompilerResults.Errors);
					monitor.Step (1);
					if (res != null && res.ErrorCount > 0) {
						failedBuilds++;
						break;
					}
				}
				return new DefaultCompilerResult (cres, "", builds, failedBuilds);
			} finally {
				monitor.EndTask ();
			}
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
			CombineEntryCollection projects = GetAllProjects();
			
			restart:
			foreach (Project projectEntry in projects) {
				foreach (ProjectFile fInfo in projectEntry.ProjectFiles) {
					if (fInfo.Name.StartsWith(dirName)) {
						projectEntry.ProjectFiles.Remove(fInfo);
						goto restart;
					}
				}
			}
		}
		
		void RemoveFileFromAllProjects (string fileName)
		{
			CombineEntryCollection projects = GetAllProjects();
			
			restart:
			foreach (Project projectEntry in projects) {
				foreach (ProjectReference rInfo in projectEntry.ProjectReferences) {
					if (rInfo.ReferenceType == ReferenceType.Assembly && rInfo.Reference == fileName) {
						projectEntry.ProjectReferences.Remove(rInfo);
						goto restart;
					}
				}
				foreach (ProjectFile fInfo in projectEntry.ProjectFiles) {
					if (fInfo.Name == fileName) {
						projectEntry.ProjectFiles.Remove(fInfo);
						goto restart;
					}
				}
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
			CombineEntryCollection projects = GetAllProjects();
			
			foreach (Project projectEntry in projects) {
				foreach (ProjectFile fInfo in projectEntry.ProjectFiles) {
					if (fInfo.Name == oldName) {
						fInfo.Name = newName;
					}
				}
			}
		}

		void RenameDirectoryInAllProjects (string oldName, string newName)
		{
			CombineEntryCollection projects = GetAllProjects();
			
			foreach (Project projectEntry in projects) {
				foreach (ProjectFile fInfo in projectEntry.ProjectFiles) {
					if (fInfo.Name.StartsWith(oldName)) {
						fInfo.Name = newName + fInfo.Name.Substring(oldName.Length);
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
		
		internal void NotifyEntryModified (object sender, CombineEntryEventArgs e)
		{
			OnEntryModified (e);
		}
		
		internal void NotifyEntrySaved (object sender, CombineEntryEventArgs e)
		{
			OnEntrySaved (e);
		}
		
		internal void NotifyEntryAddedToCombine (object sender, CombineEntryChangeEventArgs e)
		{
			if (EntryAddedToCombine != null)
				EntryAddedToCombine (sender, e);
			if (RootCombine != null && RootCombine != this)
				RootCombine.NotifyEntryAddedToCombine (sender, e);
		}
		
		internal void NotifyEntryRemovedFromCombine (object sender, CombineEntryChangeEventArgs e)
		{
			if (EntryRemovedFromCombine != null)
				EntryRemovedFromCombine (sender, e);
			if (RootCombine != null && RootCombine != this)
				RootCombine.NotifyEntryRemovedFromCombine (sender, e);
		}
		
		protected virtual void OnStartupPropertyChanged(EventArgs e)
		{
			if (StartupPropertyChanged != null) {
				StartupPropertyChanged(this, e);
			}
		}
			
		protected virtual void OnEntryAdded (CombineEntryChangeEventArgs e)
		{
			if (EntryAdded != null) {
				EntryAdded (this, e);
			}
			NotifyEntryAddedToCombine (this, e);
		}
		
		protected virtual void OnEntryRemoved (CombineEntryChangeEventArgs e)
		{
			if (EntryRemoved != null) {
				EntryRemoved (this, e);
			}
			NotifyEntryRemovedFromCombine (this, e);
		}
		
		protected virtual void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			if (FileRemovedFromProject != null) {
				FileRemovedFromProject (this, e);
			}
		}

		protected virtual void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			if (FileChangedInProject != null) {
				FileChangedInProject (this, e);
			}
		}
		
		protected virtual void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			if (FilePropertyChangedInProject != null) {
				FilePropertyChangedInProject (this, e);
			}
		}
		
		protected virtual void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			if (FileAddedToProject != null) {
				FileAddedToProject (this, e);
			}
		}
		
		protected virtual void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			if (FileRenamedInProject != null) {
				FileRenamedInProject (this, e);
			}
		}
		
		protected virtual void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			if (ReferenceRemovedFromProject != null) {
				ReferenceRemovedFromProject (this, e);
			}
		}
		
		protected virtual void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			if (ReferenceAddedToProject != null) {
				ReferenceAddedToProject (this, e);
			}
		}

		protected virtual void OnEntryModified (CombineEntryEventArgs e)
		{
			if (EntryModified != null)
				EntryModified (this, e);
		}
		
		protected virtual void OnEntrySaved (CombineEntryEventArgs e)
		{
			if (EntrySaved != null)
				EntrySaved (this, e);
		}
		
		public event EventHandler StartupPropertyChanged;
		public event CombineEntryChangeEventHandler EntryAdded;
		public event CombineEntryChangeEventHandler EntryRemoved;
		public event CombineEntryChangeEventHandler EntryAddedToCombine;     // Fires for child combines
		public event CombineEntryChangeEventHandler EntryRemovedFromCombine; // Fires for child combines
		public event ProjectFileEventHandler FileAddedToProject;
		public event ProjectFileEventHandler FileRemovedFromProject;
		public event ProjectFileEventHandler FileChangedInProject;
		public event ProjectFileEventHandler FilePropertyChangedInProject;
		public event ProjectFileRenamedEventHandler FileRenamedInProject;
		public event ProjectReferenceEventHandler ReferenceAddedToProject;
		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;
		public event CombineEntryEventHandler EntryModified;
		public event CombineEntryEventHandler EntrySaved;
	}
}
