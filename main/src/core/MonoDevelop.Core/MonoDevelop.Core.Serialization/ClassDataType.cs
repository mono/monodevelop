//
// ClassDataType.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MonoDevelop.Core.Serialization
{
	public class ClassDataType: DataType
	{
		Hashtable properties = new Hashtable ();
		List<ItemProperty> sortedPoperties = new List<ItemProperty> ();
		ArrayList subtypes;
		Type fallbackType;
		
		public ClassDataType (Type propType): base (propType)
		{
		}
		
		public override bool IsSimpleType { get { return false; } }
		public override bool CanCreateInstance { get { return true; } }
		public override bool CanReuseInstance { get { return true; } }
		
		protected override void Initialize ()
		{
			DataItemAttribute atd = (DataItemAttribute) Context.AttributeProvider.GetCustomAttribute (ValueType, typeof(DataItemAttribute), false);
			if (atd != null) {
				if (!string.IsNullOrEmpty (atd.Name)) {
					Name = atd.Name;
				}
				if (atd.FallbackType != null) {
					fallbackType = atd.FallbackType;
					if (!typeof(IExtendedDataItem).IsAssignableFrom (fallbackType))
						throw new InvalidOperationException ("Fallback type '" + fallbackType + "' must implement IExtendedDataItem");
					if (!ValueType.IsAssignableFrom (fallbackType))
						throw new InvalidOperationException ("Fallback type '" + fallbackType + "' must be a subclass of '" + ValueType + "'");
				}
			}
			
			object[] incs = Attribute.GetCustomAttributes (ValueType, typeof (DataIncludeAttribute), true);
			foreach (DataIncludeAttribute incat in incs) {
				Context.IncludeType (incat.Type);
			}
			
			if (ValueType.BaseType != null) {
				ClassDataType baseType = (ClassDataType) Context.GetConfigurationDataType (ValueType.BaseType);
				baseType.AddSubtype (this);
				int n=0;
				foreach (ItemProperty prop in baseType.Properties) {
					properties.Add (prop.Name, prop);
					sortedPoperties.Insert (n++, prop);
				}
				
				// Inherit the fallback type
				if (fallbackType == null && baseType.fallbackType != null)
					fallbackType = baseType.fallbackType;
			}

			foreach (Type interf in ValueType.GetInterfaces ()) {
				ClassDataType baseType = (ClassDataType) Context.GetConfigurationDataType (interf);
				baseType.AddSubtype (this);
			}
			
			MemberInfo[] members = ValueType.GetMembers (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (MemberInfo member in members) {
				if ((member is FieldInfo || member is PropertyInfo) && member.DeclaringType == ValueType) {
					Type memberType = member is FieldInfo ? ((FieldInfo)member).FieldType : ((PropertyInfo)member).PropertyType;
					AddProperty (member, member.Name, memberType);
				}
			}
			
			foreach (ItemMember member in Context.AttributeProvider.GetItemMembers (ValueType)) {
				AddProperty (member, member.Name, member.Type);
			}
			
			if (fallbackType != null)
				Context.IncludeType (fallbackType);
		}
		
		void AddProperty (object member, string name, Type memberType)
		{
			object[] ats = Context.AttributeProvider.GetCustomAttributes (member, typeof(Attribute), true);
			
			ItemPropertyAttribute at = FindPropertyAttribute (ats, "");
			if (at == null)
				return;
			
			ItemProperty prop = new ItemProperty ();
			prop.Name = (at.Name != null) ? at.Name : name;
			prop.ExpandedCollection = Context.AttributeProvider.IsDefined (member, typeof(ExpandedCollectionAttribute), true);
			prop.DefaultValue = at.DefaultValue;
			prop.IsExternal = at.IsExternal;

			if (prop.ExpandedCollection) {
				ICollectionHandler handler = Context.GetCollectionHandler (memberType);
				if (handler == null)
					throw new InvalidOperationException ("ExpandedCollectionAttribute can't be applied to property '" + prop.Name + "' in type '" + ValueType + "' becuase it is not a valid collection.");
					
				memberType = handler.GetItemType ();
				prop.ExpandedCollectionHandler = handler;
			}

			if (at.ValueType != null)
				prop.PropertyType = at.ValueType;
			else
				prop.PropertyType = memberType;
				
			if (at.SerializationDataType != null) {
				try {
					prop.DataType = (DataType) Activator.CreateInstance (at.SerializationDataType, new object[] { prop.PropertyType } );
				} catch (MissingMethodException ex) {
					throw new InvalidOperationException ("Constructor not found for custom data type: " + at.SerializationDataType.Name + " (Type propertyType);", ex);
				}
			}
			
			if (member is MemberInfo) {
				prop.Member = (MemberInfo) member;
				AddProperty (prop);
			} else {
				prop.InitValue = ((ItemMember)member).InitValue;
				AddProperty (prop, ((ItemMember)member).InsertBefore);
			}
			
			prop.Initialize (ats, "");
			
			if (prop.ExpandedCollection && prop.DataType.IsSimpleType)
				throw new InvalidOperationException ("ExpandedCollectionAttribute is not allowed in collections of simple types");
		}
		
		internal protected override object GetMapData (object[] attributes, string scope)
		{
			// We only need the fallback type for now

			ItemPropertyAttribute at = FindPropertyAttribute (attributes, scope);
			if (at != null)
				return at.FallbackType;
			else
				return null;
		}
		
		private void AddSubtype (ClassDataType subtype)
		{
			if (subtypes == null)
				subtypes = new ArrayList ();
			subtypes.Add (subtype); 
		}

		ICollection Properties {
			get { return sortedPoperties; }
		}
		
		public void AddProperty (ItemProperty prop)
		{
			AddProperty (prop, null);
		}
		
		void AddProperty (ItemProperty prop, string insertBefore)
		{
			if (!prop.IsNested) {
				foreach (ItemProperty p in sortedPoperties) {
					if (p.IsNested && p.NameList[0] == prop.Name)
						throw CreateNestedConflictException (prop, p);
				}
			} else {
				ItemProperty p = properties [prop.NameList[0]] as ItemProperty;
				if (p != null)
					throw CreateNestedConflictException (prop, p);
			}
			
			prop.SetContext (Context);
			if (properties.ContainsKey (prop.Name))
				throw new InvalidOperationException ("Duplicate property '" + prop.Name + "' in class '" + ValueType);
			
			properties.Add (prop.Name, prop);
			
			if (insertBefore != null) {
				bool added = false;
				for (int n=0; n<sortedPoperties.Count; n++) {
					ItemProperty p = sortedPoperties [n];
					if (p.MemberName == insertBefore) {
						sortedPoperties.Insert (n, prop);
						added = true;
						break;
					}
				}
				if (!added)
					sortedPoperties.Add (prop);
			}
			else if (prop.Unsorted) {
				int foundPos = sortedPoperties.Count;
				for (int n=0; n < sortedPoperties.Count; n++) {
					ItemProperty cp = (ItemProperty) sortedPoperties [n];
					if (cp.Unsorted && prop.Name.CompareTo (cp.Name) < 0) {
						foundPos = n;
						break;
					}
				}
				sortedPoperties.Insert (foundPos, prop);
			} else
				sortedPoperties.Add (prop);
			
			if (subtypes != null && subtypes.Count > 0) {
				foreach (ClassDataType subtype in subtypes)
					subtype.AddProperty (prop);
			}
		}
		
		public void RemoveProperty (string name)
		{
			ItemProperty prop = (ItemProperty) properties [name];
			if (prop == null)
				return;
			properties.Remove (name);
			sortedPoperties.Remove (prop);
			
			if (subtypes != null && subtypes.Count > 0) {
				foreach (ClassDataType subtype in subtypes)
					subtype.RemoveProperty (name);
			}
		}
		
		public IEnumerable<ItemProperty> GetProperties (SerializationContext serCtx, object instance)
		{
			foreach (ItemProperty prop in sortedPoperties) {
				if (serCtx.Serializer.CanHandleProperty (prop, serCtx, instance))
					yield return prop;
			}
		}
		
		Exception CreateNestedConflictException (ItemProperty p1, ItemProperty p2)
		{
			return new InvalidOperationException ("There is a conflict between the properties '" + p1.Name + "' and '" + p2.Name + "'. Nested element properties can't be mixed with normal element properties.");
		}
		
		internal protected override DataNode OnSerialize (SerializationContext serCtx, object mapData, object obj)
		{
			string ctype = null;
			
			if (obj.GetType () != ValueType) {
				if (obj is IExtendedDataItem) {
					// This is set by fallback types, to make sure the original type name is serialized back
					ctype = (string) ((IExtendedDataItem)obj).ExtendedProperties ["__raw_ctype"];
				}
				if (ctype == null) {
					DataType subtype = Context.GetConfigurationDataType (obj.GetType ());
					DataItem it = (DataItem) subtype.Serialize (serCtx, mapData, obj);
					it.ItemData.Add (new DataValue ("ctype", subtype.Name));
					it.Name = Name;
					return it;
				}
			} 
			
			DataItem item = new DataItem ();
			item.Name = Name;
			
			ICustomDataItem citem = Context.AttributeProvider.GetCustomDataItem (obj);
			if (citem != null) {
				ClassTypeHandler handler = new ClassTypeHandler (serCtx, this);
				item.ItemData = citem.Serialize (handler);
			}
			else
				item.ItemData = Serialize (serCtx, obj);
				
			if (ctype != null)
				item.ItemData.Add (new DataValue ("ctype", ctype));

			return item;
		}
		
		internal DataCollection Serialize (SerializationContext serCtx, object obj)
		{
			DataCollection itemCol = new DataCollection ();
			
			foreach (ItemProperty prop in Properties) {
				if (prop.ReadOnly || !prop.CanSerialize (serCtx, obj))
					continue;
				object val = prop.GetValue (obj);
				if (val == null)
					continue;
				if (val.Equals (prop.DefaultValue))
					continue;
				
				DataCollection col = itemCol;
				if (prop.IsNested)
					col = GetNestedCollection (col, prop.NameList, 0);
				
				if (prop.ExpandedCollection) {
					ICollectionHandler handler = prop.ExpandedCollectionHandler;
					object pos = handler.GetInitialPosition (val);
					while (handler.MoveNextItem (val, ref pos)) {
						object item = handler.GetCurrentItem (val, pos);
						if (item == null) continue;
						DataNode data = prop.Serialize (serCtx, obj, item);
						data.Name = prop.SingleName;
						col.Add (data);
					}
				}
				else {
					DataNode data = prop.Serialize (serCtx, obj, val);
					if (data == null)
						continue;
					// Don't write empty collections
					if (data is DataItem && !((DataItem)data).HasItemData && prop.DataType is CollectionDataType)
						continue;
					col.Add (data);
				}
			}
			
			if (obj is IExtendedDataItem) {
				// Serialize raw data which could not be deserialized
				DataItem uknData = (DataItem) ((IExtendedDataItem)obj).ExtendedProperties ["__raw_data"];
				if (uknData != null)
					itemCol.Merge (uknData.ItemData);
			}
			
			return itemCol;
		}
		
		DataCollection GetNestedCollection (DataCollection col, string[] nameList, int pos)
		{
			if (pos == nameList.Length - 1) return col;

			DataItem item = col [nameList[pos]] as DataItem;
			if (item == null) {
				item = new DataItem ();
				item.Name = nameList[pos];
				col.Add (item);
			}
			return GetNestedCollection (item.ItemData, nameList, pos + 1);
		}
		
		internal protected override object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			DataItem item = data as DataItem;
			if (item == null)
				throw new InvalidOperationException ("Invalid value found for type '" + Name + "' " + data);
				
			DataValue ctype = item ["ctype"] as DataValue;
			if (ctype != null && ctype.Value != Name) {
				bool isFallbackType;
				DataType stype = FindDerivedType (ctype.Value, mapData, out isFallbackType);
				
				if (isFallbackType) {
					// Remove the ctype attribute, to make sure it is not checked again
					// by the fallback type
					item.ItemData.Remove (ctype);
				}
				
				if (stype != null) {
					object sobj = stype.Deserialize (serCtx, mapData, data);
					
					// Store the original data type, so it can be serialized back
					if (isFallbackType && sobj is IExtendedDataItem) {
						((IExtendedDataItem)sobj).ExtendedProperties ["__raw_ctype"] = ctype.Value;
					}
						
					return sobj;
				}
				else
					throw new InvalidOperationException ("Type not found: " + ctype.Value);
			}
			
			ConstructorInfo ctor = ValueType.GetConstructor (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
			if (ctor == null) throw new InvalidOperationException ("Default constructor not found for type '" + ValueType + "'");

			object obj = ctor.Invoke (null);
			Deserialize (serCtx, null, item, obj);
			return obj;
		}
		
		internal protected override object OnCreateInstance (SerializationContext serCtx, DataNode data)
		{
			DataItem item = data as DataItem;
			if (item == null)
				throw new InvalidOperationException ("Invalid value found for type '" + Name + "'");
				
			DataValue ctype = item ["ctype"] as DataValue;
			if (ctype != null && ctype.Value != Name) {
				bool isFallbackType;
				DataType stype = FindDerivedType (ctype.Value, null, out isFallbackType);
				if (isFallbackType) {
					// Remove the ctype attribute, to make sure it is not checked again
					// by the fallback type
					item.ItemData.Remove (ctype);
				}
				if (stype != null) {
					object sobj = stype.CreateInstance (serCtx, data);
					// Store the original data type, so it can be serialized back
					if (isFallbackType && sobj is IExtendedDataItem)
						((IExtendedDataItem)sobj).ExtendedProperties ["__raw_ctype"] = ctype;
					return sobj;
				}
				else throw new InvalidOperationException ("Type not found: " + ctype.Value);
			}
			
			ConstructorInfo ctor = ValueType.GetConstructor (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
			if (ctor == null) throw new InvalidOperationException ("Default constructor not found for type '" + ValueType + "'");

			return ctor.Invoke (null);
		}
		
		protected internal override void OnDeserialize (SerializationContext serCtx, object mapData, DataNode data, object obj)
		{
			DataItem item = (DataItem) data;
			ICustomDataItem citem = Context.AttributeProvider.GetCustomDataItem (obj);
			if (citem != null) {
				ClassTypeHandler handler = new ClassTypeHandler (serCtx, this);
				citem.Deserialize (handler, item.ItemData);
			}
			else
				DeserializeNoCustom (serCtx, obj, item.ItemData);
		}
		
		internal void DeserializeNoCustom (SerializationContext serCtx, object obj, DataCollection itemData)
		{
			foreach (ItemProperty prop in Properties)
				if (!prop.CanDeserialize (serCtx, obj) && prop.DefaultValue != null)
					prop.SetValue (obj, prop.DefaultValue);
			
			// ukwnDataRoot is where to store values for which a property cannot be found.
			DataItem ukwnDataRoot = (obj is IExtendedDataItem) ? new DataItem () : null;
			
			Deserialize (serCtx, obj, itemData, ukwnDataRoot, "");
			
			// store unreadable raw data to a special property so it can be 
			// serialized back an the original format is kept
			if (ukwnDataRoot != null && ukwnDataRoot.HasItemData)
				((IExtendedDataItem)obj).ExtendedProperties ["__raw_data"] = ukwnDataRoot;
		}
		
		void Deserialize (SerializationContext serCtx, object obj, DataCollection itemData, DataItem ukwnDataRoot, string baseName)
		{
			Hashtable expandedCollections = null;
			
			foreach (DataNode value in itemData) {
				ItemProperty prop = (ItemProperty) properties [baseName + value.Name];
				if (prop == null) {
					if (value is DataItem) {
						DataItem root = new DataItem ();
						root.Name = value.Name;
						root.UniqueNames = ((DataItem)value).UniqueNames;
						if (ukwnDataRoot != null)
							ukwnDataRoot.ItemData.Add (root);
						Deserialize (serCtx, obj, ((DataItem)value).ItemData, root, baseName + value.Name + "/");

						// If no unknown data has been added, there is no need to keep this
						// in the unknown items list.
						if (!root.HasItemData)
							ukwnDataRoot.ItemData.Remove (root);
					}
					else if (obj is IExtendedDataItem && (value.Name != "ctype" || baseName.Length > 0)) {
						// store unreadable raw data to a special property so it can be 
						// serialized back an the original format is kept
						// The ctype attribute don't need to be stored for the root object, since
						// it is generated by the serializer
						ukwnDataRoot.ItemData.Add (value);
					}
					continue;
				}
				if (prop.WriteOnly || !prop.CanDeserialize (serCtx, obj))
					continue;
				
				try {
					if (prop.ExpandedCollection) {
						ICollectionHandler handler = prop.ExpandedCollectionHandler;
						if (expandedCollections == null) expandedCollections = new Hashtable ();
						
						object pos, col;
						if (!expandedCollections.ContainsKey (prop)) {
							col = handler.CreateCollection (out pos, -1);
						} else {
							pos = expandedCollections [prop];
							col = prop.GetValue (obj);
						}
						handler.AddItem (ref col, ref pos, prop.Deserialize (serCtx, obj, value));
						expandedCollections [prop] = pos;
						prop.SetValue (obj, col);
					}
					else {
						if (prop.HasSetter && prop.DataType.CanCreateInstance)
							prop.SetValue (obj, prop.Deserialize (serCtx, obj, value));
						else if (prop.DataType.CanReuseInstance) {
							object pval = prop.GetValue (obj);
							if (pval == null) {
								if (prop.HasSetter)
									throw new InvalidOperationException ("The property '" + prop.Name + "' is null and a new instance of '" + prop.PropertyType + "' can't be created.");
								else
									throw new InvalidOperationException ("The property '" + prop.Name + "' is null and it does not have a setter.");
							}
							prop.Deserialize (serCtx, obj, value, pval);
						} else {
							throw new InvalidOperationException ("The property does not have a setter.");
						}
					}
				}
				catch (Exception ex) {
					throw new InvalidOperationException ("Could not set property '" + prop.Name + "' in type '" + Name + "'", ex);
				}
			}
		}
		
		DataType FindDerivedType (string name, object mapData, out bool isFallbackType)
		{
			isFallbackType = false;
			if (subtypes != null) {
				foreach (ClassDataType stype in subtypes) {
					if (stype.Name == name) 
						return stype;
						
					bool fb;
					DataType cst = stype.FindDerivedType (name, null, out fb);
					if (cst != null && !fb) {
						isFallbackType = false;
						return cst;
					}
				}
			}
			if (mapData != null) {
				isFallbackType = true;
				return Context.GetConfigurationDataType ((Type)mapData);
			}
			
			if (fallbackType != null) {
				isFallbackType = true;
				return Context.GetConfigurationDataType (fallbackType);
			}
			else
				return null;
		}
	}
	
	internal class ClassTypeHandler: ITypeSerializer
	{
		SerializationContext ctx;
		ClassDataType cdt;
		
		internal ClassTypeHandler (SerializationContext ctx, ClassDataType cdt)
		{
			this.ctx = ctx;
			this.cdt = cdt;
		}
		
		public DataCollection Serialize (object instance)
		{
			return cdt.Serialize (ctx, instance);
		}
		
		public void Deserialize (object instance, DataCollection data)
		{
			cdt.DeserializeNoCustom (ctx, instance, data);
		}
		
		public SerializationContext SerializationContext {
			get { return ctx; }
		}
	}
}
