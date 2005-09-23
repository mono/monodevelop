//
// Makes a widget from a glade file. Does reparenting.
// This is nice because you can use it for things like
// option dialogs.
//


using System;
using Gtk;
using Glade;

using Assembly = System.Reflection.Assembly;

namespace MonoDevelop.Gui.Widgets {
	public abstract class GladeWidgetExtract : HBox {
		
		Glade.XML glade;
		string dialog_name;
		
		private GladeWidgetExtract (string dialog_name) : base (false, 0)
		{
			this.dialog_name = dialog_name;
		}
		
		protected GladeWidgetExtract (string resource_name, string dialog_name) : this (dialog_name)
		{
			// we must do it from *here* otherwise, we get this assembly, not the caller
			glade = new XML (Assembly.GetCallingAssembly (), resource_name, dialog_name, null);
			Init ();
		}
		
		
		protected GladeWidgetExtract (Assembly assembly, string resource_name, string dialog_name) : this (dialog_name)
		{
			// we must do it from *here* otherwise, we get this assembly, not the caller
			glade = new XML (assembly, resource_name, dialog_name, null);
			Init ();
		}
		
		protected GladeWidgetExtract (string resource_name, string dialog_name, string domain) : this (dialog_name)
		{
			// we must do it from *here* otherwise, we get this assembly, not the caller
			glade = new XML (resource_name, dialog_name, domain);
			Init ();
		}
		
		void Init ()
		{
			glade.Autoconnect (this);
			
			Window win = (Window) glade [dialog_name];
			Widget child = win.Child;
			
			child.Reparent (this);
			win.Destroy ();
		}
	}
}
