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
using MonoDevelop.Core.Services;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Components;
using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Internal.Serialization;

namespace MonoDevelop.Internal.Project
{
	public abstract class CombineEntry : ICustomDataItem, IDisposable, IExtendedDataItem
	{
		ConfigurationCollection configurations;
		Hashtable extendedProperties;

		Combine parentCombine;
		IConfiguration activeConfiguration;
		string name;
		string path;
		
		IFileFormat fileFormat;
		
		public CombineEntry ()
		{
			configurations = new ConfigurationCollection ();
			configurations.ConfigurationAdded += new ConfigurationEventHandler (OnConfigurationAddedToCollection);
			configurations.ConfigurationRemoved += new ConfigurationEventHandler (OnConfigurationRemovedFromCollection);
		}
		
		public virtual void InitializeFromTemplate (XmlElement template)
		{
		}
		
		IDictionary IExtendedDataItem.ExtendedProperties {
			get {
				if (extendedProperties == null)
					extendedProperties = new Hashtable ();
				return extendedProperties;
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
			}
		}
		
		public virtual IFileFormat FileFormat {
			get { return fileFormat; }
			set {
				fileFormat = value;
				FileName = fileFormat.GetValidFormatName (FileName);
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
		
		public virtual void Save (string fileName, IProgressMonitor monitor)
		{
			FileName = fileName;
			Save (monitor);
		}
		
		public Combine RootCombine {
			get { return parentCombine != null ? parentCombine.RootCombine : this as Combine; }
		}
		
		public virtual void Save (IProgressMonitor monitor)
		{
			Runtime.ProjectService.WriteFile (FileName, this, monitor);
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
					OnActiveConfigurationChanged (new ConfigurationEventArgs (this, value));
				}
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
				return Runtime.FileUtilityService.RelativeToAbsolutePath (BaseDirectory, relPath);
		}
		
		public string GetRelativeChildPath (string absPath)
		{
			return Runtime.FileUtilityService.AbsoluteToRelativePath (BaseDirectory, absPath);
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
			
			if (NameChanged != null) {
				NameChanged (this, e);
			}
		}
		
		void OnConfigurationAddedToCollection (object ob, ConfigurationEventArgs args)
		{
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
			OnConfigurationRemoved (new ConfigurationEventArgs (this, args.Configuration));
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
		
		public abstract void Clean ();
		public abstract ICompilerResult Build (IProgressMonitor monitor);
		public abstract void Execute (IProgressMonitor monitor, ExecutionContext context);
		public abstract bool NeedsBuilding { get; set; }
		
		public virtual void GenerateMakefiles (Combine parentCombine)
		{
		}
		
		public event CombineEntryRenamedEventHandler NameChanged;
		public event ConfigurationEventHandler ActiveConfigurationChanged;
		public event ConfigurationEventHandler ConfigurationAdded;
		public event ConfigurationEventHandler ConfigurationRemoved;
	}
}
