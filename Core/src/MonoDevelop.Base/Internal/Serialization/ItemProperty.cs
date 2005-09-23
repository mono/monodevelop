//
// ItemProperty.cs
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
using System.Reflection;

namespace MonoDevelop.Internal.Serialization
{
	public sealed class ItemProperty
	{
		string name;
		MemberInfo member;
		DataType dataType;
		Type propType;
		object defaultValue;
		DataContext ctx;
		object mapData;
		bool expandedCollection;
		ICollectionHandler expandedCollectionHandler;
		string[] nameList;
		bool readOnly;
		bool writeOnly;
		
		public ItemProperty ()
		{
		}
		
		public ItemProperty (string name, Type propType)
		{
			this.name = name;
			this.propType = propType;
			BuildNameList ();
		}
		
		void BuildNameList ()
		{
			if (name.IndexOf ('/') != -1) {
				nameList = name.Split ('/');
			}
		}
		
		internal void SetContext (DataContext ctx)
		{
			this.ctx = ctx;
			if (dataType == null) {
				if (propType == null) throw new InvalidOperationException ("Property type not specified");
				dataType = ctx.GetConfigurationDataType (propType);
			}
		}
		
		internal void Initialize (object[] attributes, int scope)
		{
			mapData = dataType.GetMapData (attributes, scope);
		}
		
		internal MemberInfo Member {
			get { return member; }
			set { CheckReadOnly (); member = value; }
		}
		
		public string Name {
			get { return name; }
			set { CheckReadOnly (); name = value; BuildNameList (); }
		}
		
		public object DefaultValue {
			get {
				// Workaround for a bug in mono 1.0 (enum values are encoded as ints in attributes)
				if (defaultValue != null && propType != null && propType.IsEnum && !(propType.IsInstanceOfType (defaultValue)))
					defaultValue = Enum.ToObject (propType, defaultValue);
				return defaultValue;
			}
			set { CheckReadOnly (); defaultValue = value; }
		}
		
		public Type PropertyType {
			get { return propType; }
			set { CheckReadOnly (); propType = value; }
		}
		
		public bool ExpandedCollection {
			get { return expandedCollection; }
			set { expandedCollection = value; }
		}
		
		internal ICollectionHandler ExpandedCollectionHandler {
			get { return expandedCollectionHandler; }
			set { expandedCollectionHandler = value; }
		}
		
		public bool ReadOnly {
			get { return readOnly; }
			set { readOnly = value; }
		}
		
		public bool WriteOnly {
			get { return writeOnly; }
			set { writeOnly = value; }
		}
		
		public DataType DataType {
			get { return dataType; }
			set { CheckReadOnly (); dataType = value; }
		}
		
		internal string[] NameList {
			get { return nameList; }
		}
		
		internal bool IsNested {
			get { return nameList != null; }
		}
		
		internal string SingleName {
			get { return nameList != null ? nameList [nameList.Length-1] : name; }
		}
		
		internal DataContext Context {
			get { return ctx; }
		}
		
		internal object GetValue (object obj)
		{
			if (member != null) {
				FieldInfo field = member as FieldInfo;
				if (field != null) return field.GetValue (obj);
				else return ((PropertyInfo)member).GetValue (obj, null);
			} else
				throw new InvalidOperationException ("Invalid object property");
		}

		internal void SetValue (object obj, object value)
		{
			if (member != null) {
				FieldInfo field = member as FieldInfo;
				if (field != null)
					field.SetValue (obj, value);
				else {
					PropertyInfo pi = member as PropertyInfo;
					pi.SetValue (obj, value, null);
				}
			} else
				throw new InvalidOperationException ("Invalid object property");
		}
		
		internal bool HasSetter {
			get { return member == null || (member is FieldInfo) || ((member is PropertyInfo) && ((PropertyInfo)member).CanWrite); }
		}

		internal DataNode Serialize (SerializationContext serCtx, object value)
		{
			DataNode data = dataType.Serialize (serCtx, mapData, value);
			if (data != null) data.Name = SingleName;
			return data;
		}
		
		internal object Deserialize (SerializationContext serCtx, DataNode data)
		{
			return dataType.Deserialize (serCtx, mapData, data);
		}
		
		internal void Deserialize (SerializationContext serCtx, DataNode data, object valueInstance)
		{
			dataType.Deserialize (serCtx, mapData, data, valueInstance);
		}
		
		void CheckReadOnly ()
		{
			if (ctx != null)
				throw new InvalidOperationException ("Property can't be modified, it is already bound to a configuration context");
		}
	}
}
