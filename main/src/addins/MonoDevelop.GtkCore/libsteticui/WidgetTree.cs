
using System;

namespace Stetic
{
	public class WidgetTree: PluggableWidget
	{
		ProjectViewFrontend frontend;
		
		internal WidgetTree (Application app): base (app)
		{
			frontend = new ProjectViewFrontend (app);
		}
		
		public event ComponentEventHandler ComponentActivated {
			add { frontend.ComponentActivated += value; }
			remove { frontend.ComponentActivated -= value; }
		}
		
		public event ComponentEventHandler SelectionChanged {
			add { frontend.SelectionChanged += value; }
			remove { frontend.SelectionChanged -= value; }
		}
		
		protected override void OnCreatePlug (uint socketId)
		{
			app.Backend.CreateProjectWidgetPlug (frontend, socketId);
		}
		
		protected override void OnDestroyPlug (uint socketId)
		{
			app.Backend.DestroyProjectWidgetPlug ();
		}
		
		protected override Gtk.Widget OnCreateWidget ()
		{
			return app.Backend.GetProjectWidget (frontend);
		}
		
		public override void Dispose ()
		{
			frontend.disposed = true;
			System.Runtime.Remoting.RemotingServices.Disconnect (frontend);
			base.Dispose ();
		}
	}
	
	
	internal class ProjectViewFrontend: MarshalByRefObject
	{
		Application app;
		internal bool disposed;
		
		public event ComponentEventHandler ComponentActivated;
		public event ComponentEventHandler SelectionChanged;
		
		public ProjectViewFrontend (Application app)
		{
			this.app = app;
		}
		
		public void NotifyWidgetActivated (object ob, string widgetName, string widgetType)
		{
			Gtk.Application.Invoke (
				(o, args) => {
					if (disposed) return;
					Component c = app.GetComponent (ob, widgetName, widgetType);
					if (c != null && ComponentActivated != null)
						ComponentActivated (null, new ComponentEventArgs (app.ActiveProject, c));
				}
			);
		}

		public void NotifySelectionChanged (object ob, string widgetName, string widgetType)
		{
			Gtk.Application.Invoke (
				(o, args) => {
					if (disposed) return;
					Component c = ob != null ? app.GetComponent (ob, widgetName, widgetType) : null;
					if (SelectionChanged != null)
						SelectionChanged (null, new ComponentEventArgs (app.ActiveProject, c));
				}
			);
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
	}
}
