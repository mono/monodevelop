//
// ItemPropertyAttribute.cs
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
	[AttributeUsage (AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=true)]
	public class ItemPropertyAttribute: Attribute
	{
		Type confType;
		object defaultValue;
		string name;
		int scope;
		Type dataType;
		bool readOnly;
		bool writeOnly;
		
		public ItemPropertyAttribute ()
		{
		}
		
		public ItemPropertyAttribute (Type dataType)
		{
			this.dataType = dataType;
		}
		
		public ItemPropertyAttribute (string name)
		{
			this.name = name;
		}
		
		public object DefaultValue {
			get { return defaultValue; }
			set { defaultValue = value; }
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public int Scope {
			get { return scope; }
			set { scope = value; }
		}
		
		public Type SerializationDataType {
			get { return confType; }
			set { confType = value; }
		}
		
		public Type ValueType {
			get { return dataType; }
			set { dataType = value; }
		}
		
		public bool ReadOnly {
			get { return readOnly; }
			set { readOnly = value; }
		}
		
		public bool WriteOnly {
			get { return writeOnly; }
			set { writeOnly = value; }
		}
	}
}
