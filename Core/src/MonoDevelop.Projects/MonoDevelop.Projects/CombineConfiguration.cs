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

using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class CombineConfiguration : AbstractConfiguration
	{
		[ExpandedCollection]
		[ItemProperty ("Entry", ValueType=typeof(CombineConfigurationEntry))]
		ArrayList configurations = new ArrayList();
		Combine combine;
		
		public CombineConfiguration ()
		{
		}
		
		public CombineConfiguration (string name)
		{
			this.Name = name;
		}
		
		internal void SetCombine (Combine combine)
		{
			this.combine = combine;
			foreach (CombineConfigurationEntry conf in configurations) {
				conf.SetCombine (combine);
				if (conf.ConfigurationName == null)
					conf.ConfigurationName = Name;
			}
			if (combine != null)
				combine.UpdateActiveConfigurationTree ();
		}
		
		public ICollection Entries {
			get { return configurations; }
		}
		
		public CombineConfigurationEntry GetConfiguration(int number)
		{
			if (number < configurations.Count) {
				return (CombineConfigurationEntry)configurations[number];
			} 
			Debug.Assert(false, "Configuration number " + number + " not found.\n" + configurations.Count + " configurations avaiable.");
			return null;
		}
		
		public CombineConfigurationEntry AddEntry (CombineEntry combineEntry)
		{
			CombineConfigurationEntry conf = new CombineConfigurationEntry();
			conf.SetCombine (combine);
			conf.Entry = combineEntry;
			conf.ConfigurationName = combineEntry.ActiveConfiguration != null ? combineEntry.ActiveConfiguration.Name : String.Empty;
			conf.Build = true;
			configurations.Add(conf);
			if (combine != null)
				combine.UpdateActiveConfigurationTree ();
			return conf;
		}
		
		public void RemoveEntry (CombineEntry entry)
		{
			CombineConfigurationEntry removeConfig = null;
			
			foreach (CombineConfigurationEntry config in configurations) {
				if (config.Entry == entry) {
					removeConfig = config;
					break;
				}
			}
			
			Debug.Assert(removeConfig != null);
			configurations.Remove(removeConfig);
		}
		
		public override void CopyFrom (IConfiguration configuration)
		{
			base.CopyFrom (configuration);
			CombineConfiguration conf = (CombineConfiguration) configuration;
			
			configurations.Clear ();
			foreach (CombineConfigurationEntry cce in conf.configurations) {
				CombineConfigurationEntry nc = new CombineConfigurationEntry ();
				nc.SetCombine (combine);
				nc.Entry = cce.Entry;
				nc.ConfigurationName = cce.ConfigurationName;
				nc.Build = cce.Build;
				configurations.Add (nc);
			}
			if (combine != null)
				combine.UpdateActiveConfigurationTree ();
		}
	}
	
	public class CombineConfigurationEntry 
	{
		string entryName;
		string configName;
		Combine combine;
		
		[ItemProperty ("name")]
		internal string EntryName {
			get { return Entry != null ? Entry.Name : entryName; }
			set { entryName = value; }
		}
		
		public CombineEntry entry;
		
		[ItemProperty ("configuration")]
		public string ConfigurationName {
			get { return configName; }
			set { 
				configName = value;
				if (Entry != null && combine != null)
					combine.UpdateActiveConfigurationTree ();
			}
		}
		
		[ItemProperty ("build")]
		public bool Build;
		
		public CombineEntry Entry {
			get { return entry; }
			set { entry = value; if (entry != null) entryName = entry.Name; }
		}
		
		internal void SetCombine (Combine combine)
		{
			this.combine = combine;
			if (entryName != null && combine != null)
				Entry = combine.Entries [entryName];
		}
	}
}
