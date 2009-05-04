using System;
using System.ComponentModel;
using System.Collections;
using System.Reflection;
using System.Xml;

namespace Stetic 
{
	public class TypedPropertyDescriptor : PropertyDescriptor {

		PropertyInfo memberInfo, propertyInfo, runtimePropertyInfo, runtimeMemberInfo;
		ParamSpec pspec;
		TypedPropertyDescriptor gladeProperty;
		bool isWrapperProperty;
		TypedClassDescriptor klass;
		bool defaultSet;

		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		public TypedPropertyDescriptor (XmlElement elem, ItemGroup group, TypedClassDescriptor klass) : base (elem, group, klass)
		{
			this.klass = klass;
			string propertyName = elem.GetAttribute ("name");
			int dot = propertyName.IndexOf ('.');

			if (dot != -1) {
				// Sub-property (eg, "Alignment.Value")
				memberInfo = FindProperty (klass.WrapperType, klass.WrappedType, propertyName.Substring (0, dot));
				isWrapperProperty = memberInfo.DeclaringType.IsSubclassOf (typeof (ObjectWrapper));
				gladeProperty = new TypedPropertyDescriptor (isWrapperProperty ? klass.WrapperType : klass.WrappedType, memberInfo.Name);
				propertyInfo = FindProperty (memberInfo.PropertyType, propertyName.Substring (dot + 1));
			} else {
				// Basic simple property
				propertyInfo = FindProperty (klass.WrapperType, klass.WrappedType, propertyName);
				isWrapperProperty = propertyInfo.DeclaringType.IsSubclassOf (typeof (ObjectWrapper));
			}
			
			// Wrapper properties that override widgets properties (using the same name)
			// must be considered runtime properties (will be available at run-time).
			if (!isWrapperProperty || klass.WrappedType.GetProperty (propertyName) != null)
				isRuntimeProperty = true;
			
			if (!IsInternal && propertyInfo.PropertyType.IsEnum &&
			    Registry.LookupEnum (propertyInfo.PropertyType.FullName) == null)
				throw new ArgumentException ("No EnumDescriptor for " + propertyInfo.PropertyType.FullName + "(" + klass.WrappedType.FullName + "." + propertyName + ")");

			pspec = FindPSpec (propertyInfo);
			
			if (isWrapperProperty && pspec == null) {
				PropertyInfo pinfo = klass.WrappedType.GetProperty (propertyInfo.Name, flags);
				if (pinfo != null)
					pspec = FindPSpec (pinfo);
			}

			if (pspec != null) {
				// This information will be overridden by what's specified in the xml file
				description = pspec.Blurb;
				minimum = pspec.Minimum;
				maximum = pspec.Maximum;
				label = pspec.Nick;
				if (!elem.HasAttribute ("ignore-default"))
					hasDefault = Type.GetTypeCode (PropertyType) != TypeCode.Object || PropertyType.IsEnum;
			} else {
				label = propertyInfo.Name;
				gladeOverride = true;
			}

			string typeName = elem.GetAttribute ("editor");
			if (typeName.Length > 0)
				editorType = Registry.GetType (typeName, false);
			
			// Look for a default value attribute
			
			object[] ats = propertyInfo.GetCustomAttributes (typeof(DefaultValueAttribute), true);
			if (ats.Length > 0) {
				DefaultValueAttribute at = (DefaultValueAttribute) ats [0];
				defaultValue = at.Value;
			}
			
			// Load default data
			Load (elem);
		}

		TypedPropertyDescriptor (Type objectType, string propertyName)
		{
			propertyInfo = FindProperty (objectType, propertyName);
			isWrapperProperty = false;

			pspec = FindPSpec (propertyInfo);
			if (pspec != null) {
				label = pspec.Nick;
				description = pspec.Blurb;
				minimum = pspec.Minimum;
				maximum = pspec.Maximum;
				hasDefault = Type.GetTypeCode (PropertyType) != TypeCode.Object || PropertyType.IsEnum;
			} else
				label = propertyInfo.Name;
		}

		static PropertyInfo FindProperty (Type type, string propertyName) {
			return FindProperty (null, type, propertyName);
		}

		static PropertyInfo FindProperty (Type wrapperType, Type objectType, string propertyName)
		{
			PropertyInfo info = null;

			if (wrapperType != null) {
				info = wrapperType.GetProperty (propertyName, flags);
				if (info != null)
					return info;
			}

			try {
				info = objectType.GetProperty (propertyName, flags);
			}
			catch (AmbiguousMatchException) {
				foreach (PropertyInfo pi in objectType.GetProperties ()) {
					if (pi.Name == propertyName) {
						info = pi;
						break;
					}
				}
			}

			if (info != null)
				return info;

			throw new ArgumentException ("Invalid property name " + objectType.Name + "." + propertyName);
		}

