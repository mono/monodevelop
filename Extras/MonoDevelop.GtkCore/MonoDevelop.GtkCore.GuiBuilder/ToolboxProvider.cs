
using System;
using System.Collections.Generic;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
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
				ComponentToolboxNode node = new ComponentToolboxNode (type);
				list.Add (node);
				list.Sort ();
			}
			return list;
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
		public ComponentType componentType;
		
		[ItemProperty]
		ReferenceType refType;
		[ItemProperty]
		string reference;
		[ItemProperty]
		string className;
		
		public ComponentToolboxNode ()
		{
		}
		
		public ComponentToolboxNode (ComponentType type)
		{
			Name = type.Description.Length > 0 ? type.Description : type.Name;
			componentType = type;
			className = type.ClassName;
			Category = GetCategoryName (type.Category);
			Icon = type.Icon;
		}
		
		public Stetic.ComponentType ComponentType {
			get {
				return componentType;
			}
		}

		public ReferenceType ReferenceType {
			get {
				return refType;
			}
			set {
				refType = value;
			}
		}

		public string Reference {
			get {
				return reference;
			}
			set {
				reference = value;
			}
		}

		public string ClassName {
			get {
				return className;
			}
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
