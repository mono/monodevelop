//
// CollectionDataType.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Internal.Serialization
{
	public class CollectionDataType: DataType
	{
		ICollectionHandler handler;
		MapData defaultData;
		
		protected class MapData
		{
			public string ItemName;
			public DataType ItemType;
			public object ItemMapData;
		}
		
		internal CollectionDataType (Type type, ICollectionHandler handler): base (type)
		{
			this.handler = handler;
		}
		
		public override bool IsSimpleType { get { return false; } }
		public override bool CanCreateInstance { get { return handler.CanCreateInstance; } }
		public override bool CanReuseInstance { get { return true; } }
		
		internal protected override object GetMapData (object[] attributes, int scope)
		{
			DataType itemDataType = null;
			Type itemType = null;
			
			ItemPropertyAttribute at = FindPropertyAttribute (attributes, scope + 1);
			if (at != null) {
				itemType = at.ValueType;
				if (at.SerializationDataType != null)
					itemDataType = (DataType) Activator.CreateInstance (at.SerializationDataType, new object[] { handler.GetItemType() });
			}
			
			if (itemType == null) itemType = handler.GetItemType ();
			if (itemDataType == null) itemDataType = Context.GetConfigurationDataType (itemType);

			object itemMapData = itemDataType.GetMapData (attributes, scope + 1);
			if (at == null && itemMapData == null) return null;

			MapData data = new MapData ();
			data.ItemType = itemDataType;
			data.ItemName = (at != null && at.Name != null) ? at.Name : itemDataType.Name;
			data.ItemMapData = itemMapData;
			return data;
		}
		
		protected virtual MapData GetDefaultData ()
		{
			if (defaultData != null) return defaultData;
			defaultData = new MapData ();
			defaultData.ItemType = Context.GetConfigurationDataType (handler.GetItemType ());
			defaultData.ItemName = defaultData.ItemType.Name;
			return defaultData;
		}

		public override DataNode Serialize (SerializationContext serCtx, object mdata, object collection)
		{
			MapData mapData = (mdata != null) ? (MapData) mdata : GetDefaultData ();
			DataItem item = new DataItem ();
			item.Name = Name;
			item.UniqueNames = false;
			object pos = handler.GetInitialPosition (collection);
			while (handler.MoveNextItem (collection, ref pos)) {
				object val = handler.GetCurrentItem (collection, pos);
				if (val == null) continue;
				DataNode data = mapData.ItemType.Serialize (serCtx, mapData.ItemMapData, val);
				data.Name = mapData.ItemName;
				item.ItemData.Add (data);
			}
			return item;
		}
		
		public override object Deserialize (SerializationContext serCtx, object mdata, DataNode data)
		{
			DataCollection items = ((DataItem) data).ItemData;
			object position;
			object collectionInstance = handler.CreateCollection (out position, items.Count);
			Deserialize (serCtx, mdata, items, collectionInstance, position);
			return collectionInstance;
		}
		
		public override void Deserialize (SerializationContext serCtx, object mdata, DataNode data, object collectionInstance)
		{
			DataCollection items = ((DataItem) data).ItemData;
			object position;
			handler.ResetCollection (collectionInstance, out position, items.Count);
			Deserialize (serCtx, mdata, items, collectionInstance, position);
		}
		
		void Deserialize (SerializationContext serCtx, object mdata, DataCollection items, object collectionInstance, object position)
		{
			MapData mapData = (mdata != null) ? (MapData) mdata : GetDefaultData ();
			
			foreach (DataNode val in items) {
				handler.AddItem (ref collectionInstance, ref position, mapData.ItemType.Deserialize (serCtx, mapData.ItemMapData, val));
			}
			handler.FinishCreation (ref collectionInstance, position);
		}
	}
}
