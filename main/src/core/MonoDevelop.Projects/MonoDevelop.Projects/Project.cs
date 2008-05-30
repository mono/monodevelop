//  Project.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Xml;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using Mono.Addins;
using MonoDevelop.Projects.Ambience;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	public enum NewFileSearch {
		None,
		OnLoad,
		OnLoadAutoInsert
	}
	
	/// <summary>
	/// External language bindings must extend this class
	/// </summary>
	[DataInclude (typeof(ProjectFile))]
	[DataItem (FallbackType=typeof(UnknownProject))]
	public abstract class Project : SolutionEntityItem
	{
		[ItemProperty ("Description", DefaultValue="")]
		protected string description     = "";

		[ItemProperty ("UseParentDirectoryAsNamespace", DefaultValue=false)]
		protected bool useParentDirectoryAsNamespace = false;

		[ItemProperty ("newfilesearch", DefaultValue = NewFileSearch.None)]
		protected NewFileSearch newFileSearch  = NewFileSearch.None;
		
		[ItemProperty ("AppDesignerFolder")]
		string designerFolder;

		ProjectFileCollection projectFiles;
		
		bool isDirty = false;
		
		public Project ()
		{
			FileService.FileChanged += OnFileChanged;
		}
		
		public string Description {
			get {
				return description;
			}
			set {
				description = value;
				NotifyModified ("Description");
			}
		}
		
		public bool UseParentDirectoryAsNamespace {
			get { return useParentDirectoryAsNamespace; }
			set { 
				useParentDirectoryAsNamespace = value; 
				NotifyModified ("UseParentDirectoryAsNamespace");
			}
		}
		
		public ProjectFileCollection Files {
			get {
				if (projectFiles != null) return projectFiles;
				return projectFiles = new ProjectFileCollection (this);
			}
		}
		
		public NewFileSearch NewFileSearch {
			get {
				return newFileSearch;
			}

			set {
				newFileSearch = value;
				NotifyModified ("NewFileSearch");
			}
		}
		
		public abstract string ProjectType {
			get;
		}
		
		public virtual MonoDevelop.Projects.Ambience.Ambience Ambience {
			get { return Services.Ambience.GenericAmbience; }
		}
		
		[Browsable(false)]
		public virtual string [] SupportedLanguages {
			get {
				return new String [] { "" };
			}
		}

		public string DesignerFolder {
			get {
				return designerFolder;
			}
			set {
				designerFolder = value;
			}
		}

		public bool IsFileInProject(string filename)
		{
			return GetProjectFile (filename) != null;
		}
		
		public ProjectFile GetProjectFile (string fileName)
		{
			return Files.GetFile (fileName);
		}
		
		public virtual bool IsCompileable (string fileName)
		{
			return false;
		}
				
		public static Project LoadProject (string filename, IProgressMonitor monitor)
		{
			Project prj = Services.ProjectService.ReadSolutionItem (monitor, filename) as Project;
			if (prj == null)
				throw new InvalidOperationException ("Invalid project file: " + filename);
			
			return prj;
		}

		
		public override void Dispose()
		{
			foreach (ProjectFile file in Files) {
				file.Dispose ();
			}
			base.Dispose ();
		}
		
		public ProjectFile AddFile (string filename, BuildAction action)
		{
			foreach (ProjectFile fInfo in Files) {
				if (fInfo.Name == filename) {
					return fInfo;
				}
			}
			ProjectFile newFileInformation = new ProjectFile (filename, action);
			Files.Add (newFileInformation);
			return newFileInformation;
		}
		
		public void AddFile (ProjectFile projectFile) {
			Files.Add (projectFile);
		}
		
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
		
		protected internal override BuildResult OnBuild (IProgressMonitor monitor, string configuration)
		{
			// create output directory, if not exists
			ProjectConfiguration conf = GetConfiguration (configuration) as ProjectConfiguration;
			if (conf == null)
				return null;
			string outputDir = conf.OutputDirectory;
			try {
				DirectoryInfo directoryInfo = new DirectoryInfo(outputDir);
				if (!directoryInfo.Exists) {
					directoryInfo.Create();
				}
			} catch (Exception e) {
				throw new ApplicationException("Can't create project output directory " + outputDir + " original exception:\n" + e.ToString());
			}
		
			StringParserService.Properties["Project"] = Name;
			
			monitor.Log.WriteLine (GettextCatalog.GetString ("Performing main compilation..."));
			BuildResult res = DoBuild (monitor, configuration);
			
			isDirty = false;
			
			if (res != null) {
				string errorString = GettextCatalog.GetPluralString("{0} error", "{0} errors", res.ErrorCount, res.ErrorCount);
				string warningString = GettextCatalog.GetPluralString("{0} warning", "{0} warnings", res.WarningCount, res.WarningCount);
			
				monitor.Log.WriteLine(GettextCatalog.GetString("Build complete -- ") + errorString + ", " + warningString);
			}
			
			return res;
		}
		
		protected internal override void OnClean (IProgressMonitor monitor, string configuration)
		{
			isDirty = true;
			
			// Delete the generated assembly
			string file = GetOutputFileName (configuration);
			if (file != null) {
				if (File.Exists (file))
					FileService.DeleteFile (file);
			}

			// Delete referenced assemblies
			ProjectConfiguration config = GetConfiguration (configuration) as ProjectConfiguration;
			if (config != null)
				ItemHandler.RunTarget (monitor, "Clean", config.Id);
		}
		
		void GetBuildableReferencedItems (List<SolutionItem> referenced, SolutionItem item, string configuration)
		{
			if (referenced.Contains (item)) return;
			
			if (item.NeedsBuilding (configuration))
				referenced.Add (item);

			foreach (SolutionItem ritem in item.GetReferencedItems (configuration))
				GetBuildableReferencedItems (referenced, ritem, configuration);
		}
		
		protected virtual BuildResult DoBuild (IProgressMonitor monitor, string configuration)
		{
			return ItemHandler.RunTarget (monitor, "Build", configuration);
		}
		
		protected internal override void OnExecute (IProgressMonitor monitor, ExecutionContext context, string configuration)
		{
			DoExecute (monitor, context, configuration);
		}
		
		protected virtual void DoExecute (IProgressMonitor monitor, ExecutionContext context, string configuration)
		{
		}
		
		public virtual string GetOutputFileName (string configuration)
		{
			return null;
		}
		
		protected internal override bool OnGetNeedsBuilding (string configuration)
		{
			if (!isDirty) CheckNeedsBuild (configuration);
			return isDirty;
		}
		
		protected internal override void OnSetNeedsBuilding (bool value, string configuration)
		{
			isDirty = value;
		}
		
		protected virtual void CheckNeedsBuild (string configuration)
		{
			DateTime tim = GetLastBuildTime (configuration);
			if (tim == DateTime.MinValue) {
				isDirty = true;
				return;
			}
			
			foreach (ProjectFile file in Files) {
				if (file.BuildAction == BuildAction.Exclude || file.BuildAction == BuildAction.Nothing)
					continue;
				FileInfo finfo = new FileInfo (file.FilePath);
				if (finfo.Exists && finfo.LastWriteTime > tim) {
					isDirty = true;
					return;
				}
			}
			
			foreach (SolutionItem pref in GetReferencedItems (configuration)) {
				if (pref.NeedsBuilding (configuration)) {
					isDirty = true;
					return;
				}
			}
		}
		
		protected virtual DateTime GetLastBuildTime (string configuration)
		{
			return GetLastWriteTime (GetOutputFileName (configuration));
		}

		DateTime GetLastWriteTime (string file)
		{
			if (file == null)
				return DateTime.MinValue;

			FileInfo finfo = new FileInfo (file);
			if (!finfo.Exists) return DateTime.MinValue;
			else return finfo.LastWriteTime;
		}

		void OnFileChanged (object source, FileEventArgs e)
		{
			ProjectFile file = GetProjectFile (e.FileName);
			if (file != null) {
				isDirty = true;
				try {
					NotifyFileChangedInProject (file);
				} catch {
					// Workaround Mono bug. The watcher seems to
					// stop watching if an exception is thrown in
					// the event handler
				}
			}

		}
		
		internal protected override List<string> OnGetItemFiles (bool includeReferencedFiles)
		{
			List<string> col = base.OnGetItemFiles (includeReferencedFiles);
			if (includeReferencedFiles) {
				foreach (ProjectFile pf in Files) {
					if (pf.Subtype != Subtype.Directory)
						col.Add (pf.FilePath);
				}
			}
			return col;
		}
		
		internal override void SetItemHandler (ISolutionItemHandler handler)
		{
			// The new handler may have different defaults for resource ids. To ensure
			// the existing ids don't change, we need to explicitely set them after the
			// format change.
			
			Dictionary<ProjectFile,string> rids = new Dictionary<ProjectFile,string> ();
			foreach (ProjectFile p in Files) {
				if (p.BuildAction == BuildAction.EmbedAsResource)
					rids [p] = p.ResourceId;
			}
			
			base.SetItemHandler (handler);
			
			foreach (KeyValuePair<ProjectFile,string> entry in rids) {
				if (entry.Key.ResourceId != entry.Value)
					entry.Key.ResourceId = entry.Value;
			}
		}


 		internal void NotifyFileChangedInProject (ProjectFile file)
		{
			OnFileChangedInProject (new ProjectFileEventArgs (this, file));
		}
		
 		internal void NotifyFilePropertyChangedInProject (ProjectFile file)
		{
			NotifyModified ("Files");
			OnFilePropertyChangedInProject (new ProjectFileEventArgs (this, file));
		}
		
		internal void NotifyFileRemovedFromProject (ProjectFile file)
		{
			isDirty = true;
			NotifyModified ("Files");
			OnFileRemovedFromProject (new ProjectFileEventArgs (this, file));
		}
		
		internal void NotifyFileAddedToProject (ProjectFile file)
		{
			isDirty = true;
			NotifyModified ("Files");
			OnFileAddedToProject (new ProjectFileEventArgs (this, file));
		}
		
		internal void NotifyFileRenamedInProject (ProjectFileRenamedEventArgs args)
		{
			isDirty = true;
			NotifyModified ("Files");
			OnFileRenamedInProject (args);
		}
		
		internal void NotifyReferenceRemovedFromProject (ProjectReference reference)
		{
			isDirty = true;
			NotifyModified ("References");
			OnReferenceRemovedFromProject (new ProjectReferenceEventArgs (this, reference));
		}
		
		internal void NotifyReferenceAddedToProject (ProjectReference reference)
		{
			isDirty = true;
			NotifyModified ("References");
			OnReferenceAddedToProject (new ProjectReferenceEventArgs (this, reference));
		}
		
		protected virtual void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			if (FileRemovedFromProject != null) {
				FileRemovedFromProject (this, e);
			}
		}
		
		protected virtual void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			if (FileAddedToProject != null) {
				FileAddedToProject (this, e);
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
		
 		protected virtual void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			if (FileRenamedInProject != null) {
				FileRenamedInProject (this, e);
			}
		}
				
		public event ProjectFileEventHandler FileRemovedFromProject;
		public event ProjectFileEventHandler FileAddedToProject;
		public event ProjectFileEventHandler FileChangedInProject;
		public event ProjectFileEventHandler FilePropertyChangedInProject;
		public event ProjectFileRenamedEventHandler FileRenamedInProject;
		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;
		public event ProjectReferenceEventHandler ReferenceAddedToProject;
	}
	
	public class UnknownProject: Project
	{
		public override string ProjectType {
			get { return ""; }
		}

		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			return null;
		}
	}
}
