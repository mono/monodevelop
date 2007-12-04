//  AbstractConfiguration.cs
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
