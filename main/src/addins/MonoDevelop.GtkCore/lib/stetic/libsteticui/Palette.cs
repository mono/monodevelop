
using System;
using Gtk;

namespace Stetic
{
	public class Palette: PluggableWidget
	{
		bool showWindowCategory = true;
		
		internal Palette (Application app): base (app)
		{
		}
		
		protected override void OnCreatePlug (uint socketId)
		{
			app.Backend.CreatePaletteWidgetPlug (socketId);
			Update ();
		}
		
		protected override void OnDestroyPlug (uint socketId)
		{
			app.Backend.DestroyPaletteWidgetPlug ();
		}
		
		protected override Gtk.Widget OnCreateWidget ()
		{
			Update ();
			return app.Backend.GetPaletteWidget ();
		}
		
		public bool ShowWindowCategory {
			get { return showWindowCategory; }
			set { 
				showWindowCategory = value;
				Update ();
			}
		}
		
		void Update ()
		{
			if (!showWindowCategory)
				app.Backend.HidePaletteGroup ("window");
		}
	}
}
