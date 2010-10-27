using System;
using System.Collections;
using System.Xml;
using System.Reflection;

namespace Stetic
{
	[Serializable]
	public abstract class ItemDescriptor
	{
		[NonSerialized]
		ArrayList deps;
		
		[NonSerialized]
		ArrayList visdeps;
		
		[NonSerialized]
		bool isInternal;
		
		[NonSerialized]
		ClassDescriptor klass;
		
		protected string targetGtkVersion;

		protected ItemDescriptor () {}

		protected ItemDescriptor (XmlElement elem, ItemGroup group, ClassDescriptor klass)
		{
			this.klass = klass;
			isInternal = elem.HasAttribute ("internal");
			deps = AddSubprops (elem.SelectNodes ("./disabled-if"), group, klass);
			visdeps = AddSubprops (elem.SelectNodes ("./invisible-if"), group, klass);
			targetGtkVersion = elem.GetAttribute ("gtk-version");
			if (targetGtkVersion.Length == 0)
				targetGtkVersion = null;
		}

		ArrayList AddSubprops (XmlNodeList nodes, ItemGroup group, ClassDescriptor klass)
		{
			ArrayList list = null;
			
			// Sub-properties can have a name+value (which checks for the value of a
			// property) or a method name, which should return true if the item has
			// to be disabled/hidden.

			foreach (XmlElement elem in nodes) {
				string name = elem.GetAttribute ("name");
				if (name.Length > 0) {
					string value = elem.GetAttribute ("value");

					PropertyDescriptor prop = (PropertyDescriptor)group[name];
					if (prop == null)
						prop = (PropertyDescriptor)klass[name];
					if (prop == null)
						throw new ArgumentException ("Bad sub-prop " + name);
					if (list == null)
						list = new ArrayList ();
						
					DepInfo info = new DepInfo ();
					info.Property = prop;
					info.Value = prop.StringToValue (value);
					list.Add (info);
				} else if ((name = elem.GetAttribute ("check")).Length > 0) {
					DepInfo info = new DepInfo ();
					info.CheckName = name;
					if (list == null)
						list = new ArrayList ();
					list.Add (info);
				} else {
					throw new ArgumentException ("Bad sub-prop");
				}
			}
			return list;
		}

		// The property's display name
		public abstract string Name { get; }
		
		public virtual string TargetGtkVersion {
			get {
				if (targetGtkVersion == null)
					return klass.TargetGtkVersion;
				else
					return targetGtkVersion; 
			}
		}
		
		public bool SupportsGtkVersion (string targetVersion)
		{
			return WidgetUtils.CompareVersions (TargetGtkVersion, targetVersion) >= 0;
		}

		public bool HasDependencies {
			get {
				return deps != null || visdeps != null;
			}
		}

		public bool EnabledFor (object obj)
		{
			if (deps == null)
				return true;

			foreach (DepInfo dep in deps) {
				if (dep.Check (obj))
					return false;
			}
			return true;
		}

		public bool HasVisibility {
			get {
				return visdeps != null;
			}
		}

		public bool VisibleFor (object obj)
		{
			if (visdeps == null)
				return true;

			foreach (DepInfo dep in visdeps) {
				if (dep.Check (obj))
					return false;
			}
			return true;
		}

		public bool IsInternal {
			get {
				return isInternal;
			}
		}
		
		public ClassDescriptor ClassDescriptor {
			get { return klass; }
		}
		
		class DepInfo
		{
			public string CheckName;
			public PropertyDescriptor Property;
			public object Value;
			
			public bool Check (object obj)
			{
				if (Property != null) {
					object depValue = Property.GetValue (obj);
					return Value.Equals (depValue);
				} else {
					object wrapper = ObjectWrapper.Lookup (obj);
					object res = wrapper.GetType ().InvokeMember (CheckName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, wrapper, null);
					return !(bool) res;
				}
			}
		}
	}
}
