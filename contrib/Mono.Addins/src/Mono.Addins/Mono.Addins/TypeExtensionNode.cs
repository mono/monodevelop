//
// TypeExtensionNode.cs
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
using System.Xml;

namespace Mono.Addins
{
	[ExtensionNode ("Type", Description="Specifies a class that will be used to create an extension object.")]
	[NodeAttribute ("class", typeof(Type), false, Description="Name of the class. If a value is not provided, the class name will be taken from the 'id' attribute")]
	public class TypeExtensionNode: InstanceExtensionNode
	{
		string typeName;
		
		internal protected override void Read (NodeElement elem)
		{
			base.Read (elem);
			typeName = elem.GetAttribute ("type");
			if (typeName.Length == 0)
				typeName = elem.GetAttribute ("class");
			if (typeName.Length == 0)
				typeName = elem.GetAttribute ("id");
		}
		
		public override object CreateInstance ()
		{
			if (typeName.Length == 0)
				throw new InvalidOperationException ("Type name not specified.");

			Type t = Addin.GetType (typeName, true);
			return Activator.CreateInstance (t);
		}
	}
}
