using System;
using System.Collections;
using System.Xml;
using Mono.Cecil;

namespace Stetic
{
	class CecilPropertyDescriptor: Stetic.PropertyDescriptor
	{
		string name;
		Type type;
		object initialValue;
		bool canWrite;
		
		public CecilPropertyDescriptor (CecilWidgetLibrary lib, XmlElement elem, Stetic.ItemGroup group, Stetic.ClassDescriptor klass, PropertyDefinition pinfo): base (elem, group, klass)
		{
			string tname;
			
			if (pinfo != null) {
				name = pinfo.Name;
				tname = pinfo.PropertyType.FullName;
				canWrite = pinfo.SetMethod != null;
			}
			else {
				name = elem.GetAttribute ("name");
				tname = elem.GetAttribute ("type");
				canWrite = elem.Attributes ["canWrite"] == null;
			}
			
			Load (elem);
			
			type = Stetic.Registry.GetType (tname, false);
			
			if (type == null) {
				Console.WriteLine ("Could not find type: " + tname);
				type = typeof(string);
			}
			if (type.IsValueType)
				initialValue = Activator.CreateInstance (type);
				
			// Consider all properties runtime-properties, since they have been created
			// from class properties.
			isRuntimeProperty = true;
			
			if (pinfo != null)
				SaveCecilXml (elem);
		}
		
		public CecilPropertyDescriptor (XmlElement elem, Stetic.ItemGroup group, Stetic.ClassDescriptor klass, PropertyDescriptor prop): base (elem, group, klass)
		{
			this.name = prop.Name;
			this.type = prop.PropertyType;
			this.canWrite = prop.CanWrite;
			if (type.IsValueType)
				initialValue = Activator.CreateInstance (type);
			this.label = prop.Label;
			this.description = prop.Description;
			this.maximum = prop.Maximum;
			this.minimum = prop.Minimum;
			this.initWithName = prop.InitWithName;
			this.translatable = prop.Translatable;
		}
		
		internal void SaveCecilXml (XmlElement elem)
		{
			elem.SetAttribute ("name", name);
			elem.SetAttribute ("type", type.FullName);
			if (!canWrite)
				elem.SetAttribute ("canWrite", "false");
		}
		
		public override string Name {
			get { return name; }
		}
		
		// The property's type
		public override Type PropertyType {
			get { return type; }
		}
		
		public override bool IsDefaultValue (object value)
		{
			return false;
		}

		// Gets the value of the property on @obj
		public override object GetValue (object obj)
		{
			Stetic.ObjectWrapper wrapper = (Stetic.ObjectWrapper) Stetic.ObjectWrapper.Lookup (obj);
			Hashtable props = (Hashtable) wrapper.ExtendedData [typeof(CecilPropertyDescriptor)];
			object val = props != null ? props [name] : null;
			if (val == null && initialValue != null)
				return initialValue;
			else
				return val;
		}

		// Whether or not the property is writable
		public override bool CanWrite {
			get { return canWrite; }
		}

		// Sets the value of the property on @obj
		public override void SetValue (object obj, object value)
		{
			Stetic.ObjectWrapper wrapper = (Stetic.ObjectWrapper) Stetic.ObjectWrapper.Lookup (obj);
			Hashtable props = (Hashtable) wrapper.ExtendedData [typeof(CecilPropertyDescriptor)];
			if (props == null) {
				props = new Hashtable ();
				wrapper.ExtendedData [typeof(CecilPropertyDescriptor)] = props;
			}
			
			props [name] = value;
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

