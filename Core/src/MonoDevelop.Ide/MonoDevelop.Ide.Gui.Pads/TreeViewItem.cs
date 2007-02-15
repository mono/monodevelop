
using System;

namespace MonoDevelop.Ide.Gui.Pads
{
	public class TreeViewItem
	{
		string stockIcon;
		string label;
		Gdk.Pixbuf icon;
		
		public TreeViewItem (string label, string stockIcon)
		{
			this.stockIcon = stockIcon;
			this.label = label;
		}

		public TreeViewItem (string label, Gdk.Pixbuf icon)
		{
			this.icon = icon;
			this.label = label;
		}
		
		public TreeViewItem (string label)
		{
			this.label = label;
		}
		
		public string StockIcon {
			get { return stockIcon; }
		}
		
		public Gdk.Pixbuf Icon {
			get { return icon; }
		}
		
		public string Label {
			get { return label; }
		}
	}
	
	internal class TreeViewItemBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(TreeViewItem); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			TreeViewItem it = (TreeViewItem) thisNode.DataItem;
			return it.Label;
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			TreeViewItem it = (TreeViewItem) dataObject;
			label = it.Label;
			if (it.StockIcon != null)
				icon = closedIcon = Context.GetIcon (it.StockIcon);
			else if (it.Icon != null)
				icon = closedIcon = it.Icon;
		}

	}
}
