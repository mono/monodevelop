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

using Mono.Unix;
using FileMode = Mono.Unix.FilePermissions;

using MonoDevelop.Core.Services;

using MonoDevelop.Services;
using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Properties;
using MonoDevelop.Gui.Components;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Internal.Serialization;

namespace MonoDevelop.Internal.Project
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
		
		bool   eventsAllowed = true;
		
		[ProjectPathItemProperty ("outputpath")]
		string outputdir     = null;
		
		bool deserializing;
		
		ProjectFileEventHandler fileAddedToProjectHandler;
		ProjectFileEventHandler fileRemovedFromProjectHandler;
		ProjectFileEventHandler fileChangedInProjectHandler;
		ProjectFileRenamedEventHandler fileRenamedInProjectHandler;

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
			}
		}
		
		string DefaultOutputDirectory {
			get {
				return (BaseDirectory != null) ? Path.Combine (BaseDirectory, Path.Combine ("build", "bin")) : null;
			}
		}

		
		[LocalizedProperty("${res:MonoDevelop.Internal.Project.Combine.Description}",
		                   Description ="${res:MonoDevelop.Internal.Project.Combine.Description.Description}")]
		public string Description {
			get {
				return description;
			}
			set {
				description = value;
			}
		}
		
		[LocalizedProperty("${res:MonoDevelop.Internal.Project.Combine.NeedsBuilding}",
		                   Description ="${res:MonoDevelop.Internal.Project.Combine.NeedsBuilding.Description}")]
		public override bool NeedsBuilding {
			get {
				foreach (CombineEntry entry in Entries)
					if (entry.NeedsBuilding) return true;
				return false;
			}
			set {
				// Ignore
			}
		}
		
		public Combine()
		{
			fileAddedToProjectHandler = new ProjectFileEventHandler (NotifyFileAddedToProject);
			fileChangedInProjectHandler = new ProjectFileEventHandler (NotifyFileChangedInProject);
			fileRemovedFromProjectHandler = new ProjectFileEventHandler (NotifyFileRemovedFromProject);
			fileRenamedInProjectHandler = new ProjectFileRenamedEventHandler (NotifyFileRenamedInProject);
			referenceAddedToProjectHandler = new ProjectReferenceEventHandler (NotifyReferenceAddedToProject);
			referenceRemovedFromProjectHandler = new ProjectReferenceEventHandler (NotifyReferenceRemovedFromProject);
		}
		
		public override IConfiguration CreateConfiguration (string name)
		{
			CombineConfiguration cc = new CombineConfiguration ();
			cc.Name = name;
			return cc;
		}
		
		protected override void OnActiveConfigurationChanged (ConfigurationEventArgs args)
		{
			if (ActiveConfiguration != null && !deserializing) {
				foreach (CombineConfigurationEntry cce in ((CombineConfiguration)ActiveConfiguration).Entries) {
					IConfiguration conf = cce.Entry.GetConfiguration (cce.ConfigurationName);
					cce.Entry.ActiveConfiguration = conf;
				}
			}
			base.OnActiveConfigurationChanged  (args);
		}
		
		internal void NotifyEntryAdded (CombineEntry entry)
		{
			if (StartupEntry == null)
				StartupEntry = entry;
			
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
			
			foreach (CombineConfiguration conf in Configurations)
				conf.AddEntry (entry);

			combineExecuteDefinitions.Add (new CombineExecuteDefinition (entry, EntryExecuteType.None));
			
			if (eventsAllowed)
				OnEntryAdded (new CombineEntryEventArgs (entry));

			if (entry is Project)
			{
				Project project = entry as Project;
				project.FileRemovedFromProject += fileRemovedFromProjectHandler;
				project.FileAddedToProject += fileAddedToProjectHandler;
				project.FileChangedInProject += fileChangedInProjectHandler;
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
				combine.FileRenamedInProject += fileRenamedInProjectHandler;
				combine.ReferenceRemovedFromProject += referenceRemovedFromProjectHandler;
				combine.ReferenceAddedToProject += referenceAddedToProjectHandler;
			}
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
			}
		}

		public override void Save (IProgressMonitor monitor)
		{
			base.Save (monitor);
			foreach (CombineEntry entry in Entries)
			{
				if (entry is Combine || entry is Project)
					entry.Save (monitor);
			}
			//GenerateMakefiles ();
		}

		public CombineEntry AddEntry (string filename, IProgressMonitor monitor)
		{
			if (monitor == null) monitor = new NullProgressMonitor ();
			CombineEntry entry = Runtime.ProjectService.ReadFile (filename, monitor);
			Entries.Add (entry);
			return entry;
		}

		internal void NotifyEntryRemoved (CombineEntry entry)
		{
			Project pce = entry as Project;
			if (pce != null) {
				pce.FileRemovedFromProject -= fileRemovedFromProjectHandler;
				pce.FileAddedToProject -= fileAddedToProjectHandler;
				pce.FileChangedInProject -= fileChangedInProjectHandler;
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
					cce.FileRenamedInProject -= fileRenamedInProjectHandler;
					cce.ReferenceRemovedFromProject -= referenceRemovedFromProjectHandler;
					cce.ReferenceAddedToProject -= referenceAddedToProjectHandler;
				}
			}

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

			OnEntryRemoved (new CombineEntryEventArgs (entry));
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
		
		public override void Execute (IProgressMonitor monitor, ExecutionContext context)
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
					
					Runtime.DispatchService.ThreadDispatch (new StatefulMessageHandler (ExecuteEntryAsync), sd);
					list.Add (sd.Monitor.AsyncOperation);
				}
				foreach (IAsyncOperation op in list)
					op.WaitForCompleted ();
				
				monitor.EndTask ();
			}
		}
		
		void ExecuteEntryAsync (object ob)
		{
			EntryStartData sd = (EntryStartData) ob;
			using (sd.Monitor) {
				sd.Entry.Execute (sd.Monitor, sd.Context);
			}
		}
		
		class EntryStartData {
			public IProgressMonitor Monitor;
			public ExecutionContext Context;
			public CombineEntry Entry;
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
			CombineEntryCollection list = new CombineEntryCollection();
			GetAllProjects (list);
			return list;
		}
		
		void GetAllProjects (CombineEntryCollection list)
		{
			foreach (CombineEntry entry in Entries) {
				if (entry is Project) {
					list.Add (entry);
				} else if (entry is Combine) {
					((Combine)entry).GetAllProjects (list);
				}
			}
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
		
		public Project GetProjectEntryContaining (string fileName) 
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
		
		public override void Clean ()
		{
			foreach (CombineConfigurationEntry cce in ((CombineConfiguration)ActiveConfiguration).Entries)
				cce.Entry.Clean ();
		}
		
		public override ICompilerResult Build (IProgressMonitor monitor)
		{
			CombineEntryCollection allProjects = GetAllBuildableEntries (ActiveConfiguration.Name);
			monitor.BeginTask (string.Format (GettextCatalog.GetString ("Building Solution {0}"), Name), allProjects.Count);
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
					cres.Errors.AddRange (res.CompilerResults.Errors);
					monitor.Step (1);
					if (res.ErrorCount > 0) {
						failedBuilds++;
						break;
					}
				}
				return new DefaultCompilerResult (cres, "", builds, failedBuilds);
			} finally {
				monitor.EndTask ();
			}
		}

		public void GenerateMakefiles ()
		{
			GenerateMakefiles (null);
		}

		public override void GenerateMakefiles (Combine parentCombine)
		{
			CombineEntryCollection allProjects = TopologicalSort (GetAllProjects ());
			ArrayList projects = new ArrayList ();
			foreach (CombineEntry entry in allProjects) {
				if (entry is Project) {
					entry.GenerateMakefiles (this);
					projects.Add ((Project)entry);
				}
				else
					Runtime.LoggingService.Info ("Dont know how to generate makefiles for " + entry);
			}
			
			string rel_outputdir = Runtime.FileUtilityService.AbsoluteToRelativePath (BaseDirectory, outputdir);
			
			StreamWriter buildstream = new StreamWriter (Path.Combine (BaseDirectory, "make.sh"));
			buildstream.WriteLine ("#!/bin/sh");
			buildstream.WriteLine ("# This file is autogenerated by MonoDevelop");
			buildstream.WriteLine ("# Do not edit it.");
			buildstream.WriteLine ();
			buildstream.WriteLine ("make -f Makefile.solution.{0} \"$@\"", Name.Replace (" ",""));
			buildstream.Flush ();
			buildstream.Close ();
			
			Syscall.chmod (Path.Combine (BaseDirectory, "make.sh"), FileMode.S_IRUSR | FileMode.S_IWUSR | FileMode.S_IXUSR | FileMode.S_IRGRP | FileMode.S_IWGRP | FileMode.S_IROTH);

			StreamWriter stream = new StreamWriter (Path.Combine (BaseDirectory, "Makefile.solution." + Name.Replace (" ", "")));
			stream.WriteLine ("# This file is autogenerated by MonoDevelop");
			stream.WriteLine ("# Do not edit it.");
			stream.WriteLine ();

			stream.WriteLine ("RUNTIME := mono");
			stream.WriteLine ();
			stream.WriteLine ("OUTPUTDIR := {0}", rel_outputdir);
			stream.WriteLine ();
			stream.Write ("all: depcheck __init ");
			foreach (Project proj in projects) {
				stream.Write ("Makefile.{0}.all ", proj.Name.Replace (" ",""));
			}
			stream.WriteLine ();
			stream.WriteLine ();

			stream.WriteLine ("__init:");
			stream.WriteLine ("\tmkdir -p $(OUTPUTDIR)");
			stream.WriteLine ();

			stream.Write ("clean: ");
			foreach (Project proj in projects) {
				stream.Write ("Makefile.{0}.clean ", proj.Name.Replace (" ", ""));
			}
			stream.WriteLine ();
			stream.WriteLine ();

			stream.Write ("depcheck: ");
			foreach (Project proj in projects) {
				stream.Write ("Makefile.{0}.depcheck ", proj.Name.Replace (" ", ""));
			}
			stream.WriteLine ();
			stream.WriteLine ();

			stream.WriteLine ("run: all");
			if (!SingleStartupProject) {
				stream.WriteLine ("\t@echo `run'ning multiple startup projects is not yet support");
			} else {
				if (StartupEntry != null)
					stream.WriteLine ("\tcd $(OUTPUTDIR) && $(RUNTIME) {0}", ((Project)StartupEntry).GetOutputFileName ());
				else
					stream.WriteLine ("\t@echo No startup project defined");
			}
			stream.WriteLine ();

			foreach (Project proj in projects) {
				string relativeLocation = Runtime.FileUtilityService.AbsoluteToRelativePath (BaseDirectory, proj.BaseDirectory);
				stream.WriteLine ("Makefile.{0}.%:", proj.Name.Replace (" ", ""));
				stream.WriteLine ("\t@cd {0} && $(MAKE) -f $(subst .$*,,$@) $*", relativeLocation);
				stream.WriteLine ();
			}

			stream.Flush ();
			stream.Close ();
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
		
		protected virtual void OnStartupPropertyChanged(EventArgs e)
		{
			if (StartupPropertyChanged != null) {
				StartupPropertyChanged(this, e);
			}
		}
			
		protected virtual void OnEntryAdded(CombineEntryEventArgs e)
		{
			if (EntryAdded != null) {
				EntryAdded (this, e);
			}
		}
		
		protected virtual void OnEntryRemoved(CombineEntryEventArgs e)
		{
			if (EntryRemoved != null) {
				EntryRemoved (this, e);
			}
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

		public event EventHandler StartupPropertyChanged;
		public event CombineEntryEventHandler EntryAdded;
		public event CombineEntryEventHandler EntryRemoved;
		public event ProjectFileEventHandler FileAddedToProject;
		public event ProjectFileEventHandler FileRemovedFromProject;
		public event ProjectFileEventHandler FileChangedInProject;
		public event ProjectFileRenamedEventHandler FileRenamedInProject;
		public event ProjectReferenceEventHandler ReferenceAddedToProject;
		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;
	}
	
	public class CombineActiveConfigurationTypeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context,Type sourceType)
		{
			return true;
		}
		
		public override  bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return true;
		}
		
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture,  object value)
		{
			Combine combine = (Combine)context.Instance;
			foreach (IConfiguration configuration in combine.Configurations) {
				if (configuration.Name == value.ToString()) {
					return configuration;
				}
			}
			return null;
		}
		
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			IConfiguration config = value as IConfiguration;
			Debug.Assert(config != null, String.Format("Tried to convert {0} to IConfiguration", config));
			if (config != null) {
				return config.Name;
			}
			return String.Empty;
		}
		
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}
		
		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}
		
		public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context)
		{
			return new TypeConverter.StandardValuesCollection(((Combine)context.Instance).Configurations);
		}
	}
}
