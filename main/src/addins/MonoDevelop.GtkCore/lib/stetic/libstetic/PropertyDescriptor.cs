using System;
using System.ComponentModel;
using System.Collections;
using System.Reflection;
using System.Xml;

namespace Stetic {

	class TranslationInfo {
		public bool Translated;
		public string Context, Comment;
	}

	public abstract class PropertyDescriptor : ItemDescriptor
	{
		protected string label, description, gladeName;
		protected bool gladeOverride;
		
		protected bool isRuntimeProperty, hasDefault, initWithName;
		protected Type editorType;
		protected object minimum, maximum;
		protected object defaultValue;
		protected TypeConverter typeConverter;
		protected bool translatable;

		protected PropertyDescriptor ()
		{
		}
		
		protected PropertyDescriptor (XmlElement elem, ItemGroup group, ClassDescriptor klass): base (elem, group, klass)
		{
		}

		protected void Load (XmlElement elem)
		{
			if (elem.HasAttribute ("label"))
				label = elem.GetAttribute ("label");
				
			if (label == null)
				label = elem.GetAttribute ("name");
				
			if (elem.HasAttribute ("description"))
				description = elem.GetAttribute ("description");

			if (elem.HasAttribute ("min"))
				minimum = StringToValue (elem.GetAttribute ("min"));

			if (elem.HasAttribute ("max"))
				maximum = StringToValue (elem.GetAttribute ("max"));

			if (elem.HasAttribute ("glade-override"))
				gladeOverride = true;

			if (elem.HasAttribute ("glade-name"))
				gladeName = elem.GetAttribute ("glade-name");

			if (elem.HasAttribute ("init-with-name"))
				initWithName = true;

			if (elem.HasAttribute ("translatable"))
				translatable = true;
				
			if (elem.HasAttribute ("default")) {
				defaultValue = StringToValue (elem.GetAttribute ("default"));
				hasDefault = true;
			}
				
			string convTypeName = elem.GetAttribute ("type-converter");
			if (convTypeName.Length > 0) {
				Type type = Registry.GetType (convTypeName, true);
				typeConverter = (TypeConverter) Activator.CreateInstance (type);
			}
		}

		// The property's user-visible name
		public virtual string Label {
			get {
				return label;
			}
		}

		// The property's type
		public abstract Type PropertyType {
			get ;
		}

		// The property's user-visible description
		public virtual string Description {
			get {
				return description;
			}
		}
		
		// The property's GUI editor type, if overridden
		public virtual Type EditorType {
			get {
				return editorType;
			}
		}

		// The property's minimum value, if declared
		public virtual object Minimum {
			get {
				return minimum;
			}
		}

		// The property's maximum value, if declared
		public virtual object Maximum {
			get {
				return maximum;
			}
		}
		
		public virtual string InternalChildId {
			get { return null; }
		}

		// Whether or not the property has a default value
		public virtual bool HasDefault {
			get {
				return hasDefault;
			}
			set {
				hasDefault = value;
			}
		}
		
		public virtual bool IsDefaultValue (object value)
		{
			if (value == null)
				return true;
			if (defaultValue != null)
				return value.Equals (defaultValue);
			return false;
		}
		
		public virtual void ResetValue (object instance) 
		{
			if (HasDefault)
				SetValue (instance, defaultValue);
		}

		// The property's type at run time
		public virtual Type RuntimePropertyType {
			get { return PropertyType; }
		}
		
		// Gets the value of the property on @obj
		public abstract object GetValue (object obj);

		// Gets the value of the property on @obj, bypassing the wrapper.
		public virtual object GetRuntimeValue (object obj)
		{
			return GetValue (obj);
		}

		// Whether or not the property is writable
		public virtual bool CanWrite {
			get { return true; }
		}

		// Sets the value of the property on @obj
		public abstract void SetValue (object obj, object value);

		// Sets the value of the property on @obj, bypassing the wrapper.
		public virtual void SetRuntimeValue (object obj, object value)
		{
			SetValue (obj, value);
		}
		
