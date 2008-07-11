using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;

namespace Stetic {

	
	public abstract class ClassDescriptor
	{
		protected string label, category, cname;
		protected bool deprecated, hexpandable, vexpandable, allowChildren = true;
		
		protected ItemGroupCollection groups = new ItemGroupCollection ();
		protected ItemGroupCollection signals = new ItemGroupCollection ();

		protected int importantGroups;
		protected ItemGroup contextMenu;
		protected ItemGroup internalChildren;
		protected string baseType;
		protected bool isInternal;
		protected string targetGtkVersion;
		
		WidgetLibrary library;
		PropertyDescriptor[] initializationProperties;
		static PropertyDescriptor[] emptyPropArray = new PropertyDescriptor[0];

		int counter;

		protected void Load (XmlElement elem)
		{
			if (elem.HasAttribute ("cname"))
				cname = elem.GetAttribute ("cname");
			else if (cname == null)
				cname = elem.GetAttribute ("type");

			label = elem.GetAttribute ("label");
			if (label == "") {
				label = WrappedTypeName;
				int i = label.LastIndexOf ('.');
				if (i != -1)
					label = label.Substring (i+1);
			}
			
			if (elem.HasAttribute ("allow-children"))
				allowChildren = elem.GetAttribute ("allow-children") == "yes" || elem.GetAttribute ("allow-children") == "true";
				
			category = elem.GetAttribute ("palette-category");

			if (elem.HasAttribute ("deprecated"))
				deprecated = true;
			if (elem.HasAttribute ("hexpandable"))
				hexpandable = true;
			if (elem.HasAttribute ("vexpandable"))
				vexpandable = true;
			if (elem.GetAttribute ("internal") == "true")
				isInternal = true;
			
			contextMenu = ItemGroup.Empty;
			
			baseType = elem.GetAttribute ("base-type");
			if (baseType.Length > 0) {
				ClassDescriptor basec = Registry.LookupClassByName (baseType);
				if (basec == null)
					throw new InvalidOperationException ("Base type '" + baseType + "' not found.");
				foreach (ItemGroup group in basec.ItemGroups)
					groups.Add (group);
				foreach (ItemGroup group in basec.SignalGroups)
					signals.Add (group);
				contextMenu = basec.ContextMenu;
			} else
				baseType = null;

			XmlElement groupsElem = elem["itemgroups"];
			if (groupsElem != null) {
				foreach (XmlElement groupElem in groupsElem.SelectNodes ("itemgroup")) {
					ItemGroup itemgroup;

					if (groupElem.HasAttribute ("ref")) {
						string refname = groupElem.GetAttribute ("ref");
						itemgroup = Registry.LookupItemGroup (refname);
					} else
						itemgroup = new ItemGroup (groupElem, this);
					groups.Add (itemgroup);

					if (groupElem.HasAttribute ("important")) {
						if (groupElem.GetAttribute ("important") == "true")
							importantGroups++;
					} else if (groups.Count == 1)
						importantGroups++;
				}
			}

			XmlElement signalsElem = elem["signals"];
			if (signalsElem != null) {
				foreach (XmlElement groupElem in signalsElem.SelectNodes ("itemgroup")) {
					ItemGroup itemgroup;
					if (groupElem.HasAttribute ("ref")) {
						string refname = groupElem.GetAttribute ("ref");
						itemgroup = Registry.LookupSignalGroup (refname);
					} else
						itemgroup = new ItemGroup (groupElem, this);
					signals.Add (itemgroup);
				}
			}

			XmlElement contextElem = elem["contextmenu"];
			if (contextElem != null) {
				if (contextElem.HasAttribute ("ref")) {
					string refname = contextElem.GetAttribute ("ref");
					contextMenu = Registry.LookupContextMenu (refname);
				} else
					contextMenu = new ItemGroup (contextElem, this);
			}

			XmlElement ichildElem = elem["internal-children"];
			if (ichildElem != null)
				internalChildren = new ItemGroup (ichildElem, this);
			else
				internalChildren = ItemGroup.Empty;

			string initProps = elem.GetAttribute ("init-properties");
			if (initProps.Length > 0) {
				string[] props = initProps.Split (' ');
				ArrayList list = new ArrayList ();
				foreach (string prop in props) {
					PropertyDescriptor idesc = this [prop] as PropertyDescriptor;
					if (idesc == null)
						throw new InvalidOperationException ("Initialization property not found: " + prop);
					list.Add (idesc);
				}
				initializationProperties = (PropertyDescriptor[]) list.ToArray (typeof(PropertyDescriptor));
			} else
				initializationProperties = emptyPropArray;
			
			targetGtkVersion = elem.GetAttribute ("gtk-version");
			if (targetGtkVersion.Length == 0)
				targetGtkVersion = null;
		}
		
