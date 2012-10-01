using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;

namespace Stetic
{
	public class TypedClassDescriptor: ClassDescriptor 
	{
		Type wrapped, wrapper;
		GLib.GType gtype;

		MethodInfo ctorMethodInfo;
		MethodInfo ctorMethodInfoWithClass;
		ConstructorInfo cinfo;
		bool useGTypeCtor;
		Gdk.Pixbuf icon;
		bool defaultValuesLoaded;
		
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		
		static Gdk.Pixbuf missingIcon;

		public TypedClassDescriptor (Assembly assembly, XmlElement elem)
		{
			bool inheritedWrapper = false;
			
			wrapped = Registry.GetType (elem.GetAttribute ("type"), true);
			if (elem.HasAttribute ("wrapper"))
			    wrapper = Registry.GetType (elem.GetAttribute ("wrapper"), true);
			else {
				inheritedWrapper = true;
				string baseClass = elem.GetAttribute ("base-type");
				if (baseClass.Length > 0) {
					// If a base type is specified, use the wrapper of that base type
					TypedClassDescriptor parent = Registry.LookupClassByName (baseClass) as TypedClassDescriptor;
					if (parent != null)
						wrapper = parent.WrapperType;
				}
				else {
					for (Type type = wrapped.BaseType; type != null; type = type.BaseType) {
						TypedClassDescriptor parent = Registry.LookupClassByName (type.FullName) as TypedClassDescriptor;
						if (parent != null) {
							wrapper = parent.WrapperType;
							break;
						}
					}
				}
				if (wrapper == null)
					throw new ArgumentException (string.Format ("No wrapper type for class {0}", wrapped.FullName));
			}

			gtype = (GLib.GType)wrapped;
			cname = gtype.ToString ();

			string iconname = elem.GetAttribute ("icon");
			if (iconname.Length > 0) {
				try {
					// Using the pixbuf resource constructor generates a gdk warning.
					Gdk.PixbufLoader loader = new Gdk.PixbufLoader (assembly, iconname);
					icon = loader.Pixbuf;
				} catch {
					Console.WriteLine ("Could not load icon: " + iconname);
					icon = GetDefaultIcon ();
				}
			} else
				icon = GetDefaultIcon ();
			
			BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
						
			// If the wrapper is inherited from a base class, ignore the CreateInstance method
			// since it is going to create an instance of the base class.
			if (!inheritedWrapper) {
				ctorMethodInfoWithClass = wrapper.GetMethod ("CreateInstance", flags, null, new Type[] { typeof(ClassDescriptor)}, null);
				if (ctorMethodInfoWithClass == null) {
					ctorMethodInfo = wrapper.GetMethod ("CreateInstance", flags, null, Type.EmptyTypes, null);
				}
			}
			
			// Look for a constructor even if a CreateInstance method was
			// found, since it may return null.
			cinfo = wrapped.GetConstructor (Type.EmptyTypes);
			if (cinfo == null) {
				useGTypeCtor = true;
				cinfo = wrapped.GetConstructor (new Type[] { typeof (IntPtr) });
			}
			
			Load (elem);
		}
		
		public override Gdk.Pixbuf Icon {
			get {
				return icon;
			}
		}

		public override string WrappedTypeName {
			get { return WrappedType.FullName; }
		}
		
		public Type WrapperType {
			get {
				return wrapper;
			}
		}
		
		public Type WrappedType {
			get {
				return wrapped;
			}
		}

		public GLib.GType GType {
			get {
				return gtype;
			}
		}
		
		public override ObjectWrapper CreateWrapper ()
		{
			return (ObjectWrapper) Activator.CreateInstance (WrapperType);
		}

		[DllImport ("libgobject-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_object_new (IntPtr gtype, IntPtr dummy);

		public override object CreateInstance (IProject proj)
		{
			object inst;

			if (ctorMethodInfoWithClass != null) {
				inst = ctorMethodInfoWithClass.Invoke (null, new object[] { this });
				if (inst != null) return inst;
			}
			if (ctorMethodInfo != null) {
				inst = ctorMethodInfo.Invoke (null, new object[0]);
				if (inst != null) return inst;
			}
			
			if (cinfo == null)
				throw new InvalidOperationException ("The class '" + wrapped + "' does not have a default constructor.");
			
			if (!useGTypeCtor)
				inst = cinfo.Invoke (new object[0]);
			else {
				IntPtr raw = g_object_new (gtype.Val, IntPtr.Zero);
				inst = cinfo.Invoke (new object[] { raw });
			}

			return inst;
		}
		
		internal protected override ItemDescriptor CreateItemDescriptor (XmlElement elem, ItemGroup group)
		{
			if (elem.Name == "property")
				return new TypedPropertyDescriptor (elem, group, this);
			else if (elem.Name == "signal")
				return new TypedSignalDescriptor (elem, group, this);
			else
				return base.CreateItemDescriptor (elem, group);
		}
		
		Gdk.Pixbuf GetDefaultIcon ()
		{
			if (missingIcon == null)
				missingIcon = WidgetUtils.MissingIcon;
			return missingIcon;
		}
		
		internal void LoadDefaultValues ()
		{
			// This is a hack because there is no managed way of getting
			// the default value of a GObject property.
			// This method creates an dummy instance of this class and
			// gets the values for their properties. Those values are
			// considered the default
			
			if (defaultValuesLoaded)
				return;
			defaultValuesLoaded = true;
			
			object ob = NewInstance (null, false);
			
			foreach (ItemGroup group in ItemGroups) {
				foreach (ItemDescriptor item in group) {
					TypedPropertyDescriptor prop = item as TypedPropertyDescriptor;
					if (prop == null)
						continue;
						
					if (!prop.HasDefault) {
						prop.SetDefault (null);
					} else {
						object val = prop.GetValue (ob);
						prop.SetDefault (val);
					}
				}
			}
			ObjectWrapper ww = ObjectWrapper.Lookup (ob);
			ww.Dispose ();
		}
	}
}