		// Parses a string an returns a value valid for this property
		public virtual object StringToValue (string value)
		{
			if (typeConverter != null && typeConverter.CanConvertFrom (typeof(string)))
				return typeConverter.ConvertFromString (value);
			else if (PropertyType.IsEnum)
				return Enum.Parse (PropertyType, value);
			else if (PropertyType == typeof(ImageInfo))
				return ImageInfo.FromString (value);
			else if (PropertyType == typeof(string[]))
				return value.Split ('\n');
			else if (PropertyType == typeof(DateTime))
				return new DateTime (long.Parse (value));
			else if (PropertyType == typeof(TimeSpan))
				return new TimeSpan (long.Parse (value));
			else if (PropertyType == typeof(double)) {
				try {
					return Convert.ChangeType (value, PropertyType);
				}
				catch (InvalidCastException) {
					return Convert.ChangeType (value, PropertyType, System.Globalization.CultureInfo.InvariantCulture);
				}
			} else
				return Convert.ChangeType (value, PropertyType);
		}
		
		// Returns a string representation of the provided property value
		public virtual string ValueToString (object value)
		{
			if (typeConverter != null && typeConverter.CanConvertTo (typeof(string)))
				return typeConverter.ConvertToString (value);
			else if (PropertyType == typeof(string[]))
				return string.Join ("\n", (string[])value);
			else if (PropertyType == typeof(DateTime))
				return ((DateTime)value).Ticks.ToString ();
			else if (PropertyType == typeof(TimeSpan))
				return ((TimeSpan)value).Ticks.ToString ();
			else if (PropertyType == typeof(double))
				return ((double)value).ToString (System.Globalization.CultureInfo.InvariantCulture);
			else
				return value.ToString ();
		}

		public virtual bool InitWithName {
			get {
				return initWithName;
			}
		}
		
		public virtual bool IsRuntimeProperty {
			get { return isRuntimeProperty; }
		}

		public virtual bool Translatable {
			get {
				return translatable;
			}
		}

		public virtual bool IsTranslated (object obj)
		{
			if (!translatable)
				return false;

			ObjectWrapper wrapper = ObjectWrapper.Lookup (obj);
			if (wrapper == null)
				return false;
			
			// Since translatable properties are assumed to be translated
			// by default, we return true if there is no TranslationInfo
			// for the object
			
			if (wrapper.translationInfo == null)
				return true;
				
			TranslationInfo info = (TranslationInfo)wrapper.translationInfo[obj];
			return (info == null || info.Translated == true);
		}

		public virtual void SetTranslated (object obj, bool translated)
		{
			ObjectWrapper wrapper = ObjectWrapper.Lookup (obj);
			if (wrapper == null) return;
			
			if (wrapper.translationInfo == null)
				wrapper.translationInfo = new Hashtable ();
			
			TranslationInfo info = (TranslationInfo)wrapper.translationInfo[obj];
			if (info == null) {
				info = new TranslationInfo ();
				wrapper.translationInfo[obj] = info;
			}

			if (translated)
				info.Translated = true;
			else
				info.Translated = false;
			// We leave the old Context and Comment around, so that if
			// you toggle Translated off and then back on, the old info
			// is still there.
		}

		public virtual string TranslationContext (object obj)
		{
			ObjectWrapper wrapper = ObjectWrapper.Lookup (obj);
			if (wrapper == null || wrapper.translationInfo == null) return null;
			
			TranslationInfo info = (TranslationInfo)wrapper.translationInfo[obj];
			return info != null ? info.Context : null;
		}

		public virtual void SetTranslationContext (object obj, string context)
		{
			SetTranslated (obj, true);
			
			ObjectWrapper wrapper = ObjectWrapper.Lookup (obj);
			if (wrapper == null) return;
			((TranslationInfo)wrapper.translationInfo[obj]).Context = context;
		}

		public virtual string TranslationComment (object obj)
		{
			ObjectWrapper wrapper = ObjectWrapper.Lookup (obj);
			if (wrapper == null || wrapper.translationInfo == null) return null;
			
			TranslationInfo info = (TranslationInfo)wrapper.translationInfo[obj];
			return info != null ? info.Comment : null;
		}

		public virtual void SetTranslationComment (object obj, string comment)
		{
			SetTranslated (obj, true);
			
			ObjectWrapper wrapper = ObjectWrapper.Lookup (obj);
			if (wrapper == null) return;
			((TranslationInfo)wrapper.translationInfo[obj]).Comment = comment;
		}		
	}
}