		public virtual string Name {
			get {
				return WrappedTypeName;
			}
		}
		
		public virtual bool IsInternal {
			get { return isInternal; }
		}

		public abstract string WrappedTypeName {
			get;
		}

		public string CName {
			get {
				return cname;
			}
		}

		public bool Deprecated {
			get {
				return deprecated;
			}
		}

		public bool HExpandable {
			get {
				return hexpandable;
			}
		}

		public bool VExpandable {
			get {
				return vexpandable;
			}
		}

		public string Label {
			get {
				return label;
			}
		}

		public abstract Gdk.Pixbuf Icon {
			get;
		}

		public string Category {
			get {
				return category;
			}
		}
		
		public virtual string TargetGtkVersion {
			get {
				if (targetGtkVersion == null)
					return library.TargetGtkVersion;
				else
					return targetGtkVersion; 
			}
		}
		
		public bool SupportsGtkVersion (string targetVersion)
		{
			return WidgetUtils.CompareVersions (TargetGtkVersion, targetVersion) >= 0;
		}

		public PropertyDescriptor[] InitializationProperties {
			get { return initializationProperties; }
		}
		
		public object NewInstance (IProject proj)
		{
			return NewInstance (proj, true);
		}
		
		public object NewInstance (IProject proj, bool initialize)
		{
			object ob = CreateInstance (proj);
			
			string name = WrappedTypeName.ToLower () + (++counter).ToString ();
			int i = name.LastIndexOf ('.');
			if (i != -1) {
				if (i < name.Length)
					name = name.Substring (i+1);
				else
					name = name.Replace (".", "");
			}
			
			foreach (ItemGroup group in groups) {
				foreach (ItemDescriptor item in group) {
					PropertyDescriptor prop = item as PropertyDescriptor;
					if (prop != null && prop.InitWithName)
						prop.SetValue (ob, name);
				}
			}
			
			ObjectWrapper ow = CreateWrapper ();
			ObjectWrapper.Bind (proj, this, ow, ob, !initialize);
			return ob;
		}
		
		// Sets the default values for an instance
		public virtual void ResetInstance (object obj)
		{
			foreach (ItemGroup group in groups) {
				foreach (ItemDescriptor item in group) {
					PropertyDescriptor prop = item as PropertyDescriptor;
					if (prop != null)
						prop.ResetValue (obj);
				}
			}
		}
		
		public abstract object CreateInstance (IProject proj);
		
		public abstract ObjectWrapper CreateWrapper ();

		public ItemDescriptor this[string name] {
			get {
				if (groups != null) {
					foreach (ItemGroup group in groups) {
						ItemDescriptor item = group[name];
						if (item != null)
							return item;
					}
				}

				return null;
			}
		}

		public ItemGroupCollection ItemGroups {
			get {
				return groups;
			}
		}

		public ItemGroupCollection SignalGroups {
			get {
				return signals;
			}
		}

		public int ImportantGroups {
			get {
				return importantGroups;
			}
		}

		public ItemGroup ContextMenu {
			get {
				return contextMenu;
			}
		}

		public ItemGroup InternalChildren {
			get {
				return internalChildren;
			}
		}
		
		public WidgetLibrary Library {
			get { return library; }
		}
		
		public virtual bool AllowChildren {
			get { return allowChildren; }
		}
		
		internal protected virtual ItemDescriptor CreateItemDescriptor (XmlElement elem, ItemGroup group)
		{
			if (elem.Name == "command")
				return new CommandDescriptor (elem, group, this);
			else
				throw new ApplicationException ("Bad item name " + elem.Name + " in " + WrappedTypeName);
		}
		
		internal void SetLibrary (WidgetLibrary library)
		{
			this.library = library;
		}
	}
}
