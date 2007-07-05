
using System;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using Stetic;
using MonoDevelop.Core;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class ToolboxProvider: IToolboxDynamicProvider, IToolboxDefaultProvider
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
				
			Hashtable refs = new Hashtable ();
			foreach (ProjectReference pr in view.Project.ProjectReferences)
				foreach (string f in pr.GetReferencedFileNames ()) {
					refs[f] = f;
				}
			
			List<ItemToolboxNode> list = new List<ItemToolboxNode> ();
			foreach (ComponentType type in types) {
				if (type.Category == "window")
					continue;
				if (type.ClassName == "Gtk.Action" || refs.Contains (type.Library)) {
					ComponentToolboxNode node = new ComponentToolboxNode (type);
					list.Add (node);
				}
			}
			list.Sort ();
			return list;
		}
		
		public void NotifyItemsChanged ()
		{
			if (ItemsChanged != null)
				ItemsChanged (this, EventArgs.Empty);
		}

		public virtual IEnumerable<ItemToolboxNode> GetDefaultItems ()
		{
			return null;
		}

		public virtual IEnumerable<string> GetDefaultFiles ()
		{
			yield return typeof(Stetic.Wrapper.Widget).Assembly.Location;
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
		[ItemProperty]
		string gtkVersion;
		
		static ToolboxItemFilterAttribute[] attributes = new ToolboxItemFilterAttribute[] {
			new ToolboxItemFilterAttribute ("gtk-sharp", ToolboxItemFilterType.Require)
		};
		
		internal static readonly string GtkWidgetDomain = GettextCatalog.GetString ("GTK# Widgets");
		
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
			gtkVersion = type.TargetGtkVersion;
		}
		
		public override IList ItemFilters {
			get { return attributes; }
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

		public string GtkVersion {
			get {
				return gtkVersion;
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

		public override string ItemDomain {
			get { return GtkWidgetDomain; }
		}
	}
}
