
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
		
		public IEnumerable<BaseToolboxNode> GetDynamicItems (IToolboxConsumer consumer)
		{
			GuiBuilderView view = consumer as GuiBuilderView;
			if (view == null)
				return null;
			
			ComponentType[] types = view.GetComponentTypes ();
			if (types == null)
				return null;
				
			Hashtable refs = new Hashtable ();
			string of = FileService.GetFullPath (view.Project.GetOutputFileName ());
			refs [of] = of;
			foreach (ProjectReference pr in view.Project.ProjectReferences)
				foreach (string f in pr.GetReferencedFileNames ()) {
					refs[FileService.GetFullPath (f)] = f;
				}
			
			List<BaseToolboxNode> list = new List<BaseToolboxNode> ();
			foreach (ComponentType type in types) {
				if (type.Category == "window")
					continue;
				if (type.ClassName == "Gtk.Action" || (!String.IsNullOrEmpty (type.Library) && refs.Contains (FileService.GetFullPath (type.Library)))) {
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
	
	class ComponentToolboxNode: ItemToolboxNode
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
			if (type.Description.Length > 0)
				Name = type.Description;
			else {
				int i = type.Name.LastIndexOf ('.');
				if (i == -1)
					Name = type.Name;
				else
					Name = type.Name.Substring (i+1);
			}
			
			componentType = type;
			className = type.ClassName;
			Category = GetCategoryName (type.Category);
			Icon = type.Icon;
			gtkVersion = type.TargetGtkVersion;
		}
		
		public override IList<ToolboxItemFilterAttribute> ItemFilters {
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

		public override string ItemDomain {
			get { return GtkWidgetDomain; }
		}
	}
}
