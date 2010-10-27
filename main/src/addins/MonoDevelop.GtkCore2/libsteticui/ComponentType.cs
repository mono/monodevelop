
using System;

namespace Stetic
{
	public class ComponentType
	{
		Application app;
		string name;
		string description;
		string className;
		string category;
		Gdk.Pixbuf icon;
		ActionComponent action;
		string targetGtkVersion;
		string library;
		static ComponentType unknown;
		
		internal ComponentType (Application app, string name, string desc, string className, string category, string targetGtkVersion, string library, Gdk.Pixbuf icon)
		{
			this.app = app;
			this.name = name;
			this.description = desc;
			this.icon = icon;
			this.className = className;
			this.category = category;
			this.targetGtkVersion = targetGtkVersion;
			this.library = library;
		}
		
		internal ComponentType (Application app, ActionComponent action)
		{
			this.action = action;
			this.app = app;
			this.name = action.Name;
			this.description = action.Label != null ? action.Label.Replace ("_", "") : action.Name;
			this.icon = action.Icon;
			this.className = "Gtk.Action";
			this.category = "Actions / " + action.ActionGroup.Name;
			this.targetGtkVersion = "2.4"; // Not version-specific
		}
		
		public string Name {
			get { return name; }
		}
		
		public string ClassName {
			get { return className; }
		}
		
		public string Category {
			get { return category; }
		}
		
		public string Description {
			get { return description; }
		}
		
		public string Library {
			get { return library; }
		}
		
		public Gdk.Pixbuf Icon {
			get { return icon; }
		}
		
		internal ActionComponent Action {
			get { return action; }
		}
		
		internal static ComponentType Unknown {
			get {
				if (unknown == null) {
					unknown = new ComponentType (null, "Unknown", "Unknown", "", "", "2.4", null, WidgetUtils.MissingIcon);
				}
				return unknown;
			}
		}
		
		public object[] InitializationValues {
			get {
				if (app == null)
					return new object [0];
				return app.Backend.GetClassDescriptorInitializationValues (name);
			}
		}

		public string TargetGtkVersion {
			get {
				return targetGtkVersion;
			}
		}
	}
}
