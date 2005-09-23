// 

using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Reflection;

namespace MonoDevelop.Gui.Components
{
	/// <summary>
	/// GlobalizedObject implements ICustomTypeDescriptor to enable 
	/// required functionality to describe a type (class).<br></br>
	/// The main task of this class is to instantiate our own property descriptor 
	/// of type GlobalizedPropertyDescriptor.  
	/// </summary>
	public class LocalizedObject : ICustomTypeDescriptor
	{
		PropertyDescriptorCollection globalizedProps;
		
		public string GetClassName()
		{
			return TypeDescriptor.GetClassName(this,true);
		}

		public AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes(this,true);
		}

		public string GetComponentName()
		{
			return TypeDescriptor.GetComponentName(this, true);
		}

		public TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter(this, true);
		}

		public EventDescriptor GetDefaultEvent() 
		{
			return TypeDescriptor.GetDefaultEvent(this, true);
		}

		public PropertyDescriptor GetDefaultProperty() 
		{
			return TypeDescriptor.GetDefaultProperty(this, true);
		}

		public object GetEditor(Type editorBaseType) 
		{
			return TypeDescriptor.GetEditor(this, editorBaseType, true);
		}

		public EventDescriptorCollection GetEvents(Attribute[] attributes) 
		{
			return TypeDescriptor.GetEvents(this, attributes, true);
		}

		public EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents(this, true);
		}

		/// <summary>
		/// Called to get the properties of a type.
		/// </summary>
		/// <param name="attributes"></param>
		/// <returns></returns>
		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			if ( globalizedProps == null) {
				// Get the collection of properties
				PropertyDescriptorCollection baseProps = TypeDescriptor.GetProperties(this, attributes, true);

				globalizedProps = new PropertyDescriptorCollection(null);

				// For each property use a property descriptor of our own that is able to be globalized
				foreach (PropertyDescriptor oProp in baseProps) {
					globalizedProps.Add(new LocalizedPropertyDescriptor(oProp));
				}
			}
			return globalizedProps;
		}

		public PropertyDescriptorCollection GetProperties()
		{
			// Only do once
			if (globalizedProps == null) {
				// Get the collection of properties
				PropertyDescriptorCollection baseProps = TypeDescriptor.GetProperties(this, true);
				globalizedProps = new PropertyDescriptorCollection(null);

				// For each property use a property descriptor of our own that is able to be globalized
				foreach (PropertyDescriptor oProp in baseProps) {
					globalizedProps.Add(new LocalizedPropertyDescriptor(oProp));
				}
			}
			return globalizedProps;
		}

		public object GetPropertyOwner(PropertyDescriptor pd) 
		{
			return this;
		}
	}
}
