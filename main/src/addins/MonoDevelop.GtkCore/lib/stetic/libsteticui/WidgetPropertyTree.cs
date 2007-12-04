
using System;
using Gtk;

namespace Stetic
{
	public class WidgetPropertyTree: PluggableWidget
	{
		internal WidgetPropertyTree (Application app): base (app)
		{
		}
		
		protected override void OnCreatePlug (uint socketId)
		{
			app.Backend.CreatePropertiesWidgetPlug (socketId);
		}
		
		protected override void OnDestroyPlug (uint socketId)
		{
			app.Backend.DestroyPropertiesWidgetPlug ();
		}
		
		protected override Gtk.Widget OnCreateWidget ()
		{
			return app.Backend.GetPropertiesWidget ();
		}
	}
}
