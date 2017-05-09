using System;
namespace MonoDevelop.FSharp.Gui
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class FSharpSettingsWidget : Gtk.Bin
	{
		public FSharpSettingsWidget ()
		{
			this.Build ();
		}
		
		public Gtk.CheckButton AdvanceLine { get { return advanceToNextLineCheckbox; } }
		public Gtk.CheckButton CheckCompilerUseDefault { get { return checkCompilerUseDefault; } }
		public Gtk.Entry EntryCompilerPath { get { return entryCompilerPath; } }
		public Gtk.CheckButton CheckHighlightMutables { get { return checkHighlightMutables; } }
		
	}
}