		ParamSpec FindPSpec (PropertyInfo pinfo)
		{
			foreach (object attr in pinfo.GetCustomAttributes (false)) {
				if (attr is GLib.PropertyAttribute) {
					GLib.PropertyAttribute pattr = (GLib.PropertyAttribute)attr;
					return ParamSpec.LookupObjectProperty (pinfo.DeclaringType, pattr.Name);
				}

				if (attr is Gtk.ChildPropertyAttribute) {
					Gtk.ChildPropertyAttribute cpattr = (Gtk.ChildPropertyAttribute)attr;
					return ParamSpec.LookupChildProperty (pinfo.DeclaringType.DeclaringType, cpattr.Name);
				}
			}
			return null;
		}

		// The property's internal name
		public override string Name {
			get {
				return propertyInfo.Name;
			}
		}

		// The property's type
		public override Type PropertyType {
			get {
				return propertyInfo.PropertyType;
			}
		}

		// The property's PropertyInfo
		public PropertyInfo PropertyInfo {
			get {
				return propertyInfo;
			}
		}

		// The property's ParamSpec
		public virtual ParamSpec ParamSpec {
			get {
				return pspec;
			}
		}

		public override bool IsDefaultValue (object value)
		{
			if (defaultValue != null)
				return base.IsDefaultValue (value);
			if (ParamSpec != null && value != null)
				return ParamSpec.IsDefaultValue (value);
			else
				return false;
		}

		public override void ResetValue (object instance) 
		{
			// This is a hack because there is no managed way of getting
			// the default value of a GObject property. The call to LoadDefaultValues
			// will guess the default values from a dummy instance
			if (!defaultSet) {
				ObjectWrapper ww = ObjectWrapper.Lookup (instance);
				TypedClassDescriptor td = ww.ClassDescriptor as TypedClassDescriptor;
				if (td != null)
					td.LoadDefaultValues ();
				defaultSet = true;
			}
			base.ResetValue (instance);
		}
		
		internal void SetDefault (object val)
		{
			defaultValue = val;
			defaultSet = true;
		}
		
		// Gets the value of the property on @obj
		public override object GetValue (object obj)
		{
			try {
				if (isWrapperProperty)
					obj = ObjectWrapper.Lookup (obj);
				if (memberInfo != null)
					obj = memberInfo.GetValue (obj, null);
				return propertyInfo.GetValue (obj, null);
			} catch (Exception ex) {
				throw new InvalidOperationException ("Could not get value for property " + klass.Name + "." + Name + " from object '" + obj + "'", ex); 
			}
		}

		// Whether or not the property is writable
		public override bool CanWrite {
			get {
				return propertyInfo.CanWrite;
			}
		}

		// Sets the value of the property on @obj
		public override void SetValue (object obj, object value)
		{
			ObjectWrapper ww = ObjectWrapper.Lookup (obj);
			IDisposable t = ww != null && !ww.Loading? ww.UndoManager.AtomicChange : null;
			try {
				if (isWrapperProperty)
					obj = ww;
				if (memberInfo != null)
					obj = memberInfo.GetValue (obj, null);
				propertyInfo.SetValue (obj, value, null);
			} catch (Exception ex) {
				throw new InvalidOperationException ("Could not set value for property " + klass.Name + "." + Name + " to object '" + obj + "'", ex); 
			} finally {
				if (t != null)
					t.Dispose ();
			}
		}

		// The property's type at run time
		public override Type RuntimePropertyType {
			get {
				if (runtimePropertyInfo == null)
					SetupRuntimeProperties ();
				return runtimePropertyInfo.PropertyType;
			}
		}
		
		public override void SetRuntimeValue (object obj, object value)
		{
			if (runtimePropertyInfo == null)
				SetupRuntimeProperties ();
			if (runtimeMemberInfo != null)
				obj = runtimeMemberInfo.GetValue (obj, null);
			
			if (runtimePropertyInfo.PropertyType.IsInstanceOfType (value))
				runtimePropertyInfo.SetValue (obj, value, null);
		}
		
		public override object GetRuntimeValue (object obj)
		{
			if (runtimePropertyInfo == null)
				SetupRuntimeProperties ();
			if (runtimeMemberInfo != null)
				obj = runtimeMemberInfo.GetValue (obj, null);
			return runtimePropertyInfo.GetValue (obj, null);
		}
		
		void SetupRuntimeProperties ()
		{
			if (isWrapperProperty) {
				Type t = klass.WrappedType;
				if (memberInfo != null) {
					runtimeMemberInfo = t.GetProperty (memberInfo.Name, flags);
					t = runtimeMemberInfo.PropertyType;
				}
				runtimePropertyInfo = t.GetProperty (propertyInfo.Name, flags);
			} else {
				runtimeMemberInfo = memberInfo;
				runtimePropertyInfo = propertyInfo;
			}
		}
		
		public virtual bool GladeOverride {
			get {
				return gladeOverride;
			}
		}

		public TypedPropertyDescriptor GladeProperty {
			get {
				if (gladeProperty != null)
					return gladeProperty;
				else
					return this;
			}
		}

		public virtual string GladeName {
			get {
				if (gladeName != null)
					return gladeName;
				else if (pspec != null && pspec.Name != null)
					return pspec.Name.Replace ('-', '_');
				else
					return null;
			}
		}
		
		public override string InternalChildId {
			get { return GladeName; }
		}
	}
}
