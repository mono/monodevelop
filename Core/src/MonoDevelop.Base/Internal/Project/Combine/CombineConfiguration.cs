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

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Serialization;

using MonoDevelop.Core.Properties;
using MonoDevelop.Gui;

namespace MonoDevelop.Internal.Project
{
	public class CombineConfiguration : AbstractConfiguration
	{
		[ExpandedCollection]
		[ItemProperty ("Entry", ValueType=typeof(CombineConfigurationEntry))]
		ArrayList configurations = new ArrayList();
		
		public CombineConfiguration ()
		{
		}
		
		public CombineConfiguration (string name)
		{
			this.Name = name;
		}
		
		internal void SetCombine (Combine combine)
		{
			foreach (CombineConfigurationEntry conf in configurations) {
				conf.SetCombine (combine);
				if (conf.ConfigurationName == null)
					conf.ConfigurationName = Name;
			}
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
		
		public CombineConfigurationEntry AddEntry (CombineEntry combine)
		{
			CombineConfigurationEntry conf = new CombineConfigurationEntry();
			conf.Entry = combine;
			conf.ConfigurationName = combine.ActiveConfiguration != null ? combine.ActiveConfiguration.Name : String.Empty;
			conf.Build = true;
			configurations.Add(conf);
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
				nc.Entry = cce.Entry;
				nc.ConfigurationName = cce.ConfigurationName;
				nc.Build = cce.Build;
				configurations.Add (nc);
			}
		}
	}
	
	[DataItem ("Entry")]
	public class CombineConfigurationEntry 
	{
		string entryName;
		
		[ItemProperty ("name")]
		internal string EntryName {
			get { return Entry != null ? Entry.Name : entryName; }
			set { entryName = value; }
		}
		
		public CombineEntry entry;
		
		[ItemProperty ("configuration")]
		public string ConfigurationName;
		
		[ItemProperty ("build")]
		public bool Build;
		
		public CombineEntry Entry {
			get { return entry; }
			set { entry = value; if (entry != null) entryName = entry.Name; }
		}
		
		internal void SetCombine (Combine combine)
		{
			if (entryName != null)
				Entry = combine.Entries [entryName];
		}
	}
}
