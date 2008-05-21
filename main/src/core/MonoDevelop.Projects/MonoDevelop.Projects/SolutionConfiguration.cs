//  CombineConfiguration.cs
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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.CodeDom.Compiler;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class SolutionConfiguration : ItemConfiguration
	{
		Solution parentSolution;
		
		List<SolutionConfigurationEntry> configurations = new List<SolutionConfigurationEntry> ();
		
		public SolutionConfiguration ()
		{
		}
		
		public SolutionConfiguration (string id): base (id)
		{
		}
		
		public SolutionConfiguration (string name, string platform): base (name, platform)
		{
		}
		
		public Solution ParentSolution {
			get { return parentSolution; }
			internal set { parentSolution = value; }
		}
		
		public ReadOnlyCollection<SolutionConfigurationEntry> Configurations {
			get { return configurations.AsReadOnly (); }
		}
		
		public bool BuildEnabledForItem (SolutionEntityItem item)
		{
			foreach (SolutionConfigurationEntry entry in configurations) {
				if (entry.Item == item)
					return entry.Build;
			}
			return false;
		}
		
		public string GetMappedConfiguration (SolutionEntityItem item)
		{
			foreach (SolutionConfigurationEntry entry in configurations) {
				if (entry.Item == item)
					return entry.ItemConfiguration;
			}
			return null;
		}
		
		public SolutionConfigurationEntry GetEntryForItem (SolutionEntityItem item)
		{
			foreach (SolutionConfigurationEntry entry in configurations) {
				if (entry.Item == item)
					return entry;
			}
			return null;
		}
		
		public SolutionConfigurationEntry AddItem (SolutionEntityItem item)
		{
			return AddItem (item, true, Id);
		}
		
		public SolutionConfigurationEntry AddItem (SolutionEntityItem item, bool build, string itemConfiguration)
		{
			SolutionConfigurationEntry conf = new SolutionConfigurationEntry (this, item);
			conf.Build = build;
			conf.ItemConfiguration = itemConfiguration;
			configurations.Add (conf);
			if (parentSolution != null)
				parentSolution.UpdateDefaultConfigurations ();
			return conf;
		}
		
		public void RemoveItem (SolutionEntityItem item)
		{
			for (int n=0; n<configurations.Count; n++) {
				if (configurations [n].Item == item) {
					configurations.RemoveAt (n);
					return;
				}
			}
		}
		
		public override void CopyFrom (ItemConfiguration configuration)
		{
			base.CopyFrom (configuration);
			
			SolutionConfiguration conf = (SolutionConfiguration) configuration;
			parentSolution = conf.parentSolution;
			
			configurations.Clear ();
			foreach (SolutionConfigurationEntry entry in conf.configurations)
				configurations.Add (new SolutionConfigurationEntry (this, entry));

			if (parentSolution != null)
				parentSolution.UpdateDefaultConfigurations ();
		}
	}
	
	public class SolutionConfigurationEntry 
	{
		SolutionEntityItem item;
		SolutionConfiguration parentConfig;
		
		[ItemProperty ("name")]
		string itemId;
		
		[ItemProperty]
		string configuration;
		
		[ItemProperty (DefaultValue=true)]
		bool build = true;
		
		internal SolutionConfigurationEntry (SolutionConfiguration parentConfig, SolutionConfigurationEntry other)
		{
			this.parentConfig = parentConfig;
			this.itemId = other.itemId;
			this.configuration = other.configuration;
			this.build = other.build;
		}
		
		internal SolutionConfigurationEntry (SolutionConfiguration parentConfig, SolutionEntityItem item)
		{
			this.parentConfig = parentConfig;
			this.item = item;
			configuration = parentConfig.Id;
			itemId = item.ItemId;
		}
		
		public string ItemConfiguration {
			get { return configuration; }
			set { 
				configuration = value;
				if (parentConfig != null && parentConfig.ParentSolution != null)
					parentConfig.ParentSolution.UpdateDefaultConfigurations ();
			}
		}
		
		public bool Build {
			get { return build; }
			set { build = value; }
		}
		
		public SolutionEntityItem Item {
			get {
				if (item == null && parentConfig != null) {
					Solution sol = parentConfig.ParentSolution;
					if (sol != null)
						item = sol.GetSolutionItem (itemId) as SolutionEntityItem;
				}
				return item;
			}
		}
	}
}
