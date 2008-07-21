// PropertyBag.cs
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
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects
{
	[DataItem ("Properties")]
	public class PropertyBag: ICustomDataItem
	{
		Dictionary<string,object> properties;
		DataContext context;
		string sourceFile;
		
		public PropertyBag()
		{
		}
		
		public bool IsEmpty {
			get { return properties == null || properties.Count == 0; }
		}
		
		public T GetValue<T> (string name) where T:class
		{
			if (properties != null) {
				object val;
				if (properties.TryGetValue (name, out val)) {
					if (val is DataNode)
						val = Deserialize (name, (DataNode) val, typeof(T));
					return (T) val;
				}
			}
			return (new T[0])[0];
		}
		
		public T GetValue<T> (string name, T defaultValue)
		{
			if (properties != null) {
				object val;
				if (properties.TryGetValue (name, out val)) {
					if (val is DataNode)
						val = Deserialize (name, (DataNode) val, typeof(T));
					return (T) val;
				}
			}
			return defaultValue;
		}
		
		public void SetValue<T> (string name, T value)
		{
			if (properties == null)
				properties = new Dictionary<string,object> ();
			properties [name] = value;
		}
		
		public bool HasValue (string name)
		{
			return properties != null && properties.ContainsKey (name);
		}
		
		object Deserialize (string name, DataNode node, Type type)
		{
			if (type.IsAssignableFrom (typeof(XmlElement))) {
				// The xml content is the first child of the data node
				DataItem it = node as DataItem;
				if (it == null || it.ItemData.Count > 1)
					throw new InvalidOperationException ("Can't convert property to an XmlElement object.");
				if (it.ItemData.Count == 0)
					return null;
				XmlConfigurationWriter sw = new XmlConfigurationWriter ();
				XmlDocument doc = new XmlDocument ();
				return sw.Write (doc, it.ItemData [0]);
			}
			if (context == null)
				throw new InvalidOperationException ("Can't deserialize property '" + name + "'. Serialization context not set.");
			
			DataSerializer ser = new DataSerializer (context);
			ser.SerializationContext.BaseFile = sourceFile;
			object ob = ser.Deserialize (type, node);
			return ob;
		}

		DataCollection ICustomDataItem.Serialize (ITypeSerializer handler)
		{
			DataCollection data = new DataCollection ();
			foreach (KeyValuePair<string,object> entry in properties) {
				DataNode val;
				if (entry.Value is XmlElement) {
					DataItem xit = new DataItem ();
					XmlConfigurationReader sr = new XmlConfigurationReader ();
					xit.ItemData.Add (sr.Read ((XmlElement)entry.Value));
					val = xit;
				}
				else {
					val = handler.SerializationContext.Serializer.Serialize (entry.Value, typeof(object));
				}
				val.Name = entry.Key;
				data.Add (val);
			}
			return data;
		}

		void ICustomDataItem.Deserialize (ITypeSerializer handler, DataCollection data)
		{
			if (data.Count == 0)
				return;
			
			properties = new Dictionary<string,object> ();
			context = handler.SerializationContext.Serializer.DataContext;
			sourceFile = handler.SerializationContext.BaseFile;
			foreach (DataNode nod in data) {
				properties [nod.Name] = nod;
			}
		}
	}
}
