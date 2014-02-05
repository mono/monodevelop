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

namespace MonoDevelop.Projects
{
	[ProjectModelDataItem (FallbackType = typeof(UnknownSolutionItem))]
	public abstract class SolutionEntityItem : SolutionItem, IConfigurationTarget, IWorkspaceFileObject
	{
		internal object MemoryProbe = Counters.ItemsInMemory.CreateMemoryProbe ();
			
		ProjectItemCollection items;
		ProjectItemCollection wildcardItems;
		ItemCollection<SolutionEntityItem> dependencies = new ItemCollection<SolutionEntityItem> ();

		SolutionItemEventArgs thisItemArgs;
		
		FileStatusTracker<SolutionItemEventArgs> fileStatusTracker;

		FilePath fileName;
		string name;
		
		FileFormat fileFormat;
		
		SolutionItemConfiguration activeConfiguration;
		SolutionItemConfigurationCollection configurations;
		
		public event EventHandler ConfigurationsChanged;
		public event ConfigurationEventHandler DefaultConfigurationChanged;
		public event ConfigurationEventHandler ConfigurationAdded;
		public event ConfigurationEventHandler ConfigurationRemoved;
		public event EventHandler<ProjectItemEventArgs> ProjectItemAdded;
		public event EventHandler<ProjectItemEventArgs> ProjectItemRemoved;
		
		public SolutionEntityItem ()
		{
			items = new ProjectItemCollection (this);
			wildcardItems = new ProjectItemCollection (this);
			thisItemArgs = new SolutionItemEventArgs (this);
			configurations = new SolutionItemConfigurationCollection (this);
			configurations.ConfigurationAdded += new ConfigurationEventHandler (OnConfigurationAddedToCollection);
			configurations.ConfigurationRemoved += new ConfigurationEventHandler (OnConfigurationRemovedFromCollection);
			Counters.ItemsLoaded++;
			fileStatusTracker = new FileStatusTracker<SolutionItemEventArgs> (this, OnReloadRequired, new SolutionItemEventArgs (this));
		}
		
		public override void Dispose ()
		{
			if (Disposed)
				return;
			
			Counters.ItemsLoaded--;
			
			foreach (var item in items.Concat (wildcardItems)) {
				IDisposable disp = item as IDisposable;
				if (disp != null)
					disp.Dispose ();
			}
			
			// items = null;
			// wildcardItems = null;
			// thisItemArgs = null;
			// fileStatusTracker = null;
			// fileFormat = null;
			// activeConfiguration = null;
			// configurations = null;
			
			base.Dispose ();
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
				releaseVersion = value;
				NotifyModified ("Version");
			}
		}
		
