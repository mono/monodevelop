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

namespace MonoDevelop.Projects
{
	[DataItem (FallbackType = typeof(UnknownSolutionItem))]
	public abstract class SolutionEntityItem : SolutionItem, IConfigurationTarget, IWorkspaceFileObject
	{
		ProjectItemCollection items;
		
		SolutionItemEventArgs thisItemArgs;
		
		Dictionary<string,DateTime> lastSaveTime = new Dictionary<string,DateTime> ();
		bool savingFlag;

		FilePath fileName;
		string name;
		
		FileFormat fileFormat;
		
		SolutionItemConfiguration activeConfiguration;
		SolutionItemConfigurationCollection configurations;
		
		public event EventHandler ConfigurationsChanged;
		public event ConfigurationEventHandler DefaultConfigurationChanged;
		public event ConfigurationEventHandler ConfigurationAdded;
		public event ConfigurationEventHandler ConfigurationRemoved;
		
		public SolutionEntityItem ()
		{
			items = new ProjectItemCollection (this);
			thisItemArgs = new SolutionItemEventArgs (this);
			configurations = new SolutionItemConfigurationCollection (this);
			configurations.ConfigurationAdded += new ConfigurationEventHandler (OnConfigurationAddedToCollection);
			configurations.ConfigurationRemoved += new ConfigurationEventHandler (OnConfigurationRemovedFromCollection);
		}
		
		internal override void SetItemHandler (ISolutionItemHandler handler)
		{
			string oldName = Name;
			string oldFile = FileName;
			
			base.SetItemHandler (handler);
			
			// This will update the name if needed, when SyncFileName is true
			Name = oldName;
			if (!string.IsNullOrEmpty (oldFile))
				FileName = oldFile;
		}
		
		[ItemProperty ("releaseversion", DefaultValue = "0.1")]
		string release_version;

		public string Version {
			get {
				return release_version;
			}
			set {
				release_version = value;
			}
		}
		
		[ItemProperty ("name")]
		public override string Name {
			get {
				return name ?? string.Empty;
			}
			set {
				if (name == value)
					return;
				string oldName = name;
				name = value;
				if (ItemHandler.SyncFileName) {
					if (string.IsNullOrEmpty (fileName))
						FileName = value;
					else {
						string ext = fileName.Extension;
						FileName = fileName.ParentDirectory.Combine (value) + ext;
					}
				}
				OnNameChanged (new SolutionItemRenamedEventArgs (this, oldName, name));
			}
		}
		
		public virtual FilePath FileName {
			get {
				return fileName;
			}
			set {
				fileName = value;
				if (FileFormat != null)
					fileName = FileFormat.GetValidFileName (this, fileName);
				if (ItemHandler.SyncFileName)
					Name = fileName.FileNameWithoutExtension;
				NotifyModified ("FileName");
			}
		}
		
		public FileFormat FileFormat {
			get {
				if (ParentSolution != null) {
					if (ParentSolution.FileFormat.Format.SupportsMixedFormats && fileFormat != null)
						return fileFormat;
					return ParentSolution.FileFormat;
				}
				if (fileFormat == null)
					fileFormat = Services.ProjectService.GetDefaultFormat (this);
				return fileFormat; 
			}
			set {
				if (ParentSolution != null && !ParentSolution.FileFormat.Format.SupportsMixedFormats)
					throw new InvalidOperationException ("The file format can't be changed when the item belongs to a solution.");
				InstallFormat (value);
				fileFormat.Format.ConvertToFormat (this);
				NeedsReload = false;
				NotifyModified ("FileFormat");
			}
		}
		
		public ProjectItemCollection Items {
			get { return items; }
		}
		
		void IWorkspaceFileObject.ConvertToFormat (FileFormat format, bool convertChildren)
		{
			this.FileFormat = format;
		}
		
		internal void InstallFormat (FileFormat format)
		{
			fileFormat = format;
			if (fileName != FilePath.Null)
				fileName = fileFormat.GetValidFileName (this, fileName);
		}
		
		protected override void InitializeItemHandler ()
		{
			Services.ProjectService.GetDefaultFormat (this).Format.ConvertToFormat (this);
		}

		protected override FilePath GetDefaultBaseDirectory ( )
		{
			return FileName.IsNullOrEmpty ? FilePath.Empty : FileName.ParentDirectory; 
		}

		public void Save (FilePath fileName, IProgressMonitor monitor)
		{
			FileName = fileName;
			Save (monitor);
		}
		
		public override void Save (IProgressMonitor monitor)
		{
			if (string.IsNullOrEmpty (FileName))
				throw new InvalidOperationException ("Project does not have a file name");
			
			try {
				savingFlag = true;
				Services.ProjectService.ExtensionChain.Save (monitor, this);
				OnSaved (thisItemArgs);
				
				// Update save times
				lastSaveTime.Clear ();
				foreach (FilePath file in GetItemFiles (false))
					lastSaveTime [file] = GetLastWriteTime (file);
				
				FileService.NotifyFileChanged (FileName);
			} finally {
				savingFlag = false;
			}
		}
		
