//
// DataType.cs
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

namespace MonoDevelop.Core.Serialization
{
	public abstract class DataType
	{
		Type type;
		DataContext ctx;
		string name;
		
		public DataType (Type type)
		{
			this.type = type;
			name = GetTypeName (type);
		}
		
		string GetTypeName (Type type)
		{
			Type[] targs = type.GetGenericArguments ();
			if (targs != null && targs.Length > 0) {
				string name = type.Name;
				name = name.Substring (0, name.IndexOf ('`'));
				name += "Of";
				foreach (Type pt in targs)
					name += GetTypeName (pt);
				return name;
			} else
				return type.Name;
		}
		
		internal void SetContext (DataContext ctx)
		{
			this.ctx = ctx;
			Initialize ();
		}
		
		protected ItemPropertyAttribute FindPropertyAttribute (object[] attributes, string scope)
		{
			scope = scope.TrimStart ('/');
			
			foreach (object at in attributes) {
				ItemPropertyAttribute iat = at as ItemPropertyAttribute;
				if (iat != null && iat.Scope == scope) return iat;
			}
			return null;
		}

		protected DataContext Context {
			get { return ctx; }
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public Type ValueType {
			get { return type; }
		}
		
		protected virtual void Initialize ()
		{
		}
		
		internal protected virtual object GetMapData (object[] attributes, string scope)
		{
			return null;
		}
		
		public DataNode Serialize (SerializationContext serCtx, object mapData, object value)
		{
			return serCtx.Serializer.OnSerialize (this, serCtx, mapData, value);
		}
		
		public object Deserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			return serCtx.Serializer.OnDeserialize (this, serCtx, mapData, data);
		}
		
		public void Deserialize (SerializationContext serCtx, object mapData, DataNode data, object valueInstance)
		{
			serCtx.Serializer.OnDeserialize (this, serCtx, mapData, data, valueInstance);
		}
		
		public object CreateInstance (SerializationContext serCtx, DataNode data)
		{
			return serCtx.Serializer.OnCreateInstance (this, serCtx, data);
		}
		
		internal protected abstract DataNode OnSerialize (SerializationContext serCtx, object mapData, object value);
		internal protected abstract object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data);
		internal protected virtual void OnDeserialize (SerializationContext serCtx, object mapData, DataNode data, object valueInstance)
		{ throw new InvalidOperationException ("Could not create instance for type '" + ValueType + "'"); }
		
		internal protected virtual object OnCreateInstance (SerializationContext serCtx, DataNode data)
		{ throw new InvalidOperationException ("Could not create instance for type '" + ValueType + "'"); }
		
		public abstract bool IsSimpleType { get; }
		public abstract bool CanCreateInstance { get; }
		public abstract bool CanReuseInstance { get; }
	}
}
