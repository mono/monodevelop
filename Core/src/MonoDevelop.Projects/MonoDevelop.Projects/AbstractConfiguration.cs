// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using System.Collections;

namespace MonoDevelop.Projects
{
	public abstract class AbstractConfiguration : IConfiguration, IExtendedDataItem
	{
		[ItemProperty("name")]
		string name = null;
		
		[ItemProperty ("CustomCommands")]
		[ItemProperty ("Command", Scope=1)]
		CustomCommandCollection customCommands = new CustomCommandCollection ();

		Hashtable properties;
		
		public string Name {
			get { return name; }
			set { name = value; }
		}

		public CustomCommandCollection CustomCommands {
			get { return customCommands; }
		}
		
		public object Clone()
		{
			IConfiguration conf = (IConfiguration) MemberwiseClone ();
			conf.CopyFrom (this);
			return conf;
		}
		
		public virtual void CopyFrom (IConfiguration configuration)
		{
			AbstractConfiguration other = (AbstractConfiguration) configuration;
			if (other.properties != null) {
				properties = new Hashtable ();
				foreach (DictionaryEntry e in other.properties) {
					if (e.Value is ICloneable)
						properties [e.Key] = ((ICloneable)e.Value).Clone ();
					else
						properties [e.Key] = e.Value;
				}
			}
			else
				properties = null;
			customCommands = other.customCommands.Clone ();
		}
		
		public override string ToString()
		{
			return name;
		}
		
		public IDictionary ExtendedProperties {
			get {
				if (properties == null) properties = new Hashtable ();
				return properties;
			}
		}
	}
}
