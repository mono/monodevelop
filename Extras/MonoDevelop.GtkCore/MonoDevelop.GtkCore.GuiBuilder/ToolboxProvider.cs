
using System;
using System.Collections.Generic;
using MonoDevelop.DesignerSupport.Toolbox;
using Stetic;
using MonoDevelop.Core;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class ToolboxProvider: IToolboxDynamicProvider
	{
		internal static ToolboxProvider Instance;
		
		public ToolboxProvider ()
		{
			Instance = this;
		}
		
		public IList<ItemToolboxNode> GetDynamicItems (IToolboxConsumer consumer)
		{
			GuiBuilderView view = consumer as GuiBuilderView;
			if (view == null)
				return null;
				
			ComponentType[] types = view.GetComponentTypes ();
			if (types == null)
				return null;
				
			List<ItemToolboxNode> list = new List<ItemToolboxNode> ();
			foreach (ComponentType type in types) {
				if (type.Category == "window")
					continue;
				ComponentToolboxNode node = new ComponentToolboxNode ();
				node.Name = type.Description;
				node.ComponentType = type;
				node.Category = GetCategoryName (type.Category);
				node.Icon = type.Icon;
				list.Add (node);
				list.Sort ();
			}
			return list;
		}
		
		string GetCategoryName (string cat)
		{
			if (cat == "container")
				return GettextCatalog.GetString ("Containers");
			else if (cat == "widget")
				return GettextCatalog.GetString ("Widgets");
			else
				return cat;
		}
		
		public void NotifyItemsChanged ()
		{
			if (ItemsChanged != null)
				ItemsChanged (this, EventArgs.Empty);
		}
		
		public event EventHandler ItemsChanged;
	}
	
	class ComponentToolboxNode: ItemToolboxNode, IComparable
	{
		public ComponentType ComponentType;
		
		public int CompareTo (object obj)
		{
			ComponentToolboxNode other = obj as ComponentToolboxNode;
			if (other == null) return -1;
			if (Category == other.Category)
				return Name.CompareTo (other.Name);
			else
				return Category.CompareTo (other.Category);
		}

	}
}
