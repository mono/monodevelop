
using System;

namespace Stetic
{
	public delegate void WidgetInfoEventHandler (object sender, WidgetInfoEventArgs args);
	
	public class WidgetInfoEventArgs: EventArgs
	{
		Project project;
		WidgetInfo widget;
		
		internal WidgetInfoEventArgs (Project p, WidgetInfo w)
		{
			project = p;
			widget = w;
		}
		
		public Project Project {
			get { return project; }
		}
		
		public WidgetInfo WidgetInfo {
			get { return widget; }
		}
	}
	
/*	public class ComponentNameEventArgs: ComponentEventArgs
	{
		string oldName;
		
		internal ComponentNameEventArgs (Project p, Component c, string oldName): base (p, c)
		{
			this.oldName = oldName;
		}
		
		public string OldName {
			get { return oldName; }
		}
	}
*/}
