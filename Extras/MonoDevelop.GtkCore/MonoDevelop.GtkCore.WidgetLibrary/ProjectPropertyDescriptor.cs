//
// ProjectPropertyDescriptor.cs
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
using System.Xml;

using MonoDevelop.Projects;	
using MonoDevelop.Projects.Parser;	
using MonoDevelop.Ide.Gui;	
using MonoDevelop.GtkCore.GuiBuilder;	

namespace MonoDevelop.GtkCore.WidgetLibrary
{
	class ProjectPropertyDescriptor: Stetic.PropertyDescriptor
	{
		ProjectPropertyInfo pinfo;
		
		public ProjectPropertyDescriptor (XmlElement elem, Stetic.ItemGroup group, Stetic.ClassDescriptor klass, ProjectPropertyInfo pinfo): base (elem, group, klass)
		{
			this.pinfo = pinfo;
			Load (elem);
			
			// Consider all properties runtime-properties, since they have been created
			// from class properties.
			isRuntimeProperty = true;
		}
		
		public override string Name {
			get { return pinfo.Name; }
		}
		
		// The property's type
		public override Type PropertyType {
			get { return pinfo.PropType; }
		}
		
		public override bool IsDefaultValue (object value)
		{
			return false;
		}

		// Gets the value of the property on @obj
		public override object GetValue (object obj)
		{
			CustomWidgetWrapper wrapper = (CustomWidgetWrapper) Stetic.ObjectWrapper.Lookup (obj);
			object val = wrapper.GetProperty (pinfo.Name);
			if (val == null && pinfo.InitialValue != null)
				return pinfo.InitialValue;
			else
				return val;
		}

		// Whether or not the property is writable
		public override bool CanWrite {
			get { return pinfo.CanWrite; }
		}

		// Sets the value of the property on @obj
		public override void SetValue (object obj, object value)
		{
			CustomWidgetWrapper wrapper = (CustomWidgetWrapper) Stetic.ObjectWrapper.Lookup (obj);
			wrapper.SetProperty (pinfo.Name, value);
		}
		
		public override object GetRuntimeValue (object obj)
		{
			return null;
		}

		public override void SetRuntimeValue (object obj, object value)
		{
		}

	}
}

