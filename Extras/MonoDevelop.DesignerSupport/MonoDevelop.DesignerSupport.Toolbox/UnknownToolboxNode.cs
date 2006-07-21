
using System;
using System.Collections;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	
	
	public class UnknownToolboxNode : ItemToolboxNode, IExtendedDataItem
	{
		Hashtable dictionary;
		
		public UnknownToolboxNode()
		{
		}
		
		public IDictionary ExtendedProperties {
			get {
				if (dictionary == null)
					dictionary = new Hashtable ();
				return dictionary;
			}
		}
	}
}
