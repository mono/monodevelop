//
// DictionaryDataType.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MonoDevelop.Core.Serialization
{
	public class DictionaryDataType: DataType
	{
		MapData defaultData;
		
		protected class MapData
		{
			public string ItemName;
			public string KeyName;
			public string ValueName;
			public DataType KeyType;
			public DataType ValueType;
			public object KeyMapData;
			public object ValueMapData;
		}
		
		internal DictionaryDataType (Type type): base (type)
		{
		}
		
		public override bool IsSimpleType { get { return false; } }
		public override bool CanCreateInstance { get { return true; } }
		public override bool CanReuseInstance { get { return true; } }
		
		internal static bool IsDictionaryType (Type t)
		{
			if (!typeof(IDictionary).IsAssignableFrom (t))
				return false;
			Type keyType, valType;
			bool r = GetMapData (t, out keyType, out valType);
			return r;
		}
		
		internal static bool GetMapData (Type t, out Type keyType, out Type valueType)
		{
			keyType = valueType = null;
			MethodInfo met = t.GetMethod ("Add");
			if (met == null)
				return false;
			ParameterInfo[] args = met.GetParameters ();
			if (args.Length != 2)
				return false;
			keyType = args [0].ParameterType;
			valueType = args [1].ParameterType;
			return true;
		}
		
		internal protected override object GetMapData (object[] attributes, string scope)
		{
			Type keyType, valType;
			GetMapData (ValueType, out keyType, out valType);
			
			MapData data = new MapData ();
			data.KeyName = "Key";
			data.ValueName = "Value";
			data.ItemName = "Item";
			
			DataType keyDataType = null;
			DataType valueDataType = null;
			
			ItemPropertyAttribute at = FindPropertyAttribute (attributes, scope + "/key");
			if (at != null) {
				if (at.ValueType != null)
					keyType = at.ValueType;
				if (at.SerializationDataType != null)
					keyDataType = (DataType) Activator.CreateInstance (at.SerializationDataType, new object[] { keyType });
				if (!string.IsNullOrEmpty (at.Name))
					data.KeyName = at.Name;
			}
			if (keyDataType != null)
				data.KeyType = keyDataType;
			else
				data.KeyType = Context.GetConfigurationDataType (keyType);
			
			data.KeyMapData = data.KeyType.GetMapData (attributes, scope + "/key");
			
			at = FindPropertyAttribute (attributes, scope + "/value");
			if (at != null) {
				if (at.ValueType != null)
					valType = at.ValueType;
				if (at.SerializationDataType != null)
					valueDataType = (DataType) Activator.CreateInstance (at.SerializationDataType, new object[] { valType });
				if (!string.IsNullOrEmpty (at.Name))
					data.ValueName = at.Name;
			}
			if (valueDataType != null)
				data.ValueType = valueDataType;
			else
				data.ValueType = Context.GetConfigurationDataType (valType);
				
			data.ValueMapData = data.ValueType.GetMapData (attributes, scope + "/value");
			
			at = FindPropertyAttribute (attributes, scope + "/item");
			if (at != null && !string.IsNullOrEmpty (at.Name))
				data.ItemName = at.Name;
			
			return data;
		}
		
		protected virtual MapData GetDefaultData ()
		{
			if (defaultData != null) return defaultData;
			defaultData = new MapData ();
			
			Type keyType, valType;
			GetMapData (ValueType, out keyType, out valType);
			
			defaultData.KeyName = "Key";
			defaultData.ValueName = "Value";
			defaultData.ItemName = "Item";
			defaultData.KeyType = Context.GetConfigurationDataType (keyType);
			defaultData.ValueType = Context.GetConfigurationDataType (keyType);
			
			return defaultData;
		}

		internal protected override DataNode OnSerialize (SerializationContext serCtx, object mdata, object collection)
		{
			MapData mapData = (mdata != null) ? (MapData) mdata : GetDefaultData ();
			DataItem colItem = new DataItem ();
			colItem.Name = Name;
			colItem.UniqueNames = false;
			IDictionary dict = (IDictionary) collection;
			
			foreach (DictionaryEntry e in dict) {
				DataItem item = new DataItem ();
				item.Name = mapData.ItemName;
				item.UniqueNames = true;
				
				DataNode key = mapData.KeyType.Serialize (serCtx, null, e.Key);
				key.Name = mapData.KeyName;
				DataNode value = mapData.ValueType.Serialize (serCtx, null, e.Value);
				value.Name = mapData.ValueName;
				item.ItemData.Add (key);
				item.ItemData.Add (value);
				
				colItem.ItemData.Add (item);
			}
			return colItem;
		}
		
		internal protected override object OnDeserialize (SerializationContext serCtx, object mdata, DataNode data)
		{
			object col = Activator.CreateInstance (ValueType);
			Deserialize (serCtx, mdata, data, col);
			return col;
		}
		
		internal protected override void OnDeserialize (SerializationContext serCtx, object mdata, DataNode data, object collectionInstance)
		{
			MapData mapData = (mdata != null) ? (MapData) mdata : GetDefaultData ();
			
			DataCollection items = ((DataItem) data).ItemData;
			IDictionary dict = (IDictionary) collectionInstance;
			foreach (DataItem item in items) {
				DataNode key = item.ItemData [mapData.KeyName];
				if (key == null)
					continue;
				DataNode val = item.ItemData [mapData.ValueName];
				object keyObj = mapData.KeyType.Deserialize (serCtx, null, key);
				object valueObj = val != null ? mapData.ValueType.Deserialize (serCtx, null, val) : null;
				dict [keyObj] = valueObj;
			}
		}
	}
}
