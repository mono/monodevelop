//
// DataContext.cs
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
	public class DataContext
	{
		Hashtable configurationTypes = new Hashtable ();
		
		public virtual SerializationContext CreateSerializationContext ()
		{
			return new SerializationContext ();
		}
		
		public DataNode SaveConfigurationData (SerializationContext serCtx, object obj, Type type)
		{
			if (type == null) type = obj.GetType ();
			DataType dataType = GetConfigurationDataType (type);
			return dataType.Serialize (serCtx, null, obj);
		}
		
		public object LoadConfigurationData (SerializationContext serCtx, Type type, DataNode data)
		{
			DataType dataType = GetConfigurationDataType (type);
			return dataType.Deserialize (serCtx, null, data);
		}
		
		public void SetConfigurationItemData (SerializationContext serCtx, object obj, DataItem data)
		{
			ClassDataType dataType = (ClassDataType) GetConfigurationDataType (obj.GetType ());
			dataType.SetConfigurationItemData (serCtx, obj, data);
		}
		
		public void RegisterProperty (Type targetType, string name, Type propertyType)
		{
			if (!typeof(IExtendedDataItem).IsAssignableFrom (targetType))
				throw new InvalidOperationException ("The type '" + targetType + "' does not implement the IExtendedDataItem interface and cannot be extended with new properties");
				
			ClassDataType ctype = (ClassDataType) GetConfigurationDataType (targetType);
			ItemProperty prop = new ItemProperty (name, propertyType);
			ctype.AddProperty (prop);
		}
		
		public void IncludeType (Type type)
		{
			GetConfigurationDataType (type);
		}
		
		public void SetTypeInfo (DataItem item, Type type)
		{
			item.ItemData.Add (new DataValue ("ctype", GetConfigurationDataType (type).Name));
		}
		
		public void RegisterProperty (Type targetType, ItemProperty property)
		{
			if (!typeof(IExtendedDataItem).IsAssignableFrom (targetType))
				throw new InvalidOperationException ("The type '" + targetType + "' does not implement the IExtendedDataItem interface and cannot be extended with new properties");
				
			ClassDataType ctype = (ClassDataType) GetConfigurationDataType (targetType);
			ctype.AddProperty (property);
		}
		
		internal protected DataType GetConfigurationDataType (Type type)
		{
			lock (configurationTypes) {
				DataType itemType = (DataType) configurationTypes [type];
				if (itemType != null) return itemType;
				itemType = CreateConfigurationDataType (type);
				configurationTypes [type] = itemType;
				itemType.SetContext (this);
				return itemType;
			}
		}
		
		protected virtual DataType CreateConfigurationDataType (Type type)
		{
			if (type.IsEnum)
				return new EnumDataType (type);
			else if (type.IsPrimitive || type == typeof(string))
				return new PrimitiveDataType (type);
			else {
				ICollectionHandler handler = GetCollectionHandler (type);
				if (handler != null)
					return new CollectionDataType (type, handler);
				else
					return CreateClassDataType (type);
			}
		}
		
		protected virtual DataType CreateClassDataType (Type type)
		{
			return new ClassDataType (type);
		}
		
		internal protected virtual ICollectionHandler GetCollectionHandler (Type type)
		{
			if (type.IsArray)
				return new ArrayHandler (type);
			else if (type == typeof(ArrayList))
				return ArrayListHandler.Instance;
			else
				return GenericCollectionHandler.CreateHandler (type);
		}
	}
}
