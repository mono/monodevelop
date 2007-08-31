//
// CombineOptionsDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.ComponentModel;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui.Dialogs;

namespace MonoDevelop.Projects.Gui.Dialogs
{
	public class CombineOptionsDialog : TreeViewOptions
	{
		Combine combine;
		
		ExtensionNode configurationNode;
		ConfigurationData configData;
		Gtk.TreeIter configIter;
	
		public CombineOptionsDialog (Gtk.Window parentWindow, Combine combine, ExtensionNode node, ExtensionNode configurationNode) : base (parentWindow, null, null)
		{
			this.combine = combine;
			this.configurationNode = configurationNode;
			this.Title = GettextCatalog.GetString ("Solution Options") + " - " + combine.Name;
			
			configData = ConfigurationData.Build (combine);
			configData.ConfigurationsChanged += new EventHandler (OnConfigChanged);
			
			properties = new Properties();
			properties.Set ("Combine", combine);
			properties.Set ("CombineEntry", combine);
			properties.Set ("CombineConfigData", configData);
			
			AddNodes (properties, Gtk.TreeIter.Zero, node.GetChildObjects (false));
			
			SelectFirstNode ();	
		}
		
		protected override bool StoreContents ()
		{
			if (base.StoreContents ())
				configData.Update ();
			return true;
		}
		
		void OnConfigChanged (object o, EventArgs a)
		{
			Gtk.TreeIter iter;
			if (treeStore.IterChildren (out iter, configIter)) {
				while (treeStore.Remove (ref iter));
			}
			FillConfigurations ();
			Gtk.TreePath path = treeStore.GetPath (configIter);
			TreeView.ExpandRow (path, false);
		}
		
		void FillConfigurations ()
		{
			foreach (IConfiguration config in configData.Configurations) {
				Properties configNodeProperties = new Properties();
				configNodeProperties.Set("Combine", combine);
				configNodeProperties.Set("CombineEntry", combine);
				configNodeProperties.Set("Config", config);
				configNodeProperties.Set ("CombineConfigData", configData);
				
				object[] list = configurationNode.GetChildObjects (false);
				if (list.Length > 1) {
					Gtk.TreeIter newNode = AddPath (config.Name, configIter);
					AddNodes (configNodeProperties, newNode, list);
				} else {
					AddNode (config.Name, configNodeProperties, configIter, (IDialogPanelDescriptor) list [0]);
				}
			}
		}
		
		protected override void AddChildNodes (object customizer, Gtk.TreeIter iter, IDialogPanelDescriptor descriptor)
		{
			if (descriptor.ID != "Configurations") {
				base.AddChildNodes (customizer, iter, descriptor);
			} else {
				configIter = iter;
				FillConfigurations ();
				TreeView.ExpandRow (treeStore.GetPath (configIter), false);
			}
		}
		
		protected override void OnSelectNode (Gtk.TreeIter iter, IDialogPanelDescriptor descriptor)
		{
			base.OnSelectNode (iter, descriptor);
			if (descriptor != null && descriptor.ID == "Configurations") {
				Gtk.TreePath path = treeStore.GetPath (iter);
				TreeView.ExpandRow (path, false);
			}
		}
	}
	
	class ConfigurationData
	{
		public ConfigurationCollection Configurations = new ConfigurationCollection ();
		public ArrayList Children = new ArrayList ();
		public CombineEntry Entry;
		
		public ConfigurationData (CombineEntry entry)
		{
			this.Entry = entry;
		}
		
		public static ConfigurationData Build (CombineEntry entry)
		{
			ConfigurationData data = new ConfigurationData (entry);
			foreach (IConfiguration conf in entry.Configurations) {
				IConfiguration copy = entry.CreateConfiguration (conf.Name);
				if (copy != null) {
					copy.CopyFrom (conf);
					data.Configurations.Add (copy);
				}
			}
			if (entry is Combine) {
				foreach (CombineEntry e in ((Combine)entry).Entries)
					data.Children.Add (ConfigurationData.Build (e));
			}
			return data;
		}
		
		public void Update ()
		{
			foreach (IConfiguration conf in Configurations) {
				IConfiguration old = Entry.Configurations [conf.Name];
				if (old != null) {
					old.CopyFrom (conf);
				} else {
					Entry.Configurations.Add (conf);
				}
			}
			ArrayList toRemove = new ArrayList ();
			foreach (IConfiguration conf in Entry.Configurations) {
				if (Configurations [conf.Name] == null)
					toRemove.Add (conf);
			}
			
			foreach (IConfiguration conf in toRemove)
				Entry.Configurations.Remove (conf);
				
			foreach (ConfigurationData data in Children)
				data.Update ();
		}
		
		public IConfiguration AddConfiguration (string name, string sourceName, bool createChildConfigurations)
		{
			IConfiguration conf = Entry.CreateConfiguration (name);
			if (sourceName != null) {
				IConfiguration sc = Configurations [sourceName];
				if (sc != null)
					conf.CopyFrom (sc);
			}
			
			if (Entry is Combine) {
				CombineConfiguration cc = (CombineConfiguration) conf;
				foreach (ConfigurationData data in Children) {
					CombineConfigurationEntry ce = cc.AddEntry (data.Entry);
					if (createChildConfigurations) {
						ce.ConfigurationName = name;
						if (data.Configurations [name] == null)
							data.AddConfiguration (name, sourceName, createChildConfigurations);
					} else {
						if (data.Configurations.Count > 0)
							ce.ConfigurationName = data.Configurations [0].Name;
					}
				}
			}
			
			Configurations.Add (conf);
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, null);
			return conf;
		}
		
		public void RemoveConfiguration (string name, bool removeChildConfigurations)
		{
			Configurations.Remove (name);
			if (removeChildConfigurations) {
				foreach (ConfigurationData data in Children)
					data.RemoveConfiguration (name, true);
			}
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, null);
		}
		
		public void RenameConfiguration (string oldName, string newName, bool renameChildConfigurations)
		{
			IConfiguration cc = Configurations [oldName];
			if (cc != null) {
				IConfiguration nc = Entry.CreateConfiguration (newName);
				nc.CopyFrom (cc);
				Configurations.Remove (oldName);
				Configurations.Add (nc);
				if (ConfigurationsChanged != null)
					ConfigurationsChanged (this, null);
			}
			if (renameChildConfigurations) {
				foreach (ConfigurationData data in Children)
					data.RenameConfiguration (oldName, newName, true);
			}
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, null);
		}
		
		public event EventHandler ConfigurationsChanged;
	}
}
