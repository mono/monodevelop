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
using System.Reflection;

namespace MonoDevelop.Internal.Serialization
{
	public class ClassDataType: DataType
	{
		Hashtable properties = new Hashtable ();
		ArrayList sortedPoperties = new ArrayList ();
		ArrayList subtypes;
		
		public ClassDataType (Type propType): base (propType)
		{
		}
		
		public override bool IsSimpleType { get { return false; } }
		public override bool CanCreateInstance { get { return true; } }
		public override bool CanReuseInstance { get { return true; } }
		
		protected override void Initialize ()
		{
			object[] incs = ValueType.GetCustomAttributes (typeof (DataIncludeAttribute), true);
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
			}

			foreach (Type interf in ValueType.GetInterfaces ()) {
				ClassDataType baseType = (ClassDataType) Context.GetConfigurationDataType (interf);
				baseType.AddSubtype (this);
			}
			
			MemberInfo[] members = ValueType.GetMembers (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (MemberInfo member in members) {
				if ((member is FieldInfo || member is PropertyInfo) && member.DeclaringType == ValueType) {
					object[] ats = member.GetCustomAttributes (true);
					
					ItemPropertyAttribute at = FindPropertyAttribute (ats, 0);
					if (at == null) continue;
					
					ItemProperty prop = new ItemProperty ();
					prop.Name = (at.Name != null) ? at.Name : member.Name;
					prop.ExpandedCollection = member.IsDefined (typeof(ExpandedCollectionAttribute), true);
					prop.DefaultValue = at.DefaultValue;
					Type memberType = member is FieldInfo ? ((FieldInfo)member).FieldType : ((PropertyInfo)member).PropertyType;

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
					
					prop.Member = member;
					AddProperty (prop);
					prop.Initialize (ats, 0);
					
					if (prop.ExpandedCollection && prop.DataType.IsSimpleType)
						throw new InvalidOperationException ("ExpandedCollectionAttribute is not allowed in collections of simple types");
				}
			}
		}
		
		private void AddSubtype (ClassDataType subtype)
		{
			if (subtypes == null) subtypes = new ArrayList (); 
			subtypes.Add (subtype); 
		}

		public ICollection Properties {
			get { return sortedPoperties; }
		}
		
		public void AddProperty (ItemProperty prop)
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
			sortedPoperties.Add (prop);
			
			if (subtypes != null && subtypes.Count > 0) {
				foreach (ClassDataType subtype in subtypes)
					subtype.AddProperty (prop);
			}
		}
		
		Exception CreateNestedConflictException (ItemProperty p1, ItemProperty p2)
		{
			return new InvalidOperationException ("There is a conflict between the properties '" + p1.Name + "' and '" + p2.Name + "'. Nested element properties can't be mixed with normal element properties.");
		}
		
		public override DataNode Serialize (SerializationContext serCtx, object mapData, object obj)
		{
			if (obj.GetType () != ValueType) {
				DataType subtype = Context.GetConfigurationDataType (obj.GetType ());
				DataItem it = (DataItem) subtype.Serialize (serCtx, mapData, obj);
				it.ItemData.Add (new DataValue ("ctype", subtype.Name));
				it.Name = Name;
				return it;
			} 
			
			DataItem item = new DataItem ();
			item.Name = Name;
			
			ICustomDataItem citem = obj as ICustomDataItem;
			if (citem != null) {
				ClassTypeHandler handler = new ClassTypeHandler (serCtx, this);
				item.ItemData = citem.Serialize (handler);
			}
			else
				item.ItemData = Serialize (serCtx, obj);
			return item;
		}
		
