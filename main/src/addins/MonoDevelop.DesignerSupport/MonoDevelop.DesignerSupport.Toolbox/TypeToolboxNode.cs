//
// TypeToolboxNode.cs: A toolbox node that refers to a .NET type.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using System.ComponentModel;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	[Serializable]
	[DataInclude (typeof(TypeReference))]
	public class TypeToolboxNode : ItemToolboxNode
	{
		//the serialiseable type reference field
		[ItemProperty ("type")]
		TypeReference type;
		
		public TypeReference Type {
			get { return type; }
			set { type = value; }
		}
		
		//blank constructor for deserialisation
		public TypeToolboxNode ()
		{
		}
		
		#region convenience constructors
		
		public TypeToolboxNode (TypeReference typeRef)
		{
			this.type = typeRef;
		}
		
		public TypeToolboxNode (string typeName, string assemblyName)
		  : this (typeName, assemblyName, string.Empty)
		{
		}
		
		public TypeToolboxNode (string typeName, string assemblyName, string assemblyLocation)
		{
			this.type = new TypeReference (typeName, assemblyName, assemblyLocation);
		}
		
		public TypeToolboxNode (Type type)
		{
			this.type = new TypeReference (type);
		}
		
		#endregion
		
		#region comparison overrides taking account of private field
		
		public override bool Equals (object o)
		{
			TypeToolboxNode node = o as TypeToolboxNode;
			return (node != null)
			    && (this.type == null? node.type == null : this.type.Equals (node.type))
			    && base.Equals (node);
		}
		
		public override int GetHashCode ()
		{
			int code = base.GetHashCode ();
			if (type != null)
				code += type.GetHashCode ();
			return code;
		}
		
		#endregion
	}
}
