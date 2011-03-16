//
// DataSerializer.cs
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

namespace MonoDevelop.Core.Serialization
{
	public class DataSerializer
	{
		SerializationContext serializationContext;
		DataContext dataContext;
		
		public DataSerializer (DataContext ctx)
		{
			dataContext = ctx;
			serializationContext = ctx.CreateSerializationContext ();
			serializationContext.Serializer = this;
		}
		
		public DataSerializer (DataContext ctx, string baseFile)
		{
			dataContext = ctx;
			serializationContext = ctx.CreateSerializationContext ();
			serializationContext.BaseFile = baseFile;
			serializationContext.Serializer = this;
		}
		
		public SerializationContext SerializationContext {
			get { return serializationContext; }
		}
		
		public DataContext DataContext {
			get { return dataContext; }
		}
		
		public DataNode Serialize (object obj)
		{
			return dataContext.SaveConfigurationData (serializationContext, obj, null);
		}
		
		public DataNode Serialize (object obj, Type type)
		{
			return dataContext.SaveConfigurationData (serializationContext, obj, type);
		}
		
		public object Deserialize (Type type, DataNode data)
		{
			return dataContext.LoadConfigurationData (serializationContext, type, data);
		}
		
		public void Deserialize (object obj, DataItem data)
		{
			dataContext.SetConfigurationItemData (serializationContext, obj, data);
		}
		
		public object CreateInstance (Type type, DataItem data)
		{
			return dataContext.CreateConfigurationData (serializationContext, type, data);
		}
		
		public IEnumerable<ItemProperty> GetProperties (object instance)
		{
			return dataContext.GetProperties (serializationContext, instance);
		}
		
		internal protected virtual DataNode OnSerialize (DataType dataType, SerializationContext serCtx, object mapData, object value)
		{
			return dataType.OnSerialize (serCtx, mapData, value);
		}
		
		internal protected virtual object OnDeserialize (DataType dataType, SerializationContext serCtx, object mapData, DataNode data)
		{
			return dataType.OnDeserialize (serCtx, mapData, data);
		}
		
		internal protected virtual void OnDeserialize (DataType dataType, SerializationContext serCtx, object mapData, DataNode data, object valueInstance)
		{
			dataType.OnDeserialize (serCtx, mapData, data, valueInstance);
		}
		
		internal protected virtual object OnCreateInstance (DataType dataType, SerializationContext serCtx, DataNode data)
		{
			return dataType.OnCreateInstance (serCtx, data);
		}
		
		internal protected virtual DataNode OnSerializeProperty (ItemProperty prop, SerializationContext serCtx, object instance, object value)
		{
			return prop.OnSerialize (serCtx, value);
		}
		
		internal protected virtual object OnDeserializeProperty (ItemProperty prop, SerializationContext serCtx, object instance, DataNode data)
		{
			return prop.OnDeserialize (serCtx, data);
		}
		
		internal protected virtual void OnDeserializeProperty (ItemProperty prop, SerializationContext serCtx, object instance, DataNode data, object valueInstance)
		{
			prop.OnDeserialize (serCtx, data, valueInstance);
		}
		
		internal protected virtual bool CanSerializeProperty (ItemProperty property, SerializationContext serCtx, object instance)
		{
			return CanHandleProperty (property, serCtx, instance);
		}
		
		internal protected virtual bool CanDeserializeProperty (ItemProperty property, SerializationContext serCtx, object instance)
		{
			return CanHandleProperty (property, serCtx, instance);
		}
		
		internal protected virtual bool CanHandleProperty (ItemProperty property, SerializationContext serCtx, object instance)
		{
			return true;
		}
	}
}
