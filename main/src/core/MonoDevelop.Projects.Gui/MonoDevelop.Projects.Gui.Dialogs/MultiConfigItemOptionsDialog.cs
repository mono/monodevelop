// MultiConfigItemOptionsDialog.cs
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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

namespace MonoDevelop.Projects.Gui.Dialogs
{
	public class MultiConfigItemOptionsDialog: OptionsDialog
	{
		ConfigurationData configData;
		string currentConfig;
		string currentPlatform;
		
		public MultiConfigItemOptionsDialog (string extensionPath): this (null, null, extensionPath)
		{
		}
		
		public MultiConfigItemOptionsDialog (Gtk.Window parentWindow, object dataObject, string extensionPath): base (parentWindow, dataObject, extensionPath)
		{
			IConfigurationTarget ct = DataObject as IConfigurationTarget;
			if (ct == null)
				throw new System.InvalidOperationException ("MultiConfigItemOptionsDialog can only be used for SolutionEntityItem and Solution objects");
			if (ct.DefaultConfiguration != null) {
				currentConfig = ct.DefaultConfiguration.Name;
				currentPlatform = ct.DefaultConfiguration.Platform;
			}
		}
		
		internal ConfigurationData ConfigurationData {
			get {
				if (configData == null) {
					IConfigurationTarget ct = (IConfigurationTarget) DataObject;
					configData = MonoDevelop.Projects.Gui.Dialogs.ConfigurationData.Build (ct);
				}
				return configData;
			}
		}
		
		protected override void ApplyChanges ()
		{
			base.ApplyChanges ();
			
			if (configData != null)
				configData.Update ();
		}


		public string CurrentConfig {
			get {
				return currentConfig;
			}
			set {
				currentConfig = value;
			}
		}

		public string CurrentPlatform {
			get {
				return currentPlatform;
			}
			set {
				currentPlatform = value;
			}
		}
	}
	
	public class ConfigurationData
	{
		ItemConfigurationCollection<ItemConfiguration> configurations = new ItemConfigurationCollection<ItemConfiguration> ();
		IConfigurationTarget entry;
		List<ConfigurationData> children = new List<ConfigurationData> ();
		
		internal ConfigurationData (IConfigurationTarget obj)
		{
			this.entry = obj;
		}
		
		internal static ConfigurationData Build (IConfigurationTarget entry)
		{
			ConfigurationData data = new ConfigurationData (entry);

			foreach (ItemConfiguration conf in entry.Configurations) {
				ItemConfiguration copy = (ItemConfiguration) conf.Clone ();
				data.Configurations.Add (copy);
			}
			if (entry is Solution) {
				foreach (SolutionItem e in ((Solution)entry).Items) {
					if (e is SolutionEntityItem)
						data.children.Add (ConfigurationData.Build ((SolutionEntityItem) e));
				}
			}
			return data;
		}
		
		public IConfigurationTarget Entry {
			get { return entry; }
		}
		
		public ItemConfigurationCollection<ItemConfiguration> Configurations {
			get {
				return configurations;
			}
		}
		
		public void Update ()
		{
			foreach (ItemConfiguration conf in configurations) {
				ItemConfiguration old = entry.Configurations [conf.Id];
				if (old != null) {
					old.CopyFrom (conf);
				} else {
					entry.Configurations.Add (conf);
				}
			}
			List<ItemConfiguration> toRemove = new List<ItemConfiguration> ();
			foreach (ItemConfiguration conf in entry.Configurations) {
				if (configurations [conf.Id] == null)
					toRemove.Add (conf);
			}
			
			foreach (ItemConfiguration conf in toRemove)
				entry.Configurations.Remove (conf);
				
			foreach (ConfigurationData data in children)
				data.Update ();
		}
		
		public ItemConfiguration AddConfiguration (string name, string sourceName, bool createChildConfigurations)
		{
			ItemConfiguration conf = entry.CreateConfiguration (name);

			if (sourceName != null) {
				ItemConfiguration sc = configurations [sourceName];
				if (sc != null)
					conf.CopyFrom (sc);
			}
			
			if (entry is Solution) {
				SolutionConfiguration cc = (SolutionConfiguration) conf;
				foreach (ConfigurationData data in children) {
					SolutionConfigurationEntry ce = cc.AddItem ((SolutionEntityItem) data.Entry);
					if (createChildConfigurations) {
						ce.ItemConfiguration = name;
						if (data.Configurations [name] == null)
							data.AddConfiguration (name, sourceName, createChildConfigurations);
					} else {
						if (data.Configurations.Count > 0)
							ce.ItemConfiguration = data.Configurations [0].Id;
					}
				}
			}
			
			configurations.Add (conf);
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, null);
			return conf;
		}
		
		public void RemoveConfiguration (string name, bool removeChildConfigurations)
		{
			configurations.Remove (name);
			if (removeChildConfigurations) {
				foreach (ConfigurationData data in children)
					data.RemoveConfiguration (name, true);
			}
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, null);
		}
		
		public void RenameConfiguration (string oldName, string newName, bool renameChildConfigurations)
		{
			ItemConfiguration cc = configurations [oldName];
			if (cc != null) {
				ItemConfiguration nc = entry.CreateConfiguration (newName);
				nc.CopyFrom (cc);
				configurations.Remove (oldName);
				configurations.Add (nc);
				if (ConfigurationsChanged != null)
					ConfigurationsChanged (this, null);
			}
			if (renameChildConfigurations) {
				foreach (ConfigurationData data in children)
					data.RenameConfiguration (oldName, newName, true);
			}
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, null);
		}
		
		public event EventHandler ConfigurationsChanged;
	}
}
