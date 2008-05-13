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
using Mono.Debugging.Backend;
using System.Collections.Generic;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class ObjectValue
	{
		string path;
		int arrayCount = -1;
		object value;
		ObjectValueKind kind;
		IObjectValueSource source;
		List<ObjectValue> children;
		
		public ObjectValue (IObjectValueSource source, string path, ObjectValue[] children)
			: this (source, path)
		{
			this.path = path;
			kind = ObjectValueKind.Object;
			if (children != null)
				this.children.AddRange (children);
		}
		
		public ObjectValue (IObjectValueSource source, string path, object value)
			: this (source, path)
		{
			kind = ObjectValueKind.Prmitive;
			this.value = value;
		}
		
		public ObjectValue (IObjectValueSource source, string path, int arrayCount, ObjectValue[] children)
			: this (source, path)
		{
			kind = ObjectValueKind.Array;
			if (children != null)
				this.children.AddRange (children);
		}
		
		public ObjectValue (IObjectValueSource source, string path)
		{
			this.source = source;
			this.path = path;
			kind = ObjectValueKind.Unknown;
		}
		
		public ObjectValueKind Kind {
			get { return kind; }
		}
		
		public string Name {
			get {
				int i = path.LastIndexOf ('/');
				if (i == -1)
					return path;
				else
					return path.Substring (i+1);
			}
		}
		
		public object Value {
			get { return value; }
		}
		
		public ObjectValue GetChild (string name)
		{
			if (IsArray)
				throw new InvalidOperationException ("Object is an array.");
			
			if (children == null) {
				children = new List<ObjectValue> ();
				children.AddRange (source.GetChildren (path, -1, -1));
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
					children.AddRange (source.GetChildren (path, -1, -1));
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
				ObjectValue[] items = source.GetChildren (path, children.Count, nc);
				children.AddRange (items);
			}
			return children [index];
		}
		
		public bool IsArray {
			get { return kind == ObjectValueKind.Array; }
		}
		
		public int ArrayCount {
			get {
				if (!IsArray)
					throw new InvalidOperationException ("Object is not an array.");
				return arrayCount; 
			}
		}
	}
}
