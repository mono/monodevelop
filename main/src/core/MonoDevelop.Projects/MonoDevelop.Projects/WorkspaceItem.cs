//  SolutionEntityItem.cs
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
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Diagnostics;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects
{
	public abstract class WorkspaceItem : IBuildTarget, IWorkspaceFileObject, ILoadController
	{
		Workspace parentWorkspace;
		FileFormat format;
		Hashtable extendedProperties;
		string fileName;
		int loading;
		
		[ProjectPathItemProperty ("BaseDirectory", DefaultValue=null)]
		string baseDirectory;
		
		Dictionary<string,DateTime> lastSaveTime = new Dictionary<string,DateTime> ();
		bool savingFlag;
		
		public Workspace ParentWorkspace {
			get { return parentWorkspace; }
			internal set { parentWorkspace = value; }
		}
		
		public IDictionary ExtendedProperties {
			get {
				if (extendedProperties == null)
					extendedProperties = new Hashtable ();
				return extendedProperties;
			}
		}
		
		[ItemProperty ("name")]
		public virtual string Name {
			get {
				if (string.IsNullOrEmpty (fileName))
					return string.Empty;
				else
					return Path.GetFileNameWithoutExtension (fileName);
			}
			set {
				if (string.IsNullOrEmpty (fileName))
					FileName = value;
				else {
					string dir = Path.GetDirectoryName (fileName);
					string ext = Path.GetExtension (fileName);
					FileName = Path.Combine (dir, value) + ext;
				}
			}
		}
		
		public virtual string FileName {
			get {
				return fileName;
			}
			set {
				string oldName = Name;
				fileName = value;
				if (FileFormat != null)
					fileName = FileFormat.GetValidFileName (this, fileName);
				if (oldName != Name)
					OnNameChanged (new WorkspaceItemRenamedEventArgs (this, oldName, Name));
				NotifyModified ();
			}
		}
		
		public string BaseDirectory {
			get {
				if (baseDirectory == null)
					return Path.GetFullPath (Path.GetDirectoryName (FileName));
				else
					return baseDirectory;
			}
			set {
				if (value != null && FileName != null && Path.GetFullPath (Path.GetDirectoryName (FileName)) == Path.GetFullPath (value))
					baseDirectory = null;
				else if (string.IsNullOrEmpty (value))
					baseDirectory = null;
				else
					baseDirectory = Path.GetFullPath (value);
				NotifyModified ();
			}
		}
		
		protected bool Loading {
			get { return loading > 0; }
		}
		
		public WorkspaceItem ()
		{
			MonoDevelop.Projects.Extensions.ProjectExtensionUtil.LoadControl (this);
		}
		
		public virtual List<string> GetItemFiles (bool includeReferencedFiles)
		{
			List<string> col = FileFormat.Format.GetItemFiles (this);
			if (!string.IsNullOrEmpty (FileName) && !col.Contains (FileName))
				col.Add (FileName);
			return col;
		}
		
		public virtual SolutionEntityItem FindSolutionItem (string fileName)
		{
			return null;
		}
		
		public virtual bool ContainsItem (IWorkspaceObject obj)
		{
			return this == obj;
		}
		
		public ReadOnlyCollection<SolutionItem> GetAllSolutionItems ()
		{
			return GetAllSolutionItems<SolutionItem> ();
		}
		
		public virtual ReadOnlyCollection<T> GetAllSolutionItems<T> () where T: SolutionItem
		{
			return new List<T> ().AsReadOnly ();
		}
		
		public ReadOnlyCollection<Project> GetAllProjects ()
		{
			return GetAllSolutionItems<Project> ();
		}
		
		public virtual ReadOnlyCollection<Solution> GetAllSolutions ()
		{
			return GetAllItems<Solution> ();
		}
		
		public ReadOnlyCollection<WorkspaceItem> GetAllItems ()
		{
			return GetAllItems<WorkspaceItem> ();
		}
		
		public virtual ReadOnlyCollection<T> GetAllItems<T> () where T: WorkspaceItem
		{
			List<T> list = new List<T> ();
			if (this is T)
				list.Add ((T)this);
			return list.AsReadOnly ();
		}
		
		public virtual Project GetProjectContainingFile (string fileName)
		{
			return null;
		}
		
		public virtual ReadOnlyCollection<string> GetConfigurations ()
		{
			return new ReadOnlyCollection<string> (new string [0]);
		}
		
		protected internal virtual void OnSave (IProgressMonitor monitor)
		{
			Services.ProjectService.InternalWriteWorkspaceItem (monitor, FileName, this);
		}
		
		internal void SetParentWorkspace (Workspace workspace)
		{
			parentWorkspace = workspace;
		}
		
		public BuildResult RunTarget (IProgressMonitor monitor, string target, string configuration)
		{
			return Services.ProjectService.ExtensionChain.RunTarget (monitor, this, target, configuration);
		}
		
		public void Clean (IProgressMonitor monitor, string configuration)
		{
			Services.ProjectService.ExtensionChain.RunTarget (monitor, this, ProjectService.CleanTarget, configuration);
		}
		
		public BuildResult Build (IProgressMonitor monitor, string configuration)
		{
			return InternalBuild (monitor, configuration);
		}
		
		public void Execute (IProgressMonitor monitor, ExecutionContext context, string configuration)
		{
			Services.ProjectService.ExtensionChain.Execute (monitor, this, context, configuration);
		}
		
		public bool CanExecute (ExecutionContext context, string configuration)
		{
			return Services.ProjectService.ExtensionChain.CanExecute (this, context, configuration);
		}
		
		public bool NeedsBuilding (string configuration)
		{
			return Services.ProjectService.ExtensionChain.GetNeedsBuilding (this, configuration);
		}
		
		public void SetNeedsBuilding (bool value)
		{
			foreach (string conf in GetConfigurations ())
				SetNeedsBuilding (value, conf);
		}
		
		public void SetNeedsBuilding (bool needsBuilding, string configuration)
		{
			Services.ProjectService.ExtensionChain.SetNeedsBuilding (this, needsBuilding, configuration);
		}
		
		public virtual FileFormat FileFormat {
			get {
				if (format == null) {
					format = Services.ProjectService.GetDefaultFormat (this);
				}
				return format;
			}
		}
		
		public virtual void ConvertToFormat (FileFormat format, bool convertChildren)
		{
			this.format = format;
			if (!string.IsNullOrEmpty (FileName))
				FileName = format.GetValidFileName (this, FileName);
		}
		
		internal virtual BuildResult InternalBuild (IProgressMonitor monitor, string configuration)
		{
			return Services.ProjectService.ExtensionChain.RunTarget (monitor, this, ProjectService.BuildTarget, configuration);
		}
		
		protected virtual void OnConfigurationsChanged ()
		{
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, EventArgs.Empty);
			if (ParentWorkspace != null)
				ParentWorkspace.OnConfigurationsChanged ();
		}
		
		public void Save (string fileName, IProgressMonitor monitor)
		{
			FileName = fileName;
			Save (monitor);
		}
		
		public void Save (IProgressMonitor monitor)
		{
			try {
				savingFlag = true;
				Services.ProjectService.ExtensionChain.Save (monitor, this);
				OnSaved (new WorkspaceItemEventArgs (this));
				
				// Update save times
				lastSaveTime.Clear ();
				foreach (string file in GetItemFiles (false))
					lastSaveTime [file] = GetLastWriteTime (file);
				
				FileService.NotifyFileChanged (FileName);
			} finally {
				savingFlag = false;
			}
		}
		
		public virtual bool NeedsReload {
			get {
				if (savingFlag)
					return false;
				foreach (string file in GetItemFiles (false))
					if (GetLastSaveTime (file) != GetLastWriteTime (file))
						return true;
				return false;
			}
			set {
				lastSaveTime.Clear ();
				foreach (string file in GetItemFiles (false)) {
					if (value)
						lastSaveTime [file] = DateTime.MinValue;
					else
						lastSaveTime [file] = GetLastWriteTime (file);
				}
			}
		}
		
		DateTime GetLastWriteTime (string file)
		{
			try {
				if (file != null && file.Length > 0 && File.Exists (file))
					return File.GetLastWriteTime (file);
			} catch {
			}
			return GetLastSaveTime (file);
		}
					
		DateTime GetLastSaveTime (string file)
		{
			DateTime dt;
			if (lastSaveTime.TryGetValue (file, out dt))
				return dt;
			else
				return DateTime.MinValue;
		}
		
		internal protected virtual BuildResult OnRunTarget (IProgressMonitor monitor, string target, string configuration)
		{
			if (target == ProjectService.BuildTarget)
				return OnBuild (monitor, configuration);
			else if (target == ProjectService.CleanTarget) {
				OnClean (monitor, configuration);
				return null;
			}
			return null;
		}
		
		protected virtual void OnClean (IProgressMonitor monitor, string configuration)
		{
		}
		
		protected virtual BuildResult OnBuild (IProgressMonitor monitor, string configuration)
		{
			return null;
		}
		
		internal protected virtual void OnExecute (IProgressMonitor monitor, ExecutionContext context, string configuration)
		{
		}
		
		internal protected virtual bool OnGetCanExecute (ExecutionContext context, string configuration)
		{
			return true;
		}
		
		internal protected virtual bool OnGetNeedsBuilding (string configuration)
		{
			return false;
		}
		
		internal protected virtual void OnSetNeedsBuilding (bool val, string configuration)
		{
		}
		
		void ILoadController.BeginLoad ()
		{
			loading++;
			OnBeginLoad ();
		}
		
		void ILoadController.EndLoad ()
		{
			loading--;
			OnEndLoad ();
		}
		
		protected virtual void OnBeginLoad ()
		{
		}
		
		protected virtual void OnEndLoad ()
		{
		}
		
		public string GetAbsoluteChildPath (string relPath)
		{
			if (Path.IsPathRooted (relPath))
				return relPath;
			return FileService.RelativeToAbsolutePath (BaseDirectory, relPath);
		}
		
		public string GetRelativeChildPath (string absPath)
		{
			return FileService.AbsoluteToRelativePath (BaseDirectory, absPath);
		}
		
		public virtual void Dispose()
		{
			if (extendedProperties != null) {
				foreach (object ob in extendedProperties.Values) {
					IDisposable disp = ob as IDisposable;
					if (disp != null)
						disp.Dispose ();
				}
			}
		}
		
		protected virtual void OnNameChanged (WorkspaceItemRenamedEventArgs e)
		{
			NotifyModified ();
			if (NameChanged != null)
				NameChanged (this, e);
		}
		
		protected void NotifyModified ()
		{
			OnModified (new WorkspaceItemEventArgs (this));
		}
		
		protected virtual void OnModified (WorkspaceItemEventArgs args)
		{
			if (Modified != null)
				Modified (this, args);
		}
		
		protected virtual void OnSaved (WorkspaceItemEventArgs args)
		{
			if (Saved != null)
				Saved (this, args);
		}
		
		public event EventHandler ConfigurationsChanged;
		public event EventHandler<WorkspaceItemRenamedEventArgs> NameChanged;
		public event EventHandler<WorkspaceItemEventArgs> Modified;
		public event EventHandler<WorkspaceItemEventArgs> Saved;
	}
}
