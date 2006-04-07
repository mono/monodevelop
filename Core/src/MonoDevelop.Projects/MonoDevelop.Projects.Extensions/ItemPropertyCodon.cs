//
// ItemPropertyCodon.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.ComponentModel;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Projects
{
	[Description ("A custom property. The type specified in the 'class' property is the type to which the property has to be added. Only types which implement IExtendedDataItem can be extended in this way.")]
	[CodonNameAttribute("ItemProperty")]
	public class ItemPropertyCodon: ClassCodon
	{
		[Description ("Name of the property.")]
		[XmlMemberAttribute("name", IsRequired=true)]
		string propName;
		
		[Description ("Full name of the property type.")]
		[XmlMemberAttribute("type", IsRequired=true)]
		string propType;
		
		Type cls, type;
		
		public Type PropertyType {
			get { return type; }
		}
		
		public string PropertyName {
			get { return propName; }
		}
		
		public Type ClassType {
			get { return cls; }
		}
		
		public override object BuildItem(object owner, ArrayList subItems, ConditionCollection conditions)
		{
			cls = AddIn.GetType (Class);
			type = AddIn.GetType (propType);
			return this;
		}
	}
	
}
