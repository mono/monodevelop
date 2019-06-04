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
		string framework;

		[ItemProperty ("CustomCommands", SkipEmpty = true)]
		[ItemProperty ("Command", Scope="*")]
		CustomCommandCollection customCommands = new CustomCommandCollection ();

		Hashtable properties;
		
		public ItemConfiguration (string id)
		{
			ParseConfigurationId (id, out name, out platform);
		}
		
		public ItemConfiguration (string name, string platform)
			: this (name, platform, null)
		{
		}

		internal ItemConfiguration (string name, string platform, string framework)
		{
			this.name = name;
			this.platform = platform;
			this.framework = framework;
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
		}
		
		public string Id {
			get {
				bool hasPlatform = !string.IsNullOrEmpty (platform);
				bool hasFramework = !string.IsNullOrEmpty (framework);

				if (hasPlatform && hasFramework)
					return name + "|" + platform + "|" + framework;
				else if (hasPlatform)
					return name + "|" + platform;
				else if (hasFramework)
					return name + "||" + framework;
				else
					return name;
			}
		}
		
		public string Platform {
			get { return platform ?? string.Empty; }
		}

		public string Framework {
			get { return framework; }
		}

		public CustomCommandCollection CustomCommands {
			get { return customCommands; }
		}

		/// <summary>
		/// Copies the data of a configuration into this configuration
		/// </summary>
		/// <param name="configuration">Configuration from which to get the data.</param>
		/// <param name="isRename">If true, it means that the copy is being made as a result of a rename or clone operation. In this case,
		/// the overriden method may change the value of some properties that depend on the configuration name. For example, if the
		/// copied configuration is Debug and the OutputPath property has "bin/Debug" as value, then that value may be changed
		/// to match the new configuration name instead of keeping "bin/Debug"</param>
		public void CopyFrom (ItemConfiguration configuration, bool isRename = false)
		{
			OnCopyFrom (configuration, isRename);
		}

		protected virtual void OnCopyFrom (ItemConfiguration configuration, bool isRename)
		{
			if (configuration.properties != null) {
				properties = new Hashtable ();
				foreach (DictionaryEntry e in configuration.properties) {
					if (e.Value is ICloneable)
						properties [e.Key] = ((ICloneable)e.Value).Clone ();
					else
						properties [e.Key] = e.Value;
				}
			}
			else
				properties = null;
			customCommands = configuration.customCommands.Clone ();
		}
		
		public override string ToString()
		{
			return Id;
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
