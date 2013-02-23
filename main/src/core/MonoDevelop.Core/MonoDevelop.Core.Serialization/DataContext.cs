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
using System.Collections.Generic;
using Mono.Addins;

namespace MonoDevelop.Core.Serialization
{
	public class DataContext
	{
		Dictionary<Type,DataType> configurationTypes = new Dictionary<Type,DataType> ();
		Dictionary<string,DataType> configurationTypesByName = new Dictionary<string,DataType> ();
		Dictionary<string, TypeRef> pendingTypes = new Dictionary<string, TypeRef> ();
		Dictionary<string, TypeRef> pendingTypesByTypeName = new Dictionary<string, TypeRef> ();
		ISerializationAttributeProvider attributeProvider;
		
		class TypeRef
		{
			public string TypeName;
			public RuntimeAddin Addin;
			public PropertyRef Properties;
			public DataType DataType;
			
			public TypeRef (RuntimeAddin addin, string typeName)
			{
				this.TypeName = typeName;
				this.Addin = addin;
			}
		}
		
		class PropertyRef
		{
			public string TargetType;
			public string Name;
			public string PropertyType;
			public bool IsExternal;
			public bool SkipEmpty;
			public PropertyRef Next;
			public RuntimeAddin Addin;
		
			public PropertyRef (RuntimeAddin addin, string targetType, string name, string propertyType, bool isExternal, bool skipEmpty)
			{
				this.Addin = addin;
				this.TargetType = targetType;
				this.Name = name;
				this.PropertyType = propertyType;
				this.IsExternal = isExternal;
				this.SkipEmpty = skipEmpty;
			}
		}
		
		public DataContext ()
		{
			attributeProvider = TypeAttributeProvider.Instance;
		}
		
		public DataContext (ISerializationAttributeProvider attributeProvider)
		{
			this.attributeProvider = attributeProvider;
		}
		
		public ISerializationAttributeProvider AttributeProvider {
			get {
				return attributeProvider;
			}
			set {
				attributeProvider = value;
			}
		}
		
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
			dataType.Deserialize (serCtx, null, data, obj);
		}
		
		public object CreateConfigurationData (SerializationContext serCtx, Type type, DataNode data)
		{
			DataType dataType = GetConfigurationDataType (type);
			return dataType.CreateInstance (serCtx, data);
		}
		
		public void RegisterProperty (Type targetType, string name, Type propertyType)
		{
			RegisterProperty (targetType, name, propertyType, true, false);
		}
		
		public void RegisterProperty (Type targetType, string name, Type propertyType, bool isExternal, bool skipEmpty)
		{
			if (!typeof(IExtendedDataItem).IsAssignableFrom (targetType))
				throw new InvalidOperationException ("The type '" + targetType + "' does not implement the IExtendedDataItem interface and cannot be extended with new properties");
				
			ClassDataType ctype = (ClassDataType) GetConfigurationDataType (targetType);
			ItemProperty prop = new ItemProperty (name, propertyType);
			prop.Unsorted = true;
			prop.IsExternal = isExternal;
			prop.SkipEmpty = skipEmpty;
			ctype.AddProperty (prop);
		}
		
		public void RegisterProperty (RuntimeAddin addin, string targetType, string name, string propertyType, bool isExternal, bool skipEmpty)
		{
			TypeRef tr;
			if (!pendingTypesByTypeName.TryGetValue (targetType, out tr)) {
				tr = new TypeRef (addin, targetType);
				pendingTypesByTypeName [targetType] = tr;
			}
			if (tr.DataType != null) {
				RegisterProperty (addin.GetType (targetType, true), name, addin.GetType (propertyType, true), isExternal, skipEmpty);
				return;
			}
			PropertyRef prop = new PropertyRef (addin, targetType, name, propertyType, isExternal, skipEmpty);
			if (tr.Properties == null)
				tr.Properties = prop;
			else {
				PropertyRef plink = tr.Properties;
				while (plink.Next != null)
					plink = plink.Next;
				plink.Next = prop;
			}
		}
		
