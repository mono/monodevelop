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
using System.Text;
using System.Globalization;
using System.Collections.Immutable;

namespace MonoDevelop.Core
{
	[DataItem ("Properties")]
	public sealed class PropertyBag: ICustomDataItem, IDisposable
	{
		ImmutableDictionary<string,object> properties = ImmutableDictionary<string,object>.Empty;
		DataContext context;
		string sourceFile;
		bool isShared;

		class DataNodeInfo
		{
			public DataNode DataNode;
			public object Object;
		}

		public PropertyBag()
		{
		}
		
		void AssertMainThread ()
		{
			if (isShared)
				Runtime.AssertMainThread ();
		}

		public void SetShared ()
		{
			isShared = true;
		}

		public bool IsEmpty {
			get { return properties.Count == 0; }
		}
		
		public T GetValue<T> ()
		{
			return GetValue<T> (typeof(T).FullName);
		}
		
		public T GetValue<T> (string name)
		{
			return GetValue<T> (name, (DataContext) null);
		}
		
		public T GetValue<T> (string name, DataContext ctx)
		{
			return GetValue<T> (name, default(T), ctx);
		}
		
		public T GetValue<T> (string name, T defaultValue)
		{
			return GetValue<T> (name, defaultValue, null);
		}
		
		public T GetValue<T> (string name, T defaultValue, DataContext ctx)
		{
			if (properties != null) {
				object val;
				if (properties.TryGetValue (name, out val)) {
					var di = val as DataNodeInfo;
					if (di != null) {
						if (di.Object == null) {
							di.Object = Deserialize (name, di.DataNode, typeof(T), ctx ?? context);
							di.DataNode = null;
						}
						val = di.Object;
					}
					return (T) val;
				}
			}
			return defaultValue;
		}
		
		public void SetValue<T> (T value)
		{
			SetValue<T> (typeof(T).FullName, value);
		}
		
		public void SetValue<T> (string name, T value)
		{
			AssertMainThread ();
			properties = properties.SetItem (name, value);
			OnChanged (name);
		}
		
		public bool RemoveValue<T> ()
		{
			return RemoveValue (typeof(T).FullName);
		}
		
		public bool RemoveValue (string name)
		{
			AssertMainThread ();
			var cc = properties.Count;

			properties = properties.Remove (name);
			if (cc == properties.Count)
				return false;

			OnChanged (name);
			return true;
		}
		
		public bool HasValue<T> ()
		{
			return HasValue (typeof(T).FullName);
		}
		
		public bool HasValue (string name)
		{
			return properties.ContainsKey (name);
		}

		public event EventHandler<PropertyBagChangedEventArgs> Changed;

		void OnChanged (string name)
		{
			var handler = Changed;

			if (handler != null)
				handler (this, new PropertyBagChangedEventArgs (name));
		}
		
		public void Dispose ()
		{
			AssertMainThread ();
			foreach (object ob in properties.Values) {
				IDisposable disp = ob as IDisposable;
				if (disp != null)
					disp.Dispose ();
			}
			properties = properties.Clear ();
		}
		
		object Deserialize (string name, DataNode node, Type type, DataContext ctx)
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
			if (ctx == null)
				throw new InvalidOperationException ("Can't deserialize property '" + name + "'. Serialization context not set.");
			
			DataSerializer ser = new DataSerializer (ctx);
			ser.SerializationContext.BaseFile = sourceFile;
			object ob = ser.Deserialize (type, node);
			return ob;
		}

		DataCollection ICustomDataItem.Serialize (ITypeSerializer handler)
		{
			DataCollection data = new DataCollection ();

			if (IsEmpty)
				return data;

			foreach (KeyValuePair<string,object> entry in properties) {
				DataNode val;
				if (entry.Value == null)
					continue;
				else if (entry.Value is XmlElement) {
					DataItem xit = new DataItem ();
					XmlConfigurationReader sr = new XmlConfigurationReader ();
					xit.ItemData.Add (sr.Read ((XmlElement)entry.Value));
					val = xit;
				}
				else if (entry.Value is DataNode) {
					val = (DataNode) entry.Value;
				} else if (entry.Value is DataNodeInfo) {
					var di = (DataNodeInfo)entry.Value;
					if (di.DataNode != null)
						val = di.DataNode;
					else if (di.Object != null) {
						val = handler.SerializationContext.Serializer.Serialize (di.Object, di.Object.GetType ());
					} else
						continue;
				} else {
					val = handler.SerializationContext.Serializer.Serialize (entry.Value, entry.Value.GetType ());
				}
				val.Name = EscapeName (entry.Key);
				data.Add (val);
			}
			return data;
		}

		void ICustomDataItem.Deserialize (ITypeSerializer handler, DataCollection data)
		{
			if (data.Count == 0)
				return;

			var propertiesBuilder = ImmutableDictionary.CreateBuilder<string, object> ();
			context = handler.SerializationContext.Serializer.DataContext;
			sourceFile = handler.SerializationContext.BaseFile;
			foreach (DataNode nod in data) {
				if (nod.Name != "ctype")
					propertiesBuilder.Add (UnescapeName (nod.Name), new DataNodeInfo { DataNode = nod });
			}

			properties = propertiesBuilder.ToImmutable ();
		}
		
		string EscapeName (string str)
		{
			StringBuilder sb = StringBuilderCache.Allocate ();
			for (int n=0; n<str.Length; n++) {
				char c = str [n];
				if (c == '_')
					sb.Append ("__");
				else if (c != '.' && c != '-' && !char.IsLetter (c) && (!char.IsNumber (c) || n==0)) {
					string s = ((int)c).ToString ("X");
					sb.Append ("_").Append (s.Length.ToString ());
					sb.Append (s);
				}
				else
					sb.Append (c);
			}
			return StringBuilderCache.ReturnAndFree (sb);
		}
		
		string UnescapeName (string str)
		{
			StringBuilder sb = StringBuilderCache.Allocate ();
			for (int n=0; n<str.Length; n++) {
				char c = str [n];
				if (c == '_') {
					if (n + 1 >= str.Length)
						return StringBuilderCache.ReturnAndFree (sb);
					if (str [n + 1] == '_') {
						sb.Append (c);
						n++;
					} else {
						int len = int.Parse (str.Substring (n+1,1));
						if (n + 2 + len - 1 >= str.Length)
							return StringBuilderCache.ReturnAndFree (sb);
						int ic;
						if (int.TryParse (str.Substring (n + 2, len), NumberStyles.HexNumber, null, out ic))
							sb.Append ((char)ic);
						n+=len+1;
					}
				} else
					sb.Append (c);
			}
			return StringBuilderCache.ReturnAndFree (sb);
		}
	}

	public class PropertyBagChangedEventArgs : EventArgs
	{
		public string PropertyName { get; private set; }

		public PropertyBagChangedEventArgs (string name)
		{
			PropertyName = name;
		}
	}
}
