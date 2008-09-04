// TreeViewItem.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//


using System;

namespace MonoDevelop.Ide.Gui.Components
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