		internal DataCollection Serialize (SerializationContext serCtx, object obj)
		{
			DataCollection itemCol = new DataCollection ();
			
			foreach (ItemProperty prop in Properties) {
				if (prop.ReadOnly) continue;
				object val = GetPropValue (prop, obj);
				if (val == null) continue;
				if (val.Equals (prop.DefaultValue)) continue;
				DataCollection col = itemCol;
				if (prop.IsNested) col = GetNestedCollection (col, prop.NameList, 0);
				
				if (prop.ExpandedCollection) {
					ICollectionHandler handler = prop.ExpandedCollectionHandler;
					object pos = handler.GetInitialPosition (val);
					while (handler.MoveNextItem (val, ref pos)) {
						object item = handler.GetCurrentItem (val, pos);
						if (item == null) continue;
						DataNode data = prop.Serialize (serCtx, item);
						data.Name = prop.SingleName;
						col.Add (data);
					}
				}
				else {
					DataNode data = prop.Serialize (serCtx, val);
					if (data == null) continue;
					col.Add (data);
				}
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
		
		public override object Deserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			DataItem item = data as DataItem;
			if (item == null)
				throw new InvalidOperationException ("Invalid value found for type '" + Name + "'");
				
			DataValue ctype = item ["ctype"] as DataValue;
			if (ctype != null && ctype.Value != Name) {
				DataType stype = FindDerivedType (ctype.Value);
				if (stype != null) return stype.Deserialize (serCtx, mapData, data);
				else throw new InvalidOperationException ("Type not found: " + ctype.Value);
			}
			
			ConstructorInfo ctor = ValueType.GetConstructor (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
			if (ctor == null) throw new InvalidOperationException ("Default constructor not found for type '" + ValueType + "'");

			object obj = ctor.Invoke (null);
			SetConfigurationItemData (serCtx, obj, item);
			return obj;
		}
		
		public void SetConfigurationItemData (SerializationContext serCtx, object obj, DataItem item)
		{
			ICustomDataItem citem = obj as ICustomDataItem;
			if (citem != null) {
				ClassTypeHandler handler = new ClassTypeHandler (serCtx, this);
				citem.Deserialize (handler, item.ItemData);
			}
			else
				Deserialize (serCtx, obj, item.ItemData);
		}
		
		internal void Deserialize (SerializationContext serCtx, object obj, DataCollection itemData)
		{
			foreach (ItemProperty prop in Properties)
				if (prop.DefaultValue != null)
					SetPropValue (prop, obj, prop.DefaultValue);
			
			Deserialize (serCtx, obj, itemData, "");
		}
		
		void Deserialize (SerializationContext serCtx, object obj, DataCollection itemData, string baseName)
		{
			Hashtable expandedCollections = null;
			
			foreach (DataNode value in itemData) {
				ItemProperty prop = (ItemProperty) properties [baseName + value.Name];
				if (prop == null) {
					if (value is DataItem)
						Deserialize (serCtx, obj, ((DataItem)value).ItemData, baseName + value.Name + "/");
					continue;
				}
				if (prop.WriteOnly)
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
							col = GetPropValue (prop, obj);
						}
						handler.AddItem (ref col, ref pos, prop.Deserialize (serCtx, value));
						expandedCollections [prop] = pos;
						SetPropValue (prop, obj, col);
					}
					else {
						if (prop.HasSetter && prop.DataType.CanCreateInstance)
							SetPropValue (prop, obj, prop.Deserialize (serCtx, value));
						else if (prop.DataType.CanReuseInstance) {
							object pval = GetPropValue (prop, obj);
							if (pval == null) {
								if (prop.HasSetter)
									throw new InvalidOperationException ("The property '" + prop.Name + "' is null and a new instance of '" + prop.PropertyType + "' can't be created.");
								else
									throw new InvalidOperationException ("The property '" + prop.Name + "' is null and it does not have a setter.");
							}
							prop.Deserialize (serCtx, value, pval);
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
		
		void SetPropValue (ItemProperty prop, object obj, object value)
		{
			if (prop.Member != null)
				prop.SetValue (obj, value);
			else if (obj is IExtendedDataItem)
				((IExtendedDataItem)obj).ExtendedProperties [prop.Name] = value;
		}
		
		object GetPropValue (ItemProperty prop, object obj)
		{
			if (prop.Member != null)
				return prop.GetValue (obj);
			else if (obj is IExtendedDataItem)
				return ((IExtendedDataItem)obj).ExtendedProperties [prop.Name];
			else
				return null;
		}
		
		public DataType FindDerivedType (string name)
		{
			if (subtypes == null) return null;
			
			foreach (ClassDataType stype in subtypes) {
				if (stype.Name == name) return stype;
				DataType cst = stype.FindDerivedType (name);
				if (cst != null) return cst;
			}
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
			cdt.Deserialize (ctx, instance, data);
		}
		
		public SerializationContext SerializationContext {
			get { return ctx; }
		}
	}
}
