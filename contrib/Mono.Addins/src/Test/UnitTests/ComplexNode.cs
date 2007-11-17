
using System;
using Mono.Addins;

namespace UnitTests
{
	[ExtensionNode ("ItemSet", "A set of items")]
	[ExtensionNodeChild (typeof(ItemNode))]
	[ExtensionNodeChild (typeof(ItemSetNode))]
	public class ItemSetNode: ExtensionNode
	{
		[NodeAttribute ("label", true, Description="Item label")]
		public string Label;
		
		[NodeAttribute (Description="Item icon")]
		public string icon;
	}
	
	[ExtensionNode ("Item", "An item")]
	[ExtensionNodeChild (typeof(ItemDataNode))]
	[NodeAttribute ("info", typeof(string), Description="Some info")]
	public class ItemNode: ExtensionNode
	{
	}
	
	[ExtensionNode ("Data", "Item data")]
	[ExtensionNodeChild (typeof(ItemSetNode))]
	public class ItemDataNode: ExtensionNode
	{
	}
}
