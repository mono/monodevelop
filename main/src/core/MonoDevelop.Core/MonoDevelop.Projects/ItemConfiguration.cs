// ItemConfiguration.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	public class ItemConfiguration: IExtendedDataItem
	{
		string name = null;
		
		string platform;

		bool isImported = false;
		
		[ItemProperty ("CustomCommands", SkipEmpty = true)]
		[ItemProperty ("Command", Scope="*")]
		CustomCommandCollection customCommands = new CustomCommandCollection ();

		Hashtable properties;
		
		public ItemConfiguration ()
		{
		}
		
		public ItemConfiguration (string id)
		{
			ParseConfigurationId (id, out name, out platform);
		}
		
		public ItemConfiguration (string name, string platform)
		{
			this.name = name;
			this.platform = platform;
		}
		
		public static void ParseConfigurationId (string id, out string name, out string platform)
		{
			if (string.IsNullOrEmpty (id)) {
				name = "";
				platform = "";
				return;
			}
			
			int i = id.IndexOf ('|');
			if (i < 0) {
				name = id;
				platform = string.Empty;
			}
			else {
				name = id.Substring (0, i);
				platform = id.Substring (i+1);
			}
		}
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public string Id {
			get {
				if (string.IsNullOrEmpty (platform))
					return name;
				else
					return name + "|" + platform;
			}
			set {
				ParseConfigurationId (value, out name, out platform);
			}
		}
		
		public string Platform {
			get { return platform ?? string.Empty; }
			set { platform = value; }
		}

		public CustomCommandCollection CustomCommands {
			get { return customCommands; }
		}

		public bool IsImported {
			get { return isImported; }
			set { isImported = value; }
		}

		public object Clone()
		{
			ItemConfiguration conf = (ItemConfiguration) Activator.CreateInstance (GetType ());
			conf.CopyFrom (this);
			conf.name = name;
			conf.platform = platform;
			conf.isImported = isImported;
			return conf;
		}
		
		public virtual void CopyFrom (ItemConfiguration configuration)
		{
			ItemConfiguration other = (ItemConfiguration) configuration;
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
		
		public virtual ConfigurationSelector Selector {
			get { return new SolutionConfigurationSelector (Id); }
		}
	}
}
