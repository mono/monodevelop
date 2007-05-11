
using System;

namespace MonoDevelop.Ide.Projects.Item
{
	public class UnknownProjectItem : ProjectItem
	{
		string itemType;
		
		public string ItemType {
			get { return itemType; }
			set { itemType = value; }
		}
		
		public UnknownProjectItem(string itemType)
		{
			this.ItemType = itemType;
		}
	}
}