		public bool SyncVersionWithSolution {
			get {
				return syncReleaseVersion;
			}
			set {
				syncReleaseVersion = value;
				if (syncReleaseVersion && ParentSolution != null)
					Version = ParentSolution.Version;
				NotifyModified ("SyncVersionWithSolution");
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
				if (!Loading && ItemHandler.SyncFileName) {
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

		public bool Enabled {
			get { return ParentSolution != null ? ParentSolution.IsSolutionItemEnabled (FileName) : true; }
			set { 
				if (ParentSolution != null)
					ParentSolution.SetSolutionItemEnabled (FileName, value);
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

		internal ProjectItemCollection WildcardItems {
			get { return wildcardItems; }
		}
		
		/// <summary>
		/// Projects that need to be built before building this one
		/// </summary>
		/// <value>The dependencies.</value>
		public ItemCollection<SolutionEntityItem> ItemDependencies {
			get { return dependencies; }
		}

		public override IEnumerable<SolutionItem> GetReferencedItems (ConfigurationSelector configuration)
		{
			return base.GetReferencedItems (configuration).Concat (dependencies);
		}

		void IWorkspaceFileObject.ConvertToFormat (FileFormat format, bool convertChildren)
		{
			this.FileFormat = format;
		}
		
		public virtual bool SupportsFormat (FileFormat format)
		{
			return true;
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
				fileStatusTracker.BeginSave ();
				Services.ProjectService.GetExtensionChain (this).Save (monitor, this);
				OnSaved (thisItemArgs);
			} finally {
				fileStatusTracker.EndSave ();
			}
			FileService.NotifyFileChanged (FileName);
		}
		
		protected override void OnEndLoad ()
		{
			base.OnEndLoad ();
			fileStatusTracker.ResetLoadTimes ();
			
			if (syncReleaseVersion && ParentSolution != null)
				releaseVersion = ParentSolution.Version;
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
		
		public virtual bool ItemFilesChanged {
			get { return fileStatusTracker.ItemFilesChanged; }
		}
		
		internal protected override BuildResult OnRunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			if (target == ProjectService.BuildTarget) {
				SolutionItemConfiguration conf = GetConfiguration (configuration) as SolutionItemConfiguration;
				if (conf != null && conf.CustomCommands.HasCommands (CustomCommandType.Build)) {
					conf.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.Build, configuration);
					return new BuildResult ();
				}
			} else if (target == ProjectService.CleanTarget) {
				SolutionItemConfiguration config = GetConfiguration (configuration) as SolutionItemConfiguration;
				if (config != null && config.CustomCommands.HasCommands (CustomCommandType.Clean)) {
					config.CustomCommands.ExecuteCommand (monitor, this, CustomCommandType.Clean, configuration);
					return new BuildResult ();
				}
			}
			return base.OnRunTarget (monitor, target, configuration);
		}
		
		protected internal virtual void OnSave (IProgressMonitor monitor)
		{
			ItemHandler.Save (monitor);
		}
		
		[Obsolete ("This method will be removed in future releases")]
		public void SetNeedsBuilding (bool value)
		{
			// Nothing to be done
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
			return Services.ProjectService.GetExtensionChain (this).GetItemFiles (this, includeReferencedFiles);
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
			fileStatusTracker.ResetLoadTimes ();
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
		
		public virtual SolutionItemConfiguration GetConfiguration (ConfigurationSelector configuration)
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
		
		public override StringTagModelDescription GetStringTagModelDescription (ConfigurationSelector conf)
		{
			StringTagModelDescription model = base.GetStringTagModelDescription (conf);
			SolutionItemConfiguration config = GetConfiguration (conf);
			if (config != null)
				model.Add (config.GetType ());
			else
				model.Add (typeof(SolutionItemConfiguration));
			return model;
		}
		
		public override StringTagModel GetStringTagModel (ConfigurationSelector conf)
		{
			StringTagModel source = base.GetStringTagModel (conf);
			SolutionItemConfiguration config = GetConfiguration (conf);
			if (config != null)
				source.Add (config);
			return source;
		}
		
		internal protected virtual void OnItemsAdded (IEnumerable<ProjectItem> objs)
		{
			NotifyModified ("Items");
			var args = new ProjectItemEventArgs ();
			args.AddRange (objs.Select (pi => new ProjectItemEventInfo (this, pi)));
			if (ProjectItemAdded != null)
				ProjectItemAdded (this, args);
		}
		
		internal protected virtual void OnItemsRemoved (IEnumerable<ProjectItem> objs)
		{
			NotifyModified ("Items");
			var args = new ProjectItemEventArgs ();
			args.AddRange (objs.Select (pi => new ProjectItemEventInfo (this, pi)));
			if (ProjectItemRemoved != null)
				ProjectItemRemoved (this, args);
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
		
		protected virtual void OnReloadRequired (SolutionItemEventArgs args)
		{
			fileStatusTracker.FireReloadRequired (args);
		}
		
		public event SolutionItemEventHandler Saved;
		
/*		public event EventHandler<SolutionItemEventArgs> ReloadRequired {
			add { fileStatusTracker.ReloadRequired += value; }
			remove { fileStatusTracker.ReloadRequired -= value; }
		}
*/	}
	
	[Mono.Addins.Extension]
	class SolutionEntityItemTagProvider: StringTagProvider<SolutionEntityItem>, IStringTagProvider
	{
		public override IEnumerable<StringTagDescription> GetTags ()
		{
			yield return new StringTagDescription ("ProjectFile", "Project File");
		}
		
		public override object GetTagValue (SolutionEntityItem item, string tag)
		{
			switch (tag) {
				case "ITEMFILE":
				case "PROJECTFILE":
				case "PROJECTFILENAME":
					return item.FileName;
			}
			throw new NotSupportedException ();
		}
	}
}
