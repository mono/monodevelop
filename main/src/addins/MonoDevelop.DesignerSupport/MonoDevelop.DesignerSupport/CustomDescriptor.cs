//
// CustomDescriptor.cs
//
// Authors:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.ComponentModel;

namespace MonoDevelop.DesignerSupport
{
	// This is a default implementation of the ICustomTypeDescriptor interface.
	// It provides an overridable GetCustomAttributes method which can be used
	// to dynamically change the attributes of a property. For example, it can
	// be used to enable/disabled properties depending on values of other properties.
	
	public class CustomDescriptor: ICustomTypeDescriptor
	{
		public virtual Object GetPropertyOwner (PropertyDescriptor pd)
		{
			return this;
		}

		public virtual PropertyDescriptorCollection GetProperties (Attribute[] arr)
		{
			PropertyDescriptorCollection props = TypeDescriptor.GetProperties (this, arr, true);
			PropertyDescriptor[] newProps = new PropertyDescriptor [props.Count];
			
			for (int n=0; n<props.Count; n++) {
				PropertyDescriptor prop = props [n];
				Attribute[] atts = GetCustomAttributes (prop.Name);
				if (atts != null)
					newProps [n] = new CustomProperty (prop, atts);
				else
					newProps [n] = prop;
			}
			return new PropertyDescriptorCollection (newProps);
		}

		public virtual PropertyDescriptorCollection GetProperties()
		{
			return GetProperties (null);
		}

		public virtual EventDescriptorCollection GetEvents(Attribute[] arr)
		{
			return TypeDescriptor.GetEvents (this, arr, true);
		}

		public virtual EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents (this, true);
		}

		public virtual Object GetEditor(Type editorBaseType)
		{
			return TypeDescriptor.GetEditor (this, editorBaseType, true);
		}

		public virtual PropertyDescriptor GetDefaultProperty()
		{
			return TypeDescriptor.GetDefaultProperty (this, true);
		}

		public virtual EventDescriptor GetDefaultEvent()
		{
			return TypeDescriptor.GetDefaultEvent (this, true);
		}

		public virtual TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter (this, true);
		}

		public virtual String GetComponentName()
		{
			return TypeDescriptor.GetComponentName (this, true);
		}

		public virtual String GetClassName()
		{
			return TypeDescriptor.GetClassName (this, true);
		}

		public virtual AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes (this, true);
		}
		
		protected virtual Attribute[] GetCustomAttributes (string propertyName)
		{
			if (IsReadOnly (propertyName))
				return new Attribute[] { ReadOnlyAttribute.Yes };
			else
				return null;
		}
		
		protected virtual bool IsReadOnly (string propertyName)
		{
			return false;
		}
	}
	
	class CustomProperty: PropertyDescriptor
	{
		PropertyDescriptor prop;
		Attribute[] customAtts;
		
		public CustomProperty (PropertyDescriptor prop, Attribute[] customAtts): base (prop)
		{
			this.prop = prop;
			this.customAtts = customAtts;
		}
		
		public override Type ComponentType {
			get { return prop.ComponentType; }
		}

		public override TypeConverter Converter {
			get { return prop.Converter; }
		}
		
		public override bool IsLocalizable {
			get { return prop.IsLocalizable; }
		}

		public override bool IsReadOnly {
			get { return true; }
		}

		public override Type PropertyType {
			get { return prop.PropertyType; }
		}

		public override void AddValueChanged (object component, EventHandler handler)
		{
			prop.AddValueChanged (component, handler);
		}
 
		public override void RemoveValueChanged (object component, EventHandler handler)
		{
			prop.RemoveValueChanged (component, handler);
		}

		public override object GetValue (object component)
		{
			return prop.GetValue (component);
		}
		
		public override void SetValue (object component, object value)
		{
			prop.SetValue (component, value);
		}
		
		public override void ResetValue (object component)
		{
			prop.ResetValue (component);
		}
		
		public override bool CanResetValue (object component)
		{
			return prop.CanResetValue (component);
		}

		public override bool ShouldSerializeValue (object component)
		{
			return prop.ShouldSerializeValue (component);
		}

		public override bool Equals (object o)
		{
			return prop.Equals (o);
		}
		
		public override int GetHashCode ()
		{
			return prop.GetHashCode ();
		}
		
		public override PropertyDescriptorCollection GetChildProperties (object instance, Attribute[] filter)
		{
			return prop.GetChildProperties (instance, filter);
		}

		public override object GetEditor (Type editorBaseType)
		{
			return prop.GetEditor (editorBaseType);
		}

        protected override Attribute [] AttributeArray {
			get {
				List<Attribute> atts = new List<Attribute> ();
				foreach (Attribute at in prop.Attributes)
					atts.Add (at);
				atts.AddRange (customAtts);
				return atts.ToArray ();
			}
		}
	}
}
