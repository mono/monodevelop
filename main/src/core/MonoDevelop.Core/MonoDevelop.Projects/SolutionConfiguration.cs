// 
// SolutionConfiguration.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.CodeDom.Compiler;

using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

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
		
		public override ConfigurationSelector Selector {
			get { return new SolutionConfigurationSelector (Id); }
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
					return entry.Build && item.Configurations [entry.ItemConfiguration] != null;
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
			string conf = FindMatchingConfiguration (item);
			return AddItem (item, conf != null, conf);
		}
		
		string FindMatchingConfiguration (SolutionEntityItem item)
		{
			SolutionItemConfiguration startupConfiguration = null;

			// There are no configurations so do nothing
			if (item.Configurations.Count == 0)
				return null;

			// Direct match if there's the same name and platform
			if (item.Configurations [Id] != null)
				return Id;

			// This configuration is not present in the project. Try to find the best match.
			// First of all try matching name
			foreach (SolutionItemConfiguration iconf in item.Configurations) {
				if (iconf.Name == Name && iconf.Platform == "")
					return iconf.Id;
			}

			// Run some heuristics based on the startup project if it exists
			if (ParentSolution != null && ParentSolution.StartupItem != null) {
				var startup = ParentSolution.StartupItem;
				startupConfiguration = startup.GetConfiguration (Selector);
				if (startupConfiguration != null) {
					var match = startupConfiguration.FindBestMatch (item.Configurations);
					if (match != null)
						return match.Id;
				}
			}
			if (Platform.Length > 0) {
				// No name coincidence, now try matching the platform
				foreach (SolutionItemConfiguration iconf in item.Configurations) {
					if (iconf.Platform == Platform)
						return iconf.Id;
				}
			}
			
			// Now match name, ignoring platform
			foreach (SolutionItemConfiguration iconf in item.Configurations) {
				if (iconf.Name == Name)
					return iconf.Id;
			}
			
			// No luck. Pick whatever.
			return item.Configurations [0].Id;
		}
		
		public SolutionConfigurationEntry AddItem (SolutionEntityItem item, bool build, string itemConfiguration)
		{
			if (itemConfiguration == null)
				itemConfiguration = Name;
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
			if (parentSolution == null)
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
		
		public ConfigurationSelector ItemConfigurationSelector {
			get { return new ItemConfigurationSelector (ItemConfiguration); }
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
