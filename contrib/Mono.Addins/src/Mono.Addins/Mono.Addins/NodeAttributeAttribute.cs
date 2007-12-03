//
// NodeAttributeAttribute.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

namespace Mono.Addins
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Field, AllowMultiple=true)]
	public class NodeAttributeAttribute: Attribute
	{
		string name;
		bool required;
		bool localizable;
		Type type;
		string description;
		
		public NodeAttributeAttribute ()
		{
		}
		
		public NodeAttributeAttribute (string name)
			:this (name, false, null)
		{
		}
		
		public NodeAttributeAttribute (string name, string description)
			:this (name, false, description)
		{
		}
		
		public NodeAttributeAttribute (string name, bool required)
			: this (name, required, null)
		{
		}
		
		public NodeAttributeAttribute (string name, bool required, string description)
		{
			this.name = name;
			this.required = required;
			this.description = description;
		}
		
		public NodeAttributeAttribute (string name, Type type)
			: this (name, type, false, null)
		{
		}
		
		public NodeAttributeAttribute (string name, Type type, string description)
			: this (name, type, false, description)
		{
		}
		
		public NodeAttributeAttribute (string name, Type type, bool required)
			: this (name, type, false, null)
		{
		}
		
		public NodeAttributeAttribute (string name, Type type, bool required, string description)
		{
			this.name = name;
			this.type = type;
			this.required = required;
			this.description = description;
		}
		
		public string Name {
			get { return name != null ? name : string.Empty; }
			set { name = value; }
		}
		
		public bool Required {
			get { return required; }
			set { required = value; }
		}
		
		public Type Type {
			get { return type; }
			set { type = value; }
		}
		
		public string Description {
			get { return description != null ? description : string.Empty; }
			set { description = value; }
		}

		public bool Localizable {
			get { return localizable; }
			set { localizable = value; }
		}
	}
}
