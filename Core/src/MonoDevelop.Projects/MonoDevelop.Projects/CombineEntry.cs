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
using System.Reflection;
using System.Diagnostics;
using System.CodeDom.Compiler;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects
{
	[DataItem (FallbackType = typeof(UnknownCombineEntry))]
	public abstract class CombineEntry : ICustomDataItem, IDisposable, IExtendedDataItem
	{
		ConfigurationCollection configurations;
		Hashtable extendedProperties;
		CombineEntryEventArgs thisCombineArgs;

		Combine parentCombine;
		IConfiguration activeConfiguration;
		string name;
		string path;
		
		IFileFormat fileFormat;
		
		[ItemProperty]
		string defaultDeployTarget;
		
		DeployTargetCollection deployTargets;
		
		public CombineEntry ()
		{
			configurations = new ConfigurationCollection ();
			configurations.ConfigurationAdded += new ConfigurationEventHandler (OnConfigurationAddedToCollection);
			configurations.ConfigurationRemoved += new ConfigurationEventHandler (OnConfigurationRemovedFromCollection);
			thisCombineArgs = new CombineEntryEventArgs (this);
		}
		
		public virtual void InitializeFromTemplate (XmlElement template)
		{
		}
		
		public IDictionary ExtendedProperties {
			get {
				if (extendedProperties == null)
					extendedProperties = new Hashtable ();
				return extendedProperties;
			}
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
		public virtual string Name {
			get {
				return name;
			}
			set {
				if (name != value && value != null && value.Length > 0) {
					string oldName = name;
					name = value;
					NotifyModified ();
					OnNameChanged (new CombineEntryRenamedEventArgs (this, oldName, name));
				}
			}
		}
		
		public virtual string FileName {
			get {
				if (parentCombine != null && path != null)
					return parentCombine.GetAbsoluteChildPath (path);
				else
					return path;
			}
			set {
				if (parentCombine != null && path != null)
					path = parentCombine.GetRelativeChildPath (value);
				else
					path = value;
				if (fileFormat != null)
					path = fileFormat.GetValidFormatName (FileName);
				NotifyModified ();
			}
		}
		
		public virtual IFileFormat FileFormat {
			get { return fileFormat; }
			set {
				fileFormat = value;
				FileName = fileFormat.GetValidFormatName (FileName);
				NotifyModified ();
			}
		}
		
		public virtual string RelativeFileName {
			get {
				if (path != null && parentCombine != null)
					return parentCombine.GetRelativeChildPath (path);
				else
					return path;
			}
		}
		
		public string BaseDirectory {
			get { return Path.GetDirectoryName (FileName); }
		}
		
		[ItemProperty ("fileversion")]
		protected virtual string CurrentFileVersion {
			get { return "2.0"; }
			set {}
		}
		
		public Combine ParentCombine {
			get { return parentCombine; }
		}
		
		public Combine RootCombine {
			get { return parentCombine != null ? parentCombine.RootCombine : this as Combine; }
		}
		
		// Returns a path which can be used to store local data related to the combine entry
		public string LocalDataPath {
			get {
				return Path.Combine (BaseDirectory, "." + Path.GetFileName (FileName) + ".local");
			}
		}
		
		public void Save (string fileName, IProgressMonitor monitor)
		{
			FileName = fileName;
			Save (monitor);
		}
		
		public void Save (IProgressMonitor monitor)
		{
			Services.ProjectService.ExtensionChain.Save (monitor, this);
			OnSaved (thisCombineArgs);
		}
		
		protected internal virtual void OnSave (IProgressMonitor monitor)
		{
			Services.ProjectService.WriteFile (FileName, this, monitor);
		}
		
		internal void SetParentCombine (Combine combine)
		{
			parentCombine = combine;
		}
		
		[ItemProperty ("Configurations")]
		[ItemProperty ("Configuration", ValueType=typeof(IConfiguration), Scope=1)]
		public ConfigurationCollection Configurations {
			get {
				return configurations;
			}
		}
		
		public IConfiguration ActiveConfiguration {
			get {
				if (activeConfiguration == null && configurations.Count > 0) {
					return (IConfiguration)configurations[0];
				}
				return activeConfiguration;
			}
			set {
				if (activeConfiguration != value) {
					activeConfiguration = value;
					NotifyModified ();
					OnActiveConfigurationChanged (new ConfigurationEventArgs (this, value));
				}
			}
		}
		
		public DeployTarget DefaultDeployTarget {
			get {
				if (defaultDeployTarget == null)
					return null;
				if (deployTargets == null)
					return null;
				foreach (DeployTarget dt in deployTargets)
					if (dt.Name == defaultDeployTarget)
						return dt;
				return null;
			}
			set {
				if (value != null)
					defaultDeployTarget = value.Name;
				else
					defaultDeployTarget = null;
			}
		}
		
		[ItemProperty]
		public DeployTargetCollection DeployTargets {
			get { 
				if (deployTargets == null)
					deployTargets = new DeployTargetCollection ();
				return deployTargets;
			}
		}
		
		public virtual DataCollection Serialize (ITypeSerializer handler)
		{
			DataCollection data = handler.Serialize (this);
			if (activeConfiguration != null) {
				DataItem confItem = data ["Configurations"] as DataItem;
				confItem.UniqueNames = true;
				if (confItem != null)
					confItem.ItemData.Add (new DataValue ("active", activeConfiguration.Name));
			}
			return data;
		}
		
		public virtual void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			DataValue ac = null;
			DataItem confItem = data ["Configurations"] as DataItem;
			if (confItem != null)
				ac = (DataValue) confItem.ItemData.Extract ("active");
				
			handler.Deserialize (this, data);
			if (ac != null)
				activeConfiguration = GetConfiguration (ac.Value);
				
			if (deployTargets != null)
				foreach (DeployTarget target in deployTargets)
					target.SetCombineEntry (this);
		}
		
		public abstract IConfiguration CreateConfiguration (string name);
		
		public IConfiguration GetConfiguration (string name)
		{
			foreach (IConfiguration conf in configurations)
				if (conf.Name == name) return conf;
			return null;
		}

		public string GetAbsoluteChildPath (string relPath)
		{
			if (Path.IsPathRooted (relPath))
				return relPath;
			else
				return Runtime.FileService.RelativeToAbsolutePath (BaseDirectory, relPath);
		}
		
		public string GetRelativeChildPath (string absPath)
		{
			return Runtime.FileService.AbsoluteToRelativePath (BaseDirectory, absPath);
		}
		
		public virtual void Dispose()
		{
		}
		
		protected virtual void OnNameChanged (CombineEntryRenamedEventArgs e)
		{
			Combine topMostParentCombine = this.parentCombine;

			if (topMostParentCombine != null) {
				while (topMostParentCombine.ParentCombine != null) {
					topMostParentCombine = topMostParentCombine.ParentCombine;
				}
				
				foreach (Project project in topMostParentCombine.GetAllProjects()) {
					if (project == this) {
						continue;
					}
					
					project.RenameReferences(e.OldName, e.NewName);
				}
			}
			
			NotifyModified ();
			if (NameChanged != null) {
				NameChanged (this, e);
			}
		}
		
		void OnConfigurationAddedToCollection (object ob, ConfigurationEventArgs args)
		{
			NotifyModified ();
			OnConfigurationAdded (new ConfigurationEventArgs (this, args.Configuration));
			if (activeConfiguration == null)
				ActiveConfiguration = args.Configuration;
		}
		
		void OnConfigurationRemovedFromCollection (object ob, ConfigurationEventArgs args)
		{
			if (activeConfiguration == args.Configuration) {
				if (Configurations.Count > 0)
					ActiveConfiguration = Configurations [0];
				else
					ActiveConfiguration = null;
			}
			NotifyModified ();
			OnConfigurationRemoved (new ConfigurationEventArgs (this, args.Configuration));
		}
		
		protected void NotifyModified ()
		{
			OnModified (thisCombineArgs);
		}
		
		protected virtual void OnModified (CombineEntryEventArgs args)
		{
			if (Modified != null)
				Modified (this, args);
		}
		
		protected virtual void OnSaved (CombineEntryEventArgs args)
		{
			if (Saved != null)
				Saved (this, args);
		}
		
		protected virtual void OnActiveConfigurationChanged (ConfigurationEventArgs args)
		{
			if (ActiveConfigurationChanged != null)
				ActiveConfigurationChanged (this, args);
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
		
		public void Clean ()
		{
			Services.ProjectService.ExtensionChain.Clean (this);
		}
		
		public ICompilerResult Build (IProgressMonitor monitor)
		{
			return Services.ProjectService.ExtensionChain.Build (monitor, this);
		}
		
		public void Execute (IProgressMonitor monitor, ExecutionContext context)
		{
			Services.ProjectService.ExtensionChain.Execute (monitor, this, context);
		}
		
		public bool NeedsBuilding {
			get { return Services.ProjectService.ExtensionChain.GetNeedsBuilding (this); }
			set { Services.ProjectService.ExtensionChain.SetNeedsBuilding (this, value); }
		}
		
		internal protected abstract void OnClean ();
		internal protected abstract ICompilerResult OnBuild (IProgressMonitor monitor);
		internal protected abstract void OnExecute (IProgressMonitor monitor, ExecutionContext context);
		internal protected abstract bool OnGetNeedsBuilding ();
		internal protected abstract void OnSetNeedsBuilding (bool val);
		
		public event CombineEntryRenamedEventHandler NameChanged;
		public event ConfigurationEventHandler ActiveConfigurationChanged;
		public event ConfigurationEventHandler ConfigurationAdded;
		public event ConfigurationEventHandler ConfigurationRemoved;
		public event CombineEntryEventHandler Modified;
		public event CombineEntryEventHandler Saved;
	}
}