		public void UnregisterProperty (Type targetType, string name)
		{
			ClassDataType ctype = (ClassDataType) GetConfigurationDataType (targetType);
			ctype.RemoveProperty (name);
		}
		
		public void UnregisterProperty (RuntimeAddin addin, string targetType, string name)
		{
			TypeRef tr;
			if (!pendingTypesByTypeName.TryGetValue (targetType, out tr))
				return;

			if (tr.DataType != null) {
				Type t = addin.GetType (targetType, false);
				if (t != null)
					UnregisterProperty (t, name);
				return;
			}
			
			PropertyRef prop = tr.Properties;
			PropertyRef prev = null;
			while (prop != null) {
				if (prop.Name == name) {
					if (prev != null)
						prev.Next = prop.Next;
					else
						tr.Properties = null;
					break;
				}
				prev = prop;
				prop = prop.Next;
			}
		}
		
		public IEnumerable<ItemProperty> GetProperties (SerializationContext serCtx, object instance)
		{
			ClassDataType ctype = (ClassDataType) GetConfigurationDataType (instance.GetType ());
			return ctype.GetProperties (serCtx, instance);
		}
		
		public void IncludeType (Type type)
		{
			GetConfigurationDataType (type);
		}
		
		public void IncludeType (RuntimeAddin addin, string typeName, string itemName)
		{
			if (string.IsNullOrEmpty (itemName)) {
				int i = typeName.LastIndexOf ('.');
				if (i >= 0)
					itemName = typeName.Substring (i + 1);
				else
					itemName = typeName;
			}
			TypeRef tr;
			if (!pendingTypesByTypeName.TryGetValue (typeName, out tr)) {
				tr = new TypeRef (addin, typeName);
				pendingTypesByTypeName [typeName] = tr;
			} else
				tr.Addin = addin;
			pendingTypes [itemName] = tr;
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
		
		public IEnumerable<DataType> DataTypes {
			get { return configurationTypes.Values; }
		}
		
		public virtual DataType GetConfigurationDataType (string typeName)
		{
			DataType dt;
			if (configurationTypesByName.TryGetValue (typeName, out dt))
				return dt;
			
			TypeRef tr;
			if (pendingTypes.TryGetValue (typeName, out tr)) {
				Type at = tr.Addin.GetType (tr.TypeName, true);
				dt = GetConfigurationDataType (at);
				tr.DataType = dt;
				return dt;
			}
			
			Type t = Type.GetType ("System." + typeName);
			if (t != null)
				return GetConfigurationDataType (t);
			return null;
		}
		
		public DataType GetConfigurationDataType (Type type)
		{
			lock (configurationTypes) {
				DataType itemType;
				if (!configurationTypes.TryGetValue (type, out itemType)) {
					if (itemType != null) return itemType;
					itemType = CreateConfigurationDataType (type);
					configurationTypes [type] = itemType;
					itemType.SetContext (this);
					configurationTypesByName [itemType.Name] = itemType;
					
					TypeRef tr;
					if (pendingTypesByTypeName.TryGetValue (type.FullName, out tr)) {
						tr.DataType = itemType;
						pendingTypes.Remove (itemType.Name);
						PropertyRef pr = tr.Properties;
						while (pr != null) {
							RegisterProperty (pr.Addin, tr.TypeName, pr.Name, pr.PropertyType, pr.IsExternal, pr.SkipEmpty);
							pr = pr.Next;
						}
						tr.Properties = null;
					}
				}
				return itemType;
			}
		}
		
		protected virtual DataType CreateConfigurationDataType (Type type)
		{
			if (type.IsEnum)
				return new EnumDataType (type);
			else if (type.IsPrimitive)
				return new PrimitiveDataType (type);
			else if (type == typeof(string))
				return new StringDataType ();
			else if (type == typeof(DateTime))
				return new DateTimeDataType ();
			else if (type == typeof(TimeSpan))
				return new TimeSpanDataType ();
			else if (type == typeof(FilePath))
				return new FilePathDataType ();
			else if (type == typeof(XmlElement))
				return new XmlElementDataType ();
			else if (DictionaryDataType.IsDictionaryType (type))
				return new DictionaryDataType (type);
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
