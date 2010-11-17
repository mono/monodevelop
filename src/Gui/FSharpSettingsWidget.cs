using System;
namespace FSharp.MonoDevelop.Gui
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class FSharpSettingsWidget : Gtk.Bin
	{
		public FSharpSettingsWidget ()
		{
			this.Build ();
		}
		
    	public Gtk.Button ButtonBrowse { get { return buttonBrowse; } }
    	public Gtk.Entry EntryArguments { get { return entryArguments; } }
	    public Gtk.Entry EntryPath { get { return entryPath; } }
	    public Gtk.FontButton FontInteractive { get { return fontbutton1; } }
	}
}

