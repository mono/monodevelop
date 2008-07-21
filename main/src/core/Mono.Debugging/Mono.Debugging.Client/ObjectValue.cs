// ObjectValue.cs
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
using System.Text;
using Mono.Debugging.Backend;
using System.Collections.Generic;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class ObjectValue
	{
		ObjectPath path;
		int arrayCount = -1;
		string name;
		string value;
		string typeName;
		ObjectValueFlags flags;
		IObjectValueSource source;
		List<ObjectValue> children;
		
		static ObjectValue Create (IObjectValueSource source, ObjectPath path, string typeName)
		{
			ObjectValue ob = new ObjectValue ();
			ob.source = source;
			ob.path = path;
			ob.typeName = typeName;
			return ob;
		}
		
		public static ObjectValue CreateObject (IObjectValueSource source, ObjectPath path, string typeName, string value, ObjectValueFlags flags, ObjectValue[] children)
		{
			ObjectValue ob = Create (source, path, typeName);
			ob.path = path;
			ob.flags = flags | ObjectValueFlags.Object;
			ob.value = value;
			if (children != null) {
				ob.children = new List<ObjectValue> ();
				ob.children.AddRange (children);
			}
			return ob;
		}
		
		public static ObjectValue CreateNullObject (string name, string typeName, ObjectValueFlags flags)
		{
			ObjectValue ob = Create (null, new ObjectPath (name), typeName);
			ob.flags = flags | ObjectValueFlags.Object;
			ob.value = "(null)";
			return ob;
		}
		
		public static ObjectValue CreatePrimitive (IObjectValueSource source, ObjectPath path, string typeName, string value, ObjectValueFlags flags)
		{
			ObjectValue ob = Create (source, path, typeName);
			ob.flags = flags | ObjectValueFlags.Primitive;
			ob.value = value;
			return ob;
		}
		
		public static ObjectValue CreateArray (IObjectValueSource source, ObjectPath path, string typeName, int arrayCount, ObjectValueFlags flags, ObjectValue[] children)
		{
			ObjectValue ob = Create (source, path, typeName);
			ob.arrayCount = arrayCount;
			ob.flags = flags | ObjectValueFlags.Array;
			ob.value = "[" + arrayCount + "]";
			if (children != null) {
				ob.children = new List<ObjectValue> ();
				ob.children.AddRange (children);
			}
			return ob;
		}
		
		public static ObjectValue CreateUnknown (IObjectValueSource source, ObjectPath path, string typeName)
		{
			ObjectValue ob = Create (source, path, typeName);
			ob.flags = ObjectValueFlags.Unknown | ObjectValueFlags.ReadOnly;
			return ob;
		}
		
		public static ObjectValue CreateUnknown (string name)
		{
			return CreateUnknown (null, new ObjectPath (name), "");
		}
		
		public static ObjectValue CreateError (IObjectValueSource source, ObjectPath path, string typeName, string value, ObjectValueFlags flags)
		{
			ObjectValue ob = Create (source, path, typeName);
			ob.path = path;
			ob.flags = flags | ObjectValueFlags.Error;
			ob.value = value;
			return ob;
		}
		
		public static ObjectValue CreateError (string name, string message, ObjectValueFlags flags)
		{
			ObjectValue ob = new ObjectValue ();
			ob.flags = flags | ObjectValueFlags.Error;
			ob.value = message;
			ob.name = name;
			return ob;
		}
		
		public ObjectValueFlags Flags {
			get { return flags; }
		}
		
		public string Name {
			get {
				if (name == null)
					return path [path.Length - 1];
				else
					return name;
			}
			set {
				name = value;
			}
		}
		
		public virtual string Value {
			get {
				return value;
			}
			set {
				if (IsReadOnly || source == null)
					throw new InvalidOperationException ("Value is not editable");
				this.value = source.SetValue (path, value);
			}
		}
		
		public string TypeName {
			get { return typeName; }
		}
		
		public bool HasChildren {
			get {
				if (children != null)
					return children.Count > 0;
				else if (source == null)
					return false;
				else if (IsArray)
					return arrayCount > 0;
				else if (IsObject)
					return true;
				else
					return false;
			}
		}
		
		public ObjectValue GetChild (string name)
		{
			if (IsArray)
				throw new InvalidOperationException ("Object is an array.");
			
			if (children == null) {
				children = new List<ObjectValue> ();
				try {
					children.AddRange (source.GetChildren (path, -1, -1));
				} catch (Exception ex) {
					children = null;
					return CreateError ("", ex.Message, ObjectValueFlags.ReadOnly);
				}
			}
			foreach (ObjectValue ob in children)
				if (ob.Name == name)
					return ob;
			return null;
		}
		
		public ObjectValue[] GetAllChildren ()
		{
			if (IsArray) {
				GetArrayItem (arrayCount - 1);
				return children.ToArray ();
			} else {
				if (children == null) {
					children = new List<ObjectValue> ();
					try {
						children.AddRange (source.GetChildren (path, -1, -1));
					} catch (Exception ex) {
						children.Add (CreateError ("", ex.Message, ObjectValueFlags.ReadOnly));
					}
				}
				return children.ToArray ();
			}
		}
		
		public ObjectValue GetArrayItem (int index)
		{
			if (!IsArray)
				throw new InvalidOperationException ("Object is not an array.");
			if (index >= arrayCount || index < 0)
				throw new IndexOutOfRangeException ();
			
			if (children == null)
				children = new List<ObjectValue> ();
			if (index >= children.Count) {
				int nc = (index + 50);
				if (nc > arrayCount) nc = arrayCount;
				nc = nc - children.Count;
				try {
					ObjectValue[] items = source.GetChildren (path, children.Count, nc);
					children.AddRange (items);
				} catch (Exception ex) {
					return CreateError ("", ex.Message, ObjectValueFlags.ArrayElement | ObjectValueFlags.ReadOnly);
				}
			}
			return children [index];
		}
		
		public int ArrayCount {
			get {
				if (!IsArray)
					throw new InvalidOperationException ("Object is not an array.");
				return arrayCount; 
			}
		}

		public bool IsReadOnly {
			get { return HasFlag (ObjectValueFlags.ReadOnly); }
		}
		
		public bool IsArray {
			get { return HasFlag (ObjectValueFlags.Array); }
		}
		
		public bool IsObject {
			get { return HasFlag (ObjectValueFlags.Object); }
		}
		
		public bool IsPrimitive {
			get { return HasFlag (ObjectValueFlags.Primitive); }
		}
		
		public bool IsUnknown {
			get { return HasFlag (ObjectValueFlags.Unknown); }
		}
		
		public bool IsError {
			get { return HasFlag (ObjectValueFlags.Error); }
		}
		
		public bool HasFlag (ObjectValueFlags flag)
		{
			return (flags & flag) != 0;
		}
	}
}
