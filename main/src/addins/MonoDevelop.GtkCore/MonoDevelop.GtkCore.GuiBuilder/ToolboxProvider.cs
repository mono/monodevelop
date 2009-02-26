
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using Stetic;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class ToolboxProvider: IToolboxDynamicProvider, IToolboxDefaultProvider
	{
		internal static ToolboxProvider Instance;
		
		public ToolboxProvider ()
		{
			Instance = this;
		}
		
		public IEnumerable<ItemToolboxNode> GetDynamicItems (IToolboxConsumer consumer)
		{
			GuiBuilderView view = consumer as GuiBuilderView;
			if (view == null)
				return null;
			
			ComponentType[] types = view.GetComponentTypes ();
			if (types == null)
				return null;
				
			Hashtable refs = new Hashtable ();
			Hashtable projects = new Hashtable ();
			string of = FileService.GetFullPath (view.Project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration));
			projects [of] = view.Project.Name;
			foreach (ProjectReference pr in ((DotNetProject)view.Project).References)
				foreach (string f in pr.GetReferencedFileNames (IdeApp.Workspace.ActiveConfiguration)) {
					if (pr.ReferenceType == ReferenceType.Project)
						projects[FileService.GetFullPath (f)] = pr.Reference;
					else
						refs[FileService.GetFullPath (f)] = f;
				}
			
			List<ItemToolboxNode> list = new List<ItemToolboxNode> ();
			foreach (ComponentType type in types) {
				if (type.Category == "window")
					continue;

				string fullName = null;
				if (!String.IsNullOrEmpty (type.Library))
					fullName = FileService.GetFullPath (type.Library);

				if (type.ClassName == "Gtk.Action" || (fullName != null && refs.Contains (fullName))) {
					ComponentToolboxNode node = new ComponentToolboxNode (type);
					list.Add (node);
				} else if (fullName != null && projects.Contains (fullName)) {
					ComponentToolboxNode node = new ComponentToolboxNode (type);
					node.Category = (string) projects [fullName];
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
		
		[Browsable (false)]
		public override IList<ToolboxItemFilterAttribute> ItemFilters {
			get { return attributes; }
		}
		
		[Browsable (false)]
		public Stetic.ComponentType ComponentType {
			get {
				return componentType;
			}
		}
		
		[ReadOnly (true)]
		[LocalizedCategory ("Misc")]
		[LocalizedDisplayName ("Reference Type")]
		[LocalizedDescription ("The type of the project or assembly from which this component originates.")]
		public ReferenceType ReferenceType {
			get {
				return refType;
			}
			set {
				refType = value;
			}
		}
		
		[ReadOnly (true)]
		[LocalizedCategory ("Misc")]
		[LocalizedDisplayName ("Reference Path")]
		[LocalizedDescription ("The project or assembly from which this component originates.")]
		public string Reference {
			get {
				return reference;
			}
			set {
				reference = value;
			}
		}
		
		[LocalizedCategory ("Misc")]
		[LocalizedDisplayName ("Class Name")]
		[LocalizedDescription ("The name of the component class.")]
		public string ClassName {
			get {
				return className;
			}
		}
		
		[LocalizedCategory ("Misc")]
		[LocalizedDisplayName ("GTK# Version")]
		[LocalizedDescription ("The minimum GTK# version required to use this component.")]
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
		
		[Browsable (false)]
		public override string ItemDomain {
			get { return GtkWidgetDomain; }
		}
	}
}
