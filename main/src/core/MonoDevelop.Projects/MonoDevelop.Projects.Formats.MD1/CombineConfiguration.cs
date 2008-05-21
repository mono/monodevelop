// CombineConfiguration.cs
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
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Projects;

namespace MonoDevelop.Projects.Formats.MD1
{
	class CombineConfiguration: ICustomDataItem
	{
		[ItemProperty]
		string name;
		
		[ExpandedCollection]
		[ItemProperty ("Entry", ValueType=typeof(CombineConfigurationEntry))]
		List<CombineConfigurationEntry> entries = new List<CombineConfigurationEntry> ();
		
		SolutionConfiguration solutionConfiguration;
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public List<CombineConfigurationEntry> Entries {
			get {
				return entries;
			}
			set {
				entries = value;
			}
		}

		public SolutionConfiguration SolutionConfiguration {
			get {
				return solutionConfiguration;
			}
			set {
				solutionConfiguration = value;
			}
		}
		
		public string GetMappedConfig (string itemName)
		{
			foreach (CombineConfigurationEntry ce in entries) {
				if (ce.Name == itemName) {
					if (ce.Build)
						return ce.Configuration;
					else
						return null;
				}
			}
			return null;
		}

		public DataCollection Serialize (ITypeSerializer handler)
		{
			DataCollection col = handler.Serialize (this);
			if (solutionConfiguration != null) {
				DataItem it = (DataItem) handler.SerializationContext.Serializer.Serialize (solutionConfiguration);
				col.Merge (it.ItemData);
			}
			return col;
		}

		public void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			DataItem item = new DataItem ();
			item.Name = "SolutionConfiguration";
			DataCollection col = item.ItemData;
			
			foreach (DataNode val in data) {
				if (val.Name != "name" && val.Name != "ctype" && val.Name != "Entry")
					col.Add (val);
			}
			
			handler.Deserialize (this, data);
			
			if (data.Count > 0) {
				solutionConfiguration = new SolutionConfiguration (name);
				handler.SerializationContext.Serializer.Deserialize (solutionConfiguration, item);
			}
		}
	}
}