		internal bool IsSaved {
			get {
				return !string.IsNullOrEmpty (FileName) && File.Exists (FileName);
			}
		}
		
		public override bool NeedsReload {
			get {
				if (savingFlag)
					return false;
				foreach (FilePath file in GetItemFiles (false))
					if (GetLastSaveTime (file) != GetLastWriteTime (file))
						return true;
				return false;
			}
			set {
				lastSaveTime.Clear ();
				foreach (FilePath file in GetItemFiles (false)) {
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
		
		protected internal virtual void OnSave (IProgressMonitor monitor)
		{
			ItemHandler.Save (monitor);
		}
		
		public void SetNeedsBuilding (bool value)
		{
			foreach (string conf in GetConfigurations ())
				SetNeedsBuilding (value, conf);
		}

		public FilePath GetAbsoluteChildPath (FilePath relPath)
		{
			return relPath.ToAbsolute (BaseDirectory);
		}

		public FilePath GetRelativeChildPath (FilePath absPath)
		{
			return absPath.ToRelative (BaseDirectory);
		}

		public List<FilePath> GetItemFiles (bool includeReferencedFiles)
		{
			return Services.ProjectService.ExtensionChain.GetItemFiles (this, includeReferencedFiles);
		}

		internal protected virtual List<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			List<FilePath> col = FileFormat.Format.GetItemFiles (this);
			if (!string.IsNullOrEmpty (FileName) && !col.Contains (FileName))
				col.Add (FileName);
			return col;
		}
		
		protected override void OnNameChanged (SolutionItemRenamedEventArgs e)
		{
			Solution solution = this.ParentSolution;

			if (solution != null) {
				foreach (DotNetProject project in solution.GetAllSolutionItems<DotNetProject>()) {
					if (project == this)
						continue;
					
					project.RenameReferences (e.OldName, e.NewName);
				}
			}
			base.OnNameChanged (e);
		}
		
		protected virtual void OnSaved (SolutionItemEventArgs args)
		{
			if (Saved != null)
				Saved (this, args);
		}
		
		public virtual string[] SupportedPlatforms {
			get {
				return new string [0];
			}
		}
		
		public SolutionItemConfiguration GetConfiguration (string id)
		{
			if (id == ProjectService.DefaultConfiguration)
				return DefaultConfiguration;
			foreach (SolutionItemConfiguration conf in configurations)
				if (conf.Id == id) return conf;
			return null;
		}

		public SolutionItemConfiguration GetActiveConfiguration (string solutionConfiguration)
		{
			if (ParentSolution != null) {
				SolutionConfiguration config = ParentSolution.Configurations [solutionConfiguration];
				if (config != null) {
					string mc = config.GetMappedConfiguration (this);
					if (mc != null)
						solutionConfiguration = mc;
				}
			}
			foreach (SolutionItemConfiguration conf in configurations)
				if (conf.Id == solutionConfiguration) return conf;
			return DefaultConfiguration;
		}
		
		public string GetActiveConfigurationId (string solutionConfiguration)
		{
			if (ParentSolution != null) {
				SolutionConfiguration config = ParentSolution.Configurations [solutionConfiguration];
				if (config != null) {
					string mc = config.GetMappedConfiguration (this);
					if (mc != null)
						return mc;
				}
			}
			return ProjectService.DefaultConfiguration;
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
				DefaultConfiguration = GetConfiguration (value);
			}
		}
		
		public virtual ReadOnlyCollection<string> GetConfigurations ()
		{
			List<string> configs = new List<string> ();
			foreach (SolutionItemConfiguration conf in Configurations)
				configs.Add (conf.Id);
			return configs.AsReadOnly ();
		}
		
		[ItemProperty ("Configurations")]
		[ItemProperty ("Configuration", ValueType=typeof(SolutionItemConfiguration), Scope="*")]
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
		
		public SolutionItemConfiguration AddNewConfiguration (string name)
		{
			SolutionItemConfiguration config = CreateConfiguration (name);
			Configurations.Add (config);
			return config;
		}
		
		ItemConfiguration IConfigurationTarget.CreateConfiguration (string name)
		{
			return CreateConfiguration (name);
		}

		public virtual SolutionItemConfiguration CreateConfiguration (string name)
		{
			return new SolutionItemConfiguration (name);
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
		
		internal protected virtual void OnItemAdded (object obj)
		{
		}
		
		internal protected virtual void OnItemRemoved (object obj)
		{
		}
		
		protected virtual void OnDefaultConfigurationChanged (ConfigurationEventArgs args)
		{
			if (DefaultConfigurationChanged != null)
				DefaultConfigurationChanged (this, args);
		}
		
		protected virtual void OnConfigurationAdded (ConfigurationEventArgs args)
		{
			if (ConfigurationAdded != null)
				ConfigurationAdded (this, args);
		}
		
		protected virtual void OnConfigurationRemoved (ConfigurationEventArgs args)
		{
			if (ConfigurationRemoved != null)
				ConfigurationRemoved (this, args);
		}
		
		public event SolutionItemEventHandler Saved;
	}
}
