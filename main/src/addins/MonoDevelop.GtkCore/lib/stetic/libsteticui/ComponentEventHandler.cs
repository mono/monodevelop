
using System;

namespace Stetic
{
	public delegate void ComponentEventHandler (object sender, ComponentEventArgs args);
	public delegate void ComponentNameEventHandler (object sender, ComponentNameEventArgs args);
	public delegate void ComponentRemovedEventHandler (object sender, ComponentRemovedEventArgs args);
	
	public class ComponentEventArgs: EventArgs
	{
		Project project;
		Component component;
		
		internal ComponentEventArgs (Project p, Component c)
		{
			project = p;
			component = c;
		}
		
		public Project Project {
			get { return project; }
		}
		
		public Component Component {
			get { return component; }
		}
	}
	
	public class ComponentNameEventArgs: ComponentEventArgs
	{
		string oldName;
		
		internal ComponentNameEventArgs (Project p, Component c, string oldName): base (p, c)
		{
			this.oldName = oldName;
		}
		
		public string OldName {
			get { return oldName; }
		}
		
		public string NewName {
			get { return Component.Name; }
		}
	}
	
	public class ComponentRemovedEventArgs: EventArgs
	{
		Project project;
		string componentName;
		
		internal ComponentRemovedEventArgs (Project p, string name)
		{
			project = p;
			componentName = name;
		}
		
		public Project Project {
			get { return project; }
		}
		
		public string ComponentName {
			get { return componentName; }
		}
	}
}
