using System;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;

namespace Stetic {
	public class ItemGroup : IEnumerable
	{
		public static ItemGroup Empty;

		string label, name;
		ListDictionary items = new ListDictionary ();
		ClassDescriptor declaringType;

		static ItemGroup ()
		{
			Empty = new ItemGroup ();
		}
		
		private ItemGroup ()
		{
		}

		public ItemGroup (XmlElement elem, ClassDescriptor klass)
		{
			declaringType = klass;
			label = elem.GetAttribute ("label");
			name = elem.GetAttribute ("name");

			XmlNodeList nodes = elem.SelectNodes ("property | command | signal");
			for (int i = 0; i < nodes.Count; i++) {
				XmlElement item = (XmlElement)nodes[i];
				string refname = item.GetAttribute ("ref");
				if (refname != "") {
					if (refname.IndexOf ('.') != -1) {
						ItemDescriptor desc = (ItemDescriptor) Registry.LookupItem (refname);
						items.Add (desc.Name, desc);
					} else {
						ItemDescriptor desc = (ItemDescriptor) klass[refname];
						items.Add (desc.Name, desc);
					}
					continue;
				}

				ItemDescriptor idesc = klass.CreateItemDescriptor ((XmlElement)item, this);
				if (idesc != null)
					items.Add (idesc.Name, idesc);
			}
		}

		public string Label {
			get {
				return label;
			}
		}

		public string Name {
			get {
				return name;
			}
		}

		public IEnumerator GetEnumerator ()
		{
			return items.Values.GetEnumerator ();
		}

		public ItemDescriptor this [string name] {
			get {
				return (ItemDescriptor) items [name];
			}
		}
		
		public ClassDescriptor DeclaringType {
			get { return declaringType; }
		}
	}
}
